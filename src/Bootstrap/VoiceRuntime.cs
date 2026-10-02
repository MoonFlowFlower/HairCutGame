using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
namespace Hairball;
public partial class Main
{
    sealed class VoiceSpeaker
    {
        public VoiceJitter Jitter=new();public VoiceDecoder Decoder=new();public AudioStreamPlayer3D Player=null!;public AudioStreamGenerator Stream=null!;public AudioStreamGeneratorPlayback Playback=null!;public Label3D Icon=null!;
        public double Until;public long Opus,Features,Decoded,Concealed;public bool ToneMatched;
        public int Capacity;public long ProbeFrames,ProbeMatches,ReceiveAgeSamples;public double ReceiveAgeSum,QueueMsSum;public readonly List<double> Latencies=new();
        public readonly List<float> Pending=new();public long PlaybackDiscarded;
        public double StartedAt;
    }
    sealed class VoicePreview
    {
        public VoiceEncoder Encoder=new();public AudioStreamPlayer Player=null!;public AudioStreamGenerator Stream=null!;public AudioStreamGeneratorPlayback Playback=null!;
        public readonly List<float> Pending=new();public long Pushed;
    }
    VoicePreview? voicePreview;readonly List<(VoicePreview Preview,double ReleaseAt)> retiredPreviews=new();
    readonly Dictionary<int,VoiceSpeaker> speakers=new();readonly Dictionary<int,(double At,int Count,int Bytes)> voiceLimits=new();
    readonly List<(VoiceSpeaker Speaker,double ReleaseAt)> retiredVoice=new();bool quitting,quitGcDrained;int quitCode;double quitAt;
    long voiceCustomerOpusStart;bool voiceWasFiltered;
    VoiceEncoder voiceEncoder=new();AudioEffectCapture? voiceCapture;AudioStreamPlayer? microphone;
    VoicePanel voicePanel=null!;int microphoneBus=-1,injectFrame;double injectClock,voiceLastMetric,voiceStart,voiceInputAt,lastAiRemark=-1;
    long voiceSentBytes,voiceRelayBytes,voiceRejected,voiceRelayPackets,voiceCaptured,voiceReceiveBytes;
    readonly Dictionary<int,double> voiceClockOffsets=new();
    readonly List<double> voiceClockSamples=new();double hostVoiceOffset,voiceClockAt;bool voiceClockReady;
    bool VoiceInjected=>args.ContainsKey("voice-inject");
    bool VoiceFiltered(int actor)=>world.Player(actor)?.Customer==true||args.ContainsKey("voice-filter-test")&&(args["voice-filter-test"]=="true"||args["voice-filter-test"]==actor.ToString());
    VoiceSpecies VoiceSpeciesFor=>args.GetValueOrDefault("voice-species","human")=="bird"?VoiceSpecies.Bird:VoiceSpecies.Human;
    void InitVoice(){voicePanel=new VoicePanel();AddChild(voicePanel);voicePanel.Changed=UpdateMicrophone;voicePanel.PreviewChanged=UpdateVoicePreview;voicePanel.AttachMenuPreview(hud.VoiceSettingsHost);hud.MenuHidden=voicePanel.CancelPreview;if(args.ContainsKey("voice-test-mute"))AudioServer.SetBusMute(0,true);if(args.ContainsKey("voice-settings-review"))voicePanel.ShowSettings(true);}
    void BeginVoice(){voiceWasFiltered=false;voiceCustomerOpusStart=0;ClearSpeakers();voiceEncoder=new();voiceLimits.Clear();voiceClockOffsets.Clear();voiceClockSamples.Clear();voiceClockAt=0;voiceClockReady=authority;hostVoiceOffset=0;injectFrame=0;injectClock=0;voiceStart=voiceInputAt=NetNow;lastAiRemark=-1;voiceSentBytes=voiceRelayBytes=voiceRejected=voiceRelayPackets=voiceCaptured=voiceReceiveBytes=0;if(VoiceInjected){voicePanel.Enabled=true;voicePanel.PushToTalk=false;voicePanel.Gain=1;}UpdateMicrophone();}
    void RetireSpeaker(VoiceSpeaker s){s.Player.Stop();s.Icon.Visible=false;retiredVoice.Add((s,NetNow+.12));}
    void ReleaseVoice(){foreach(var r in retiredVoice.Where(r=>NetNow>=r.ReleaseAt).ToArray()){r.Speaker.Playback.Dispose();r.Speaker.Player.Stream=null;r.Speaker.Stream.Dispose();r.Speaker.Player.QueueFree();r.Speaker.Icon.QueueFree();retiredVoice.Remove(r);}foreach(var r in retiredPreviews.Where(r=>NetNow>=r.ReleaseAt).ToArray()){r.Preview.Playback.Dispose();r.Preview.Player.Stream=null;r.Preview.Stream.Dispose();r.Preview.Player.QueueFree();retiredPreviews.Remove(r);}}
    void ClearSpeakers(){foreach(var s in speakers.Values)RetireSpeaker(s);speakers.Clear();}
    public void QuitGracefully(int code=0){if(quitting)return;quitting=true;quitCode=code;quitAt=NetNow;EndVoice();}
    void StopVoiceStreams(){microphone?.Stop();foreach(var s in speakers.Values)s.Player.Stop();}
    void EndVoice(){WriteVoiceReport();voicePanel?.ShowSettings(false);StopVoicePreview();CloseMicrophone();ClearSpeakers();}
    void StopVoicePreview(){if(voicePreview==null)return;voicePreview.Player.Stop();voicePreview.Pending.Clear();retiredPreviews.Add((voicePreview,NetNow+.12));voicePreview=null;}
    void UpdateVoicePreview()
    {
        StopVoicePreview();if(voicePanel.PreviewHeld&&!quitting)
        {
            if(speakers.Remove(localId,out var own)){RetireSpeaker(own);}
            var stream=new AudioStreamGenerator{MixRateMode=AudioStreamGenerator.AudioStreamGeneratorMixRate.Custom,MixRate=48000,BufferLength=.03f};
            var player=new AudioStreamPlayer{Stream=stream,VolumeDb=Mathf.LinearToDb(Math.Max(.00001f,voicePanel.Output))};AddChild(player);player.Play();
            voicePreview=new(){Player=player,Stream=stream,Playback=(AudioStreamGeneratorPlayback)player.GetStreamPlayback()};
        }
        UpdateMicrophone();
    }
    void FlushVoicePreview(){if(voicePreview is not {} v)return;v.Player.VolumeDb=Mathf.LinearToDb(Math.Max(.00001f,voicePanel.Output));int count=Math.Min(v.Pending.Count,v.Playback.GetFramesAvailable());if(count>0&&v.Playback.PushBuffer(v.Pending.Take(count).Select(x=>new Vector2(x,x)).ToArray())){v.Pending.RemoveRange(0,count);v.Pushed+=count;}if(v.Pending.Count>1920)v.Pending.RemoveRange(0,v.Pending.Count-1920);}
    void PreviewVoiceFrame(float[] pcm,uint stamp)
    {
        if(voicePreview is not {} v||!voicePanel.PreviewHeld)return;
        // Identical feature quantization and synthesis to the customer monitor;
        // this encoder and 2D output never enter the room's upload/relay path.
        var p=v.Encoder.Process(pcm,stamp,true,voicePanel.PreviewSpecies,true,false,true,voicePanel.Gain,voicePanel.Gate);
        if(p==null)return;v.Pending.AddRange(SpeciesVoice.Synthesize(VoiceFeature.Unpack(p.Payload),p.Species,p.Sequence));FlushVoicePreview();voicePanel.SetPreviewSignal(true);
    }
    void CloseMicrophone(){if(microphone!=null){microphone.Stop();microphone.QueueFree();microphone=null;}voiceCapture?.ClearBuffer();voiceCapture=null;if(microphoneBus>=0){AudioServer.RemoveBus(microphoneBus);microphoneBus=-1;}}
    void UpdateMicrophone()
    {
        CloseMicrophone();if(quitting||((!running||!voicePanel.Enabled)&&!voicePanel.PreviewHeld)||VoiceInjected||DisplayServer.GetName()=="headless")return;
        var devices=AudioServer.GetInputDeviceList();if(devices.Length==0){voicePanel.Missing=true;return;}AudioServer.InputDevice=devices.Contains(voicePanel.Device)?voicePanel.Device:"Default";
        microphoneBus=AudioServer.BusCount;AudioServer.AddBus();AudioServer.SetBusName(microphoneBus,"HairballCapture");voiceCapture=new AudioEffectCapture{BufferLength=.12f};AudioServer.AddBusEffect(microphoneBus,voiceCapture);
        // Capture precedes mute. Raw microphone sound never loops to output.
        AudioServer.SetBusMute(microphoneBus,true);microphone=new AudioStreamPlayer{Stream=new AudioStreamMicrophone(),Bus="HairballCapture"};AddChild(microphone);microphone.Play();voiceInputAt=NetNow;
    }
    VoiceSpeaker Speaker(int actor)
    {
        if(speakers.TryGetValue(actor,out var s))return s;
        var stream=new AudioStreamGenerator{MixRateMode=AudioStreamGenerator.AudioStreamGeneratorMixRate.Custom,MixRate=48000,BufferLength=.03f};
        var player=new AudioStreamPlayer3D{Stream=stream,AttenuationModel=AudioStreamPlayer3D.AttenuationModelEnum.Disabled,MaxDistance=0,MaxDb=0};AddChild(player);player.Play();
        var icon=new Label3D{Text="◖)))",Font=LanguageSettings.Font,FontSize=40,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,Modulate=Colors.LightGreen,NoDepthTest=false};AddChild(icon);
        s=new(){Player=player,Stream=stream,Playback=(AudioStreamGeneratorPlayback)player.GetStreamPlayback(),Icon=icon,StartedAt=NetNow};s.Capacity=s.Playback.GetFramesAvailable();speakers[actor]=s;return s;
    }
    void TickVoice(double dt)
    {
        ReleaseVoice();if(quitting){if(NetNow-quitAt>=.18&&retiredVoice.Count==0&&retiredPreviews.Count==0){
            // Finalize temporary native wrappers while Godot is still alive, then give its deferred deletion queue another frame.
            if(!quitGcDrained){quitGcDrained=true;GC.Collect();GC.WaitForPendingFinalizers();quitAt=NetNow;return;}
            GetTree().Quit(quitCode);
        }return;}
        if(voicePanel==null)return;voicePanel.UpdateNames(world.Players.Where(p=>p.Active).Select(p=>(p.Id,L.PlayerName(p.Name))));FlushVoicePreview();
        bool connected=!Networked||authority||clientReady&&peer?.GetConnectionStatus()==MultiplayerPeer.ConnectionStatus.Connected;
        if(running&&!authority&&connected&&NetNow-voiceClockAt>2){voiceClockAt=NetNow;NetRpc(1,MethodName.VoiceClockRequest,connectionEpoch,NetNow);}
        if(voicePanel.PreviewHeld||running&&voicePanel.Enabled&&connected)
        {
            double rtt=Networked&&peer?.GetConnectionStatus()==MultiplayerPeer.ConnectionStatus.Connected?(authority?links.Values.Where(l=>l.Identity.Peer>0).Select(l=>peer.GetPeer(l.Identity.Peer).GetStatistic(ENetPacketPeer.PeerStatistic.RoundTripTime)).DefaultIfEmpty(0).Max():peer.GetPeer(1).GetStatistic(ENetPacketPeer.PeerStatistic.RoundTripTime)):0;voiceEncoder.Bitrate=rtt>280?16000:24000;
            if(VoiceInjected){injectClock=Math.Min(.16,injectClock+dt);while(injectClock>=.02){injectClock-=.02;var pcm=Enumerable.Range(0,960).Select(i=>(float)(.13*Math.Sin(2*Math.PI*(180+localId*70)*(injectFrame*960L+i)/48000))).ToArray();injectFrame++;ConsumeVoiceFrame(pcm,true,(uint)Math.Max(1,Time.GetTicksMsec()-injectClock*1000));}}
            else if(voiceCapture!=null)
            {
                int sourceSamples=Math.Max(1,(int)(AudioServer.GetMixRate()*.02));int frames=0;
                while(voiceCapture.GetFramesAvailable()>=sourceSamples&&frames++<8){var source=voiceCapture.GetBuffer(sourceSamples);uint stamp=(uint)Math.Max(1,Time.GetTicksMsec()-voiceCapture.GetFramesAvailable()/AudioServer.GetMixRate()*1000);var mono=new float[960];for(int i=0;i<960;i++){double pos=i*(source.Length-1d)/959;int ix=(int)pos;var v=source[ix].Lerp(source[Math.Min(ix+1,source.Length-1)],(float)(pos-ix));mono[i]=(v.X+v.Y)*.5f;}voiceCaptured++;if(mono.Any(x=>Math.Abs(x)>.0001f))voiceInputAt=NetNow;ConsumeVoiceFrame(mono,Input.IsPhysicalKeyPressed(Key.V),stamp);}voicePanel.Missing=NetNow-voiceInputAt>5;
            }
            else voicePanel.Missing=true;
        }
        if(!running)return;
        TickBodySounds();
        if(world.Experiment.IsB&&world.Experiment.CustomerActor==0&&voicePanel.Enabled&&world.Experiment.RemarkAt!=lastAiRemark&&world.Experiment.Remark!=Remark.None){lastAiRemark=world.Experiment.RemarkAt;var s=Speaker(0);PushVoice(s,SpeciesVoice.Synthesize(new(.7f,CustomerRemarks.Priority(world.Experiment.Remark)>=3?340:170,true,true),VoiceSpecies.Human,(uint)world.Experiment.Remark));s.Until=NetNow+.3;}
        foreach(var (actor,s) in speakers.ToArray())
        {
            if(actor!=0&&world.Player(actor)==null){RetireSpeaker(s);speakers.Remove(actor);continue;}s.Player.Position=actor==0?Art.V(world.SharedHead.Position):Art.V(world.Player(actor)!.Position)+Vector3.Up*1.7f;s.Icon.Position=s.Player.Position+Vector3.Up*.55f;s.Icon.Visible=voicePanel.Enabled&&s.Until>NetNow&&actor!=localId;
            FlushVoice(s);int steps=0;while(s.Jitter.Pop(NetNow,out var p,out var following)&&steps++<8)
            {
                float[] pcm;
                if(p?.Kind==VoiceKind.Features){s.Features++;pcm=SpeciesVoice.Synthesize(VoiceFeature.Unpack(p.Payload),p.Species,p.Sequence);}
                else if(p!=null||following?.Kind==VoiceKind.Opus){try{pcm=s.Decoder.Decode(p?.Payload??following?.Payload,p==null);s.Opus+=p!=null?1:0;s.Concealed+=p==null?1:0;}catch(Exception ex) when(ex is ArgumentException or Concentus.OpusException){voiceRejected++;continue;}}
                else {pcm=s.Jitter.Kind==VoiceKind.Features?new float[1920]:s.Decoder.Decode(null);s.Concealed++;}s.Decoded++;
                if(VoiceInjected&&p?.Kind==VoiceKind.Opus&&s.Opus%10==0){s.ProbeFrames++;if(VoiceProbe.Matches(pcm,actor,world.Players.Where(p=>p.Active).Select(p=>p.Id)))s.ProbeMatches++;s.ToneMatched=s.ProbeFrames>=5&&s.ProbeMatches/(double)s.ProbeFrames>.85;}
                if(p!=null&&actor!=localId&&(authority||voiceClockReady)&&p.Timestamp>0&&s.Latencies.Count<12000)
                {
                    double age=(NetNow+(authority?0:hostVoiceOffset))-p.Timestamp/1000d;
                    // The stamp describes capture frame END. Add frame acquisition and
                    // samples already queued in the actual Godot playback ring.
                    double queue=(Math.Max(0,s.Capacity-s.Playback.GetFramesAvailable())+s.Pending.Count)/48d;
                    double ms=age*1000+20+queue;
                    if(ms>=0&&ms<2000){s.Latencies.Add(ms);s.QueueMsSum+=queue;}
                }
                if(voicePanel.Enabled)PushVoice(s,pcm);
            }
            s.Player.VolumeDb=Mathf.LinearToDb(Math.Max(.00001f,voicePanel.Enabled?voicePanel.Output*voicePanel.Volume(actor):0));
        }
        voicePanel.SetSpeaking(speakers.Where(p=>p.Value.Until>NetNow).Select(p=>p.Key==0?L.T("Customer"):L.PlayerName(world.Player(p.Key)?.Name??"")));
        if(NetNow-voiceLastMetric>5){voiceLastMetric=NetNow;NetEvent("voice_metrics",VoiceMetrics());}
    }
    void FlushVoice(VoiceSpeaker s){int count=Math.Min(s.Pending.Count,s.Playback.GetFramesAvailable());if(count>0&&s.Playback.PushBuffer(s.Pending.Take(count).Select(x=>new Vector2(x,x)).ToArray()))s.Pending.RemoveRange(0,count);}
    void PushVoice(VoiceSpeaker s,float[] pcm){s.Pending.AddRange(pcm);FlushVoice(s);if(s.Pending.Count>1920){int excess=s.Pending.Count-1920;s.Pending.RemoveRange(0,excess);s.PlaybackDiscarded+=excess;}}
    void SendVoiceFrame(float[] pcm,bool held,uint stamp)
    {
        bool filtered=VoiceFiltered(localId);if(filtered&&!voiceWasFiltered)voiceCustomerOpusStart=voiceEncoder.OpusPackets;voiceWasFiltered=filtered;
        var p=voiceEncoder.Process(pcm,stamp,filtered,VoiceSpeciesFor,voicePanel.Enabled,voicePanel.PushToTalk,held,voicePanel.Gain,voicePanel.Gate);if(p==null)return;var bytes=p.Encode();voiceSentBytes+=bytes.Length;
        // Local monitor only consumes feature packets, never raw PCM.
        var s=Speaker(localId);s.Until=NetNow+.25;if(p.Kind==VoiceKind.Features)s.Jitter.Add(p,NetNow);
        if(!Networked)return;if(authority)RelayVoice(localId,bytes,p);else NetRpc(1,MethodName.VoiceUpload,connectionEpoch,bytes);
    }
    void ConsumeVoiceFrame(float[] pcm,bool held,uint stamp){if(voicePanel.PreviewHeld)PreviewVoiceFrame(pcm,stamp);else if(running)SendVoiceFrame(pcm,held,stamp);}
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Unreliable,TransferChannel=6)]
    void VoiceUpload(int epoch,byte[] bytes)
    {
        if(!authority||quitting)return;int actor=SenderActor();if(!links.TryGetValue(actor,out var l)||l.Lost||!l.Ready||epoch!=l.Identity.Epoch)return;var p=VoicePacket.Parse(bytes);if(p==null||!VoiceRoute.Allowed(p,VoiceFiltered(actor))){voiceRejected++;return;}
        var limit=voiceLimits.GetValueOrDefault(actor);if(NetNow-limit.At>=1)limit=(NetNow,0,0);limit.Count++;limit.Bytes+=bytes.Length;voiceLimits[actor]=limit;if(limit.Count>60||limit.Bytes>10500){voiceRejected++;return;}RelayVoice(actor,bytes,p);
    }
    void RelayVoice(int actor,byte[] bytes,VoicePacket p)
    {
        // Convert capture timestamps to host clock, without touching voice content.
        if(actor!=localId){p=p with{Timestamp=voiceClockOffsets.TryGetValue(actor,out var offset)?(uint)Math.Max(1,p.Timestamp+offset*1000):0};bytes=p.Encode();}
        // Relay does not decode; the host playback recipient is separate.
        foreach(var l in links.Values.Where(l=>l.Identity.Actor!=actor&&l.Identity.Peer>0&&l.Ready&&!l.Lost)){NetRpc(l.Identity.Peer,MethodName.VoiceDownload,l.Identity.Epoch,actor,bytes);voiceRelayBytes+=bytes.Length;voiceRelayPackets++;}if(actor!=localId)ReceiveVoice(actor,p,bytes.Length);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Unreliable,TransferChannel=6)]
    void VoiceDownload(int epoch,int actor,byte[] bytes){if(authority||quitting||epoch!=connectionEpoch||actor==localId||world.Player(actor)==null)return;var p=VoicePacket.Parse(bytes);if(p==null||!VoiceRoute.Allowed(p,VoiceFiltered(actor))){voiceRejected++;return;}ReceiveVoice(actor,p,bytes.Length);}
    void ReceiveVoice(int actor,VoicePacket p,int bytes){voiceReceiveBytes+=bytes;var s=Speaker(actor);if(p.Timestamp>0&&(authority||voiceClockReady)){s.ReceiveAgeSamples++;s.ReceiveAgeSum+=((NetNow+(authority?0:hostVoiceOffset))-p.Timestamp/1000d)*1000;}s.Jitter.Add(p,NetNow);s.Until=NetNow+.25;}
    object VoiceMetrics()=>new{sentPackets=voiceEncoder.OpusPackets+voiceEncoder.FeaturePackets,opusPackets=voiceEncoder.OpusPackets,featurePackets=voiceEncoder.FeaturePackets,voiceSentBytes,voiceRelayBytes,voiceRelayPackets,voiceReceiveBytes,voiceRejected,bitrate=voiceEncoder.Bitrate,mixRate=AudioServer.GetMixRate(),seconds=Math.Max(.001,NetNow-voiceStart),upstreamKbps=(authority?voiceRelayBytes:voiceSentBytes)*8/Math.Max(.001,NetNow-voiceStart)/1000,streams=speakers.Where(p=>p.Key!=localId&&p.Key!=0).Select(p=>new{actor=p.Key,speakerSeconds=NetNow-p.Value.StartedAt,mixedSeconds=p.Value.Player.GetPlaybackPosition(),generatorSkips=p.Value.Playback.GetSkips(),capacity=p.Value.Capacity,playbackDiscarded=p.Value.PlaybackDiscarded,receiveAgeMs=p.Value.ReceiveAgeSamples>0?(double?)(p.Value.ReceiveAgeSum/p.Value.ReceiveAgeSamples):null,queueMs=p.Value.Latencies.Count>0?(double?)(p.Value.QueueMsSum/p.Value.Latencies.Count):null,received=p.Value.Jitter.Received,late=p.Value.Jitter.Late,lost=p.Value.Jitter.Lost,played=p.Value.Jitter.Played,catchUp=p.Value.Jitter.CatchUp,bufferMs=p.Value.Jitter.BufferSeconds*1000,jitterMs=p.Value.Jitter.JitterSeconds*1000,decoded=p.Value.Decoded,opus=p.Value.Opus,features=p.Value.Features,p.Value.Concealed,toneMatched=VoiceInjected?(bool?)p.Value.ToneMatched:null,latencySamples=p.Value.Latencies.Count,latencyEstimateMs=p.Value.Latencies.Count>0?(double?)p.Value.Latencies.Average():null,latencyP95Ms=p.Value.Latencies.Count>0?(double?)Percentile(p.Value.Latencies,.95):null,latencyMaxMs=p.Value.Latencies.Count>0?(double?)p.Value.Latencies.Max():null})};
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Unreliable,TransferChannel=6)]
    void VoiceClockRequest(int epoch,double sent){int actor=SenderActor();if(!authority||!double.IsFinite(sent)||!links.TryGetValue(actor,out var l)||l.Identity.Epoch!=epoch)return;NetRpc(l.Identity.Peer,MethodName.VoiceClockReply,epoch,sent,NetNow,NetNow);}
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Unreliable,TransferChannel=6)]
    void VoiceClockReply(int epoch,double sent,double received,double replied)
    {
        if(authority||epoch!=connectionEpoch||!double.IsFinite(sent)||!double.IsFinite(received)||!double.IsFinite(replied)||NetNow-sent>1||NetNow<sent)return;
        voiceClockSamples.Add((received+replied-sent-NetNow)/2);if(voiceClockSamples.Count>9)voiceClockSamples.RemoveAt(0);hostVoiceOffset=Percentile(voiceClockSamples,.5);voiceClockReady=true;NetRpc(1,MethodName.VoiceClockAck,epoch,hostVoiceOffset);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Unreliable,TransferChannel=6)]
    void VoiceClockAck(int epoch,double offset){int actor=SenderActor();if(authority&&double.IsFinite(offset)&&Math.Abs(offset)<3600&&links.TryGetValue(actor,out var l)&&l.Identity.Epoch==epoch)voiceClockOffsets[actor]=offset;}
    void WriteVoiceReport(){if(reportPath.Length>0&&voiceStart>0)File.WriteAllText(Path.ChangeExtension(reportPath,"voice.json"),JsonSerializer.Serialize(VoiceMetrics(),new JsonSerializerOptions{WriteIndented=true}));}
    bool VoiceSmokeGood()=>!VoiceInjected||speakers.Count(p=>p.Key>0&&p.Key!=localId&&p.Value.Decoded>20&&(VoiceFiltered(p.Key)?p.Value.Features>20:p.Value.Opus>20&&p.Value.ToneMatched))>=expected-1&&(!VoiceFiltered(localId)||voiceEncoder.OpusPackets==voiceCustomerOpusStart);
}
