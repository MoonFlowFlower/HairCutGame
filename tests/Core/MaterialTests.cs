using Hairball.Core;
using System.Numerics;
internal static class MaterialTests
{
    public static void Run(Action<string,Action> test,Action<bool,string> check)
    {
        void C(bool value,string message="v05 assertion")=>check(value,message);
        Session World(int goal=0){var s=new Session();s.AddPlayer(1,"A");s.StartMatch();s.State.Phase=Phase.Build;s.State.Job.Goal=goal;PhysicalProps.Stage(s.State);return s;}
        test("v05 six materials have distinct appearance/mobility and Young matures",()=>{
            var h=Head.Create(0,0);var p=h.Patches[0];var colors=new HashSet<Vector3>();var motion=new HashSet<float>();
            foreach(var state in Enum.GetValues<HairState>()){HairMaterials.Set(p,state);C(HairMaterials.State(p)==state);colors.Add(HairMaterials.Color(p,new(.47f,.32f,.23f)));motion.Add(HairMaterials.Motion(p));}
            C(colors.Count==6&&motion.Count==6);HairMaterials.Set(p,HairState.Young);float soft=p.Resistance;HairSystem.Tick(h,36);C(p.Young==0&&p.Resistance>soft);
        });
        test("v05 wet resists ignition and fresh glue; heat dries and melts",()=>{
            var h=Head.Create(0,0);var p=h.Patches[0];HairMaterials.Set(p,HairState.Wet);HairSystem.Apply(h,p,new(EffectKind.Ignite,1),false);C(!p.Burning);
            HairSystem.Apply(h,p,new(EffectKind.Glue,1),false);C(p.Glue<.25f);HairSystem.Apply(h,p,new(EffectKind.ChangeTemperature,65),false);C(p.Temperature<50&&p.Wet<1);
            p.Glue=1;p.Temperature=100;HairSystem.Tick(h,1);C(p.Glue<1);
        });
        test("v05 transport changes geometry without creating material",()=>{
            var h=Head.Create(0,0);var before=(byte[])h.Volume.Data.Clone();float mass=h.Mass;
            for(int i=0;i<12;i++)h.Volume.Brush(new(EffectKind.ApplyForce,.1f,Vector3.UnitX),new(0,.65f,.25f),.7f);
            C(Math.Abs(mass-h.Mass)<.03f,$"transport mass {mass} -> {h.Mass}");C(!before.SequenceEqual(h.Volume.Data));
        });
        test("v05 board blocks host tool use without consuming held slot",()=>{
            var s=World();var p=s.State.Player(1)!;p.Held=1;s.State.Tools[1].Holder=1;p.ReferenceUp=true;float mass=s.State.SharedHead.Mass;s.UseTool(p,false);C(p.Held==1&&mass==s.State.SharedHead.Mass);
        });
        test("v05 targets and reference use the same signed geometry",()=>{
            foreach(var goal in Goals.All){var h=GoalMaterials.Reference(goal.Id);C(Scoring.Shape(h.Volume.Samples(),goal)>75,goal.Name);foreach(var v in goal.TargetSamples)C(GoalMaterials.Distance(goal.Target,v)>=-.001f);}
        });
        test("v05 live prop readiness cache follows real head movement",()=>{
            var s=World(1);GoalFixtures.Build(s.State.SharedHead,1);GoalFixtures.Place(s.State);var evaluator=new GoalQuality();float before=evaluator.Read(s.State).Value;s.State.SharedHead.Position+=Vector3.UnitX*3;C(evaluator.Read(s.State).Value<before-30);
        });
        test("v05 missing props cannot be fabricated by validation",()=>{
            for(int goal=0;goal<8;goal++){var s=World(goal);GoalFixtures.Build(s.State.SharedHead,goal);s.State.Props.Clear();Validation.Begin(s.State);Validation.Tick(s.State,.1f,5);Validation.Finish(s.State);C(s.State.Props.Count==0&&s.State.Job.Result.Function==0);C(s.State.Job.Result.Final<=69);}
        });
        test("v05 staged nest eggs must actually be placed",()=>{
            var s=World(1);GoalFixtures.Build(s.State.SharedHead,1);var ids=s.State.Props.Select(p=>p.Id).ToArray();Validation.Begin(s.State);for(int i=0;i<90;i++)Validation.Tick(s.State,.1f,i*.1f);Validation.Finish(s.State);C(s.State.Job.Result.Function==0);C(ids.SequenceEqual(s.State.Props.Select(p=>p.Id)));
            GoalFixtures.Place(s.State);Validation.Begin(s.State);for(int i=0;i<90;i++)Validation.Tick(s.State,.1f,i*.1f);Validation.Finish(s.State);C(s.State.Job.Result.Function==100);
        });
        test("v05 state and functional gates prevent a shape-only near-perfect result",()=>{
            C(new Score{Goal=4,Shape=100,State=0,Function=100}.Final<80);C(new Score{Goal=0,Shape=100,State=100,Function=0}.Final<=69);C(new Score{Goal=4,Shape=100,State=100,Function=100}.Final==100);
        });
        test("v05 props and materials survive late snapshot and replay immutably",()=>{
            var s=World(1);var p=s.State.SharedHead.Patches[0];p.Young=.8f;p.Wet=.6f;s.State.Props[0].Position=new(1,2,3);var copy=Wire.Decode<WorldState>(Wire.Encode(s.State));C(copy.SharedHead.Patches[0].Wet==.6f&&copy.Props[0].Position==new Vector3(1,2,3));
            s.Replay.Record(s.State,.11f);s.State.Props[0].Position=default;p.Wet=0;C(s.Replay.Rolling[0].Props[0].Position==new Vector3(1,2,3));C(s.Replay.Rolling[0].Heads.First(h=>h.Id==0).Patches[0].Wet==.6f);
        });
        test("v05 uneven helipad has four viable tool correction routes",()=>{
            foreach(int tool in new[]{0,2,3,9}){
                var s=World();var h=s.State.SharedHead;var player=s.State.Player(1)!;
                h.Volume.Fill(v=>Math.Max(GoalMaterials.Distance(Goals.All[0].Target,v),Math.Min(.19f-new Vector2(v.X,v.Z).Length(),Math.Min(v.Y-.5f,1.23f-v.Y))));
                foreach(var patch in h.Patches){patch.Glue=0;patch.Stiffness=0;}
                float before=Helipad.Measure(h).Quality;var held=s.State.Tools.First(t=>t.Definition==tool);held.Holder=1;player.Held=held.Id;player.BrushSize=2;
                void Aim(Vector3 origin,Vector3 point){player.Position=h.ToWorld(origin)-Vector3.UnitY*1.7f;var d=Vector3.Normalize(h.ToWorld(point)-Session.Eye(player));player.Yaw=MathF.Atan2(-d.X,-d.Z);player.Pitch=MathF.Asin(d.Y);C(Math.Abs(player.Pitch)<=1.35f,"route must respect FPS pitch limit");player.Cooldown=0;s.EndStroke(1);s.State.Time+=.15f;}
                for(int i=0;i<(tool==9?2:30)&&Helipad.Measure(h).Variance>.15f;i++){
                    if(tool==9)Aim(new(0,.875f,i==0?2:-2),new(0,.875f,0));else Aim(new(0,2.1f,.3f),new(0,.8f,0));
                    s.UseTool(player,tool==3);
                }
                if(tool==2){float total=h.Mass+player.Reservoir;Aim(new(2,.3f,0),new(.65f,.3f,0));s.UseTool(player,true);C(Math.Abs(total-h.Mass-player.Reservoir)<.02f,"relocation conservation");}
                foreach(var patch in h.Patches)HairSystem.Apply(h,patch,new(EffectKind.Glue,1),false);
                var result=Helipad.Measure(h);Console.WriteLine($"V05_ROUTE {Tools.Get(tool).Id} quality={before:0.0}->{result.Quality:0.0} variance={result.Variance:0.000}");C(result.Quality>before+10&&result.Stable,$"route {tool}: quality {before}->{result.Quality}, variance={result.Variance}, contacts={result.Contacts}");
            }
        });
        test("v05 uninserted rocket cannot launch from above the silo",()=>{
            var s=World(2);GoalFixtures.Build(s.State.SharedHead,2);s.State.Props[0].Position=s.State.SharedHead.ToWorld(new(0,2.4f,0));Validation.Begin(s.State);for(int i=0;i<90;i++)Validation.Tick(s.State,.1f,i*.1f);Validation.Finish(s.State);C(s.State.Job.Result.Function==0);
        });
        test("v05 props lose support and fall after their hair is removed",()=>{
            var s=World(1);var prop=s.State.Props[0];prop.Attached=true;prop.Local=new(0,.7f,0);prop.Position=s.State.SharedHead.ToWorld(prop.Local);float height=prop.Position.Y;s.State.SharedHead.Volume.Fill(_=>-1);PhysicalProps.Tick(s.State,.2f,null);C(prop.Position.Y<height&&(!prop.Attached||prop.OnScalp));
        });
        test("v05 glued contact preserves loose-piece placement and mass",()=>{
            var s=World();var player=s.State.Player(1)!;var head=s.State.SharedHead;var piece=s.SpawnWig(head.Position+new Vector3(.1f,.15f,0),0);piece.Volume=HairVolume.Create();player.Held=4;s.State.Tools[4].Holder=1;var old=piece.Position;float mass=piece.Mass+head.Mass;s.UseTool(player,false);C(piece.AttachedTo==head.Id&&piece.Bonded);C(piece.Position==old);C(Math.Abs(piece.Mass+head.Mass-mass)<.01f);
        });
        test("v05 prop pickup authority prevents distant and occupied pickup",()=>{
            var s=World(1);var p=s.State.Player(1)!;C(!s.HandleProp(p));var egg=s.State.Props[0];p.Position=egg.Position+new Vector3(0,-1.7f,1);p.Yaw=0;p.Pitch=0;C(s.HandleProp(p));C(egg.Holder==1&&p.CarriedProp==egg.Id);s.RemovePlayer(1);C(egg.Holder==0);
        });
    }
}
