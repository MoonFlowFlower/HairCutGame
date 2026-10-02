using System;
using System.Linq;
using System.Numerics;
namespace Hairball.Core;
public sealed class ShopOrder
{
    public int Actor,Option=-1,Serial,Delivered,Cancelled,Spent;
    public float CommitAt,ReadyAt;
    public bool Pending;
    public string Notice="";
}
public static class SpecialOrders
{
    public static readonly int[] Prices=[35,25];
    public static readonly string[] Names=["Growth Burst","Cryo Canister"];
    public static Vector3 Button(int option)=>new(3+(option-1)*.55f,1.3f,-4.85f);
    public static Vector3 Delivery=>new(3,1.05f,-4.25f);
    public static string ToolName(ToolState tool)=>tool.Special>0?Names[tool.Special-1]:Tools.Get(tool.Definition).Name;
}
public sealed partial class Session
{
    public bool RequestOrder(int actor,int option)
    {
        var p=State.Player(actor);var order=State.Order;
        if(State.Experiment.IsB)return false; // Party scope excludes spending/progression.
        if(p==null||option is <0 or >1||State.Phase!=Phase.Build||order.Pending||State.Time<order.ReadyAt||!LookingAt(p,SpecialOrders.Button(option),.22f,2.5f))return false;
        if(State.Job.Wallet<SpecialOrders.Prices[option]){order.Notice="Not enough shared money.";return false;}
        if(State.Tools.Count(t=>t.Special>0&&t.Charge>0)>=8){order.Notice="Use the delivered specials first.";return false;}
        order.Actor=actor;order.Option=option;order.Pending=true;order.CommitAt=State.Time+2.5f;order.Notice="Order pending — anyone may cancel.";return true;
    }
    public bool CancelOrder(int actor)
    {
        var p=State.Player(actor);if(p==null||State.Phase!=Phase.Build||!State.Order.Pending||!LookingAt(p,SpecialOrders.Button(2),.22f,2.5f))return false;
        State.Order.Pending=false;State.Order.Cancelled++;State.Order.ReadyAt=State.Time+.75f;State.Order.Notice="Order cancelled. No money spent.";return true;
    }
    void TickOrder(float dt)
    {
        var order=State.Order;if(!order.Pending)return;
        if(State.Phase!=Phase.Build||State.Remaining<=dt||State.Player(order.Actor)==null){order.Pending=false;order.Notice="Order cancelled. No money spent.";return;}
        if(State.Time<order.CommitAt)return;
        order.Pending=false;order.ReadyAt=State.Time+.75f;int price=SpecialOrders.Prices[order.Option];
        if(State.Job.Wallet<price){order.Notice="Not enough shared money.";return;}
        State.Job.Wallet-=price;order.Spent+=price;order.Delivered++;order.Serial++;
        State.Tools.RemoveAll(t=>t.Special>0&&t.Charge==0&&t.Holder==0);
        State.Tools.Add(new(){Id=4000+order.Serial,Definition=order.Option==0?1:5,Special=order.Option+1,Charge=1,Position=SpecialOrders.Delivery+new Vector3((order.Serial%3-1)*.22f,.1f,0),Velocity=new(0,.1f,.2f)});
        EnsureLedger();float q=LiveHealth().Value;
        Ledger.Observe(order.Actor,order.Option==0?1:5,"order",q,q,State.Time,State.Remaining,discrete:true,context:ImpactContext.Ordered,position:SpecialOrders.Delivery,money:-price);
        order.Notice="Special delivered — shared wallet charged.";
        Event(order.Actor,1,SpecialOrders.Delivery,$"{State.Player(order.Actor)!.Name}: ordered {SpecialOrders.Names[order.Option]} (-{price})",10);
    }
    void UseSpecial(PlayerState actor,ToolState tool,bool secondary)
    {
        if(secondary)return;
        var origin=Eye(actor);var direction=Aim(actor);float range=ObstructionDistance?.Invoke(origin,direction,4)??4;
        var center=origin+direction*Math.Min(range,3);float closest=range;
        foreach(var head in State.Heads.Where(h=>!h.Locked&&(!h.Barber||h.Owner!=actor.Id)))
            if(head.Raycast(origin,direction,closest,out var contact,out var distance)){closest=distance;center=head.ToWorld(contact);}
        tool.Charge=0;actor.Cooldown=.5f;tool.LastUse=State.Time;tool.Contact=center;tool.HitHair=true;int targets=0;
        var effect=tool.Special==1?new HairEffect(EffectKind.AddHair,.4f,Vector3.UnitY,actor.Id):new HairEffect(EffectKind.ChangeTemperature,-170,Source:actor.Id);
        foreach(var head in State.Heads.Where(h=>!h.Locked).ToArray())
        {
            float before=head.Mass;int changed=0;var local=head.ToLocal(center);
            foreach(var patch in head.Patches)
                if(Vector3.Distance(head.ToWorld(patch.Root+patch.Direction*patch.Length),center)<1.3f){HairSystem.Apply(head,patch,effect,false);changed++;}
            if(tool.Special==1)head.Volume.Brush(effect,local,.8f/head.GeometryScale);
            if(changed>0||head.Mass!=before){targets++;actor.Added+=Math.Max(0,head.Mass-before);FinalizeHair(head,actor.Id);}
        }
        foreach(var other in State.Players.Where(p=>p.Active&&Vector3.Distance(Eye(p),center)<1.4f))
        {var offset=Eye(other)-center;if(offset.LengthSquared()>.001f)PushPlayer(other,Vector3.Normalize(offset)*1.2f,actor.Id,false);}
        Stimulate(.22f,actor.Id,CustomerStimulus.Noise);Event(actor.Id,targets,center,$"{actor.Name}: {SpecialOrders.ToolName(tool)} → {targets} head(s)",12);
    }
}
