using Godot;
using Hairball.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using V=System.Numerics.Vector3;
namespace Hairball;
// QA injects body positions and held tools. Tool buttons use real remote InputBatch.
public partial class Main
{
    int accidentStage;
    float accidentStageAt=-1;
    readonly HashSet<string> accidentObserved=new();
    static readonly int[] AccidentTools=[5,0,4,3,8,10];
    static readonly PartyAction[] AccidentActions=[PartyAction.FreezeFriend,PartyAction.ThawFriend,PartyAction.GlueFriend,PartyAction.ReleaseFriend,PartyAction.FireFriend,PartyAction.WaterFriend];
    void ObserveAccidents()
    {
        foreach(var p in world.Players){if(p.FreezeUntil>world.Time)accidentObserved.Add("freeze");if(p.GlueUntil>world.Time)accidentObserved.Add("glue");if(p.FireUntil>world.Time)accidentObserved.Add("fire");}
    }
    bool AccidentFixture(PlayerState actor)
    {
        if(!args.ContainsKey("party-accident-smoke")||!world.Props.Any(m=>m.Mold==MoldKind.Pad&&m.Holes.Count>0))return false;
        ObserveAccidents();
        if(accidentStageAt<0)accidentStageAt=world.Time;
        int victimId=world.Players.Single(p=>p.Active&&p.Slot==0).Id;
        if(accidentStage<6&&simulation.ActionLog.Any(a=>a.Kind==AccidentActions[accidentStage]&&a.Actor==actor.Id&&a.Target==victimId&&world.Time-a.Time>=1)){accidentStage++;accidentStageAt=world.Time;}
        if(accidentStage>=6){world.Notice="QA accidents complete";return false;}
        world.Notice="QA accident stage "+accidentStage;
        var victim=world.Players.Single(p=>p.Active&&p.Slot==0);victim.Position=new(3.8f,0,0);victim.Impulse=default;actor.Position=new(3.8f,0,1.8f);actor.Impulse=default;
        foreach(var p in new[]{actor,victim})if(salon!.Bodies.TryGetValue(p.Id,out var body)){body.Position=Art.V(p.Position);body.Velocity=Vector3.Zero;}
        int definition=AccidentTools[accidentStage];var tool=world.Tools.First(t=>t.Definition==definition);
        if(actor.Held!=tool.Id){simulation.Drop(actor);tool.Holder=actor.Id;actor.Held=tool.Id;actor.Cooldown=0;simulation.EndStroke(actor.Id);}
        return true;
    }
    bool AccidentInput(PlayerState p,ref Vector2 movement,ref Buttons buttons)
    {
        if(!args.ContainsKey("party-accident-smoke")||p.Slot!=1||!world.Notice.StartsWith("QA accident stage "))return false;
        if(!int.TryParse(world.Notice[18..],out int stage)||stage<0||stage>=6)return false;
        movement=Vector2.Zero;p.Position=new(3.8f,0,1.8f);if(salon!.Bodies.TryGetValue(p.Id,out var body)){body.Position=Art.V(p.Position);body.Velocity=Vector3.Zero;}
        var point=new V(3.8f,1.35f,0);var d=V.Normalize(point-Session.Eye(p));yaw=MathF.Atan2(-d.X,-d.Z);pitch=MathF.Asin(d.Y);
        buttons=elapsed%1.2f<.45f?(stage==3?Buttons.Secondary:Buttons.Primary):Buttons.None;return true;
    }
    bool AccidentSmokeGood()=>!args.ContainsKey("party-accident-smoke")||accidentObserved.Count==3&&(!authority||accidentStage==6);
}
