using Hairball.Core;
using System.Numerics;
internal static class PartyMoldTests
{
    public static void Run(Action<string,Action> test,Action<bool,string> check)
    {
        void C(bool b,string m="mold assertion")=>check(b,m);
        test("party one mold cannot build a whole platform; two joined patches can",()=>{
            foreach(var kind in new[]{MoldKind.Pad,MoldKind.Bowl,MoldKind.Ring}){
                var s=ExperimentTests.World();var w=s.State;var h=w.SharedHead;h.Volume.Fill(_=>-1);
                foreach(var q in h.Patches){q.Glue=1;q.Temperature=20;q.Burning=false;}
                var prop=w.Props.Single(p=>p.Goal==0);float top=kind==MoldKind.Bowl?.84f:.82f;prop.Position=h.ToWorld(new(0,top+.26f,0));
                void Patch(Vector3 offset){var m=new PropState{Mold=kind,Position=h.ToWorld(new Vector3(0,.7f,0)+offset)};for(int i=0;i<30;i++)MoldSystem.Grow(h,m,.05f);}
                Patch(Vector3.Zero);var single=Session.LandingSupport(w,prop);C(!single.Stable,$"single {kind} has {single.Contacts} contacts and must require assembly");
                h.Volume.Fill(_=>-1);var axis=kind==MoldKind.Pad?Vector3.UnitZ:Vector3.UnitX;Patch(axis*.18f);Patch(-axis*.18f);
                var joined=Session.LandingSupport(w,prop);C(joined.Stable,$"joined {kind} has {joined.Contacts} contacts, spread {joined.Variance}, issues {joined.Issues}");
            }
        });
        test("party combo discovery follows actual effects and occurs once across rounds",()=>{
            var s=ExperimentTests.World();var p=s.State.Player(1)!;ExperimentTests.Aim(s,p,new(0,2,2),new(0,2,0));
            foreach(var q in s.State.SharedHead.Patches)q.Wet=.95f;ExperimentTests.Equip(s,p,8);s.UseTool(p,false);C(s.State.Experiment.Discoveries.Contains(Combo.WetFire));
            foreach(var q in s.State.SharedHead.Patches){q.Wet=0;q.Glue=.9f;q.Temperature=20;q.Burning=false;}p.Cooldown=0;s.UseTool(p,false);C(s.State.Experiment.Discoveries.Contains(Combo.HeatGlue)&&s.State.SharedHead.Patches.Any(q=>q.Glue<.9f));
            foreach(var q in s.State.SharedHead.Patches){q.Burning=false;q.Temperature=-40;}ExperimentTests.Equip(s,p,6);p.Cooldown=0;s.UseTool(p,false);C(s.State.Experiment.Discoveries.Contains(Combo.FrozenImpact));
            s.NextRound();C(s.State.Experiment.Discoveries.Contains(Combo.WetFire)&&s.KnownCombos.Count==3);C(s.State.Experiment.ComboUntil==0,"known combos do not replay the discovery banner");
        });
        test("party held mold is filled by teammate input and actual nail frees holder",()=>{
            var s=ExperimentTests.World();var holder=s.State.Player(1)!;var helper=s.AddPlayer(2,"Helper")!;s.Tick(.01f);var mold=s.State.Props.First(m=>m.Mold==MoldKind.Pad);
            ExperimentTests.Aim(s,holder,mold.Position+new Vector3(0,1,1),mold.Position);C(s.HandleProp(holder));C(holder.Held<0&&holder.CarriedProp==mold.Id);
            ExperimentTests.Aim(s,holder,s.State.SharedHead.Position+new Vector3(0,.6f,1.1f),s.State.SharedHead.Position+new Vector3(0,.6f,.25f));PhysicalProps.Tick(s.State,.01f,null);C(MoldSystem.Touching(s.State,mold));
            ExperimentTests.Equip(s,helper,1);ExperimentTests.Aim(s,helper,s.State.SharedHead.Position+new Vector3(1.2f,1.3f,1.2f),mold.Position);float before=s.State.SharedHead.Mass;
            for(int i=0;i<15;i++){helper.Cooldown=0;s.State.Time+=.05f;s.UseTool(helper,false);}C(s.State.SharedHead.Mass>before&&s.State.Tools.Single(t=>t.Id==helper.Held).GrowthRemaining<1);
            ExperimentTests.Equip(s,helper,6);helper.Cooldown=0;s.UseTool(helper,false);C(mold.Pinned&&mold.Holes.Count>0&&mold.Holder==0&&holder.CarriedProp<0,"pin frees main hand and retains mold");
            C(s.ActionLog.Any(a=>a.Kind==PartyAction.MoldHole&&a.Actor==helper.Id));
        });
        test("party mold fills cavity first and hole opens real outward growth",()=>{
            var h=Head.Create(0,0);h.Volume.Fill(v=>HairVolume.Ellipsoid(v,new(0,0,0),new(.42f,.38f,.37f)));
            var m=new PropState{Mold=MoldKind.Pad,Position=new(0,.48f,0)};float before=h.Volume.Sample(new(0,.85f,0));
            for(int i=0;i<20;i++)MoldSystem.Grow(h,m,.05f);C(MoldSystem.Full(h,m));C(h.Volume.Sample(new(0,.85f,0))<=before+.0001f,"closed mold forbids overflow");
            C(MoldSystem.Puncture(m,new(0,.60f,0)));for(int i=0;i<20;i++)MoldSystem.Grow(h,m,.035f);C(h.Volume.Sample(new(0,.85f,0))>before+.04f,"real connected density extrudes through hole");
            C(h.Volume.Sample(new(.8f,.8f,0))<0);C(m.Holes.Count==1&&MoldSystem.HoleRadius>=HairVolume.Step*2);
        });
        test("party unsupported mold falls after placement grace; pins follow head and holes clone",()=>{
            var s=ExperimentTests.World();s.MoldsEnabled=true;s.Tick(.01f);var m=s.State.Props.First(p=>p.Mold==MoldKind.Pad);C(s.State.Props.Count(p=>p.Mold!=MoldKind.None)==3);
            m.Position=s.State.SharedHead.ToWorld(new(0,.9f,0));m.Local=new(0,.9f,0);m.Attached=true;m.PlacedAt=s.State.Time;PhysicalProps.Tick(s.State,.1f,null);C(m.Attached);
            s.State.Time+=1.1f;PhysicalProps.Tick(s.State,.1f,null);C(!m.Attached&&m.Released);
            m.Position=s.State.SharedHead.ToWorld(new(0,.75f,0));m.Local=new(0,.75f,0);m.Pinned=m.Attached=true;m.Holes.Add(new(0,.12f,0));PhysicalProps.Tick(s.State,.1f,null);C(m.Attached&&m.Pinned);
            var copy=Wire.Decode<WorldState>(Wire.Encode(s.State)).Props.Single(p=>p.Id==m.Id);C(copy.Pinned&&copy.Holes.SequenceEqual(m.Holes));var clone=m.Clone();clone.Holes.Clear();C(m.Holes.Count==1);
            for(int i=0;i<100;i++)MoldSystem.Puncture(m,new(MathF.Sin(i)*.5f,MathF.Cos(i)*.5f,0));C(m.Holes.Count<=8);
        });
        test("party center of mass stays level when symmetric and leans at bounded rate",()=>{
            var s=ExperimentTests.World();s.State.SharedHead.Volume.Fill(v=>HairVolume.Ellipsoid(v,new(0,.35f,0),new(.65f,.6f,.5f)));C(Math.Abs(PartyBalance.Offset(s.State))<.01f);C(PartyBalance.Advance(0,PartyBalance.Offset(s.State),.1f)==0);
            s.State.SharedHead.Volume.Fill(v=>HairVolume.Ellipsoid(v,new(.9f,.5f,0),new(.5f,.7f,.5f)));float offset=PartyBalance.Offset(s.State);C(offset>PartyBalance.Threshold);
            float tilt=PartyBalance.Advance(0,offset,.1f);C(Math.Abs(tilt)<=PartyBalance.Rate*.1f+.0001f);for(int i=0;i<100;i++)tilt=PartyBalance.Advance(tilt,offset,.1f);C(Math.Abs(tilt)<=PartyBalance.MaxAngle+.0001f&&Math.Abs(tilt)>.1f);
            s.State.Experiment.Tilt=tilt;CustomerMotion.Apply(s.State);C(Math.Abs(s.State.SharedHead.Rotation.Z)>.1f);s.State.Player(1)!.Bracing=true;CustomerMotion.Apply(s.State);C(Math.Abs(s.State.SharedHead.Rotation.Z)<.05f);
        });
        test("party head lean changes real commission support measurements",()=>{
            var s=ExperimentTests.World();var w=s.State;w.SharedHead.Volume.Fill(v=>Math.Min(.68f-Math.Abs(v.X),Math.Min(.18f-Math.Abs(v.Y-.7f),.5f-Math.Abs(v.Z))));foreach(var q in w.SharedHead.Patches)q.Glue=1;
            var prop=w.Props.Single(p=>p.Goal==0);prop.Attached=true;prop.Local=new(0,1.14f,0);CustomerMotion.Apply(w);PhysicalProps.Tick(w,.01f,null);var level=Session.LandingSupport(w,prop);
            w.Experiment.Tilt=PartyBalance.MaxAngle;CustomerMotion.Apply(w);PhysicalProps.Tick(w,.01f,null);var leaned=Session.LandingSupport(w,prop);C(level.Stable&&leaned.Variance>level.Variance+.07f,"actual rotated gear probes measure changed surface spread");
        });
    }
}
