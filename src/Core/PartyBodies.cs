using System;
using System.Linq;
using System.Numerics;
namespace Hairball.Core;
public static class PartyBodies
{
    public const float DownSeconds=4,Immunity=3,CatchRadius=.6f;
    public static bool Empty(PlayerState p)=>p.Held<0&&p.CarriedProp<0&&p.CarriedHair<0&&p.CarriedModel<0&&p.CarryLadder<0;
    public static Vector3 Hand(PlayerState p)=>p.Position+new Vector3(0,1.25f,0)+Session.Aim(p)*.3f;
    public static float ThrowSpeed(float charge)=>4+5*Math.Clamp((charge-.2f)/.8f,0,1);
    public static Vector3 HairPosition(PlayerState p,float time)=>time<p.DownUntil?p.Position+new Vector3(0,.15f,.6f)+Vector3.Transform(new Vector3(0,1.7f,0),Quaternion.CreateFromYawPitchRoll(p.Yaw+MathF.PI,MathF.PI/2,0)):Session.Eye(p);
    public static Vector3 HairRotation(PlayerState p,float time)=>new(time<p.DownUntil?MathF.PI/2:0,p.Yaw+MathF.PI,0);
}
public sealed partial class Session
{
    public bool Down(PlayerState p,int source)
    {
        if(!State.Experiment.IsB||p.Customer||!p.AllowDown||State.Time<p.DownImmune||State.Time<p.DownUntil||State.Phase!=Phase.Build)return false;
        p.GlueUntil=0;Drop(p);SetBrace(p,false);p.SupportLadder=-1;p.DownUntil=State.Time+4;p.DownImmune=p.DownUntil+3;p.Impulse=default;RecordAction(PartyAction.Down,source,p.Id);return true;
    }
    public bool SlapRevive(PlayerState actor)
    {
        var p=State.Players.FirstOrDefault(p=>p.Active&&State.Time<p.DownUntil&&p.Id!=actor.Id&&LookingAt(actor,p.Position+Vector3.UnitY*.8f,.6f,2.5f));
        if(p==null)return false;p.DownUntil=p.FreezeUntil=0;p.DownImmune=p.FreezeImmune=State.Time+3;RecordAction(PartyAction.Revive,actor.Id,p.Id);return true;
    }
    public bool RotateChair(PlayerState actor,float seconds)
    {
        if(State.Phase!=Phase.Build||State.Experiment.Leave!=LeaveStage.Seated||!LookingAt(actor,ChairControl(0),.5f,3)||State.Time<actor.DownUntil)return false;
        var c=State.Customers[0];c.ChairYaw=MathF.IEEERemainder(c.ChairYaw+Math.Clamp(seconds,-.1f,.1f)*MathF.PI/4,MathF.Tau);CustomerMotion.Apply(State);SyncFaces();return true;
    }
    public bool HandleHair(PlayerState p)
    {
        if(p.Id==State.Twist.FamilyActor)return false;
        if(!State.Experiment.IsB||State.Phase!=Phase.Build)return false;
        if(p.Customer&&p.CarriedHair>=0)return true;
        if(p.CarriedHair>=0){var wig=State.Heads.FirstOrDefault(h=>h.Id==p.CarriedHair);if(wig==null){p.CarriedHair=-1;return true;}
            var target=LookingAt(p,State.SharedHead.Position+Vector3.UnitY*.4f,.85f,2.8f)?State.SharedHead:State.Barber(p.Slot);
            wig.Holder=0;p.CarriedHair=-1;wig.AttachedTo=target.Id;wig.AttachedOffset=new(0,.4f,0);wig.AttachedRotation=default;wig.Velocity=default;RecordAction(PartyAction.Wig,p.Id,wig.Id);return true;}
        var hair=State.Heads.Where(h=>h.Loose&&!h.Miniature&&!h.Fragment&&h.AttachedTo<0&&h.Holder==0&&!h.Locked&&LookingAt(p,h.Position+Vector3.UnitY*.25f,.7f,3)).OrderBy(h=>Vector3.DistanceSquared(Eye(p),h.Position)).FirstOrDefault();
        if(hair==null)return false;Drop(p);hair.Holder=p.Id;hair.Velocity=default;p.CarriedHair=hair.Id;return true;
    }
    public void ReleaseHair(PlayerState p){var h=State.Heads.FirstOrDefault(h=>h.Id==p.CarriedHair&&h.Holder==p.Id);p.CarriedHair=-1;if(h!=null){h.Holder=0;h.Position=Eye(p)+Aim(p)*.7f;h.Velocity=Aim(p);}}
    public bool Throw(PlayerState p,float charge)
    {
        if(Sticky(p)||State.Time<p.DownUntil||State.Phase!=Phase.Build)return false;
        var velocity=Aim(p)*PartyBodies.ThrowSpeed(charge)+Vector3.UnitY*1.5f;
        var prop=State.Props.FirstOrDefault(x=>x.Id==p.CarriedProp);
        var hair=State.Heads.FirstOrDefault(h=>h.Id==p.CarriedHair||h.Id==p.CarriedModel);
        var tool=State.Tools.FirstOrDefault(t=>t.Id==p.Held&&t.Holder==p.Id);
        if(prop?.Coop==CoopKind.LargePad&&prop.Holder2!=0)return false;
        if(prop!=null){prop.Holder=0;prop.Attached=prop.Pinned=false;prop.Released=true;prop.Position=Eye(p)+Aim(p)*.65f;prop.Velocity=velocity;prop.Thrower=p.Id;prop.ThrownAt=State.Time;p.CarriedProp=-1;}
        else if(hair!=null){hair.Holder=0;hair.AttachedTo=-1;hair.Position=Eye(p)+Aim(p)*.7f;hair.Velocity=velocity;hair.Thrower=p.Id;hair.ThrownAt=State.Time;p.CarriedHair=p.CarriedModel=-1;}
        else if(tool!=null){tool.Holder=0;tool.Position=Eye(p)+Aim(p)*.65f;tool.Velocity=velocity;tool.Thrower=p.Id;tool.ThrownAt=State.Time;p.Held=-1;EndStroke(p.Id);}
        else return false;
        RecordAction(PartyAction.Throw,p.Id,prop?.Id??hair?.Id??tool!.Id);return true;
    }
    // Swept segments prevent tunnelling during fast throws or slow frames.
    bool FlightContact(Vector3 start,Vector3 end,int source,float thrownAt,bool heavy,Action<PlayerState> catchObject,Action stop)
    {
        if(!State.Experiment.IsB||State.Phase!=Phase.Build||(end-start).LengthSquared()<.00001f)return false;
        foreach(var p in State.Players.Where(p=>p.Active&&!p.NetworkAway&&(p.Id!=source||State.Time-thrownAt>.35f)).OrderBy(p=>Vector3.DistanceSquared(start,p.Position)))
        {
            if(p.Id!=source&&State.Time>=p.DownUntil&&PartyBodies.Empty(p)&&HairSystem.DistanceToSegment(PartyBodies.Hand(p),start,end)<PartyBodies.CatchRadius){catchObject(p);RecordAction(PartyAction.Catch,p.Id,source);return true;}
            if(HairSystem.DistanceToSegment(p.Position+Vector3.UnitY*1.65f,start,end)<.32f){p.HitUntil=State.Time+.5f;if(heavy||State.Time<p.FreezeUntil)Down(p,source);stop();return true;}
        }
        if(source!=0&&HairSystem.DistanceToSegment(State.SharedHead.Position,start,end)<.48f){Stimulate(.2f,source,CustomerStimulus.Impact);State.Job.Penalty+=2;stop();return true;}
        return false;
    }
    bool PropFlight(PropState prop,Vector3 start,Vector3 end)=>FlightContact(start,end,prop.Thrower,prop.ThrownAt,prop.Goal==0||prop.Coop==CoopKind.LargePad,
        p=>{prop.Holder=p.Id;prop.Velocity=default;prop.Released=false;p.CarriedProp=prop.Id;},()=>{prop.Velocity=default;prop.Thrower=0;});
    public bool SupportLadder(PlayerState p)
    {
        var ladder=State.Ladders.FirstOrDefault(l=>l.Holder==0&&l.Supporter==0&&LookingAt(p,l.Position+Vector3.UnitY*.7f,.7f,2)&&State.Players.Any(q=>q.Id!=p.Id&&OnLadder(q,l)));
        if(ladder==null)return false;ladder.Supporter=p.Id;p.SupportLadder=ladder.Id;return true;
    }
    static bool OnLadder(PlayerState p,LadderState l){var d=Rotate(p.Position-l.Position,-l.Yaw);return p.Position.Y>l.Position.Y+.25f&&Math.Abs(d.X)<.7f&&Math.Abs(d.Z)<1.05f;}
    public static Vector3 ToolEye(WorldState w,PlayerState p)
    {
        var ladder=w.Ladders.FirstOrDefault(l=>l.Holder==0&&l.Supporter==0&&OnLadder(p,l));
        return Eye(p)+(ladder==null?Vector3.Zero:new Vector3(MathF.Sin(w.Time*14+p.Id)*.055f,MathF.Sin(w.Time*19+p.Id*3)*.035f,MathF.Cos(w.Time*17+p.Id)*.04f));
    }
}
