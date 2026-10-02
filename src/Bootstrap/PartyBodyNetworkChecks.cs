using Godot;
using Hairball.Core;
using System;
using System.Linq;
using V=System.Numerics.Vector3;
namespace Hairball;
// Only poses, shapes and held objects are fixtures. Buttons use normal remote InputBatch.
public partial class Main
{
    bool BodySmoke=>args.ContainsKey("party-body-smoke");
    int bodyStage=-1,bodyEntered=-2;float bodyAt,bodyStageClock,bodyCutMass,bodyGlueBefore;
    bool bodyCaught,bodyDown,bodyRevived,bodyCut,bodySprayed,bodyDetached;
    V BodyFixturePose(int slot)=>bodyStage<3?(slot==0?new(3,0,0):new(3,0,1.7f)):
        bodyStage==5&&slot==2?new(4,1,2.2f):new(slot==2?1:0,1,2.2f);
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel=1)]
    public void BodyFixtureStage(int stage){if(!BodySmoke||authority||stage<bodyStage)return;if(stage!=bodyStage){bodyStage=stage;bodyStageClock=elapsed;}}
    void BodyNetworkFixtures()
    {
        if(!BodySmoke||world.Phase!=Phase.Build)return;
        if(authority){
            if(bodyStage<0){bodyStage=0;bodyAt=world.Time;simulation.BuildSeconds=65;world.Remaining=65;}
            bool done=bodyStage switch{
                0=>simulation.ActionLog.Any(a=>a.Kind==PartyAction.Catch&&a.Actor==1),
                1=>simulation.ActionLog.Any(a=>a.Kind==PartyAction.Down&&a.Target==1)&&world.Time-bodyAt>.7f,
                2=>simulation.ActionLog.Any(a=>a.Kind==PartyAction.Revive&&a.Actor!=1),
                3=>simulation.ActionLog.Any(a=>a.Kind==PartyAction.CoopCut)&&world.SharedHead.Mass<bodyCutMass,
                4=>simulation.ActionLog.Any(a=>a.Kind==PartyAction.CoopSpray)&&world.SharedHead.Patches.Sum(p=>p.Glue)>bodyGlueBefore,
                5=>world.Props.Where(p=>p.Coop is CoopKind.Nozzle or CoopKind.Tank).All(p=>p.Holder==0)&&world.Time-bodyAt>.7f,_=>false};
            if(done){bodyStage++;if(bodyStage==3&&expected==2)bodyStage=6;bodyAt=world.Time;}
            if(bodyStage<6&&world.Time-bodyAt>12){Report(false,"body remote stage timeout "+bodyStage);return;}
            if(bodyStage!=bodyEntered){
                bodyEntered=bodyStage;bodyStageClock=elapsed;if(bodyStage!=5)foreach(var p in world.Players)simulation.Drop(p);simulation.Inputs.Clear();
                GD.Print("PARTY_BODY_REMOTE_STAGE "+bodyStage);
                if(bodyStage==0){var t=world.Tools.First(t=>t.Id==1);t.Holder=2;world.Player(2)!.Held=t.Id;}
                if(bodyStage==1){var p=world.Player(1)!;var t=world.Tools.First(t=>t.Id==1);t.Holder=1;p.Held=t.Id;p.AllowDown=true;
                    var prop=world.Props.Single(p=>p.Id==world.Experiment.HelicopterId);prop.Holder=0;prop.Attached=false;prop.Released=true;prop.Position=new(3,2.25f,0);prop.Velocity=new(0,-2,0);prop.Thrower=2;prop.ThrownAt=world.Time;}
                if(bodyStage==3){world.SharedHead.Volume.Fill(v=>.65f-v.Length());foreach(var p in world.SharedHead.Patches){p.Glue=0;p.Anchored=false;p.Stiffness=0;}bodyCutMass=world.SharedHead.Mass;}
                if(bodyStage==4)bodyGlueBefore=world.SharedHead.Patches.Sum(p=>p.Glue);
                foreach(var pair in bodyStage==3?new[]{(2,CoopKind.ScissorA),(3,CoopKind.ScissorB)}:bodyStage==4?new[]{(2,CoopKind.Nozzle),(3,CoopKind.Tank)}:Array.Empty<(int,CoopKind)>()){
                    var prop=world.Props.Single(p=>p.Coop==pair.Item2);prop.Holder=pair.Item1;world.Player(pair.Item1)!.CarriedProp=prop.Id;}
            }
            if(elapsed% .5f<.025f)Rpc(MethodName.BodyFixtureStage,bodyStage);
            if(bodyStage==1)world.Player(1)!.AllowDown=true;
        }
        BodyNetworkObserve();
        if(bodyStage<0||bodyStage>=6)return;
        foreach(var p in world.Players.Where(p=>p.Active&&(bodyStage<3?p.Slot<2:p.Slot is 1 or 2))){p.Position=BodyFixturePose(p.Slot);p.Impulse=default;if(salon!.Bodies.TryGetValue(p.Id,out var b)){b.Position=Art.V(p.Position);b.Velocity=Vector3.Zero;}}
    }
    void BodyNetworkInput(PlayerState p,ref Vector2 movement,ref Buttons buttons)
    {
        if(!BodySmoke||bodyStage<0||bodyStage>=6||world.Phase!=Phase.Build)return;movement=Vector2.Zero;buttons=Buttons.None;
        if(bodyStage<3&&p.Slot>=2||bodyStage>=3&&p.Slot is not (1 or 2))return;
        p.Position=BodyFixturePose(p.Slot);if(salon!.Bodies.TryGetValue(p.Id,out var b)){b.Position=Art.V(p.Position);b.Velocity=Vector3.Zero;}
        V target=bodyStage==2?new(3,.8f,0):bodyStage>=3?world.SharedHead.ToWorld(new V(0,.4f,.2f)):new(3,1.7f,0);
        if(bodyStage<3&&p.Slot==0)target=new(3,1.7f,2);
        var d=V.Normalize(target-Session.Eye(p));yaw=MathF.Atan2(-d.X,-d.Z);pitch=MathF.Asin(d.Y);
        float cycle=(elapsed-bodyStageClock)%1.8f;
        if(bodyStage==0&&p.Slot==1&&cycle>.25f&&cycle<.85f)buttons=Buttons.Drop;
        if(bodyStage==2&&p.Slot==1&&cycle>.3f&&cycle<.7f)buttons=Buttons.Interact;
        if(bodyStage==3&&cycle>.4f&&cycle<1.2f||bodyStage==4&&p.Slot==2&&cycle>.4f&&cycle<1.2f)buttons=Buttons.Primary;
    }
    void BodyNetworkObserve()
    {
        if(!BodySmoke)return;
        bodyCaught|=bodyStage>=1;bodyDown|=world.Player(1) is {} p&&p.DownUntil>world.Time;
        bodyRevived|=bodyStage>=3;bodyCut|=bodyStage>=4&&expected==4;bodySprayed|=bodyStage>=5&&expected==4;bodyDetached|=bodyStage>=6&&expected==4;
    }
    bool BodySmokeGood(){BodyNetworkObserve();return !BodySmoke||bodyStage==6&&bodyCaught&&bodyDown&&bodyRevived&&(expected==2?!world.Props.Any(p=>p.Coop!=CoopKind.None):bodyCut&&bodySprayed&&bodyDetached);}
    object BodyNetworkReport()=>new{enabled=BodySmoke,qaFixture=BodySmoke,playerBuilt=false,stage=bodyStage,caught=bodyCaught,down=bodyDown,revived=bodyRevived,cut=bodyCut,sprayed=bodySprayed,detached=bodyDetached,ordinaryRemoteInput=authority?receivedCount.Values.Sum():0};
}
