using Godot;
using Hairball.Core;
namespace Hairball;
public partial class Main
{
    float bodyPreferenceAt=-100;
    void UpdateBodyPreferences(){if(world.Player(localId) is not {} p)return;if(authority){p.AllowDrag=PresentationSettings.AllowDrag;p.AllowViewChanges=PresentationSettings.AllowViewChanges||args.ContainsKey("qa-twist");p.AllowDown=PresentationSettings.AllowDown;p.VoiceEnabled=voicePanel.Enabled;}else if(clientReady&&elapsed-bodyPreferenceAt>1){bodyPreferenceAt=elapsed;NetRpc(1,MethodName.BodyPreference,PresentationSettings.AllowDown,voicePanel.Enabled,PresentationSettings.AllowViewChanges||args.ContainsKey("qa-twist"),PresentationSettings.AllowDrag,inputGeneration);}}
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel=1)]
    void BodyPreference(bool allowDown,bool voiceEnabled,bool allowViewChanges,bool allowDrag,int generation){int id=SenderActor();if(authority&&CanAct(id,generation)&&world.Player(id) is {} p){p.AllowDrag=allowDrag;p.AllowViewChanges=allowViewChanges;p.AllowDown=allowDown;p.VoiceEnabled=voiceEnabled;}}
}
