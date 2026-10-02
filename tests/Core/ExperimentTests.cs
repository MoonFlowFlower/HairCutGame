using Hairball.Core;

using System.Numerics;

internal static class ExperimentTests

{

    public static Session World(ExperimentVariant variant=ExperimentVariant.B)

    {

        var s=new Session{ForceAI=true};s.State.Experiment.Variant=variant;s.AddPlayer(1,"A");s.StartMatch();

        // Focused legacy checks use an explicit shortened fixture; the B production default is 150.

        s.BuildSeconds=75;s.State.Phase=Phase.Build;s.State.Remaining=75;return s;

    }

    public static void Aim(Session s,PlayerState p,Vector3 eye,Vector3 point)

    {

        p.Position=eye-Vector3.UnitY*1.7f;var d=Vector3.Normalize(point-eye);p.Yaw=MathF.Atan2(-d.X,-d.Z);p.Pitch=MathF.Asin(d.Y);p.Cooldown=0;

    }

    public static void Equip(Session s,PlayerState p,int definition)

    {

        s.Drop(p);

        if(definition==7&&!s.State.Tools.Any(t=>t.Definition==7))s.State.Tools.Add(new(){Id=77,Definition=7,Charge=3}); // legacy shared effect fixture, not B shelf

        var t=s.State.Tools.First(t=>t.Definition==definition&&t.Holder==0);p.Held=t.Id;t.Holder=p.Id;

    }

    public static void Run(Action<string,Action> test,Action<bool,string> check)

    {

        void C(bool value,string message="v06 assertion")=>check(value,message);

        test("v06 landing reasons separate flat soft hair from geometry and absent support",()=>{

            var s=World();var h=s.State.SharedHead;GoalFixtures.Build(h,0);var heli=new PropState{Position=new(0,2.6f,0)};

            foreach(var p in h.Patches){p.Glue=0;p.Stiffness=0;p.Temperature=20;p.Anchored=false;}

            var soft=Session.LandingSupport(s.State,heli);C(soft.Contacts==9&&soft.Variance<.18f);C(soft.Issues==LandingIssue.Soft);

            foreach(var p in h.Patches)p.Glue=.8f;

            C(Session.LandingSupport(s.State,heli).Stable);

            h.Rotation=new(.45f,0,0);C(!Session.LandingSupport(s.State,heli).Stable,"tilt loses contact or level support");

            h.Rotation=default;foreach(var p in h.Patches)p.Burning=true;

            C(Session.LandingSupport(s.State,heli).Issues==LandingIssue.Burning);

            h.Volume.Fill(_=>-1);C(Session.LandingSupport(s.State,heli).Issues==LandingIssue.Coverage,"no phantom softness or slope for absent hair");

        });

        test("v06 landing reports simultaneous contact and customer blockers in both languages",()=>{

            var s=World();GoalFixtures.Build(s.State.SharedHead,0);var support=Session.LandingSupport(s.State,new(){Position=new(0,2.6f,0)});

            s.State.Job.Penalty=50;s.State.Customers[0].Recovery=1;

            C(Session.LandingBlockers(s.State,support,false)==(LandingIssue.NoContact|LandingIssue.CustomerRecovery));

            s.State.Job.Penalty=49;s.State.Customers[0].Recovery=0;C(Session.LandingBlockers(s.State,support,true)==LandingIssue.None);

            foreach(var issue in Enum.GetValues<LandingIssue>().Where(x=>x!=LandingIssue.None))foreach(bool zh in new[]{true,false})

                C(!string.IsNullOrWhiteSpace(LandingFeedback.Describe(issue,zh,true)),"every actual blocker has localized advice");

            C(LandingFeedback.Describe(LandingIssue.Soft|LandingIssue.Uneven,true).Contains("太软")&&LandingFeedback.Describe(LandingIssue.Soft|LandingIssue.Uneven,true).Contains("不平"));

        });







        test("v06 A keeps original material, tools, reference and locked validation",()=>{

            var a=World(ExperimentVariant.A);var original=World(ExperimentVariant.Off);

            C(a.State.SharedHead.Volume.Data.SequenceEqual(original.State.SharedHead.Volume.Data));

            C(a.State.Tools.Select(t=>(t.Definition,t.Charge)).SequenceEqual(original.State.Tools.Select(t=>(t.Definition,t.Charge))));

            C(a.State.Heads.Any(h=>h.Miniature));a.State.Remaining=0;a.Tick(.01f);C(a.State.Phase==Phase.Validation&&a.State.SharedHead.Locked);

        });





        test("v06 mirror route respects bodies, cover and unrelated objects",()=>{

            var s=World();var p=s.State.Player(1)!;p.Position=new(3,0,0);Vector3 danger=new(0,2,-.5f);

            C(s.NoticeDanger(danger,1,"test"));s.State.Experiment.Attention=new();

            p.Position=new(0,0,1.5f);C(Session.MirrorBlocked(s.State));C(!s.NoticeDanger(danger,1,"test"));

            p.Position=new(3,0,0);var cover=s.State.Props.Single(x=>x.Goal==-1);cover.Position=new(0,1.75f,1.5f);C(Session.MirrorBlocked(s.State));C(!s.NoticeDanger(danger,1,"test"));

            cover.Position=new(2,1.75f,1.5f);C(!Session.MirrorBlocked(s.State));C(s.NoticeDanger(danger,1,"test"));

        });

        test("v06 mirror warning cancels when evidence hidden before commit",()=>{

            var s=World();s.State.Player(1)!.Position=new(3,0,0);s.State.SharedHead.Patches[0].Burning=true;

            C(s.NoticeDanger(new(0,2,-.2f),1,"fire"));s.State.Props.Single(x=>x.Goal==-1).Position=new(0,1.7f,1.5f);s.Tick(.1f);

            C(s.State.Experiment.Attention.Stage==AttentionStage.Unaware);

        });

        test("v06 directional attention notice commit reaction with habituation",()=>{

            var s=World();C(s.Hear(new(2,2,1),1,"tool_motor"));C(!s.Hear(new(2,2,1),1,"tool_motor"));

            s.Tick(1.2f);C(s.State.Experiment.Attention.Stage==AttentionStage.Commit);s.Tick(.4f);C(s.State.Experiment.Attention.Stage==AttentionStage.React);

            s.Tick(.8f);C(s.State.SharedHead.Rotation.Y>.2f);var local=new Vector3(0,.6f,.2f);C(Vector3.Distance(s.State.SharedHead.ToWorld(local),s.State.SharedHead.Position+local)>.02f);

            s.Tick(1.3f);C(s.State.Experiment.Attention.Stage==AttentionStage.Unaware);C(!s.Hear(new(2,2,1),1,"tool_motor"));s.Tick(9);C(s.Hear(new(-2,2,1),1,"tool_motor"));

        });

        test("v06 scoped sniper is silent; sustained motor does not retrigger attention",()=>{

            var s=World();var p=s.State.Player(1)!;Equip(s,p,7);s.UseTool(p,true);C(s.State.Experiment.Attention.Stage==AttentionStage.Unaware,"zoom is not a shot");

            Equip(s,p,3);s.Inputs[1]=(default,p.Yaw,p.Pitch,Buttons.Primary);for(int i=0;i<150;i++)s.Tick(.1f);

            C(s.State.Customers[0].ReactionCount==1,"a held motor creates one onset");

            s.Inputs.Clear();s.Tick(9);s.Inputs[1]=(default,p.Yaw,p.Pitch,Buttons.Primary);s.Tick(.1f);C(s.State.Customers[0].ReactionCount==2,"quiet restores salience");

        });

        test("v06 brace suppresses tool and movement, releases on input loss and disconnect",()=>{

            var s=World();var p=s.State.Player(1)!;Equip(s,p,1);Aim(s,p,new(0,1.7f,1.3f),s.State.SharedHead.Position);

            s.Inputs[1]=(default,p.Yaw,p.Pitch,Buttons.Interact|Buttons.Primary);float mass=s.State.SharedHead.Mass;

            s.Hear(new(2,2,1),1,"test");s.Tick(.1f);C(p.Bracing);C(s.State.SharedHead.Mass<=mass+.001f);C(p.Held>=0);

            s.State.Time=s.State.Experiment.Attention.Started+2.5f;s.State.Experiment.Attention.Stage=AttentionStage.React;

            float small=Session.AttentionPose(s.State).Rotation.Length();p.Bracing=false;float normal=Session.AttentionPose(s.State).Rotation.Length();C(small>0&&small<normal*.3f);

            s.Inputs.Clear();s.Tick(.1f);C(!p.Bracing);s.Inputs[1]=(default,p.Yaw,p.Pitch,Buttons.Interact);s.Tick(.1f);C(p.Bracing);s.RemovePlayer(1);C(!p.Bracing);

            s.AddPlayer(2,"replacement");s.StartMatch();C(s.State.Players.All(p=>!p.Bracing));

        });

        test("v06 B hides exact model, caps shared growth including alternate tools",()=>{

            var s=World();C(!s.State.Heads.Any(h=>h.Miniature));var p=s.State.Player(1)!;Equip(s,p,1);Aim(s,p,new(0,2,2),new(0,2,0));

            float initial=s.State.Heads.Sum(h=>h.Mass);for(int i=0;i<120;i++){p.Cooldown=0;s.State.Time+=.1f;s.UseTool(p,false);s.EndStroke(1);}

            float left=s.State.Tools.First(t=>t.Definition==1).GrowthRemaining;C(left<GrowthSpray.BottleCapacity&&left>=0);C(s.State.Heads.Sum(h=>h.Mass)-initial<=GrowthSpray.BottleCapacity+.01f);

            C(!s.RequestOrder(1,0));C(new Score{FunctionOnly=true,Function=100,Shape=0,State=0,Penalty=30}.Final==70);

            s.State.Tools.First(t=>t.Definition==1).GrowthRemaining=0;Equip(s,p,1);float before=s.State.SharedHead.Mass;s.UseTool(p,false);C(before==s.State.SharedHead.Mass);

        });





        test("v06 snapshots and replay retain attention brace flight and budget without aliasing",()=>{

            var s=World();s.Hear(new(3,2,0),1,"shot");s.State.Player(1)!.Bracing=true;s.State.Tools.First(t=>t.Definition==1).GrowthRemaining=1.2f;

            var copy=Wire.Decode<WorldState>(Wire.Encode(s.State));C(copy.Experiment.Attention.Source==new Vector3(3,2,0)&&copy.Player(1)!.Bracing&&copy.Tools.First(t=>t.Definition==1).GrowthRemaining==1.2f);

            s.Replay.Record(s.State,.11f);s.State.Experiment.Attention.Source=default;s.State.Tools.First(t=>t.Definition==1).GrowthRemaining=0;

            C(s.Replay.Rolling[0].Tools.First(t=>t.Definition==1).GrowthRemaining==1.2f&&s.Replay.Rolling[0].Experiment.Attention.Source==new Vector3(3,2,0));C(s.Replay.Rolling[0].BracingPlayers.Contains(1));

        });





    }

}





