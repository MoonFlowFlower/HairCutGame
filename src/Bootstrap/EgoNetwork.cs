using Godot;
using Hairball.Core;
using System;
using System.Linq;
using System.Security.Cryptography;
using V=System.Numerics.Vector3;
namespace Hairball;

public partial class Main
{
    bool EgoSmoke=>args.ContainsKey("ego-smoke");
    int egoResults,egoRestoredRound=-1;
    int egoCancelPressedRound=-1;
    float egoCancelPressUntil;
    int BuyerSlot=>expected>1?1:0;
    int CancelSlot=>expected>2?2:0;
    // Bot press/release edges must use a continuously advancing local clock.
    // Sparse full snapshots can jump across the old 0.3-second release window.
    float EgoBuildAge=>authority?(fullSmoke?75:16)-world.Remaining:phaseClock;
    // Only bot fixtures relocate bodies. Actual E input travels through InputRequest,
    // host ray/proximity checks, pending order resolution and ordinary snapshots.
    void EgoNetworkFixtures()
    {
        if(world.Phase!=Phase.Build)return;
        var buyer=world.Players.FirstOrDefault(p=>p.Slot==BuyerSlot);
        bool picked=buyer!=null&&world.Tools.Any(t=>t.Special==1&&t.Holder==buyer.Id);
        if(EgoBuildAge>=3&&!picked&&EgoBuildAge<14)
        {
            foreach(var p in world.Players.Where(p=>p.Slot==BuyerSlot||p.Slot==CancelSlot))
            {
                p.Position=new(3+(p.Slot==BuyerSlot?-.65f:.65f),0,-3.5f);p.Impulse=V.Zero;
                if(salon!.Bodies.TryGetValue(p.Id,out var body)){body.Position=Art.V(p.Position);body.Velocity=Vector3.Zero;}
            }
        }
        else if(picked&&egoRestoredRound!=world.Round)
        {
            egoRestoredRound=world.Round;
            foreach(var p in world.Players.Where(p=>p.Slot==BuyerSlot||p.Slot==CancelSlot))
            {p.Position=Session.Spawn(p.Slot);if(salon!.Bodies.TryGetValue(p.Id,out var body))body.Position=Art.V(p.Position);}
        }
    }
    void EgoNetworkInput(PlayerState p,ref Vector2 move,ref Buttons buttons)
    {
        if(world.Phase!=Phase.Build||EgoBuildAge<3||(p.Slot!=BuyerSlot&&p.Slot!=CancelSlot))return;
        move=Vector2.Zero;buttons=Buttons.None;
        bool press=(int)(phaseClock*4)%2==0;
        if(p.Slot==BuyerSlot&&world.Tools.FirstOrDefault(t=>t.Special==1) is {} delivered)
        {
            // Follow replicated milestones instead of skipping a short input window
            // when snapshots arrive sparsely. Every action still takes the real E/LMB path.
            bool held=delivered.Holder==p.Id;
            var aim=V.Normalize((held?world.SharedHead.Position+new V(0,.4f,0):delivered.Position)-Session.Eye(p));
            yaw=MathF.Atan2(-aim.X,-aim.Z);pitch=MathF.Asin(aim.Y);
            if(held){if(V.Distance(p.Position,Session.Spawn(p.Slot))<.6f)buttons=Buttons.Primary;}
            else if(press)buttons=Buttons.Interact;
            return;
        }
        int option=p.Slot==BuyerSlot?0:2;
        var d=V.Normalize(SpecialOrders.Button(option)-Session.Eye(p));yaw=MathF.Atan2(-d.X,-d.Z);pitch=MathF.Asin(d.Y);
        if(p.Slot==CancelSlot)
        {
            if(press&&EgoBuildAge>=3.6f&&world.Order.Pending&&world.Order.Cancelled==0&&egoCancelPressedRound!=world.Round)
            {egoCancelPressedRound=world.Round;egoCancelPressUntil=phaseClock+.25f;}
            if(egoCancelPressedRound==world.Round&&phaseClock<egoCancelPressUntil)buttons=Buttons.Interact;
        }
        else if(press&&EgoBuildAge>=3.6f&&!world.Order.Pending)buttons=Buttons.Interact;
    }
    void EgoNetworkCheck()
    {
        if(world.Phase!=Phase.Results||world.Round<=egoResults)return;
        var buyer=world.Players.First(p=>p.Slot==BuyerSlot);
        bool good=world.Order.Cancelled==1&&world.Order.Delivered==1&&world.Order.Spent==35
            &&world.Order.Actor==buyer.Id&&world.Tools.Any(t=>t.Special==1&&t.Charge==0);
        if(authority)good&=simulation.Ledger.For(buyer.Id).Spend==35&&simulation.Ledger.Events.Any(e=>e.Actor==buyer.Id&&e.Action=="tool"&&e.Context.HasFlag(ImpactContext.Ordered));
        if(!good){Report(false,$"ego round={world.Round} cancelled={world.Order.Cancelled} delivered={world.Order.Delivered} spent={world.Order.Spent} held={buyer.Held} specials={string.Join(';',world.Tools.Where(t=>t.Special>0).Select(t=>$"{t.Id}/{t.Holder}/{t.Charge}/{t.LastUse}"))} awards={string.Join(',',world.Job.Awards.Select(a=>a.Title))}");return;}
        egoResults=world.Round;
        var facts=Wire.Encode(new SharedEgoResult{Job=world.Job,Order=world.Order});
        GD.Print($"EGO_NETWORK_RESULT job={world.Round} hash={Convert.ToHexString(SHA256.HashData(facts))[..16]} buyer={buyer.Id} cancelled={world.Order.Cancelled} spend={world.Order.Spent} specialUsed=True");
    }
}
public sealed class SharedEgoResult { public SharedJob Job=new();public ShopOrder Order=new(); }
