using Godot;
using Hairball.Core;
using System;
using System.Linq;
namespace Hairball;
public partial class Main
{
    float twistPrivateAt=-100;
    public static bool PrivateTargetEligible(WorldState w,int actor)=>w.Player(actor) is {} p&&(w.Twist.Kind is TwistKind.Reverse or TwistKind.SplitInfo?!p.Customer:p.Customer||w.Twist.FamilyActor==actor);
    void UpdateTwistPrivate()
    {
        if(authority){hud.GuessOptions=simulation.OwnOptions(localId);var recipe=simulation.OwnRecipe(localId);hud.RecipeChinese=recipe.Chinese;hud.RecipeEnglish=recipe.English;
            if(elapsed-twistPrivateAt>=1){twistPrivateAt=elapsed;foreach(var link in links.Values.Where(l=>l.Identity.Peer>0&&!l.Lost)){int actor=link.Identity.Actor;var r=simulation.OwnRecipe(actor);NetRpc(link.Identity.Peer,MethodName.PrivateTwistData,actor,world.Round,simulation.OwnOptions(actor),r.Chinese,r.English,link.Identity.Epoch);}}
        }
        if(salon!=null)salon.PrivateTarget=hud.PrivateTargetRound==world.Round?hud.PrivateTarget:null;
        if(world.Player(localId)?.Customer!=true){hud.GuessOptions=[];hud.RecipeChinese=hud.RecipeEnglish="";}
    }
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel=1)]
    void PrivateTwistData(int actor,int round,int[] options,string chinese,string english,int epoch)
    {
        if(authority||epoch!=connectionEpoch||round!=world.Round)return;if(actor!=localId){targetBuildLeak=true;return;}
        if(options.Length is not (0 or 4)||options.Any(id=>id<1||id>TargetCards.Cards.Length)||chinese.Length>160||english.Length>240)return;
        hud.GuessOptions=options;hud.RecipeChinese=chinese;hud.RecipeEnglish=english;
    }
    void GuessInput(int option){if(authority)simulation.GuessTarget(localId,option);else if(clientReady)NetRpc(1,MethodName.GuessRequest,option,world.Round,inputGeneration);}
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel=1)]
    void GuessRequest(int option,int round,int generation){int actor=SenderActor();if(authority&&round==world.Round&&CanAct(actor,generation))simulation.GuessTarget(actor,option);}
}
