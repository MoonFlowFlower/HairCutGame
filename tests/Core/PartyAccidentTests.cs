using Hairball.Core;
using System;
using System.Linq;
using System.Numerics;
internal static class PartyAccidentTests
{
    public static void Run(Action<string,Action> test,Action<bool,string> check)
    {
        void C(bool b,string m="party accident assertion")=>check(b,m);
        (Session,PlayerState,PlayerState) Pair(){var s=ExperimentTests.World();var a=s.State.Player(1)!;var b=s.AddPlayer(2,"B")!;b.Position=new(3,0,0);ExperimentTests.Aim(s,a,new(3,1.35f,2),b.Position+Vector3.UnitY*1.35f);s.Tick(.001f);return(s,a,b);}
        void Use(Session s,PlayerState a,int tool,bool secondary=false){ExperimentTests.Equip(s,a,tool);a.Cooldown=0;s.UseTool(a,secondary);}
        test("party freeze lasts two seconds, view stays free, tap rescues and immunity prevents chaining",()=>{
            var(s,a,b)=Pair();float penalty=s.State.Job.Penalty;var pose=b.Position;float yaw=b.Yaw,pitch=b.Pitch;Use(s,a,5);C(Math.Abs(b.FreezeUntil-s.State.Time-2)<.001f);C(PartyAccidents.Movement(b,Vector2.One,s.State.Time)==Vector2.Zero&&!PartyAccidents.Primary(b,s.State.Time)&&!PartyAccidents.Jump(b,s.State.Time));
            C(b.Position==pose&&b.Yaw==yaw&&b.Pitch==pitch&&b.Impulse==default&&s.State.Job.Penalty==penalty);s.Inputs[b.Id]=(Vector2.One,1.1f,.6f,Buttons.None);s.Tick(.1f);C(b.Yaw==1.1f&&b.Pitch==.6f);Use(s,a,0);C(b.FreezeUntil==0);C(s.ActionLog.Any(x=>x.Kind==PartyAction.ThawFriend&&x.Actor==a.Id&&x.Target==b.Id));Use(s,a,5);C(b.FreezeUntil==0);s.State.Time+=5;Use(s,a,5);s.Tick(2.01f);C(PartyAccidents.Movement(b,Vector2.One,s.State.Time)==Vector2.One);
        });
        test("party sticky hands keep the tool, slow movement and release by heat or timeout",()=>{
            var(s,a,b)=Pair();ExperimentTests.Equip(s,b,0);Use(s,a,4);int held=b.Held;C(Math.Abs(b.GlueUntil-s.State.Time-3)<.001f&&PartyAccidents.Movement(b,Vector2.One,s.State.Time)==Vector2.One*.4f);s.Drop(b);C(b.Held==held);var other=s.State.Tools.First(t=>t.Holder==0);other.Position=Session.Eye(b);C(!s.Pickup(b,other.Id));Use(s,a,3,true);C(b.GlueUntil==0&&s.ActionLog.Any(x=>x.Kind==PartyAction.ReleaseFriend));s.Drop(b);C(b.Held==-1);
            s.State.Time+=6;Use(s,a,4);s.Tick(3.01f);C(!s.Sticky(b));
        });
        test("party own hair fire ends by water or four seconds without changing customer result",()=>{
            var(s,a,b)=Pair();Use(s,a,8);C(Math.Abs(b.FireUntil-s.State.Time-4)<.001f&&s.State.Barber(b.Slot).Patches.Any(q=>q.Burning));Use(s,a,10);C(b.FireUntil==0&&!s.State.Barber(b.Slot).Patches.Any(q=>q.Burning));s.State.Time+=7;Use(s,a,8);s.Tick(4.01f);C(!s.State.Barber(b.Slot).Patches.Any(q=>q.Burning)&&b.FireUntil==0&&!s.State.Experiment.Resolved);C(b.Impulse==default);
        });
        test("party secret draws stay outside shared snapshots, complete from facts and reveal without pay",()=>{
            var(s,a,b)=Pair();s.NextRound();s.State.Phase=Phase.Build;s.TaskPicker=id=>id==1?SecretKind.Bell:SecretKind.Rescue;s.Tick(.001f);C(s.OwnTask(1)?.Kind==SecretKind.Bell&&s.OwnTask(2)?.Kind==SecretKind.Rescue);var wire=Wire.Decode<WorldState>(Wire.Encode(s.State));C(wire.Experiment.ResultTasks.Count==0);var pay=PartyResults.Evaluate(s.State,true);s.RecordAction(PartyAction.Bell,1);s.RecordAction(PartyAction.ThawFriend,2,1);C(s.OwnTask(1)!.Complete&&s.OwnTask(2)!.Complete&&pay==PartyResults.Evaluate(s.State,true));s.State.Experiment.Leave=LeaveStage.Done;s.Tick(.01f);C(s.State.Experiment.ResultTasks.Count==2&&s.State.Experiment.ResultTasks.All(t=>t.Complete));C(Wire.Decode<WorldState>(Wire.Encode(s.State)).Experiment.ResultTasks.Count==2);
            var off=ExperimentTests.World();off.SecretTasksEnabled=false;off.Tick(.01f);C(off.OwnTask(1)==null&&!off.State.Experiment.SecretsEnabled);
        });
        test("party door tasks require the actor's logged target and actual surviving material",()=>{
            var s=ExperimentTests.World();s.TaskPicker=_=>SecretKind.WigAtDoor;s.Tick(.01f);var wig=s.State.Heads.First(h=>h.Loose&&!h.Miniature);wig.AttachedTo=0;s.RecordAction(PartyAction.Wig,1,wig.Id);s.State.Experiment.Leave=LeaveStage.Done;s.Tick(.01f);C(s.State.Experiment.ResultTasks.Single().Complete);
            s=ExperimentTests.World();s.TaskPicker=_=>SecretKind.IceAtDoor;s.Tick(.01f);s.State.SharedHead.Patches[0].Temperature=-40;s.RecordAction(PartyAction.Freeze,1,0);s.State.Experiment.Leave=LeaveStage.Done;s.Tick(.01f);C(s.State.Experiment.ResultTasks.Single().Complete);
        });
        test("party solo tasks remain possible and all B spending stays excluded",()=>{
            for(int i=0;i<50;i++){var s=ExperimentTests.World();s.Tick(.01f);C(s.OwnTask(1)!.Kind is not (SecretKind.ClipFriend or SecretKind.FreezeFriend or SecretKind.Rescue));var p=s.State.Player(1)!;ExperimentTests.Aim(s,p,SpecialOrders.Button(1)+Vector3.UnitZ,SpecialOrders.Button(1));C(!s.RequestOrder(1,1)&&s.State.Job.Wallet==200);}
        });
        test("party attention secret waits for an actual reaction; canceled warning cannot complete it",()=>{
            var s=ExperimentTests.World();s.TaskPicker=_=>SecretKind.Attention;var p=s.State.Player(1)!;p.Position=new(2.2f,0,.2f);s.Tick(.01f);C(s.Hear(Session.Eye(p),p.Id,"task_test"));C(!s.OwnTask(p.Id)!.Complete);s.Tick(1.2f);C(!s.OwnTask(p.Id)!.Complete);s.Tick(.6f);C(s.OwnTask(p.Id)!.Complete);
            s=ExperimentTests.World();s.TaskPicker=_=>SecretKind.Attention;p=s.State.Player(1)!;p.Position=new(2.2f,0,.2f);s.Tick(.01f);C(s.NoticeDanger(Session.Eye(p),p.Id,"test_warning"));s.Tick(.1f);C(s.State.Experiment.Attention.Stage==AttentionStage.Unaware&&!s.OwnTask(p.Id)!.Complete);
        });
        test("party actual clip completes only the cutter's secret from removed teammate material",()=>{
            var(s,a,b)=Pair();s.NextRound();s.State.Phase=Phase.Build;s.TaskPicker=_=>SecretKind.ClipFriend;b.Position=new(3,0,0);s.Tick(.01f);ExperimentTests.Aim(s,a,new(3,2,2),s.State.Barber(b.Slot).ToWorld(new(0,.45f,.3f)));Use(s,a,0);C(s.OwnTask(a.Id)!.Complete&&!s.OwnTask(b.Id)!.Complete&&s.ActionLog.Any(x=>x.Kind==PartyAction.ClipFriend&&x.Target==b.Id));
        });
    }
}
