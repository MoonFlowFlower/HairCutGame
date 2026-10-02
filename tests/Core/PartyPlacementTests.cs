using Hairball.Core;
using System.Numerics;

internal static class PartyPlacementTests
{
    public static void Place(Session s,bool hard=true)
    {
        GoalFixtures.Build(s.State.SharedHead,0);
        foreach(var p in s.State.SharedHead.Patches){p.Glue=hard?1:0;p.Stiffness=0;p.Temperature=20;p.Anchored=false;}
        var prop=s.State.Props.Single(p=>p.Goal==0);prop.Local=new(0,1.14f,0);prop.Position=s.State.SharedHead.ToWorld(prop.Local);prop.Attached=true;prop.Holder=0;prop.AttachedRotation=default;
    }
    public static void Run(Action<string,Action> test,Action<bool,string> check)
    {
        void C(bool b,string m="placement assertion")=>check(b,m);
        test("party actual offset and rotated gear queries accept alternate placements",()=>{
            var s=ExperimentTests.World();var h=s.State.SharedHead;
            h.Volume.Fill(v=>Math.Min(.9f-Math.Abs(v.X),Math.Min(.18f-Math.Abs(v.Y-.7f),.9f-Math.Abs(v.Z))));foreach(var p in h.Patches)p.Glue=1;
            var prop=s.State.Props.Single(p=>p.Goal==0);prop.Position=h.Position+new Vector3(.25f,1.14f,.2f);prop.Rotation.Y=.7f;
            var probes=Session.LandingProbes(s.State,prop);C(Session.LandingSupport(s.State,prop).Stable);
            var expected=Session.Rotate(new(.27f,0,.29f),.7f);C(Vector3.Distance(probes.Last().Target,prop.Position+expected-Vector3.UnitY*.26f)<.001f);
            prop.Position+=Vector3.UnitX*2;C(!Session.LandingSupport(s.State,prop).Stable);
            prop.Position=h.Position+new Vector3(0,2,0);C(!Session.LandingSupport(s.State,prop).Stable,"floating props cannot claim contact");
        });
        test("party trials never end construction and one second of bad support slips",()=>{
            var s=ExperimentTests.World();Place(s);for(int i=0;i<40;i++)s.Tick(.1f);
            C(!s.State.Experiment.Resolved&&s.State.Experiment.StableSeconds==3);
            foreach(var p in s.State.SharedHead.Patches)p.Glue=0;
            for(int i=0;i<9;i++)s.Tick(.1f);C(s.State.Props.Single(p=>p.Goal==0).Attached);
            s.Tick(.2f);C(!s.State.Props.Single(p=>p.Goal==0).Attached&&s.State.Experiment.SlipCount==1&&s.State.Experiment.SlipIssues.HasFlag(LandingIssue.Soft));
        });
        test("party door validates supported object; empty and slipped heads fail",()=>{
            foreach(bool good in new[]{false,true}){
                var s=ExperimentTests.World();if(good)Place(s);s.State.Remaining=0;
                for(int i=0;i<140&&!s.State.Experiment.Resolved;i++)s.Tick(.1f);
                C(s.State.Experiment.Resolved&&s.State.Experiment.Success==good);C(s.State.Experiment.Leave==LeaveStage.Done);
                C(Vector3.Distance(s.State.SharedHead.Position,PartyLoop.DoorRoot+new Vector3(0,1.7f,0))<.03f);
            }
        });
        test("party brace postpones rising at most two times",()=>{
            var s=ExperimentTests.World();var p=s.State.Player(1)!;ExperimentTests.Aim(s,p,new(0,1.7f,1.3f),s.State.SharedHead.Position);
            s.Inputs[p.Id]=(default,p.Yaw,p.Pitch,Buttons.Interact);s.State.Remaining=0;
            for(int i=0;i<100;i++)s.Tick(.1f);
            C(s.State.Experiment.DelayUsed==6&&s.State.Experiment.LeaveStarted>=0);
        });
        test("party bell requires placed object and records actor and early time",()=>{
            var s=ExperimentTests.World();var p=s.State.Player(1)!;ExperimentTests.Aim(s,p,new(1.6f,1.7f,2.65f),PartyLoop.Bell);
            C(!s.RingBell(p));Place(s);C(s.RingBell(p));C(s.State.Experiment.BellActor==p.Id&&s.State.Experiment.EarlySeconds==75&&s.State.Experiment.Leave==LeaveStage.Rising);C(!s.RingBell(p));
            var clone=s.State.Experiment.Clone();C(clone.BellActor==p.Id&&clone.LeaveStarted==s.State.Experiment.LeaveStarted);
        });
        test("party moving root is common to support tool queries and bracing",()=>{
            var s=ExperimentTests.World();Place(s);s.State.Time=10;s.State.Experiment.LeaveStarted=s.State.Time-4;s.State.Experiment.Leave=LeaveStage.ToMirror;CustomerMotion.Apply(s.State);
            C(s.State.SharedHead.Position.Z>1);var pose=PartyLoop.Pose(s.State);s.State.Player(1)!.Bracing=true;var braced=PartyLoop.Pose(s.State);C(braced.Rotation.Length()<pose.Rotation.Length()*.3f);
        });
    }
}

