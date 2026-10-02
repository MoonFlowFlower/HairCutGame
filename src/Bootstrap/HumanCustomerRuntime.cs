using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Hairball;
public partial class Main
{
    bool CustomerPrivacyGood()=>!targetBuildLeak&&targetRecipients.All(id=>id==localId);
    float targetSentAt=-100;
    readonly HashSet<int> targetRecipients=new();
    int botMirrorRound=-1;
    bool HumanSmoke=>args.ContainsKey("human-customer-smoke");
    void CustomerBot(ref Vector2 movement,ref Buttons buttons)
    {
        if(!HumanSmoke||world.Player(localId) is not {Customer:true} p)return;
        movement=Vector2.Zero;buttons=Buttons.None;
        if(world.Phase==Phase.Build&&!p.Standing){yaw=HumanCustomer.SeatYaw(world);pitch=0;buttons=((int)(elapsed*8)%2)==0?Buttons.Stand:Buttons.None;return;}
        if(world.Phase==Phase.Build&&world.Experiment.LeaveStarted<0){yaw=0;pitch=0;movement=new(MathF.Sin(phaseClock*1.7f)*.45f,0);return;}
        if(world.Phase==Phase.Build&&world.Experiment.LeaveStarted>=0&&p.Standing){
            if(System.Numerics.Vector3.Distance(p.Position,PartyLoop.MirrorRoot)<.8f)botMirrorRound=world.Round;
            var destination=botMirrorRound==world.Round?PartyLoop.DoorRoot:PartyLoop.MirrorRoot;
            var d=destination-p.Position;yaw=MathF.Atan2(-d.X,-d.Z);pitch=0;movement=new(0,-.55f);
        }
        if(Session.PieTime(world))buttons=((int)(elapsed*4)%2)==0?Buttons.Primary:Buttons.None;
    }
    void UpdateCustomer(float dt,Buttons buttons)
    {
        if(world.Player(localId) is {Customer:true,Standing:false} seat){var eyes=HumanCustomer.Eyes(world,yaw,pitch);yaw=eyes.Yaw;pitch=eyes.Pitch;}
        hud.TargetHeld=buttons.HasFlag(Buttons.Reference);hud.ResultPhoto=salon?.RevealPhoto;hud.ReviewPhotoRound=salon?.ReturnPhotoRound??0;UpdateTwistPrivate();
        if(authority){hud.PrivateTarget=simulation.OwnTarget(localId);hud.PrivateTargetRound=world.Round;
            if(elapsed-targetSentAt>=1){targetSentAt=elapsed;foreach(var l in links.Values.Where(l=>l.Identity.Peer>0&&!l.Lost))if(simulation.OwnTarget(l.Identity.Actor) is {} card)NetRpc(l.Identity.Peer,MethodName.PrivateTargetData,l.Identity.Actor,world.Round,card.Id,l.Identity.Epoch);}
        }
        if(hud.PrivateTargetRound!=world.Round||simulation.OwnTarget(localId)==null&&authority||!authority&&!PrivateTargetEligible(world,localId))hud.PrivateTarget=null;
        if(salon!=null)salon.PrivateTarget=hud.PrivateTarget;
    }
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel=1)]
    void PrivateTargetData(int actor,int round,int card,int epoch)
    {
        if(authority||epoch!=connectionEpoch||round<world.Round)return;
        targetRecipients.Add(actor);if(actor!=localId){targetBuildLeak=true;GD.PushError("PRIVATE_TARGET_WRONG_RECIPIENT");return;}
        if(card<1||card>TargetCards.Cards.Length)return;hud.PrivateTarget=TargetCards.Cards[card-1];hud.PrivateTargetRound=round;
    }
}
