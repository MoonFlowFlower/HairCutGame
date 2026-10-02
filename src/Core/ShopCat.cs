using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
namespace Hairball.Core;
public enum CatMode {Absent,Idle,Telegraph,Sleeping,Carrying,Fleeing,Held}
public enum CatAction {Sleep,Steal,Swat}
public sealed class CatState
{
    public CatMode Mode;
    public CatAction Action;
    public int PropId=-1,StolenProp=-1,TargetId=-1,Actions;
    public float CueAt,NextAt,LuredUntil,HairMass;
    public DebrisMaterial Material=new();
    public Vector3 Destination;
    public CatState Clone()=>new(){Mode=Mode,Action=Action,PropId=PropId,StolenProp=StolenProp,TargetId=TargetId,Actions=Actions,CueAt=CueAt,NextAt=NextAt,LuredUntil=LuredUntil,HairMass=HairMass,Material=Material.Clone(),Destination=Destination};
}
public static class ShopCat
{
    public const float Chance=.35f,Warning=1.5f,Mass=2.5f;
    public static bool Appears(int seed)=>new Random(seed).NextDouble()<Chance;
    public static PropState? Prop(WorldState w)=>w.Props.FirstOrDefault(p=>p.Id==w.Cat.PropId);
    public static float Weight(WorldState w)=>w.Cat.Mode==CatMode.Sleeping?Mass:0;
}
public sealed partial class Session
{
    public bool CatEnabled=true;
    public Func<int,bool>? CatPicker;
    void StageCat()
    {
        State.Cat=new();if(!State.Experiment.IsB||!CatEnabled||!(CatPicker?.Invoke(State.Round)??ShopCat.Appears(RandomNumberGenerator.GetInt32(int.MaxValue))))return;
        State.Cat=new(){Mode=CatMode.Idle,PropId=State.Round*1000+980,NextAt=State.Time+12};
        State.Props.Add(new(){Id=State.Cat.PropId,Goal=-90,Position=new(-4,.22f,2)});
    }
    void CatReturn(PropState cat)
    {
        var c=State.Cat;if(c.HairMass>0){DebrisSystem.Emit(State.Debris,cat.Position+Vector3.UnitY*.2f,Vector3.Zero,c.HairMass,c.Material);c.HairMass=0;}
        if(State.Props.FirstOrDefault(p=>p.Id==c.StolenProp) is {} stolen){stolen.Holder=0;stolen.Released=true;stolen.Position=cat.Position+Vector3.UnitY*.3f;stolen.Velocity=default;}c.StolenProp=-1;
    }
    public bool HandleCat(PlayerState p)
    {
        var cat=ShopCat.Prop(State);if(cat==null||State.Phase!=Phase.Build)return false;
        if(p.CarriedProp==cat.Id){p.CarriedProp=-1;cat.Holder=0;cat.Position=Eye(p)+Aim(p)*.7f;cat.Position.Y=.22f;cat.Attached=false;State.Cat.Mode=CatMode.Fleeing;State.Cat.NextAt=State.Time+5;return true;}
        if(cat.Holder!=0||!LookingAt(p,cat.Position+Vector3.UnitY*.1f,.45f,2.5f))return false;
        Drop(p);CatReturn(cat);cat.Holder=p.Id;cat.Attached=false;p.CarriedProp=cat.Id;State.Cat.Mode=CatMode.Held;Event(p.Id,0,cat.Position,"Picked up the shop cat",1);return true;
    }
    void CatTool(PlayerState p,ToolState tool)
    {
        if(tool.Definition!=3||ShopCat.Prop(State) is not {} cat||!LookingAt(p,cat.Position+Vector3.UnitY*.1f,.65f,4))return;
        CatReturn(cat);cat.Attached=false;cat.Holder=0;foreach(var actor in State.Players.Where(p=>p.CarriedProp==cat.Id))actor.CarriedProp=-1;
        State.Cat.Mode=CatMode.Fleeing;State.Cat.Destination=new(-4,.22f,3);State.Cat.NextAt=State.Time+5;Event(p.Id,0,cat.Position,"Blower shooed the cat",1);
    }
    public bool BeginCatAction(CatAction action,int target=-1)
    {
        if(ShopCat.Prop(State) is not {} cat||cat.Holder!=0||State.Cat.Actions>=2||State.Phase!=Phase.Build)return false;
        State.Cat.Action=action;State.Cat.TargetId=target;State.Cat.Mode=CatMode.Telegraph;State.Cat.CueAt=State.Time;var targetPoint=State.Props.FirstOrDefault(p=>p.Id==target)?.Position??State.Debris.Piles.FirstOrDefault(p=>p.Id==target)?.Position??State.SharedHead.Position;var delta=targetPoint-cat.Position;cat.Rotation=new(0,MathF.Atan2(delta.X,delta.Z),0);return true;
    }
    void TickCat(float dt)
    {
        var c=State.Cat;var cat=ShopCat.Prop(State);if(cat==null&&c.Mode!=CatMode.Absent){cat=new(){Id=c.PropId,Goal=-90,Position=new(-4,.22f,2)};State.Props.Add(cat);}if(cat==null)return;
        if(!CatEnabled||State.Experiment.Resolved){CatReturn(cat);cat.Attached=false;cat.Holder=0;c.Mode=CatMode.Absent;return;}
        if(State.Phase!=Phase.Build)return;
        if(cat.Holder>0&&State.Player(cat.Holder) is {} carrier){cat.Position=Eye(carrier)+Aim(carrier)*.7f;cat.Rotation=new(0,carrier.Yaw,0);c.Mode=CatMode.Held;return;}
        if(c.Mode==CatMode.Held){cat.Holder=0;c.Mode=CatMode.Idle;c.NextAt=State.Time+8;cat.Position.Y=.22f;}
        var lure=State.Props.FirstOrDefault(p=>p.Gesture>=0&&p.Holder==0&&p.ThrownAt>0&&State.Time-p.ThrownAt<1&&Vector3.Distance(p.Position,cat.Position)<3);
        if(lure!=null){CatReturn(cat);c.Mode=CatMode.Fleeing;cat.Attached=false;c.Destination=new(lure.Position.X,.22f,lure.Position.Z);c.LuredUntil=State.Time+5;c.NextAt=State.Time+5;}
        if(c.Mode==CatMode.Fleeing){cat.Position=Vector3.Lerp(cat.Position,c.Destination,Math.Min(1,dt*2));cat.Position.Y=.22f;if(State.Time>=c.NextAt){c.Mode=CatMode.Idle;c.NextAt=State.Time+12;}return;}
        if(c.Mode==CatMode.Carrying){cat.Position=Vector3.Lerp(cat.Position,new(-4,.22f,3),Math.Min(1,dt*.8f));if(State.Props.FirstOrDefault(p=>p.Id==c.StolenProp) is {} stolen)stolen.Position=cat.Position+Vector3.UnitY*.25f;if(State.Time>=c.NextAt){CatReturn(cat);c.Mode=CatMode.Idle;c.NextAt=State.Time+12;}return;}
        if(c.Mode==CatMode.Sleeping){cat.Attached=true;cat.Position=State.SharedHead.ToWorld(cat.Local);cat.Rotation=State.SharedHead.Rotation;if(c.Actions<2&&State.Time>=c.NextAt)BeginCatAction(CatAction.Swat);return;}
        if(c.Mode==CatMode.Idle&&c.Actions<2&&State.Time>=c.NextAt&&State.Time>=c.LuredUntil){
            bool soft=State.SharedHead.Patches.Any(p=>p.Temperature>30||p.Resistance<.4f);
            var pile=State.Debris.Piles.OrderBy(p=>Vector3.DistanceSquared(p.Position,cat.Position)).FirstOrDefault();var prop=State.Props.FirstOrDefault(p=>p.Gesture>=0&&p.Holder==0);
            BeginCatAction(soft?CatAction.Sleep:pile!=null||prop!=null?CatAction.Steal:CatAction.Swat,pile?.Id??prop?.Id??-1);
        }
        if(c.Mode!=CatMode.Telegraph||State.Time-c.CueAt<ShopCat.Warning)return;
        c.Actions++;cat.Attached=false;
        if(c.Action==CatAction.Sleep){var volume=State.SharedHead.Volume;Vector3 surface;if(!volume.Raycast(new(.48f,4,0),-Vector3.UnitY,5,out surface,out _)&&!volume.Raycast(new(0,4,0),-Vector3.UnitY,5,out surface,out _))surface=new(0,.55f,0);cat.Local=surface+Vector3.UnitY*.11f;cat.Attached=true;cat.Position=State.SharedHead.ToWorld(cat.Local);c.Mode=CatMode.Sleeping;c.NextAt=State.Time+25;var commission=State.Props.FirstOrDefault(p=>p.Id==State.Experiment.HelicopterId&&p.Attached&&Vector3.Distance(p.Position,cat.Position)<.75f);if(commission!=null)CatSwat(commission);}
        else if(c.Action==CatAction.Steal){var pile=State.Debris.Piles.FirstOrDefault(p=>p.Id==c.TargetId);if(pile!=null){c.HairMass=Math.Min(.2f,pile.Mass);c.Material=pile.Material.Clone();pile.Mass-=c.HairMass;if(pile.Mass<=.000001f)State.Debris.Piles.Remove(pile);State.Debris.Revision++;}else if(State.Props.FirstOrDefault(p=>p.Gesture>=0&&p.Holder==0&&(p.Id==c.TargetId||c.TargetId<0)) is {} prop){prop.Holder=-1;c.StolenProp=prop.Id;}c.Mode=CatMode.Carrying;c.NextAt=State.Time+8;}
        else {if(State.Props.FirstOrDefault(p=>p.Id==State.Experiment.HelicopterId&&p.Attached) is {} commission)CatSwat(commission);c.Mode=CatMode.Fleeing;c.Destination=new(-4,.22f,3);c.NextAt=State.Time+5;}
        Event(0,0,cat.Position,"Shop cat: "+c.Action,1);
    }
    static void CatSwat(PropState prop){prop.Attached=false;prop.Released=true;prop.OnScalp=false;prop.Velocity=new(.65f,.4f,.5f);}
}
