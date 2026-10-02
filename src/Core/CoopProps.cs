using System;
using System.Linq;
using System.Numerics;
namespace Hairball.Core;
public enum CoopKind {None,Nozzle,Tank,ScissorA,ScissorB,LargePad}
public sealed partial class Session
{
    float lastCoopCut=-100;
    public static int BarberCount(WorldState w){return w.Players.Count(p=>p.Active&&!p.NetworkAway&&!p.Customer);}
    void StageCoop()
    {
        if(!State.Experiment.IsB||BarberCount(State)<2||State.Props.Any(p=>p.Coop!=CoopKind.None))return;
        for(int i=1;i<=5;i++)State.Props.Add(new(){Id=State.Round*1000+800+i,Goal=-20-i,Coop=(CoopKind)i,Position=new(-4.7f+(i-1)*.72f,.9f,-3.25f),MaterialScale=i==5?2:1,Mold=i==5?MoldKind.Pad:MoldKind.None});
        lastCoopCut=-100;
    }
    public bool HandleCoop(PlayerState p)
    {
        if(State.Phase!=Phase.Build)return false;
        if(State.Props.FirstOrDefault(x=>x.Id==p.CarriedProp) is {Coop:CoopKind.LargePad} held){ReleaseLarge(p,held);return true;}
        var large=State.Props.FirstOrDefault(x=>x.Coop==CoopKind.LargePad&&x.Holder2==0&&x.Holder!=p.Id&&LookingAt(p,x.Position,.8f,2.5f));
        if(large==null)return false;Drop(p);if(large.Holder==0)large.Holder=p.Id;else large.Holder2=p.Id;large.Attached=false;p.CarriedProp=large.Id;return true;
    }
    void ReleaseLarge(PlayerState p,PropState prop){if(prop.Holder==p.Id){prop.Holder=prop.Holder2;prop.Holder2=0;}else if(prop.Holder2==p.Id)prop.Holder2=0;p.CarriedProp=-1;if(prop.Holder==0){prop.Released=true;prop.Velocity=default;}}
    public static bool ScissorSync(float a,float b)=>Math.Abs(a-b)<=.35f;
    void TickCoop(float dt)
    {
        if(!State.Experiment.IsB)return;
        if(State.Phase!=Phase.Build)return;
        if(BarberCount(State)<2){foreach(var prop in State.Props.Where(p=>p.Coop!=CoopKind.None).ToArray()){foreach(var holder in State.Players.Where(p=>p.CarriedProp==prop.Id))holder.CarriedProp=-1;State.Props.Remove(prop);}return;}
        if(BarberCount(State)>=2)StageCoop();
        foreach(var l in State.Ladders.Where(l=>l.Supporter!=0))if(State.Player(l.Supporter) is not {} support||!Inputs.TryGetValue(support.Id,out var input)||!input.Buttons.HasFlag(Buttons.Interact)||Vector3.Distance(support.Position,l.Position)>1.6f||State.Time<support.DownUntil){if(State.Player(l.Supporter) is {} prior)prior.SupportLadder=-1;l.Supporter=0;}
        foreach(var large in State.Props.Where(x=>x.Coop==CoopKind.LargePad&&x.Holder!=0)){
            var a=State.Player(large.Holder);var b=State.Player(large.Holder2);if(a==null){large.Holder=large.Holder2;large.Holder2=0;continue;}
            var target=b!=null?(PartyBodies.Hand(a)+PartyBodies.Hand(b))*.5f:PartyBodies.Hand(a);if(b==null){target.Y=.16f;large.Position=Vector3.Lerp(large.Position,target,Math.Min(1,dt*.6f));}else {large.Position=target;large.Rotation=new(0,(a.Yaw+b.Yaw)*.5f,0);}large.Velocity=default;
            if(b!=null&&Vector3.Distance(a.Position,b.Position)>2.5f){ReleaseLarge(b,large);}
        }
        var nozzle=State.Props.FirstOrDefault(p=>p.Coop==CoopKind.Nozzle);var tank=State.Props.FirstOrDefault(p=>p.Coop==CoopKind.Tank);
        if(nozzle!=null&&tank!=null&&State.Player(nozzle.Holder) is {} aimer&&State.Player(tank.Holder) is {} pump&&aimer.Id!=pump.Id&&!aimer.Customer&&!pump.Customer&&aimer.Id!=State.Twist.FamilyActor&&pump.Id!=State.Twist.FamilyActor){
            if(Vector3.Distance(PartyBodies.Hand(aimer),PartyBodies.Hand(pump))>2.5f){ReleaseProp(aimer);ReleaseProp(pump);}
            else if(Inputs.TryGetValue(pump.Id,out var input)){
                if(input.Buttons.HasFlag(Buttons.Secondary)&&State.Time-pump.SecondaryAt<dt*1.1f)tank.CoopMode=(tank.CoopMode+1)%3;
                if(input.Buttons.HasFlag(Buttons.Primary)&&State.Time-nozzle.LastUse>=.15f&&PartyAccidents.Primary(pump,State.Time)&&PartyAccidents.Primary(aimer,State.Time)){
                    int definition=tank.CoopMode==0?4:tank.CoopMode==1?5:10;var origin=ToolEye(State,aimer);var direction=Aim(aimer);float range=ObstructionDistance?.Invoke(origin,direction,4)??4;
                    var hits=ToolQuery.EffectHits(State,aimer,definition,false,direction,range);nozzle.LastUse=State.Time;nozzle.Contact=origin+direction*range;nozzle.HitHair=hits.Count>0;
                    foreach(var hit in hits){nozzle.Contact=hit.head.ToWorld(hit.point);foreach(var effect in Tools.Get(definition).Effects)foreach(var patch in hit.head.Patches.Where(p=>HairSystem.DistanceToSegment(hit.point,p.Root,p.Root+p.Direction*p.Length)<.7f))HairSystem.Apply(hit.head,patch,effect with{Amount=effect.Amount*2.5f,Source=pump.Id,Point=nozzle.Contact,Direction=direction});FinalizeHair(hit.head,pump.Id);}
                    AffectPartyPlayers(aimer,new(){Definition=definition},false);RecordAction(PartyAction.CoopSpray,pump.Id,aimer.Id);
                }
            }
        }
        var left=State.Props.FirstOrDefault(p=>p.Coop==CoopKind.ScissorA);var right=State.Props.FirstOrDefault(p=>p.Coop==CoopKind.ScissorB);
        if(left!=null&&right!=null&&State.Player(left.Holder) is {} cutter&&State.Player(right.Holder) is {} partner&&cutter.Id!=partner.Id&&!cutter.Customer&&!partner.Customer&&cutter.Id!=State.Twist.FamilyActor&&partner.Id!=State.Twist.FamilyActor&&Inputs.TryGetValue(cutter.Id,out var one)&&Inputs.TryGetValue(partner.Id,out var two)&&one.Buttons.HasFlag(Buttons.Primary)&&two.Buttons.HasFlag(Buttons.Primary)&&PartyAccidents.Primary(cutter,State.Time)&&PartyAccidents.Primary(partner,State.Time)&&ScissorSync(cutter.PrimaryAt,partner.PrimaryAt)&&Math.Max(cutter.PrimaryAt,partner.PrimaryAt)>lastCoopCut){
            lastCoopCut=Math.Max(cutter.PrimaryAt,partner.PrimaryAt);var origin=ToolEye(State,cutter);var dir=Aim(cutter);float range=ObstructionDistance?.Invoke(origin,dir,3.5f)??3.5f;
            foreach(var hit in ToolQuery.EffectHits(State,cutter,9,false,dir,range)){var effect=new HairEffect(EffectKind.CutPlane,1,hit.head.LocalDirection(Vector3.UnitY),cutter.Id,hit.head.ToWorld(hit.point));float removed=-hit.head.Volume.Brush(effect,hit.point,.7f*MathF.Sqrt(1.6f)/hit.head.GeometryScale,hit.head.ToLocal(origin),range);foreach(var patch in hit.head.Patches)HairSystem.Apply(hit.head,patch,effect,false);FinalizeHair(hit.head,cutter.Id);if(removed>0){cutter.Removed+=removed;DebrisSystem.Emit(State.Debris,effect.Point,Vector3.UnitY*.1f,removed,DebrisMaterial.From(hit.head,hit.point));}left.Contact=right.Contact=effect.Point;}
            left.LastUse=right.LastUse=State.Time;RecordAction(PartyAction.CoopCut,cutter.Id,partner.Id);
        }
    }
}
