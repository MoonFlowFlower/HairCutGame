using System;
using System.Numerics;
using System.Linq;

namespace Hairball.Core;

public enum LeaveStage { Seated, Rising, ToMirror, Mirror, ToDoor, Done }

public static class PartyLoop
{
    public const float RiseSeconds=2, WalkSeconds=10, SlipSeconds=1, DelaySeconds=3, MaxDelay=6;
    public static readonly Vector3 Table=new(-1.35f,.825f,1.4f), Bell=new(1.6f,.9f,1.3f);
    public static readonly Vector3 MirrorRoot=new(0,0,2.1f), DoorRoot=new(2.9f,0,4.6f);
    public static float Age(WorldState w)=>w.Experiment.LeaveStarted<0?-1:w.Time-w.Experiment.LeaveStarted-RiseSeconds;
    public static Vector3 Root(WorldState w)
    {
        float age=Age(w);if(age<0)return Vector3.Zero;
        if(age<4)return Vector3.Lerp(Vector3.Zero,MirrorRoot,age/4);
        if(age<6)return MirrorRoot;
        return Vector3.Lerp(MirrorRoot,DoorRoot,Math.Clamp((age-6)/4,0,1));
    }
    public static (Vector3 Offset,Vector3 Rotation) Pose(WorldState w)
    {
        float age=Age(w);bool moving=age>=0&&age<4||age>=6&&age<10;
        float brace=w.Players.Any(p=>p.Active&&p.Bracing)?.22f:1;
        float stand=w.Experiment.LeaveStarted<0?0:Math.Clamp((w.Time-w.Experiment.LeaveStarted)/RiseSeconds,0,1)*.2f;
        float wave=moving?MathF.Sin(age*7)*brace:0;
        return (Root(w)+new Vector3(0,stand+Math.Abs(wave)*.025f,0),new Vector3(wave*.035f,0,wave*.065f));
    }
}

public sealed partial class Session
{
    public bool CanRingBell(PlayerState p)
    {
        var e=State.Experiment;var delta=PartyLoop.Bell-Eye(p);float distance=delta.Length();
        return e.IsB&&State.Phase==Phase.Build&&p.Active&&!p.NetworkAway&&State.Player(p.Id)==p&&p.CarriedProp<0&&
            !e.Resolved&&e.Leave==LeaveStage.Seated&&e.LeaveStarted<0&&State.Props.Any(t=>t.Id==e.HelicopterId&&t.Attached&&t.Holder==0)&&
            LookingAt(p,PartyLoop.Bell,.25f,2.5f)&&(ObstructionDistance?.Invoke(Eye(p),Vector3.Normalize(delta),distance)??distance)>=distance-.12f;
    }
    public bool RingBell(PlayerState p)
    {
        if(!CanRingBell(p))return false;
        var e=State.Experiment;e.EarlySeconds=Math.Max(0,State.Remaining);e.BellActor=p.Id;BeginLeaving();SetBrace(p,false);
        ExperimentEvent("bell",p.Id,PartyLoop.Bell,"customer rising; controls live");Event(p.Id,0,PartyLoop.Bell,"Completion bell rang",2);return true;
    }
    void BeginLeaving()
    {
        var e=State.Experiment;e.Leave=LeaveStage.Rising;e.LeaveStarted=State.Time;
        if(State.Player(e.CustomerActor) is {} human){if(!human.Standing){e.StandCooldown=0;StartStanding(human);}}
        DebrisSystem.ReleaseSupport(State.Debris,WorkCenter,.85f);
        ExperimentEvent("leave",0,State.SharedHead.Position,"Rising");
    }
    void TickParty(float dt)
    {
        var e=State.Experiment;if(!e.IsB||e.Resolved||State.Phase!=Phase.Build)return;StageMolds();
        if(e.Leave==LeaveStage.Seated)
        {
            if(State.Remaining<=20&&!e.WatchShown){e.WatchShown=true;CustomerRemarks.Say(e,Remark.Watch,State.Time);}
            if(State.Remaining<=10&&!e.CoatShown){e.CoatShown=true;CustomerRemarks.Say(e,Remark.Coat,State.Time);}
            if(State.Remaining<=0)BeginLeaving();
        }
        if(e.CustomerActor==0&&e.Leave==LeaveStage.Rising&&e.DelayUsed<PartyLoop.MaxDelay&&State.Players.FirstOrDefault(p=>p.Active&&p.Bracing) is {} bracer)
        {
            e.DelayUsed+=PartyLoop.DelaySeconds;e.Leave=LeaveStage.Seated;e.LeaveStarted=-1;State.Remaining=PartyLoop.DelaySeconds;
            ExperimentEvent("seat_delay",bracer.Id,State.SharedHead.Position,$"delay=3; used={e.DelayUsed}");
        }
        if(e.LeaveStarted>=0&&State.Player(e.CustomerActor) is {} human)
        {
            bool atMirror=Vector3.Distance(human.Position,PartyLoop.MirrorRoot)<.8f;
            if(atMirror)e.PassedMirror=true;
            bool done=Vector3.Distance(human.Position,PartyLoop.DoorRoot)<.8f||State.Time-e.LeaveStarted>=20;
            var stage=done?LeaveStage.Done:!human.Standing?LeaveStage.Rising:atMirror?LeaveStage.Mirror:!e.PassedMirror?LeaveStage.ToMirror:LeaveStage.ToDoor;
            if(stage!=e.Leave){e.Leave=stage;ExperimentEvent("leave",human.Id,State.SharedHead.Position,stage.ToString());}
        }
        else if(e.LeaveStarted>=0)
        {
            float age=PartyLoop.Age(State);
            var stage=age<0?LeaveStage.Rising:age<4?LeaveStage.ToMirror:age<6?LeaveStage.Mirror:age<10?LeaveStage.ToDoor:LeaveStage.Done;
            if(stage!=e.Leave){e.Leave=stage;ExperimentEvent("leave",0,State.SharedHead.Position,stage.ToString());if(stage==LeaveStage.Mirror){e.Curtain=PartyResults.Evaluate(State,State.Props.Any(p=>p.Id==e.HelicopterId&&p.Attached&&LandingSupport(State,p).Stable),targetCard).Reaction;CustomerRemarks.Say(e,Remark.Mirror,State.Time);}}
        }
        CustomerMotion.Apply(State);SyncFaces();
        // Attached props and wigs use exactly the same transform as queries/rendering.
        foreach(var attached in State.Heads.Where(h=>h.AttachedTo==0)){attached.Position=State.SharedHead.ToWorld(attached.AttachedOffset);attached.Rotation=State.SharedHead.Rotation+attached.AttachedRotation;}
        var prop=State.Props.FirstOrDefault(p=>p.Id==e.HelicopterId);
        if(prop==null){e.LandingIssues=LandingIssue.MissingHelicopter;ResolveExperiment(false,"missing commission object");return;}
        if(prop.Attached&&prop.Holder==0)
        {
            prop.Position=State.SharedHead.ToWorld(prop.Local);prop.Rotation=State.SharedHead.Rotation+prop.AttachedRotation;
            var support=LandingSupport(State,prop);
            e.LandingIssues=support.Issues;e.SupportContacts=support.Contacts;e.SupportVariance=support.Variance;e.SupportResistance=support.Resistance;
            e.StableSeconds=support.Stable?Math.Min(ExperimentState.Dwell,e.StableSeconds+dt):0;prop.HeldTime=e.StableSeconds;
            e.BadSupportSeconds=support.Stable?0:e.BadSupportSeconds+dt;
            e.Flight=FlightStage.Contact;
            if(e.BadSupportSeconds>=PartyLoop.SlipSeconds)
            {
                e.SlippedAt=State.Time;e.SlipPosition=prop.Position;e.SlipIssues=e.LandingIssues;e.SlipCount++;
                prop.Attached=false;prop.Released=true;prop.Velocity=new(.7f,0,.3f);e.StableSeconds=0;e.BadSupportSeconds=0;
                ExperimentEvent("commission_slip",0,prop.Position,LandingTelemetry());
            }
        }
        else {e.StableSeconds=0;e.BadSupportSeconds=0;e.LandingIssues=LandingIssue.NoContact;e.Flight=FlightStage.Staging;}
        if(e.Leave==LeaveStage.Done)
        {
            bool good=prop.Attached&&prop.Holder==0&&LandingSupport(State,prop).Stable;
            if(!good&&e.SlippedAt>=0)e.LandingIssues|=e.SlipIssues;
            ResolveExperiment(good,good?"commission supported at door":"commission missing or unsupported at door");
        }
    }
}

