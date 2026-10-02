using Godot;
using Hairball.Core;
using System;
using System.Linq;
using V=System.Numerics.Vector3;
namespace Hairball;
public static class EgoChecks
{
    static void Require(bool value,string name){if(!value)throw new Exception(name);GD.Print("EGO_PASS "+name);}
    static void Aim(PlayerState p,V target){var d=V.Normalize(target-Session.Eye(p));p.Yaw=MathF.Atan2(-d.X,-d.Z);p.Pitch=MathF.Asin(d.Y);}
    public static void Run(Main root,SalonView view,Session s,Hud hud)
    {
        for(int i=2;i<=4;i++)s.AddPlayer(i,"Barber "+i);s.StartMatch();s.State.Phase=Phase.Build;s.State.Remaining=75;
        void Button(int actor,int option,Buttons buttons){var p=s.State.Player(actor)!;p.Position=new(3+(actor==1?-.4f:.4f),0,-3.5f);Aim(p,SpecialOrders.Button(option));s.Inputs[actor]=(default,p.Yaw,p.Pitch,buttons);}
        Button(1,0,Buttons.Interact);Button(2,1,Buttons.Interact);s.Tick(.016f);
        Require(s.State.Order.Pending&&s.State.Order.Actor==1&&s.State.Job.Wallet==200,"simultaneous physical button inputs resolve one unpaid pending order");
        Button(1,0,Buttons.None);Button(2,2,Buttons.None);s.Tick(.016f);Button(2,2,Buttons.Interact);s.Tick(.016f);
        Require(!s.State.Order.Pending&&s.State.Order.Cancelled==1&&s.State.Job.Wallet==200,"teammate E on physical cancel button prevents spending");
        Button(2,2,Buttons.None);s.Tick(1);Button(1,0,Buttons.Interact);s.Tick(.016f);s.Tick(2.6f);s.Inputs.Clear();
        Require(s.State.Order.Delivered==1&&s.State.Job.Wallet==165&&s.State.Tools.Any(t=>t.Special==1&&t.Charge==1&&t.Holder==0),"commit charges shared wallet once and spawns world tool");
        view.Sync(s.State,1,.1f);
        var contact=root.GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(new(3,2,-4.25f),new(3,0,-4.25f),1));
        Require(contact.Count>0,"delivery tray has real supporting scene collision");
        Require(Enumerable.Range(0,5).All(id=>s.State.Tools.Count(t=>t.Definition==id&&t.Special==0&&t.Charge<0)==4),"four free copies of all five core tools coexist with scarce dangerous tools");
        for(int i=1;i<=4;i++)
        {
            var p=s.State.Player(i)!;p.Position=Session.Spawn(i-1);Aim(p,s.State.SharedHead.Position+new V(0,.45f,0));p.Held=i switch{1=>1,2=>11,3=>0,_=>4};s.State.Tools.First(t=>t.Id==p.Held).Holder=i;
        }
        for(int step=0;step<7;step++)
        {foreach(var p in s.State.Players){p.Cooldown=0;s.UseTool(p,false);}s.Tick(.1f);}
        s.Ledger.Advance(s.State.Time+3);
        Require(Enumerable.Range(1,4).All(id=>s.Ledger.Events.Any(e=>e.Actor==id&&e.Action=="tool")),"four simultaneous real tool queries produce attributed action windows");
        Require(s.Ledger.Events.Count<28,"continuous tool samples coalesce into bounded windows");
        var customer=s.State.Customers[0];s.Stimulate(.7f,1,CustomerStimulus.Noise);s.Tick(.02f);Require(customer.Action!=CustomerReaction.None,"production panic starts telegraphed reaction");
        var a=s.State.Player(1)!;var b=s.State.Player(2)!;a.Position=new(1.7f,0,2.8f);b.Position=new(1.7f,0,1.4f);Aim(a,b.Position+V.UnitY*1.35f);a.Held=7;s.State.Tools[7].Holder=1;a.Cooldown=0;s.UseTool(a,false);
        Require(s.Ledger.Events.Any(e=>e.Actor==1&&e.Severity>=8&&e.Context.HasFlag(ImpactContext.Reacting)),"reaction-time missed head / teammate hit is charged to actual dangerous-tool actor");
        a.Held=3;s.State.Tools[3].Holder=1;a.Cooldown=0;b.ImpactCooldown=0;var before=b.Impulse;s.UseTool(a,false);
        Require(b.Impulse!=before,"blower still moves player through the real tool pipeline with ledger enabled");
        s.Ledger.Close(1);s.State.Remaining=.001f;s.Tick(.01f);s.Tick(9.1f);
        Require(s.State.Phase==Phase.Results&&s.State.Job.Awards.Any(a=>a.Title=="Big Spender")&&s.State.Job.Awards.Any(a=>a.Title=="Biggest Accident"),"post-job spotlights derive from actual spending and accident facts");
        Require(s.State.Job.Wallet==165+s.State.Job.Delta,"awards do not alter team payout");
        var snapshot=Wire.Decode<WorldState>(Wire.Encode(s.State));Require(snapshot.Job.Awards.Select(a=>a.Title+a.Actor).SequenceEqual(s.State.Job.Awards.Select(a=>a.Title+a.Actor)),"result snapshot preserves award recipients and shared money");
        var live=Wire.Encode(s.State);var frame=s.Replay.Rolling.LastOrDefault();view.Sync(s.State,1,.1f,frame);Require(live.SequenceEqual(Wire.Encode(s.State)),"historical replay view cannot emit material or change ledger outcome");
        view.Sync(s.State,1,.1f);hud.Update(s.State,1,true,false,view.Mirror,0);hud.VerifyEgoVisibility(s.State,1,view.Mirror);
        GD.Print("EGO_CHECKS_OK");
    }
}
