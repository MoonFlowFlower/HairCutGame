using System;
using System.Linq;
using System.Numerics;
namespace Hairball.Core;
public sealed class PartyProjectile
{
    public int Source,Kind;public Vector3 Position,Velocity;public float Started;
}
public sealed partial class Session
{
    public static float PieStart(WorldState w)=>w.Experiment.ResultStarted+(w.Experiment.CustomerActor!=0&&w.Experiment.ReturnCut?5:0);
    public static bool PieTime(WorldState w)=>w.Phase==Phase.Results&&w.Time>=PieStart(w)&&w.Time<PieStart(w)+8;
    void RevealInput(PlayerState p,Buttons input,Buttons old,float dt)
    {
        if(State.Phase!=Phase.Results)return;var e=State.Experiment;
        if(p.Customer){p.Standing=true;p.HeadLook=false;}
        bool primary=input.HasFlag(Buttons.Primary)&&!old.HasFlag(Buttons.Primary),secondary=input.HasFlag(Buttons.Secondary)&&!old.HasFlag(Buttons.Secondary),interact=input.HasFlag(Buttons.Interact)&&!old.HasFlag(Buttons.Interact);
        if(p.Customer&&e.ReturnCut&&State.Time<e.ResultStarted+5){
            if(input.HasFlag(Buttons.Primary)){
                var hits=ToolQuery.Find(State,p,0,false,Aim(p),2.5f);foreach(var hit in hits){var h=State.Heads.First(h=>h.Id==hit.HeadId);if(!h.Barber&&!State.Heads.Any(b=>b.Barber&&b.Id==h.AttachedTo))continue;
                    float mass=h.Mass;h.Volume.Brush(new(EffectKind.RemoveHair,dt*3,Aim(p)),hit.Point,.21f);float removed=Math.Max(0,mass-h.Mass);
                    if(removed>0)DebrisSystem.Emit(State.Debris,h.ToWorld(hit.Point),Aim(p)*.2f,removed,DebrisMaterial.From(h,hit.Point));}
            }return;
        }
        if(!PieTime(State))return;
        if(primary&&!p.PieUsed){p.PieUsed=true;State.Projectiles.Add(new(){Source=p.Id,Kind=0,Position=Eye(p)+Aim(p)*.5f,Velocity=Aim(p)*7+Vector3.UnitY,Started=State.Time});}
        if(p.Customer&&secondary&&!p.PoopUsed){p.PoopUsed=true;State.Projectiles.Add(new(){Source=p.Id,Kind=1,Position=Eye(p)+Aim(p)*.5f,Velocity=Aim(p)*7+Vector3.UnitY,Started=State.Time});}
        if(p.Customer&&interact&&!p.LikeUsed&&State.Players.FirstOrDefault(q=>q.Active&&q.Id!=p.Id&&LookingAt(p,Eye(q),.7f,5)) is {} liked){p.LikeUsed=true;liked.LikedUntil=PieStart(State)+12;Event(p.Id,0,liked.Position,"Customer gave a thumbs-up",1);}
    }
    void TickReveal(float dt)
    {
        if(State.Phase!=Phase.Results)return;
        if(State.Time>=PieStart(State)&&State.Player(State.Experiment.CustomerActor) is {Held:1900} customer){customer.Held=-1;var gift=State.Tools.First(t=>t.Id==1900);gift.Holder=0;gift.Position=PartyLoop.Table;}
        foreach(var h in State.Heads.Where(h=>h.Barber||State.Heads.Any(b=>b.Barber&&b.Id==h.AttachedTo)))h.Locked=State.Time>=State.Experiment.ResultStarted+5||!State.Experiment.ReturnCut||State.Experiment.CustomerActor==0;
        foreach(var shot in State.Projectiles.ToArray()){
            var start=shot.Position;shot.Velocity-=Vector3.UnitY*dt*3;shot.Position+=shot.Velocity*dt;
            var hit=State.Players.FirstOrDefault(p=>p.Active&&p.Id!=shot.Source&&HairSystem.DistanceToSegment(Eye(p),start,shot.Position)<.42f);
            if(hit!=null){hit.CosmeticKind=shot.Kind;hit.CosmeticUntil=PieStart(State)+12;hit.Obscured=Math.Max(hit.Obscured,1.5f);Event(shot.Source,0,hit.Position,shot.Kind==0?"Cream pie hit":"Cartoon poop hit",1);State.Projectiles.Remove(shot);}
            else if(State.Time-shot.Started>3||shot.Position.Y<0)State.Projectiles.Remove(shot);
        }
    }
}
