using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
namespace Hairball.Core;

// Public role/motion facts contain no card or target attribute.
public static class HumanCustomer
{
    public static bool Self(WorldState w,PlayerState p,Head h)=>p.Customer&&(h.Id==0||h.ParentHead==0||h.AttachedTo==0);
    public static float SeatYaw(WorldState w)=>w.Customers[0].ChairYaw+MathF.PI;
    public static (float Yaw,float Pitch) Eyes(WorldState w,float yaw,float pitch)=>((w.Players.FirstOrDefault(p=>p.Customer)?.EyeBaseYaw??SeatYaw(w))+Math.Clamp(MathF.IEEERemainder(yaw-(w.Players.FirstOrDefault(p=>p.Customer)?.EyeBaseYaw??SeatYaw(w)),MathF.Tau),-MathF.PI/3,MathF.PI/3),Math.Clamp(pitch,-MathF.PI*2/9,MathF.PI*2/9));
    public static Vector3 Expression(WorldState w)
    {
        var p=w.Players.FirstOrDefault(p=>p.Active&&p.Customer);if(p==null)return default;
        float age=w.Time-p.ExpressionAt,duration=p.Expression==1?.6f:.8f;
        float wave=age>=0&&age<duration?MathF.Sin(age/duration*MathF.Tau):0;
        var rotation=p.Expression==1?new Vector3(MathF.PI/15*wave,0,0):new Vector3(0,MathF.PI/12*wave,0);
        if(p.HeadLook)rotation+=new Vector3(-p.Pitch,MathF.IEEERemainder(p.Yaw-SeatYaw(w),MathF.Tau),0);
        return rotation*(w.Players.Any(p=>p.Active&&p.Bracing)?.22f:1);
    }
}
public sealed partial class Session
{
    public bool ForceAI,ReturnCutEnabled=true;
    public int DebugCustomerActor;int customerCursor;
    readonly Dictionary<int,Queue<float>> standPresses=new();
    public TargetCard? OwnTarget(int actor) {var p=State.Player(actor);if(p==null)return null;if(State.Twist.Kind==TwistKind.Family&&actor==State.Twist.FamilyActor)return familyTarget;if(State.Twist.Kind is TwistKind.Reverse or TwistKind.SplitInfo)return !p.Customer?targetCard:null;return p.Customer?targetCard:null;}
    void SelectCustomer()
    {
        standPresses.Clear();var actors=State.Players.Where(p=>p.Active&&!p.NetworkAway).ToArray();
        foreach(var p in State.Players){p.Customer=false;p.Standing=false;p.Expression=0;p.ExpressionAt=-100;p.HeadLook=false;p.DisabledUntil=0;p.Position=Spawn(p.Slot);p.Yaw=-p.Slot*MathF.PI/2;}
        if(!State.Experiment.IsB||ForceAI||actors.Length<2)return;
        var customer=actors.FirstOrDefault(p=>p.Id==DebugCustomerActor)??actors[customerCursor++%actors.Length];
        customer.Customer=true;customer.Position=WorkCenter;customer.Yaw=State.Customers[0].ChairYaw+MathF.PI;customer.EyeBaseYaw=customer.Yaw;customer.Pitch=0;
        State.Experiment.CustomerActor=customer.Id;
    }
    public void CustomerDisconnected(int actor)
    {
        if(State.Player(actor) is not {Customer:true} p)return;p.Customer=false;p.Standing=false;p.HeadLook=false;
        State.Experiment.CustomerActor=0;State.Experiment.StandActive=false;standPresses.Clear();
        if(State.Experiment.LeaveStarted>=0){State.Experiment.LeaveStarted=State.Time;State.Experiment.Leave=LeaveStage.Rising;}
        CustomerMotion.Apply(State);SyncFaces();Event(actor,0,State.SharedHead.Position,"AI took over the customer",1);
    }
    bool CountStandPress(int actor)
    {
        if(!standPresses.TryGetValue(actor,out var q))standPresses[actor]=q=new();
        while(q.Count>0&&State.Time-q.Peek()>=1)q.Dequeue();if(q.Count>=12)return false;q.Enqueue(State.Time);return true;
    }
    void StartStanding(PlayerState p)
    {
        var e=State.Experiment;if(e.StandActive||p.Standing||State.Time<e.StandCooldown||State.Phase!=Phase.Build)return;
        e.StandActive=true;e.StandStarted=State.Time;e.StandProgress=.5f;standPresses.Clear();
        Event(p.Id,0,p.Position,"Customer wants to stand!",1);
    }
    bool CustomerInput(PlayerState p,Buttons buttons,Buttons old)
    {
        var e=State.Experiment;
        if(!p.Customer){if(e.StandActive&&buttons.HasFlag(Buttons.Interact)&&!old.HasFlag(Buttons.Interact)&&CanBrace(p)&&CountStandPress(p.Id)){
            int rank=State.Players.Where(q=>q.Active&&!q.Customer&&CanBrace(q)&&standPresses.TryGetValue(q.Id,out var presses)&&presses.Any(at=>State.Time-at<1)).OrderBy(q=>standPresses[q.Id].First()).ToList().FindIndex(q=>q.Id==p.Id);
            e.StandProgress-=.12f*(rank==0?1:rank==1?.6f:.4f);return true;}return false;}
        if(!p.Standing){var eyes=HumanCustomer.Eyes(State,p.Yaw,p.Pitch);p.Yaw=eyes.Yaw;p.Pitch=eyes.Pitch;p.Position=WorkCenter;}
        p.HeadLook=!p.Standing&&State.Twist.Kind!=TwistKind.PropsOnly&&buttons.HasFlag(Buttons.ChairUp);
        bool primary=buttons.HasFlag(Buttons.Primary)&&!old.HasFlag(Buttons.Primary),secondary=buttons.HasFlag(Buttons.Secondary)&&!old.HasFlag(Buttons.Secondary);
        bool drawing=FogInput(p,buttons);
        if(!drawing&&TwistCards.AllowExpression(State,p)&&p.Held<0&&(primary||secondary)&&State.Time-p.ExpressionAt>=(p.Expression==1?.6f:.8f)){p.Expression=primary?1:2;p.ExpressionAt=State.Time;}
        if(buttons.HasFlag(Buttons.Stand)&&!old.HasFlag(Buttons.Stand)&&!p.Standing){StartStanding(p);if(e.StandActive&&CountStandPress(p.Id))e.StandProgress+=.12f;}
        if(buttons.HasFlag(Buttons.Interact)&&!old.HasFlag(Buttons.Interact)){
            if(p.Standing&&e.LeaveStarted<0&&Vector2.Distance(new(p.Position.X,p.Position.Z),Vector2.Zero)<1.5f){p.Standing=false;p.Position=WorkCenter;p.EyeBaseYaw=p.Yaw;return true;}
            if(!p.Standing&&State.Players.FirstOrDefault(q=>q.Active&&!q.Customer&&LookingAt(p,PartyBodies.Hand(q),.6f,2.5f)) is {} barber){barber.DisabledUntil=State.Time+.6f;barber.HitUntil=State.Time+.6f;EndStroke(barber.Id);Event(p.Id,0,barber.Position,"Customer slapped a tool away",1);return true;}
        }
        return false;
    }
    void TickHumanCustomer()
    {
        var e=State.Experiment;var p=State.Player(e.CustomerActor);
        if(p==null||p.NetworkAway){if(e.CustomerActor!=0)CustomerDisconnected(e.CustomerActor);return;}
        if(e.StandActive){
            bool win=e.StandProgress>=1,lose=e.StandProgress<=0||State.Time-e.StandStarted>=3;
            if(win||lose){e.StandActive=false;e.StandProgress=Math.Clamp(e.StandProgress,0,1);p.Standing=win;if(win)p.StandAt=State.Time;e.StandCooldown=State.Time+5;
                if(lose&&e.LeaveStarted>=0){float delay=Math.Min(3,6-e.DelayUsed);e.DelayUsed+=delay;e.StandCooldown=State.Time+delay;}
                Event(p.Id,0,p.Position,win?"Customer stood up":"Barbers kept the customer seated",1);}
        }
        if(e.LeaveStarted>=0&&!p.Standing&&!e.StandActive&&State.Time>=e.StandCooldown&&e.DelayUsed<6)StartStanding(p);
        // Once six seconds of delaying are consumed the customer may walk; the camera is never moved.
        if(e.LeaveStarted>=0&&!p.Standing&&(e.DelayUsed>=6||State.Time-e.LeaveStarted>=6)){p.Standing=true;p.StandAt=State.Time;e.StandActive=false;}
        if(!p.Standing){p.Position=WorkCenter;var mold=State.Props.FirstOrDefault(x=>x.Id==p.CarriedProp&&x.Mold!=MoldKind.None);if(mold!=null){mold.Position=State.SharedHead.Position+Vector3.UnitY*.65f;mold.Rotation=State.SharedHead.Rotation;}
            if(State.Heads.FirstOrDefault(h=>h.Id==p.CarriedHair&&h.Holder==p.Id) is {} wig)wig.Position=State.SharedHead.Position+Vector3.UnitY*.65f;}
        CustomerMotion.Apply(State);SyncFaces();
    }
}
