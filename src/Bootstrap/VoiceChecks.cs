using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
namespace Hairball;
public partial class Main
{
    async void VoiceChecks()
    {
        try
        {
            void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);GD.Print("VOICE_ENGINE_PASS "+message);}
            var e=new VoiceEncoder();var d=new VoiceDecoder();var pcm=Enumerable.Range(0,960).Select(i=>(float)(.2*Math.Sin(i*2*Math.PI*300/48000))).ToArray();
            var output=Speaker(localId);int available=output.Playback.GetFramesAvailable();PushVoice(output,d.Decode(e.Process(pcm,0,false,0,true,false,false)!.Payload));
            Check(available>=960&&output.Playback.GetFramesAvailable()<available,"injected Opus decode feeds actual Godot generator playback");
            Check(e.Process(pcm,200,false,0,true,true,false)==null,"PTT release blocks transmission");
            Check(e.Process(pcm,200,false,0,true,true,true)!=null,"PTT press sends");
            var gated=new VoiceEncoder();Check(gated.Process(new float[960],0,false,0,true,false,false)==null,"VAD closes on silence");
            for(int i=0;i<10;i++)gated.Process(pcm,0,true,0,true,false,false);Check(gated.OpusPackets==0,"filtered source never creates Opus");
            CloseMicrophone();voicePanel.Missing=true;Check(running&&world.Player(localId)!=null,"missing microphone leaves game running");
            var before=Wire.Encode(world);foreach(string lang in new[]{"zh","en"}){L.SetLocale(lang);voicePanel.ShowSettings(true);}Check(before.SequenceEqual(Wire.Encode(world)),"voice controls and locale never enter snapshot");
            if(args.ContainsKey("voice-preview-check"))
            {
                GetViewport().GuiEmbedSubwindows=true;Input.MouseMode=Input.MouseModeEnum.Visible;voicePanel.ShowSettings(false);
                async Task Frame(){await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
                void Pointer(Vector2 position,bool down){Input.WarpMouse(position);Input.ParseInputEvent(new InputEventMouseMotion{Position=position,GlobalPosition=position});Input.ParseInputEvent(new InputEventMouseButton{Position=position,GlobalPosition=position,ButtonIndex=MouseButton.Left,Pressed=down,ButtonMask=down?MouseButtonMask.Left:0});}
                async Task Click(Control control){var at=control.GetGlobalRect().GetCenter();var window=control.GetWindow();if(window!=GetWindow())at+=new Vector2(window.Position.X,window.Position.Y);Pointer(at,true);await Frame();Pointer(at,false);await Frame();}
                var leave=(Button)hud.FindChild("LeaveRoomButton",true,false);await Click(leave);
                var confirmation=hud.FindChildren("*","ConfirmationDialog",true,false).OfType<ConfirmationDialog>().Single();Check(confirmation.Visible,"actual leave button opens confirmation");await Click(confirmation.GetOkButton());
                Check(!running&&hud.MenuVisible&&salon==null,"actual confirmed room exit reaches main menu");
                before=Wire.Encode(world);long sent=voiceSentBytes,opus=voiceEncoder.OpusPackets,features=voiceEncoder.FeaturePackets;
                bool enabled=voicePanel.Enabled,ptt=voicePanel.PushToTalk;voicePanel.Enabled=false;voicePanel.PushToTalk=true;
                Button Button()=>hud.VoiceSettingsHost.FindChild("VoicePreviewButton",true,false) as Button??throw new InvalidOperationException("menu preview button missing");
                OptionButton Species()=>hud.VoiceSettingsHost.FindChild("VoicePreviewSpecies",true,false) as OptionButton??throw new InvalidOperationException("menu species picker missing");
                async Task Capture(string name){if(DisplayServer.GetName()=="headless")return;await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);string folder=args.GetValueOrDefault("qa-output",ProjectSettings.GlobalizePath("res://artifacts/voice-preview"));Directory.CreateDirectory(folder);using var image=GetViewport().GetTexture().GetImage();Check(image.SavePng(Path.Combine(folder,name+".png"))==Error.Ok,"preview screenshot "+name);}
                // Exercise actual pointer dispatch after the same room-exit path as the owner.
                foreach(string lang in new[]{"zh","en"})
                {
                    L.SetLocale(lang);voicePanel.UpdateNames(world.Players.Where(p=>p.Active).Select(p=>(p.Id,L.PlayerName(p.Name))));await Frame();
                    Check(Button().GetParent()==Species().GetParent(),"hold button and species selector share one row");
                    foreach(var species in Enum.GetValues<VoiceSpecies>())
                    {
                        var b=Button();var pick=Species();
                        pick.Select((int)species);pick.EmitSignal(OptionButton.SignalName.ItemSelected,(long)species);
                        Check(b.IsVisibleInTree()&&!voicePanel.SettingsOpen,"preview visible directly on the returned menu without F8");Pointer(b.GetGlobalRect().GetCenter(),true);await Frame();CloseMicrophone();
                        var preview=voicePreview!;Check(preview!=null&&voicePanel.PreviewHeld,"preview starts from menu even with voice disabled and PTT released");
                        int capacity=preview!.Playback.GetFramesAvailable();for(int i=0;i<4;i++)ConsumeVoiceFrame(pcm,false,(uint)Time.GetTicksMsec());
                        Check(preview.Encoder.OpusPackets==0&&preview.Encoder.FeaturePackets>=2,"preview uses feature-only customer pipeline "+species);
                        Check(preview.Pushed>0&&preview.Playback.GetFramesAvailable()<capacity,"preview feeds actual local generator "+species);
                        Check(voiceSentBytes==sent&&voiceEncoder.OpusPackets==opus&&voiceEncoder.FeaturePackets==features,"menu preview does not encode or upload game voice");
                        await Capture("preview-"+lang+"-"+species);
                        Pointer(b.GetGlobalRect().GetCenter(),false);await Frame();
                        Check(!voicePanel.PreviewHeld&&voicePreview==null&&!preview.Player.Playing&&preview.Pending.Count==0,"pointer release immediately stops and clears preview");
                        ConsumeVoiceFrame(pcm,false,(uint)Time.GetTicksMsec());Check(voiceSentBytes==sent,"menu release cannot send leftover preview audio");
                    }
                }
                void Hold()=>Pointer(Button().GetGlobalRect().GetCenter(),true);
                Hold();await Frame();Input.ParseInputEvent(new InputEventMouseMotion{Position=Vector2.Zero,GlobalPosition=Vector2.Zero});await Frame();Check(voicePreview==null,"dragging off button stops preview");Pointer(Vector2.Zero,false);
                Hold();await Frame();Check(voicePreview!=null,"preview active before focus loss");voicePanel._Notification((int)NotificationWMWindowFocusOut);Check(voicePreview==null,"window focus loss stops preview");Pointer(Vector2.Zero,false);
                Hold();await Frame();Check(voicePreview!=null,"preview active before menu hide");hud.ShowMenu(false);Check(voicePreview==null&&!voicePanel.PreviewHeld,"hiding menu stops preview");hud.ShowMenu(true);Pointer(Vector2.Zero,false);await Frame();
                Hold();await Frame();Check(voicePreview!=null,"preview active before locale change");L.SetLocale("zh");Check(voicePreview==null&&!voicePanel.PreviewHeld,"locale rebuild stops preview");Pointer(Vector2.Zero,false);await Frame();
                for(int i=0;i<6;i++){Hold();await Frame();Pointer(Button().GetGlobalRect().GetCenter(),false);await ToSignal(GetTree().CreateTimer(.2),SceneTreeTimer.SignalName.Timeout);}
                Check(retiredPreviews.Count==0,"repeated preview releases drain native generator resources");
                Check(before.SequenceEqual(Wire.Encode(world)),"species preview leaves authoritative snapshot unchanged");voicePanel.Enabled=enabled;voicePanel.PushToTalk=ptt;
                await Click((Button)hud.VoiceSettingsHost.FindChild("MenuVoiceSettingsButton",true,false));Check(voicePanel.SettingsOpen,"menu microphone settings button opens actual panel");await Click((Button)voicePanel.FindChild("VoiceSettingsClose",true,false));Check(!voicePanel.SettingsOpen,"mouse closes microphone settings");
                async Task F8(){Input.ParseInputEvent(new InputEventKey{Keycode=Key.F8,PhysicalKeycode=Key.F8,Pressed=true});await Frame();Input.ParseInputEvent(new InputEventKey{Keycode=Key.F8,PhysicalKeycode=Key.F8,Pressed=false});}
                await F8();Check(voicePanel.SettingsOpen,"actual viewport F8 input opens voice panel");await F8();Check(!voicePanel.SettingsOpen,"actual viewport F8 input closes voice panel");
                Hold();await Frame();Check(voicePreview!=null,"menu preview restarts after settings; quit drains an active preview");
            }
            GD.Print("VOICE_ENGINE_OK");QuitGracefully();
        }
        catch(Exception ex){GD.PushError("VOICE_ENGINE_FAIL "+ex);QuitGracefully(1);}
    }
}
