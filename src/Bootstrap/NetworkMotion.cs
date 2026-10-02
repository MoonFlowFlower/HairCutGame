using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using NVector=System.Numerics.Vector3;
namespace Hairball;
public partial class Main
{
    bool LegacyNet=>args.ContainsKey("legacy-net-motion");
    readonly Dictionary<int,InputTimeline> inputTimelines=new();
    readonly Dictionary<int,int> pendingWorld=new();
    readonly List<InputTick> predictedInputs=new();
    readonly MotionBuffer motionBuffer=new();
    MotionFrame? pendingMotion;
    int inputSequence,motionSequence,lastMotionApplied,predictedRound=-1,predictedMatch=-1;
    float motionClock;
    float predictedTime;
    Vector3 predictedImpulse,cameraPrevious,cameraCurrent,cameraOffset;
    bool cameraReady;
    readonly List<double> physicsCosts=new(),renderCosts=new(),corrections=new(),motionGaps=new();
    float lastMotionArrival;
    MotionFrame? previousHeadMotion;
    float headTravel,headMaxStep,headMaxSpeed;
    int headMotionSamples;
    int hardCorrections,reverseTicks,walkTicks;
    long stateBytes,motionBytes,inputBytes;
    Vector3 metricPrevious;bool metricReady;
    StreamWriter? networkLog;
    float networkLogClock;
    float recentCorrectionMax;
    System.Threading.Tasks.Task<byte[]>? replayTransfer;
    void ResetNetworkMotion()
    {
        Wire.LegacyCompression=LegacyNet;
        replayTransfer=null;
        inputTimelines.Clear();pendingWorld.Clear();predictedInputs.Clear();motionBuffer.Clear();pendingMotion=null;hud.LiveMotion=null;
        predictedRound=predictedMatch=-1;inputSequence=motionSequence=lastMotionApplied=0;motionClock=predictedTime=0;cameraReady=false;cameraOffset=predictedImpulse=default;
        physicsCosts.Clear();renderCosts.Clear();corrections.Clear();motionGaps.Clear();lastMotionArrival=0;
        headTravel=headMaxStep=headMaxSpeed=0;headMotionSamples=0;previousHeadMotion=null;
        hardCorrections=reverseTicks=walkTicks=0;stateBytes=motionBytes=inputBytes=0;metricReady=false;recentCorrectionMax=0;
        networkLog?.Dispose();networkLogClock=0;
        networkLog=null;
        try{
            string folder=OS.HasFeature("editor")?"artifacts":Path.Combine(OS.GetUserDataDir(),"playtests");Directory.CreateDirectory(folder);
            networkLog=new StreamWriter(Path.Combine(folder,"network-"+mode+"-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+"-"+System.Environment.ProcessId+".jsonl")){AutoFlush=true};
        }catch(IOException e){GD.PushWarning("Network telemetry unavailable: "+e.Message);}
        catch(UnauthorizedAccessException e){GD.PushWarning("Network telemetry unavailable: "+e.Message);}
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.UnreliableOrdered,TransferChannel=0)]
    public void InputBatch(byte[] data,int generation)
    {
        if(!authority||LegacyNet)return;int sender=SenderActor();
        var ticks=MotionWire.ReadInputs(data);bool accepted=CanAct(sender,generation)&&world.Player(sender)!=null&&ticks.Length>0;
        if(links.TryGetValue(sender,out var inputLink)){inputLink.InputArrival=NetNow;inputLink.InputPackets++;if(accepted){inputLink.InputAccepted=NetNow;inputLink.AcceptedInputPackets++;}}
        if(!accepted)return;
        if(!inputTimelines.TryGetValue(sender,out var timeline))inputTimelines[sender]=timeline=new();
        timeline.Add(ticks);inputReceived[sender]=elapsed;receivedCount[sender]=receivedCount.GetValueOrDefault(sender)+1;inputBytes+=data.Length;
    }
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.UnreliableOrdered,TransferChannel=3)]
    public void MotionSnapshot(byte[] data)
    {
        if(authority||LegacyNet||connectionEpoch==0||health==ConnectionHealth.Failed)return;lastRawMotion=lastServer=NetNow;var frame=MotionWire.Decode(data);if(frame==null||!motionBuffer.Add(frame,elapsed))return;
        if(previousHeadMotion is {} last&&last.Round==frame.Round&&frame.Time>last.Time){float step=NVector.Distance(frame.Head.Position,last.Head.Position);headTravel+=step;headMaxStep=Math.Max(headMaxStep,step);headMaxSpeed=Math.Max(headMaxSpeed,step/(frame.Time-last.Time));headMotionSamples++;}previousHeadMotion=frame;
        pendingMotion=frame;hud.LiveMotion=frame;motionBytes+=data.Length;
        if(world.Experiment.IsB){experimentFacts.Add("leave:"+frame.Leave);if(frame.Attention!=AttentionStage.Unaware)experimentFacts.Add("attention:"+frame.Attention);if(frame.Bracing)experimentFacts.Add("brace");}
        if(lastMotionArrival>0)motionGaps.Add((elapsed-lastMotionArrival)*1000);lastMotionArrival=elapsed;
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel=4)]
    public void WorldAck(int revision)
    {
        int sender=SenderActor();if(authority&&pendingWorld.TryGetValue(sender,out int sent)&&revision==sent)pendingWorld.Remove(sender);
    }
    void SendMotion(float dt)
    {
        if(mode!="host"||LegacyNet||peer==null||Multiplayer.GetPeers().Length==0)return;
        motionClock+=dt;if(motionClock<.05f)return;motionClock%=.05f;
        var f=new MotionFrame{Sequence=++motionSequence,Round=world.Round,MatchNumber=world.MatchNumber,ExpressionKind=world.Players.FirstOrDefault(p=>p.Customer)?.Expression??0,ExpressionAt=world.Players.FirstOrDefault(p=>p.Customer)?.ExpressionAt??-100,Time=world.Time,Head=new(0,world.SharedHead.Position,world.SharedHead.Rotation)};
        f.Players=world.Players.Where(p=>p.Active&&salon!.Bodies.ContainsKey(p.Id)).Select(p=>{
            var b=salon!.Bodies[p.Id];return new PlayerMotion(p.Id,inputTimelines.GetValueOrDefault(p.Id)?.Ack??0,p.Position,Art.N(b.Velocity),p.Impulse,p.Yaw,p.Pitch,b.IsOnFloor(),b.JumpHeld,p.FreezeUntil,p.GlueUntil,p.DownUntil,p.Customer,p.Standing,p.GlueHead,p.GlueOffset);
        }).ToArray();
        f.Props=world.Props.OrderByDescending(p=>p.Holder!=0||p.Released).ThenBy(p=>p.Id).Take(MotionWire.MaxProps).Select(p=>new MovingPose(p.Id,p.Position,p.Rotation)).ToArray();
        var e=world.Experiment;f.Attention=e.Attention.Stage;f.AttentionStarted=e.Attention.Started;f.AttentionSource=e.Attention.Source;f.MirrorBlocked=e.Attention.Blocked;f.Bracing=world.Players.Any(p=>p.Active&&p.Bracing);f.Flight=e.Flight;f.Leave=e.Leave;f.StableSeconds=e.StableSeconds;f.LandingIssues=e.LandingIssues;
        var bytes=MotionWire.Encode(f);foreach(var l in links.Values.Where(l=>l.Identity.Peer>0&&!l.Lost)){motionBytes+=bytes.Length;NetRpc(l.Identity.Peer,MethodName.MotionSnapshot,bytes);}
    }
    void BeginReplayTransfer()
    {
        if(LegacyNet){Rpc(MethodName.ReplayData,Wire.Encode(playback));return;}
        // Recorded frames own cloned state. Freeze the list, encode off the main
        // thread, and use a stronger window search only for the one-off replay.
        var frames=playback.ToArray();
        replayTransfer=System.Threading.Tasks.Task.Run(()=>Wire.Encode(frames,System.IO.Compression.CompressionLevel.Optimal));
    }
    void PumpReplayTransfer()
    {
        if(replayTransfer is not {IsCompleted:true} task)return;replayTransfer=null;
        if(task.IsCompletedSuccessfully){foreach(var l in links.Values)l.Replay=task.Result;GD.Print($"REPLAY_SENT bytes={task.Result.Length} phase={world.Phase}");}
        else GD.PushError("Replay encoding failed: "+task.Exception?.GetBaseException().Message);
    }
    void PredictLocal(Body body,PlayerState local,Vector2 movement,Buttons buttons,float dt)
    {
        if(pendingMotion is {} frame&&frame.Sequence>lastMotionApplied)
        {
            lastMotionApplied=frame.Sequence;var state=frame.Players.FirstOrDefault(p=>p.Id==localId);
            if(state.Id==localId){
                var before=body.Position;bool roundInit=(predictedRound!=frame.Round||predictedMatch!=frame.MatchNumber)&&world.Phase is Phase.Lobby or Phase.Arrival or Phase.Choice or Phase.Preview;
                predictedRound=frame.Round;predictedMatch=frame.MatchNumber;
                body.RestoreMotion(Art.V(state.Position),Art.V(state.Velocity),state.Grounded,state.JumpHeld);
                if(roundInit){predictedInputs.Clear();cameraOffset=default;cameraReady=false;before=body.Position;networkLog?.WriteLine(JsonSerializer.Serialize(new{kind="round_motion_init",utc=DateTime.UtcNow.ToString("O"),seconds=elapsed,round=frame.Round,customer=state.Customer,position=state.Position}));}
                predictedInputs.RemoveAll(t=>t.Sequence<=state.Ack);predictedImpulse=Art.V(state.Impulse);
                local.FreezeUntil=state.FreezeUntil;local.GlueUntil=state.GlueUntil;local.DownUntil=state.DownUntil;local.Customer=state.Customer;local.Standing=state.Standing;local.GlueHead=state.GlueHead;local.GlueOffset=state.GlueOffset;
                predictedTime=frame.Time+predictedInputs.Count*MotionWire.Step;
                bool anchored=world.Barber(local.Slot).Patches.Any(p=>p.Anchored);
                foreach(var t in predictedInputs){PartyBodyMove(body,local,new(t.Move.X,t.Move.Y),t.Yaw,MotionWire.Step,t.Buttons.HasFlag(Buttons.Jump),predictedImpulse,frame.Time+(t.Sequence-state.Ack)*MotionWire.Step);predictedImpulse*=MathF.Exp(-MotionWire.Step*4);}
                var change=body.Position-before;corrections.Add(change.Length());recentCorrectionMax=Math.Max(recentCorrectionMax,change.Length());
                if(change.Length()>.1f)networkLog?.WriteLine(JsonSerializer.Serialize(new{kind="correction",utc=DateTime.UtcNow.ToString("O"),seconds=elapsed,meters=change.Length(),phase=world.Phase.ToString(),ack=state.Ack,pending=predictedInputs.Count}));
                if(change.Length()>2){cameraOffset=default;cameraReady=false;hardCorrections++;}
                else {cameraPrevious+=change;cameraCurrent+=change;cameraOffset-=change;}
            }
        }
        var tick=new InputTick(++inputSequence,new(movement.X,movement.Y),yaw,pitch,buttons,predictedTime);predictedInputs.Add(tick);
        if(predictedInputs.Count>180)predictedInputs.RemoveAt(0);
        predictedTime=Math.Max(world.Time,predictedTime+dt);
        PartyBodyMove(body,local,movement,yaw,dt,buttons.HasFlag(Buttons.Jump),predictedImpulse,predictedTime);
        predictedImpulse*=MathF.Exp(-dt*4);
        sendClock+=dt;if(sendClock>=.05f&&peer?.GetConnectionStatus()==MultiplayerPeer.ConnectionStatus.Connected)
        {sendClock%=.05f;var bytes=MotionWire.Inputs(predictedInputs);inputBytes+=bytes.Length;NetRpc(1,MethodName.InputBatch,bytes,inputGeneration);}
    }
    void MoveRemoteAuthority(PlayerState p,Body body,float dt)
    {
        if(inputTimelines.TryGetValue(p.Id,out var timeline)&&timeline.ForTick(dt) is {Length:>0} ticks)
        {
            foreach(var t in ticks){simulation.Inputs[p.Id]=(t.Move,t.Yaw,t.Pitch,t.Buttons);simulation.InputTimes[p.Id]=t.Time>0?t.Time:world.Time;PartyBodyMove(body,p,new(t.Move.X,t.Move.Y),t.Yaw,MotionWire.Step,t.Buttons.HasFlag(Buttons.Jump),Art.V(p.Impulse),world.Time);}
        }
        else
        {
            // No unacknowledged movement is invented between received input ticks.
            // A prolonged outage still releases tools / bracing via the usual timeout.
            if(inputReceived.TryGetValue(p.Id,out float last)&&elapsed-last>.3f){simulation.Inputs.Remove(p.Id);PartyBodyMove(body,p,Vector2.Zero,p.Yaw,dt,false,Art.V(p.Impulse),world.Time);}
        }
        p.Position=Art.N(body.Position);
    }
    void SyncRemoteColliders()
    {
        if(authority||LegacyNet||salon==null||pendingMotion is not {} frame||frame.Round!=world.Round)return;
        salon.SyncMotionColliders(frame,localId);
    }
    bool OrdinaryCamera=>!args.Keys.Any(k=>k.EndsWith("review",StringComparison.Ordinal)||k.EndsWith("-view",StringComparison.Ordinal))&&world.Phase is not (Phase.Highlight or Phase.Validation);
    void CameraPhysics(Vector2 movement,float dt)
    {
        if(salon==null||!salon.Bodies.TryGetValue(localId,out var body))return;
        if(!cameraReady){cameraPrevious=cameraCurrent=body.Position;cameraReady=true;}
        else{cameraPrevious=cameraCurrent;cameraCurrent=body.Position;}
        if(args.ContainsKey("net-walk")&&world.Phase==Phase.Build&&phaseClock>3&&movement.Length()>.1f)
        {
            var direction=new Vector3(movement.X,0,movement.Y).Rotated(Vector3.Up,yaw).Normalized();
            if(metricReady){walkTicks++;if((body.Position-metricPrevious).Dot(direction)<-.002f)reverseTicks++;}metricPrevious=body.Position;metricReady=true;
        }else metricReady=false;
    }
    void RenderNetwork(float dt)
    {
        if(salon==null||!running)return;
        if(authority&&!LegacyNet&&world.Phase is not (Phase.Highlight or Phase.Validation))salon.RenderCosmetics(world,localId,world.Time+(float)Engine.GetPhysicsInterpolationFraction()*MotionWire.Step);
        if(!NetworkFrozen&&!authority&&!LegacyNet&&motionBuffer.Advance(dt) is {} sample&&world.Phase is not (Phase.Highlight or Phase.Validation))
            salon.RenderMotion(world,localId,sample.A,sample.B,sample.Blend);
        if(cameraReady&&OrdinaryCamera&&!args.ContainsKey("legacy-camera"))
        {
            cameraOffset*=MathF.Exp(-dt*12);
            salon.Camera.Position=cameraPrevious.Lerp(cameraCurrent,(float)Engine.GetPhysicsInterpolationFraction())+cameraOffset+Vector3.Up*1.7f;
            salon.Camera.Rotation=new(pitch,yaw,0);
        }
    }
    void NetWalk(ref Vector2 movement,ref Buttons buttons)
    {
        if(!args.ContainsKey("net-walk")||world.Phase!=Phase.Build||phaseClock<3||world.Remaining<20)return;
        yaw=0;pitch=0;movement=new(MathF.Sin(phaseClock*1.7f)*.45f,0);buttons=Buttons.None;
    }
    static double Percentile(List<double> data,double p)=>data.Count==0?0:data.OrderBy(x=>x).ElementAt(Math.Min(data.Count-1,(int)((data.Count-1)*p)));
    void WriteNetReport()
    {
        if(reportPath.Length==0)return;
        var report=new{mode,legacy=LegacyNet,headTravel,headMaxStep,headMaxSpeed,headMotionSamples,physicsP50Ms=Percentile(physicsCosts,.5),physicsP95Ms=Percentile(physicsCosts,.95),physicsP99Ms=Percentile(physicsCosts,.99),renderP95Ms=Percentile(renderCosts,.95),renderP99Ms=Percentile(renderCosts,.99),correctionP95=Percentile(corrections,.95),correctionMax=corrections.Count>0?corrections.Max():0,hardCorrections,reverseTicks,walkTicks,motionGapP95Ms=Percentile(motionGaps,.95),motionGapMaxMs=motionGaps.Count>0?motionGaps.Max():0,stateBytes,motionBytes,inputBytes,seconds=elapsed,interpolationMs=motionBuffer.Delay*1000,underruns=motionBuffer.Underruns};
        File.WriteAllText(Path.ChangeExtension(reportPath,"net.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
    }
    void NetworkTelemetry(float dt)
    {
        networkLogClock+=dt;if(networkLogClock<1)return;networkLogClock=0;
        double rtt=0;if(!authority&&peer?.GetConnectionStatus()==MultiplayerPeer.ConnectionStatus.Connected)rtt=peer.GetPeer(1).GetStatistic(ENetPacketPeer.PeerStatistic.RoundTripTime);
        var sample=new{utc=DateTime.UtcNow.ToString("O"),seconds=elapsed,mode,fps=Engine.GetFramesPerSecond(),rttMs=rtt,physicsP95Ms=Percentile(physicsCosts.TakeLast(120).ToList(),.95),frameP95Ms=Percentile(renderCosts.TakeLast(120).ToList(),.95),motionAgeMs=(elapsed-lastMotionArrival)*1000,interpolationMs=motionBuffer.Delay*1000,correctionMaxMeters=recentCorrectionMax,hardCorrections,stateBytes,motionBytes};recentCorrectionMax=0;
        try{networkLog?.WriteLine(JsonSerializer.Serialize(sample));}catch(IOException){}
        RecoveryTelemetry();
        hud.NetworkStatus=$"FPS {sample.fps} · RTT {(NetNow-lastServer>2&&!authority?"stale":rtt.ToString("0"))} ms\nFrame P95 {sample.frameP95Ms:0.0} ms · Motion buffer {sample.interpolationMs:0} ms";
        // Keep long play sessions' recent telemetry live after reaching the cap.
        if(physicsCosts.Count>=36000)physicsCosts.RemoveRange(0,120);
        if(renderCosts.Count>=36000)renderCosts.RemoveRange(0,120);
        if(corrections.Count>=36000)corrections.RemoveRange(0,Math.Max(120,corrections.Count-36000));
        if(motionGaps.Count>=36000)motionGaps.RemoveRange(0,Math.Max(120,motionGaps.Count-36000));
    }
}
