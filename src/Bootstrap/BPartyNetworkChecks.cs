using Godot;
using Hairball.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using V=System.Numerics.Vector3;
namespace Hairball;
// Head shape, glue and actor poses are injected; E still travels through ordinary InputBatch.
public partial class Main
{
    bool BPartySmoke=>args.ContainsKey("b-party-smoke");
    int partyFixtureRound=-1,partyActor,partyShapeInjections,partyGlueInjections;
    bool partySlipSeen,partyWorkPreserved,partySupplyPreserved;
    float partyMass,partySupply;
    V PartyFixturePose(PlayerState actor,PropState prop)
    {
        var e=world.Experiment;
        if(actor.CarriedProp>=0)return new(1.2f,1.2f,1.2f);
        if(e.PlacementCount==0||e.PlacementCount==1&&e.SlipCount>0)return new(prop.Position.X,0,prop.Position.Z+1.6f);
        return e.PlacementCount==1?new(3,0,2):new(1.6f,0,2.65f);
    }
    void ConfigureBPartySmoke()
    {
        if(!BPartySmoke)return;
        if(!world.Experiment.IsB||expected<2||LegacyNet)throw new InvalidOperationException("party smoke requires normal B 2/4 peers");
        simulation.BuildSeconds=65;simulation.ArrivalSeconds=1;simulation.ChoiceSeconds=2;simulation.PreviewSeconds=1;simulation.ResultsSeconds=2;simulation.HighlightSeconds=1;
    }
    void BPartyNetworkFixtures()
    {
        if(!BPartySmoke||!authority||world.Phase!=Phase.Build||simulation.MoldsEnabled&&!world.Props.Any(m=>m.Mold!=MoldKind.None))return;
        var e=world.Experiment;var prop=world.Props.Single(t=>t.Id==e.HelicopterId);var actor=world.Players.Single(p=>p.Active&&p.Slot==1);partyActor=actor.Id;
        if(partyFixtureRound!=world.Round)
        {
            partyFixtureRound=world.Round;var h=world.SharedHead;
            h.Volume.Fill(v=>Math.Max(Math.Min(.68f-Math.Abs(v.X),Math.Min(.18f-Math.Abs(v.Y-.7f),.5f-Math.Abs(v.Z))),Math.Min(.45f-new System.Numerics.Vector2(v.X,v.Z).Length(),.3f-Math.Abs(v.Y-.35f))));
            foreach(var q in h.Patches){q.Glue=0;q.Stiffness=0;q.Anchored=false;q.Temperature=20;}
            partyMass=h.Mass;partySupply=Session.TotalGrowth(world);partyShapeInjections++;
            var mold=world.Props.FirstOrDefault(m=>m.Mold==MoldKind.Pad);if(mold!=null){mold.Pinned=mold.Attached=true;mold.Local=new(-.55f,.72f,0);mold.Position=h.ToWorld(mold.Local);mold.AttachedRotation=default;}
            simulation.ExperimentEvent("qa_fixture",0,h.Position,"broad soft head and actor poses injected; playerBuilt=false");
        }
        BPartyNetworkObserve();
        if(e.PlacementCount>=2&&AccidentFixture(actor))return;
        if(e.SlipCount>0&&partyGlueInjections==0){foreach(var q in world.SharedHead.Patches)q.Glue=1;partyGlueInjections++;}
        V pose=PartyFixturePose(actor,prop);
        if(e.PlacementCount>=2){var nail=world.Tools.First(t=>t.Definition==6);if(nail.Holder==0)nail.Position=PartyLoop.Bell+new V(.7f,.15f,0); }
        if(e.Leave!=LeaveStage.Seated)return;
        actor.Position=pose;actor.Impulse=default;
        if(salon!.Bodies.TryGetValue(actor.Id,out var body)){body.Position=Art.V(pose);body.Velocity=Godot.Vector3.Zero;}
    }
    void BPartyNetworkInput(PlayerState p,ref Godot.Vector2 movement,ref Buttons buttons)
    {
        if(!BPartySmoke)return;movement=Godot.Vector2.Zero;buttons=Buttons.None;
        if(p.Slot!=1||world.Phase!=Phase.Build)return;
        var e=world.Experiment;var prop=world.Props.Single(t=>t.Id==e.HelicopterId);V point;
        if(AccidentInput(p,ref movement,ref buttons))return;
        if(e.Leave!=LeaveStage.Seated)return;
        // Every fixture pose uses the same rule at both ends, including the fallen pickup.
        // A stale raised client pose must not aim from a different origin than the host.
        p.Position=PartyFixturePose(p,prop);if(salon!.Bodies.TryGetValue(p.Id,out var body)){body.Position=Art.V(p.Position);body.Velocity=Godot.Vector3.Zero;}
        if(p.CarriedProp>=0)point=world.SharedHead.ToWorld(new V(0,.88f,0));
        else if(e.PlacementCount==0||e.PlacementCount==1&&e.SlipCount>0)point=prop.Position;
        else if(e.PlacementCount>=2){var mold=world.Props.FirstOrDefault(m=>m.Mold==MoldKind.Pad);var nail=world.Tools.First(t=>t.Definition==6);point=mold!=null&&mold.Holes.Count==0?(p.Held==nail.Id?mold.Position:nail.Position):PartyLoop.Bell;}
        else return;
        var d=V.Normalize(point-Session.Eye(p));yaw=MathF.Atan2(-d.X,-d.Z);pitch=Math.Clamp(MathF.Asin(d.Y),-1.35f,1.35f);
        if(phaseClock>1.5f&&elapsed%1.2f<.35f)buttons=world.Props.Any(m=>m.Mold==MoldKind.Pad&&m.Holes.Count==0)&&e.PlacementCount>=2&&world.Tools.Any(t=>t.Id==p.Held&&t.Definition==6)?Buttons.Primary:Buttons.Interact;
    }
    void BPartyNetworkObserve()
    {
        if(!BPartySmoke||world.Round<1)return;var e=world.Experiment;
        if(!authority&&world.Phase==Phase.Build&&e.PlacementCount==0){partyMass=world.SharedHead.Mass;partySupply=Session.TotalGrowth(world);}
        if(e.SlipCount>0&&!partySlipSeen){partySlipSeen=true;partyWorkPreserved=world.SharedHead.Mass>partyMass*.9f;partySupplyPreserved=Math.Abs(Session.TotalGrowth(world)-partySupply)<.0001f;}
        if(e.BellActor>0)partyActor=e.BellActor;
    }
    bool BPartySmokeGood()
    {
        BPartyNetworkObserve();var e=world.Experiment;if(!AccidentSmokeGood())return false;
        return (!world.Props.Any(m=>m.Mold!=MoldKind.None)||world.Props.Any(m=>m.Mold==MoldKind.Pad&&m.Holes.Count>0&&m.Pinned))&&world.Phase==Phase.Complete&&world.Job.Settled&&e.Resolved&&e.Success&&e.Leave==LeaveStage.Done&&e.PlacementCount>=2&&e.PickupCount>=2&&e.BellActor!=1&&e.BellActor>0&&partySlipSeen&&partyWorkPreserved&&partySupplyPreserved&&(!authority||receivedCount.GetValueOrDefault(partyActor)>0&&partyShapeInjections==1&&partyGlueInjections==1);
    }
    object BPartyNetworkReport()=>new {moldHoles=world.Props.Sum(m=>m.Holes.Count),moldPinned=world.Props.Any(m=>m.Mold==MoldKind.Pad&&m.Pinned),bottles=world.Tools.Where(t=>t.Definition==1).Select(t=>new{t.Id,t.GrowthRemaining}).ToArray(),enabled=BPartySmoke,qaFixture=BPartySmoke,playerBuilt=false,placements=world.Experiment.PlacementCount,pickups=world.Experiment.PickupCount,slips=world.Experiment.SlipCount,bellActor=world.Experiment.BellActor,leave=world.Experiment.Leave.ToString(),success=world.Experiment.Success,slipSeen=partySlipSeen,workPreserved=partyWorkPreserved,supplyPreserved=partySupplyPreserved,shapeInjections=partyShapeInjections,glueInjections=partyGlueInjections,inputPackets=receivedCount.GetValueOrDefault(partyActor)};
}

