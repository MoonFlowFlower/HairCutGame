using System;
using System.Linq;
using System.Numerics;
namespace Hairball.Core;
public static class GlueDrag
{
    public const float MaxSpeed=.8f;
    public static Vector3 Velocity(WorldState w,PlayerState p,float time,Vector3? head=null)
    {
        if(!p.AllowDrag||!p.GlueHead||p.Customer||time>=p.GlueUntil||w.Phase!=Phase.Build||w.Experiment.LeaveStarted<0)return default;
        var destination=(head??w.SharedHead.Position)+Session.Rotate(p.GlueOffset,w.SharedHead.Rotation.Y);var delta=destination-Session.Eye(p);delta.Y=0;
        return delta.Length()<.12f?Vector3.Zero:Vector3.Normalize(delta)*Math.Min(MaxSpeed,delta.Length()*2);
    }
}
public sealed partial class Session
{
    float lastAlertActivity,wakeUntil;
    void ResetAlertness(){lastAlertActivity=State.Time;wakeUntil=0;State.Experiment.Drowsiness=0;}
    void AlertActivity(){lastAlertActivity=State.Time;}
    void NovelSound(Vector3 position,int actor,string category)
    {
        bool asleep=State.Experiment.CustomerActor==0&&State.Experiment.Drowsiness>.1f;AlertActivity();State.Experiment.Drowsiness=0;
        if(asleep)wakeUntil=State.Time+8;
        if(asleep){var a=State.Experiment.Attention;a.Stage=AttentionStage.Unaware;a.QuietUntil=State.Time;BeginAttention(position,actor,"wake:"+category,false);}
    }
    void TickAlertness(float dt)
    {
        var e=State.Experiment;if(e.CustomerActor!=0||State.Phase!=Phase.Build||e.Resolved||e.Leave!=LeaveStage.Seated){e.Drowsiness=0;return;}
        // Quiet tools still count when they actually contact this customer's material.
        if(State.Tools.Any(t=>t.Holder!=0&&t.HitHair&&State.Time-t.LastUse<.2f&&State.Heads.Any(h=>h.Id==t.ContactHead&&(h.Id==0||h.ParentHead==0||h.AttachedTo==0))))AlertActivity();
        float target=State.Time>=wakeUntil&&(State.Twist.Kind==TwistKind.SleepingAI||State.Time-lastAlertActivity>=20)?1:0;
        e.Drowsiness+=Math.Clamp(target-e.Drowsiness,-dt*2,dt*.18f);
    }
    public static Vector3 CustomerSlapPoint(WorldState w)=>w.SharedHead.ToWorld(new(0,.1f,.34f));
    public static bool CanSlapAI(WorldState w,PlayerState p)
    {
        if(!w.Experiment.IsB||w.Phase!=Phase.Build||w.Experiment.CustomerActor!=0||w.Experiment.Resolved||w.Experiment.Leave!=LeaveStage.Seated||w.Experiment.Drowsiness<=.1f||!p.Active||p.Customer||p.NetworkAway||!PartyBodies.Empty(p)||p.Bracing||p.ThrowCharge>0||p.ReferenceUp||p.Cooldown>0||w.Time<p.GlueUntil||!PartyAccidents.Primary(p,w.Time))return false;
        var face=CustomerSlapPoint(w);
        return Vector3.Distance(Eye(p),face)<=1.7f&&w.SharedHead.ToLocal(Eye(p)).Z>0&&LookingAt(p,face,.42f,1.7f);
    }
    bool SlapAI(PlayerState p)
    {
        if(!CanSlapAI(State,p))return false;
        var face=CustomerSlapPoint(State);float distance=Vector3.Dot(face-Eye(p),Aim(p));
        if((ObstructionDistance?.Invoke(Eye(p),Aim(p),distance)??distance)<distance-.35f)return false;
        NovelSound(Eye(p),p.Id,"slap");p.Cooldown=.35f;
        RecordAction(PartyAction.Attention,p.Id);ExperimentEvent("customer_slap",p.Id,face,"wake");
        Event(p.Id,0,face,"Customer slapped awake",1);CustomerMotion.Apply(State);SyncFaces();return true;
    }
    void TickHeadGlue()
    {
        foreach(var p in State.Players.Where(p=>p.Active&&!p.Customer)){
            if(p.GlueHead&&(State.Time>=p.GlueUntil||!p.AllowDrag||!State.SharedHead.Patches.Any(q=>q.Glue>.2f))){p.GlueHead=false;continue;}
            if(!p.AllowDrag||p.GlueHead||State.Phase!=Phase.Build||State.Time<p.GlueImmune)continue;
            var t=State.Tools.FirstOrDefault(t=>t.Id==p.Held&&t.Holder==p.Id&&t.HitHair&&State.Time-t.LastUse<.15f);
            if(t==null||!State.Heads.Any(h=>h.Id==t.ContactHead&&(h.Id==0||h.ParentHead==0||h.AttachedTo==0))||Vector3.Distance(t.Contact,State.SharedHead.Position)>1.6f||Vector3.Distance(PartyBodies.Hand(p),t.Contact)>1.6f||!State.SharedHead.Patches.Any(q=>q.Glue>.6f))continue;
            p.GlueHead=true;p.GlueOffset=State.SharedHead.ToLocal(Eye(p));p.GlueUntil=State.Time+3;p.GlueImmune=p.GlueUntil+3;Event(p.Id,0,t.Contact,"Hand bonded to glued customer hair",1);
        }
    }
}
