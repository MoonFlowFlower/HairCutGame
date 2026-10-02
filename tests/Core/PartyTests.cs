using Hairball.Core;
using System.Numerics;

internal static class PartyTests
{
    public static void Run(Action<string,Action> test,Action<bool,string> check)
    {
        void C(bool b,string m="party assertion")=>check(b,m);
        test("party damage cannot block otherwise valid B support",()=>{
            var s=ExperimentTests.World();GoalFixtures.Build(s.State.SharedHead,0);s.State.Job.Penalty=999;
            C(Session.LandingBlockers(s.State,Session.LandingSupport(s.State,new(){Position=new(0,2.6f,0)}),true)==LandingIssue.None);
            C(!s.State.Tools.Any(t=>t.Definition==7)&&Tools.Get(7).Id=="sniper");
        });
        test("party fire ramp cap provides eight seconds and water recovery",()=>{
            float tolerance=0;for(int i=0;i<79;i++)tolerance=CustomerRemarks.Tolerance(tolerance,64,.1f);
            C(tolerance<100);tolerance=CustomerRemarks.Tolerance(tolerance,64,.1f);C(tolerance==100);
            tolerance=CustomerRemarks.Tolerance(50,0,3);C(Math.Abs(tolerance-20)<.001f);
            tolerance=CustomerRemarks.Tolerance(0,1,5);C(tolerance==20);C(CustomerRemarks.Tolerance(tolerance,0,2)==0);
        });
        test("party danger interrupts chatter but repeated lines respect cooldown",()=>{
            var e=new ExperimentState();C(CustomerRemarks.Say(e,Remark.Wet,0));C(!CustomerRemarks.Say(e,Remark.Cold,1)==false);
            C(CustomerRemarks.Say(e,Remark.Warning,1));C(!CustomerRemarks.Say(e,Remark.Warning,2));C(!CustomerRemarks.Say(e,Remark.Wet,2));C(CustomerRemarks.Say(e,Remark.Wet,6));
        });
        test("party real fire is rescuable and exhaustion has explicit outcome",()=>{
            var s=ExperimentTests.World();var h=s.State.SharedHead;
            foreach(var p in h.Patches){p.Length=3;p.Burning=true;p.Temperature=150;}
            for(int i=0;i<20;i++)s.Tick(.1f);
            C(s.State.Experiment.Tolerance>0&&!s.State.Experiment.Resolved&&s.State.Job.Penalty==0);
            foreach(var p in h.Patches){p.Burning=false;p.Temperature=20;}
            for(int i=0;i<30;i++)s.Tick(.1f);C(s.State.Experiment.Tolerance==0&&!s.State.Experiment.Resolved);
            s.State.Experiment.Tolerance=99;foreach(var p in h.Patches){p.Length=3;p.Burning=true;p.Temperature=150;}
            s.Tick(.2f);C(s.State.Experiment.Resolved&&!s.State.Experiment.Success&&s.State.Experiment.LandingIssues==LandingIssue.CustomerFled);
            var copy=Wire.Decode<WorldState>(Wire.Encode(s.State));C(copy.Experiment.Remark==Remark.Flee&&copy.Experiment.Tolerance==100);
        });
    }
}
