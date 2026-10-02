using Hairball.Core;
using System.Numerics;

internal static class GrowthSprayTests
{
    static Session Scene(ExperimentVariant variant=ExperimentVariant.B)
    {
        var s=new Session{Lab=true};s.State.Experiment.Variant=variant;
        var p=s.AddPlayer(1,"Sprayer")!;s.StartMatch();var h=s.State.SharedHead;
        h.Volume.Fill(v=>HairVolume.Ellipsoid(v,new(0,.4f,0),new(.5f,.55f,.45f)));
        s.State.Heads.RemoveAll(h=>h.Facial);ExperimentTests.Equip(s,p,1);
        p.Position=h.Position+new Vector3(0,.4f-1.7f,2);p.Yaw=p.Pitch=0;
        // Historical pressure/quantization fixture uses 3 units; production bottles are tested separately.
        Bottle(s).GrowthRemaining=3;return s;
    }
    static ToolState Bottle(Session s)=>s.State.Tools.Single(t=>t.Id==s.State.Player(1)!.Held);
    static void Hold(Session s,float seconds,Buttons buttons=Buttons.Primary,float step=1f/60)
    {
        var p=s.State.Player(1)!;
        for(float elapsed=0;elapsed<seconds-.000001f;elapsed+=step)
        {s.Inputs[p.Id]=(Vector2.Zero,p.Yaw,p.Pitch,buttons);s.Tick(Math.Min(step,seconds-elapsed));}
    }
    static float Material(Session s)=>s.State.Heads.Sum(h=>h.Mass)+s.State.Debris.Mass;
    static float Surface(Session s)
    {
        var p=s.State.Player(1)!;var h=s.State.SharedHead;
        if(!h.Raycast(Session.Eye(p),Session.Aim(p),Tools.Get(1).Range,out var hit,out _))throw new Exception("spray surface must remain hittable");
        return h.ToWorld(hit).Z;
    }

    public static void Run(Action<string,Action> test,Action<bool,string> check)
    {
        void C(bool value,string message="growth spray assertion")=>check(value,message);
        void Near(float a,float b,float tolerance=.003f)=>C(Math.Abs(a-b)<=tolerance,$"{a} != {b}");

        test("B growth ramps real held input and consumes actual bottle material",()=>{
            var s=Scene();var p=s.State.Player(1)!;float initial=Material(s),budget=Bottle(s).GrowthRemaining,initialSurface=Surface(s);
            Hold(s,.3f);float early=Material(s)-initial,earlyAdvance=Surface(s)-initialSurface;C(early>0&&p.GrowthHeldSeconds>.25f);
            Hold(s,1.2f);float before=Material(s),beforeSurface=Surface(s);Hold(s,.3f);
            float late=Material(s)-before,lateAdvance=Surface(s)-beforeSurface;
            C(late>early*1.25f,$"early={early}, late={late}");C(lateAdvance>earlyAdvance*1.25f,$"early advance={earlyAdvance}, late advance={lateAdvance}");
            C(p.GrowthHeldSeconds>=GrowthSpray.RampSeconds-.01f);
            Near(budget-Bottle(s).GrowthRemaining,Material(s)-initial,.012f);
            C(GrowthSpray.Power(p.GrowthHeldSeconds)<=2.001f&&GrowthSpray.Power(100)<=2.001f,"bounded pressure");
            float endSurface=Surface(s)-s.State.SharedHead.Position.Z;
            foreach(float x in new[]{-.14f,.14f})C(s.State.SharedHead.Volume.Sample(new(x,.4f,endSurface-.1f))>0,"broad growth keeps a usable core instead of a long needle");
            while(p.GrowthHeldSeconds>0&&s.State.Time<5)Hold(s,1f/60);
            float exhaustedAt=s.State.Time;Hold(s,.1f);Near(p.GrowthHeldSeconds,0);
            C(exhaustedAt>=GrowthSpray.RampSeconds&&exhaustedAt<5,"full pressure is reached before supply ends");
            Near(budget-Bottle(s).GrowthRemaining,Material(s)-initial,.012f);
            Console.WriteLine($"B_GROWTH earlyMass={early:0.0000} lateMass={late:0.0000} earlyAdvance={earlyAdvance:0.0000} lateAdvance={lateAdvance:0.0000} exhaustedAt={exhaustedAt:0.000} remaining={Bottle(s).GrowthRemaining:0.000000}");
        });
        test("B initial spray is visibly faster than archived constant-rate growth",()=>{
            float Growth(ExperimentVariant variant){var s=Scene(variant);float mass=Material(s);Hold(s,.3f);return Material(s)-mass;}
            float current=Growth(ExperimentVariant.B),legacy=Growth(ExperimentVariant.Off);
            C(current>legacy*1.5f,$"initial B growth={current}, prior growth={legacy}");
        });
        test("B release fine mode tool swap brace and input loss reset pressure",()=>{
            void ResetBy(Action<Session,PlayerState> action)
            {var s=Scene();var p=s.State.Player(1)!;Hold(s,.5f);C(p.GrowthHeldSeconds>.4f,"charged on valid head");action(s,p);Near(p.GrowthHeldSeconds,0);}
            ResetBy((s,p)=>Hold(s,.1f,Buttons.None));
            ResetBy((s,p)=>{Hold(s,.1f,Buttons.Secondary);C(p.GrowthFine,"fine mode exposed to feedback");});
            ResetBy((s,p)=>{ExperimentTests.Equip(s,p,4);Hold(s,.1f);});
            ResetBy((s,p)=>{s.Inputs.Clear();s.Tick(.1f);});
            ResetBy((s,p)=>{ExperimentTests.Aim(s,p,s.State.SharedHead.Position+new Vector3(0,0,1.3f),s.State.SharedHead.Position);Hold(s,.1f,Buttons.Interact|Buttons.Primary);C(p.Bracing);});
            ResetBy((s,p)=>{s.State.Phase=Phase.Results;Hold(s,.1f);});
        });
        test("B air spraying cannot store a burst for a fresh target",()=>{
            var s=Scene();var p=s.State.Player(1)!;p.Yaw=MathF.PI;float initial=Material(s);
            Hold(s,2);Near(p.GrowthHeldSeconds,0);Near(Material(s),initial);
            p.Yaw=0;Hold(s,.1f);C(p.GrowthHeldSeconds>.05f&&p.GrowthHeldSeconds<.15f);
            C(Material(s)>initial,"fresh contact begins a low-pressure spray");
            Hold(s,.5f);p.Yaw=MathF.PI;Hold(s,.1f);Near(p.GrowthHeldSeconds,0);
            p.Yaw=0;float before=Material(s);Hold(s,.3f);Near(Material(s),before);
            Hold(s,.1f,Buttons.None);Hold(s,.2f);C(Material(s)>before,"released gesture may acquire again");
        });
        test("B away peers cannot grow from cached input and resume with fresh pressure",()=>{
            var s=Scene();var p=s.State.Player(1)!;Hold(s,.8f);C(p.GrowthHeldSeconds>.7f);
            p.NetworkAway=true;float before=Material(s);Hold(s,.2f);Near(Material(s),before);Near(p.GrowthHeldSeconds,0);
            p.NetworkAway=false;Hold(s,.1f);C(p.GrowthHeldSeconds>.05f&&p.GrowthHeldSeconds<.15f,"recovery starts at base pressure");
            var copy=Wire.Decode<WorldState>(Wire.Encode(s.State));Near(copy.Player(1)!.GrowthHeldSeconds,p.GrowthHeldSeconds);
        });
        test("B fine spray stays slow and fast-slow frames conserve growth budget",()=>{
            float Grow(float step,bool fine){var s=Scene();float initial=Material(s),budget=Bottle(s).GrowthRemaining;Hold(s,1.2f,fine?Buttons.Secondary:Buttons.Primary,step);float added=Material(s)-initial;Near(added,budget-Bottle(s).GrowthRemaining,.015f);return added;}
            float fast=Grow(1f/60,false),slow=Grow(1f/30,false),fine=Grow(1f/60,true);
            C(fast>0&&fine>0&&fine<fast*.35f,$"normal={fast}, fine={fine}");
            Near(fast,slow,Math.Max(.04f,fast*.15f));
        });
        test("B accelerated collateral targets share and never exceed remaining budget",()=>{
            var s=Scene();var first=s.State.SharedHead;
            var extra=Head.Create(200,2);extra.Position=first.Position-new Vector3(0,0,1.2f);
            extra.Volume=first.Volume.Clone();s.State.Heads.Add(extra);float extraBefore=extra.Mass;
            Bottle(s).GrowthRemaining=.18f;float initial=Material(s);Hold(s,2.5f);
            float used=.18f-Bottle(s).GrowthRemaining,added=Material(s)-initial;
            C(used>=0&&Bottle(s).GrowthRemaining>=0&&added<=.1801f,$"used={used}, added={added}");
            C(extra.Mass>extraBefore,"broad spray still reaches collateral heads");Near(used,added,.006f);
            Bottle(s).GrowthRemaining=0;float before=Material(s);Hold(s,.3f);Near(before,Material(s));Near(s.State.Player(1)!.GrowthHeldSeconds,0);
        });
        test("B retained growth balance reports current contact without inventing added volume",()=>{
            var s=Scene();var p=s.State.Player(1)!;var tool=s.State.Tools.Single(t=>t.Id==p.Held);
            Hold(s,.3f);Hold(s,4.7f);
            C(ToolQuery.Find(s.State,p,1,false,Session.Aim(p),Tools.Get(1).Range).Any(h=>h.HeadId==0),"stalled spray still physically hits its original head");
            C(Bottle(s).GrowthRemaining>0&&Bottle(s).GrowthRemaining<.01f,"positive quantized balance is retained");
            C(tool.HitHair&&tool.EffectMass==0,"actual contact and zero added material are separate facts");
            float mass=Material(s),balance=Bottle(s).GrowthRemaining;
            Hold(s,.2f);Near(Material(s),mass,.00001f);Near(Bottle(s).GrowthRemaining,balance,.000001f);
            C(tool.HitHair&&tool.EffectMass==0,"holding a clipped gesture cannot repeat an old successful effect");
            Hold(s,.1f,Buttons.None);Hold(s,.1f);
            C(tool.HitHair&&tool.EffectMass==0,"repressing a still unaffordable dab does not masquerade as a miss");
            var wire=Wire.Decode<WorldState>(Wire.Encode(s.State)).Tools.Single(t=>t.Id==tool.Id);
            s.Replay.Record(s.State,.11f);var recorded=s.Replay.Rolling.Last().Tools.Single(t=>t.Id==tool.Id);
            C(wire.HitHair&&wire.EffectMass==0&&recorded.HitHair&&recorded.EffectMass==0,"snapshots and replay preserve zero-growth contact");
        });
        test("B ended growth gestures report air obstruction and reacquired contact without unlocking effects",()=>{
            var s=Scene();var p=s.State.Player(1)!;var tool=s.State.Tools.Single(t=>t.Id==p.Held);
            Hold(s,.3f);C(tool.HitHair&&tool.EffectMass>0);float mass=Material(s),balance=Bottle(s).GrowthRemaining;
            p.Yaw=MathF.PI;Hold(s,.2f);C(!tool.HitHair&&tool.EffectMass==0,"air cannot retain a prior effect");
            p.Yaw=0;Hold(s,.2f);C(tool.HitHair&&tool.EffectMass==0,"reacquisition is contact, but the old gesture stays ended");
            Near(Material(s),mass,.00001f);Near(Bottle(s).GrowthRemaining,balance,.000001f);
            s.ObstructionDistance=(_,_,_)=>.05f;Hold(s,.1f);C(!tool.HitHair&&tool.EffectMass==0,"obstructed contact is an actual miss");
            s.ObstructionDistance=null;Hold(s,.1f);C(tool.HitHair&&tool.EffectMass==0);
            Hold(s,.1f,Buttons.None);Hold(s,.2f);C(tool.HitHair&&tool.EffectMass>0&&Material(s)>mass,"release starts a new working gesture");
            Bottle(s).GrowthRemaining=.00001f;Hold(s,.1f,Buttons.None);Hold(s,.1f);
            C(tool.HitHair&&tool.EffectMass==0,"sub-cell balance blocks growth but not contact feedback");
        });
        test("B retained small balance is still available to smaller material targets",()=>{
            var s=Scene();var p=s.State.Player(1)!;Hold(s,.3f);Hold(s,4.7f);
            float budget=Bottle(s).GrowthRemaining;
            C(budget>0&&budget<.01f,"the original scalp has retained a positive small balance");
            var small=Head.Create(200,2);small.GeometryScale=.35f;small.Position=s.State.SharedHead.Position+new Vector3(2,0,0);
            small.Volume.Fill(v=>HairVolume.Ellipsoid(v,new(0,.4f,0),new(.5f,.55f,.45f)));s.State.Heads.Add(small);
            C(GrowthSpray.CanAfford(small,Bottle(s).GrowthRemaining),"smaller material has smaller quantized increments");
            p.Position=small.Position+new Vector3(0,.4f*small.GeometryScale-1.7f,1.2f);float before=small.Mass;
            Hold(s,.1f,Buttons.None);Hold(s,.5f);float added=small.Mass-before;
            C(added>0&&added<=budget+.00001f&&Bottle(s).GrowthRemaining>=0,$"retained balance can fund real smaller-target growth: budget={budget}, added={added}, remaining={Bottle(s).GrowthRemaining}, hits={ToolQuery.Find(s.State,p,1,false,Session.Aim(p),Tools.Get(1).Range).Count}, actual={s.State.Tools.Single(t=>t.Id==p.Held).EffectMass}");
            Near(budget-Bottle(s).GrowthRemaining,added,.00001f);
        });
    }
}
