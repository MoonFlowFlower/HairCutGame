using System;
using System.Linq;
using System.Numerics;
namespace Hairball.Core;

public static class PartyAccidents
{
    public const float FreezeDuration=2,GlueDuration=3,FireDuration=4,Immunity=3;
    public static Vector2 Movement(PlayerState p,Vector2 move,float time)=>time<p.FreezeUntil||time<p.DownUntil?Vector2.Zero:move*(time<p.GlueUntil?.4f:1);
    public static bool Jump(PlayerState p,float time)=>time>=p.FreezeUntil&&time>=p.DownUntil;
    public static bool Primary(PlayerState p,float time)=>time>=p.FreezeUntil&&time>=p.DownUntil&&time>=p.DisabledUntil;
}
public sealed partial class Session
{
    public bool Sticky(PlayerState p)=>State.Experiment.IsB&&State.Time<p.GlueUntil;
    void ResetAccidents(PlayerState p){p.FreezeUntil=p.GlueUntil=p.FireUntil=p.FreezeImmune=p.GlueImmune=p.FireImmune=p.DownUntil=p.DownImmune=p.HitUntil=p.ThrowCharge=0;p.PrimaryAt=p.SecondaryAt=-100;p.GlueHead=false;p.GlueOffset=default;p.CarriedHair=p.SupportLadder=-1;}
    void TickAccidents()
    {
        if(!State.Experiment.IsB)return;
        foreach(var p in State.Players.Where(p=>p.Active&&!p.Customer)){
            if(p.DownUntil>0&&State.Time>=p.DownUntil)p.DownUntil=0;
            if(State.Phase==Phase.Build&&Inputs.TryGetValue(p.Id,out var running)&&running.Move.Length()>.8f&&State.Debris.Piles.Any(d=>d.Mass>=.12f&&Math.Abs(d.Position.Y-p.Position.Y)<.15f&&Vector2.Distance(new(d.Position.X,d.Position.Z),new(p.Position.X,p.Position.Z))<.28f))Down(p,0);
            if(p.FreezeImmune>0&&State.Time>=p.FreezeUntil)foreach(var q in State.Barber(p.Slot).Patches)q.Temperature=Math.Max(20,q.Temperature);
            if(p.FireUntil<=0&&State.Time>=p.FireImmune&&State.Barber(p.Slot).Patches.Any(q=>q.Burning))StartFriendFire(p,0);
            if(p.FireImmune>0&&State.Time>=p.FireUntil){p.FireUntil=0;foreach(var h in State.Heads.Where(h=>h.Barber&&h.Owner==p.Id))foreach(var q in h.Patches){q.Burning=false;q.Temperature=Math.Min(20,q.Temperature);}}
        }
    }
    void StartFriendFire(PlayerState p,int actor)
    {
        if(State.Time<p.FireImmune)return;p.FireUntil=State.Time+PartyAccidents.FireDuration;p.FireImmune=p.FireUntil+PartyAccidents.Immunity;RecordAction(PartyAction.FireFriend,actor,p.Id);
    }
    void AffectPartyPlayers(PlayerState source,ToolState tool,bool secondary)
    {
        if(!State.Experiment.IsB||tool.Special>0)return;int id=tool.Definition;
        if(secondary&&id!=3)return;
        var origin=ToolEye(State,source);var dir=Aim(source);float range=Math.Min(4,ObstructionDistance?.Invoke(origin,dir,Tools.Get(id).Range)??Tools.Get(id).Range);
        foreach(var p in State.Players.Where(p=>p.Active&&!p.Customer&&!p.NetworkAway&&p.Id!=source.Id)){
            bool contact=Enumerable.Range(0,5).Any(i=>{var point=p.Position+Vector3.UnitY*(.5f+i*.35f);float along=Vector3.Dot(point-origin,dir);return along>0&&along<range&&HairSystem.DistanceToSegment(point,origin,origin+dir*range)<.4f;});
            if(!contact)continue;
            if(State.Time<p.FreezeUntil&&(id is 6 or 7 or 9||id==3&&!secondary))Down(p,source.Id);
            if(State.Time<p.FreezeUntil&&id is 0 or 6 or 9){p.FreezeUntil=0;RecordAction(PartyAction.ThawFriend,source.Id,p.Id);}
            if(State.Time<p.GlueUntil&&(id==8||id==3&&secondary)){p.GlueUntil=0;RecordAction(PartyAction.ReleaseFriend,source.Id,p.Id);}
            if(State.Time<p.FireUntil&&id==10){p.FireUntil=0;foreach(var q in State.Barber(p.Slot).Patches){q.Burning=false;q.Temperature=20;q.Wet=Math.Max(q.Wet,.3f);}RecordAction(PartyAction.WaterFriend,source.Id,p.Id);}
            if(id==5&&State.Time>=p.FreezeImmune){p.FreezeUntil=State.Time+PartyAccidents.FreezeDuration;p.FreezeImmune=p.FreezeUntil+PartyAccidents.Immunity;EndStroke(p.Id);RecordAction(PartyAction.FreezeFriend,source.Id,p.Id);}
            if(id==4&&State.Time>=p.GlueImmune){p.GlueUntil=State.Time+PartyAccidents.GlueDuration;p.GlueImmune=p.GlueUntil+PartyAccidents.Immunity;RecordAction(PartyAction.GlueFriend,source.Id,p.Id);}
            if(id==8&&State.Time>=p.FireImmune&&State.Barber(p.Slot).Mass>.001f){StartFriendFire(p,source.Id);foreach(var q in State.Barber(p.Slot).Patches.Where(q=>q.Length>.02f).Take(4)){q.Burning=true;q.Temperature=100;}}
        }
    }
}
