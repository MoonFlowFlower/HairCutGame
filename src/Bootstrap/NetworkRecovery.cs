using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using NVector=System.Numerics.Vector3;

namespace Hairball;
public partial class Main
{
    const int NetProtocol=25;
    sealed class Link
    {
        public RecoveryIdentity Identity=new();
        public double Heard,Progress,Applied,Deadline,LastSend;
        public double InputArrival,InputAccepted;
        public long InputPackets,AcceptedInputPackets;
        public bool Ready,Lost;
        public ChunkSend? Transfer;
        public byte[]? Replay;
        public int AppliedRevision=-1;
    }
    readonly Dictionary<int,Link> links=new(); // stable actor id -> connection
    readonly Dictionary<int,double> pendingPeers=new();
    readonly Dictionary<ulong,double> transientTransports=new();
    double transportSweep;
    RecoveryWindow recovery=new();
    string roomId="",resumeToken="",joinAddress="",failureReason="";
    int joinPort,connectionEpoch,nextActor=2,controlGeneration=1,inputGeneration=1;
    bool quitAfterLeave;
    long transferId,lastAppliedTransfer;
    int lastAppliedRevision;
    ChunkReceive? incoming;
    ConnectionHealth health=ConnectionHealth.Normal;
    double clientDeadline,nextConnect,attemptStarted,lastHello,lastPulse,lastServer,lastState,lastProgress,lastRawMotion,leaveAt;
    int retryIndex,recoveryCount,resumeCount,clientAttempt;
    bool clientReady,serverPaused,leaving,transportClosing,controlsReleased;
    double serverPauseLeft;
    string recoverySignature="";
    double chunkPumpTime,chunkCredit;int chunkTurn;
    double NetNow=>Time.GetTicksMsec()/1000.0;
    bool Networked=>mode is "host" or "join";
    bool NetworkFrozen=>Networked&&(fixtureFinishing||leaving||(authority?recovery.Paused:!clientReady||serverPaused||health is ConnectionHealth.Reconnecting or ConnectionHealth.Failed or ConnectionHealth.Synchronizing or ConnectionHealth.Waiting));
    int SenderActor()=>links.Values.FirstOrDefault(l=>l.Identity.Peer==Multiplayer.GetRemoteSenderId())?.Identity.Actor??-1;
    int PeerFor(int actor)=>links.TryGetValue(actor,out var l)?l.Identity.Peer:0;
    bool CanAct(int actor,int generation)=>generation==controlGeneration&&CanAct(actor);
    bool CanAct(int actor)=>!NetworkFrozen&&links.TryGetValue(actor,out var l)&&l.Ready&&!l.Lost;
    void NetEvent(string kind,object detail)
    {
        string line=JsonSerializer.Serialize(new{kind,utc=DateTime.UtcNow.ToString("O"),monotonicSeconds=NetNow,mode,epoch=connectionEpoch,detail});
        try{networkLog?.WriteLine(line);}catch(System.IO.IOException){ }
        GD.Print("NET_EVENT "+line);
        if(authority)try{experimentWriter?.WriteLine(line);}catch(System.IO.IOException){ }
    }
    void SetHealth(ConnectionHealth value,string reason)
    {if(health==value)return;health=value;NetEvent("connection_state",new{state=value.ToString(),reason});}
    Error NetRpc(int target,StringName method,params Variant[] values)
    {
        if(peer==null||peer.GetConnectionStatus()!=MultiplayerPeer.ConnectionStatus.Connected)return Error.Unconfigured;
        if(target!=1&&!Multiplayer.GetPeers().Contains(target))return Error.DoesNotExist;
        var error=RpcId(target,method,values);
        if(error!=Error.Ok){NetEvent("rpc_failed",new{method=method.ToString(),error=error.ToString()});
            if(authority){var l=links.Values.FirstOrDefault(l=>l.Identity.Peer==target);if(l!=null)LoseLink(l,"rpc_send_failed");}
            else BeginClientRecovery("rpc_send_failed");}
        return error;
    }
    void InitRecovery(string address,int port)
    {
        links.Clear();pendingPeers.Clear();transientTransports.Clear();transportSweep=0;recovery=new();incoming=null;transferId=lastAppliedTransfer=0;
        controlGeneration=inputGeneration=1;clientAttempt=1;
        roomId=authority?Guid.NewGuid().ToString("N"):"";resumeToken="";connectionEpoch=0;nextActor=2;
        joinAddress=address;joinPort=port;failureReason="";health=ConnectionHealth.Normal;
        clientReady=authority;serverPaused=leaving=false;clientDeadline=nextConnect=leaveAt=0;retryIndex=recoveryCount=resumeCount=0;
        attemptStarted=lastServer=lastState=lastProgress=lastRawMotion=NetNow;lastHello=lastPulse=0;controlsReleased=true;
        NetEvent("session_network",new{protocol=NetProtocol,transport="ENet",chunkPayload=900,window=32});
        hud.NetworkContinue=()=>ContinueNetwork("host_continue");hud.NetworkRetry=RetryNetwork;hud.NetworkLeave=RequestLeave;
        if(!authority){clientDeadline=NetNow+30;SetHealth(ConnectionHealth.Synchronizing,"initial_join");}
    }
    void ConnectedRecovery()
    {
        // A successful transport starts the welcome phase. Dialling a host that
        // is still loading must not consume this phase's no-progress window.
        attemptStarted=lastServer=lastState=lastProgress=lastRawMotion=NetNow;
        SendHello();
    }
    void SendHello()
    {
        lastHello=NetNow;
        NetRpc(1,MethodName.RecoveryHello,int.Parse(args.GetValueOrDefault("test-protocol",NetProtocol.ToString())),$"{roomId}|{clientAttempt}",resumeToken);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Unreliable,TransferChannel=5)]
    public void RecoveryHello(int version,string room,string token)
    {
        if(!authority||!running||leaving)return;int remote=Multiplayer.GetRemoteSenderId();
        if(version!=NetProtocol){NetRpc(remote,MethodName.RecoveryReject,"version");return;}
        int separator=room.LastIndexOf('|');
        if(separator<0||!int.TryParse(room[(separator+1)..],out int attempt)||attempt<1){NetRpc(remote,MethodName.RecoveryReject,"version");return;}
        room=room[..separator];
        var existing=links.Values.FirstOrDefault(l=>l.Identity.Peer==remote);
        if(existing!=null){Welcome(existing);return;}
        if(!pendingPeers.ContainsKey(remote)||attempt<1)return;
        Link? link=null;
        if(token.Length>0)
        {
            link=links.Values.FirstOrDefault(l=>l.Identity.Matches(token));
            if(link==null){NetRpc(remote,MethodName.RecoveryReject,"expired");return;}
            string denied=link.Identity.ResumeError(roomId,room,token,NetNow,link.Deadline,link.Identity.Peer!=0&&!link.Lost&&NetNow-link.Heard<3,attempt);
            if(denied.Length>0){NetRpc(remote,MethodName.RecoveryReject,denied);return;}
            if(link.Identity.Peer>0)DisconnectTransport(link.Identity.Peer,"superseded_attempt");
            NetEvent("resume_accepted",new{actor=link.Identity.Actor});
        }
        else
        {
            if(links.Count>=3){NetRpc(remote,MethodName.RecoveryReject,"full");return;}
            int actor=nextActor++;
            if(simulation.AddPlayer(actor,$"Barber {links.Count+2}")==null){NetRpc(remote,MethodName.RecoveryReject,"full");return;}
            link=new(){Identity=new(){Actor=actor}};links[actor]=link;
            GD.Print($"PEER_JOINED {actor} slot={world.Player(actor)!.Slot}");
        }
        pendingPeers.Remove(remote);link.Identity.Peer=remote;link.Identity.Epoch++;link.Identity.Attempt=attempt;
        link.Heard=link.Progress=link.Applied=NetNow;link.Ready=false;link.Transfer=null;
        inputTimelines.Remove(link.Identity.Actor);simulation.Inputs.Remove(link.Identity.Actor);
        pendingWorld.Remove(link.Identity.Actor);completionAcks.Remove(link.Identity.Actor);
        if(salon!=null&&world.Player(link.Identity.Actor) is {} p)
        {
            if(world.Players.Any(other=>other.Active&&other.Id!=p.Id&&NVector.Distance(other.Position,p.Position)<.7f))p.Position=Session.Spawn(p.Slot);
            if(salon.Bodies.TryGetValue(p.Id,out var body)){body.Position=Art.V(p.Position);body.Velocity=Vector3.Zero;}
        }
        Welcome(link);
        if((world.Phase is Phase.Results or Phase.Highlight or Phase.Complete)&&playback.Count>0&&replayTransfer==null)BeginReplayTransfer();
        NetEvent("peer_bound",new{room=roomId,actor=link.Identity.Actor,peer=remote,epoch=link.Identity.Epoch,attempt});
    }
    void Welcome(Link link)=>NetRpc(link.Identity.Peer,MethodName.RecoveryWelcome,roomId,link.Identity.Token,link.Identity.Actor,link.Identity.Epoch);
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Unreliable,TransferChannel=5)]
    public void RecoveryWelcome(string room,string token,int actor,int epoch)
    {
        if(authority||leaving||health==ConnectionHealth.Failed)return;
        if(roomId.Length>0&&roomId!=room){FailRecovery("room_closed");return;}
        lastServer=NetNow;retryIndex=0;nextConnect=NetNow;
        if(connectionEpoch!=epoch||resumeToken.Length==0)
        {
            roomId=room;resumeToken=token;localId=actor;connectionEpoch=epoch;incoming=null;lastAppliedTransfer=0;
            NetEvent("peer_bound",new{room=roomId,actor=localId,peer=Multiplayer.GetUniqueId(),epoch=connectionEpoch});
            ResetPredictionForRecovery();lastState=lastProgress=lastRawMotion=NetNow;clientReady=false;
            SetHealth(ConnectionHealth.Synchronizing,"welcome");GD.Print($"CLIENT_CONNECTED {localId}");
        }
        NetRpc(1,MethodName.RecoveryWelcomeAck,epoch);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Unreliable,TransferChannel=5)]
    public void RecoveryWelcomeAck(int epoch)
    {
        if(!authority||!links.TryGetValue(SenderActor(),out var l)||l.Identity.Epoch!=epoch)return;
        l.Heard=NetNow;if(l.Transfer==null&&!l.Ready)QueueWorld(l);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Unreliable,TransferChannel=5)]
    public void RecoveryReject(string reason){if(!authority){if(reason=="stale_attempt")BeginClientRecovery(reason);else FailRecovery(reason);}}
    void QueueWorld(Link l)
    {
        if(l.Identity.Peer==0||l.Transfer!=null)return;
        var data=Wire.Encode(world);l.Transfer=new(++transferId,0,world.Revision,data);l.Progress=NetNow;l.LastSend=NetNow;
        stateBytes+=data.Length;packetBytes=data.Length;
        if(smoke&&world.Phase==Phase.Complete)GD.Print("HOST_FINAL_HASH "+Convert.ToHexString(SHA256.HashData(data))[..16]);
    }
    void PumpChunks()
    {
        if(!authority)return;
        double now=NetNow;chunkCredit=Math.Min(28800,chunkCredit+Math.Max(0,now-chunkPumpTime)*192000);chunkPumpTime=now;
        var ordered=links.Values.ToArray();if(ordered.Length>0)chunkTurn=(chunkTurn+1)%ordered.Length;
        foreach(var l in ordered.Skip(chunkTurn).Concat(ordered.Take(chunkTurn)))
        {
            if(l.Identity.Peer==0||!Multiplayer.GetPeers().Contains(l.Identity.Peer))continue;
            if(l.Transfer==null)
            {
                if(l.Replay!=null&&l.Ready){l.Transfer=new(++transferId,1,world.Revision,l.Replay);l.Replay=null;l.Progress=NetNow;}
                else if(l.Ready&&NetNow-l.LastSend>=.2)QueueWorld(l);
            }
            if(l.Transfer is not {} tx)continue;
            double rtt=peer?.GetPeer(l.Identity.Peer).GetStatistic(ENetPacketPeer.PeerStatistic.RoundTripTime)??250;
            foreach(var part in tx.Pump(Math.Min(8,(int)(chunkCredit/900)),now,Math.Clamp(rtt*.002,.35,1)))
            {
                chunkCredit-=900;NetRpc(l.Identity.Peer,MethodName.RecoveryChunk,l.Identity.Epoch,tx.Id,tx.Kind,tx.Revision,tx.Data.Length,tx.Hash,part.Index,part.Data);
            }
        }
    }
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Unreliable,TransferChannel=1)]
    public void RecoveryChunk(int epoch,long id,int kind,int revision,int length,byte[] hash,int index,byte[] bytes)
    {
        if(authority||epoch!=connectionEpoch||leaving||health==ConnectionHealth.Failed)return;
        lastServer=NetNow;
        if(id<=lastAppliedTransfer){NetRpc(1,MethodName.RecoveryApplied,epoch,id,revision);return;}
        try
        {
            if(incoming==null||id>incoming.Id)incoming=new(id,kind,revision,length,hash);
            if(incoming.Id!=id)return;
            if(incoming.Kind!=kind||incoming.Revision!=revision||incoming.Length!=length||!incoming.Hash.AsSpan().SequenceEqual(hash))throw new ArgumentException("Inconsistent transfer metadata");
            if(incoming.Add(index,bytes))lastProgress=NetNow;
            NetRpc(1,MethodName.RecoveryChunkAck,epoch,id,index);
            if(!incoming.Complete)return;
            var data=incoming.Finish();incoming=null;
            if(kind==0)
            {
                ApplySnapshot(data);lastState=NetNow;
                if(!clientReady)
                {
                    ResetPredictionForRecovery();clientReady=true;controlsReleased=false;
                    if(salon!=null){salon.Sync(world,localId,0);if(world.Player(localId) is {} p&&salon.Bodies.TryGetValue(localId,out var body)){body.RestoreMotion(Art.V(p.Position),Vector3.Zero,true,false);if(recoveryCount==0){yaw=p.Yaw;pitch=p.Pitch;}}}
                    if(clientDeadline>0&&recoveryCount>0){resumeCount++;NetEvent("recovery_ready",new{actor=localId,revision,remaining=clientDeadline-NetNow});}
                    clientDeadline=0;retryIndex=0;SetHealth(ConnectionHealth.Normal,"snapshot_applied");
                }
            }
            else ReplayData(data);
            lastAppliedTransfer=id;lastAppliedRevision=revision;NetRpc(1,MethodName.RecoveryApplied,epoch,id,revision);
        }
        catch(Exception e) when(e is ArgumentException or System.IO.InvalidDataException or JsonException or System.IO.IOException)
        {NetEvent("transfer_rejected",new{reason=e.GetType().Name,id});BeginClientRecovery("invalid_state");}
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Unreliable,TransferChannel=5)]
    public void RecoveryChunkAck(int epoch,long id,int index)
    {
        if(!authority||!links.TryGetValue(SenderActor(),out var l)||epoch!=l.Identity.Epoch)return;
        l.Heard=NetNow;if(l.Transfer is {} tx&&tx.Id==id&&tx.Ack(index))l.Progress=NetNow;
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Unreliable,TransferChannel=5)]
    public void RecoveryApplied(int epoch,long id,int revision)
    {
        if(!authority||!links.TryGetValue(SenderActor(),out var l)||epoch!=l.Identity.Epoch)return;
        l.Heard=NetNow;
        if(l.Lost&&l.Deadline>0&&NetNow>=l.Deadline)return;
        if(l.Transfer is not {} tx||tx.Id!=id||tx.Revision!=revision)return;
        if(tx.Kind==0){l.Applied=NetNow;l.AppliedRevision=revision;l.Ready=true;l.Lost=false;l.Deadline=0;}
        l.Transfer=null;l.Progress=NetNow;
    }
    // Repeated cumulative receipt prevents a single lost application ack from stopping the window.
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Unreliable,TransferChannel=5)]
    public void RecoveryPulse(int epoch,long applied,int revision,bool unhealthy,long transfer,int[] received)
    {
        if(!authority||!links.TryGetValue(SenderActor(),out var l)||epoch!=l.Identity.Epoch)return;
        l.Heard=NetNow;RecoveryApplied(epoch,applied,revision);
        if(received.Length<=33&&l.Transfer is {} tx&&tx.Id==transfer)
            foreach(int i in received){int before=tx.Outstanding;if(i<0&&i!=int.MinValue)tx.AckPrefix(-i-1);else tx.Ack(i);if(tx.Outstanding<before)l.Progress=NetNow;}
        if(unhealthy&&!l.Lost)LoseLink(l,"client_health");
    }
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Unreliable,TransferChannel=5)]
    public void RecoveryStatus(int epoch,bool paused,double seconds,bool ready,int generation)
    {
        if(authority||epoch!=connectionEpoch)return;lastServer=NetNow;serverPaused=paused;serverPauseLeft=seconds;
        if(inputGeneration!=generation){inputGeneration=generation;predictedInputs.Clear();pendingMotion=null;controlsReleased=false;}
        if(clientReady&&health is not (ConnectionHealth.Failed or ConnectionHealth.Reconnecting))
        {if(paused)SetHealth(ConnectionHealth.Waiting,"team_wait");else if(health==ConnectionHealth.Waiting)SetHealth(ConnectionHealth.Normal,"team_continue");}
    }
    void LoseLink(Link l,string reason)
    {
        if(l.Lost)return;l.Lost=true;l.Ready=false;l.Deadline=recovery.Open?recovery.Deadline:NetNow+30;
        if(recovery.Begin(NetNow)){recoveryCount++;NetEvent("pause_begin",new{reason,paused=recovery.Paused,deadline=recovery.Deadline,gameTime=world.Time});}
        l.Deadline=recovery.Deadline;ReleaseActions();
        if(!recovery.Paused)SuspendLink(l);
        NetEvent("peer_wait",new{actor=l.Identity.Actor,reason,deadline=l.Deadline});
        int oldPeer=l.Identity.Peer;l.Identity.Peer=0;l.Transfer=null;
        if(oldPeer>0)DisconnectTransport(oldPeer,reason);
    }
    void ReleaseActions()
    {
        controlGeneration++;simulation.Inputs.Clear();inputTimelines.Clear();predictedInputs.Clear();controlsReleased=false;
        foreach(var p in world.Players.Where(p=>p.Active)){simulation.EndStroke(p.Id);p.Bracing=p.UsingBlower=false;}
    }
    void SuspendLink(Link l)
    {
        if(world.Player(l.Identity.Actor) is not {} p)return;
        simulation.Drop(p);p.Bracing=false;p.UsingBlower=false;simulation.Inputs.Remove(p.Id);simulation.EndStroke(p.Id);
        if(p.CarryLadder>=0){var ladder=world.Ladders.FirstOrDefault(x=>x.Holder==p.Id);if(ladder!=null){ladder.Holder=0;ladder.Position=new(0,0,4);}p.CarryLadder=-1;}

        simulation.CustomerDisconnected(p.Id);p.NetworkAway=true;world.Revision++;
    }
    void ContinueNetwork(string reason)
    {
        if(!authority||!recovery.Open)return;recovery.Continue();foreach(var l in links.Values.Where(l=>l.Lost))SuspendLink(l);
        NetEvent("pause_continue",new{reason,gameTime=world.Time});
    }
    void BeginClientRecovery(string reason)
    {
        if(authority||leaving||health==ConnectionHealth.Failed)return;
        if(clientDeadline<=0){clientDeadline=NetNow+30;recoveryCount++;NetEvent("recovery_begin",new{reason,deadline=clientDeadline});}
        clientReady=false;SetHealth(ConnectionHealth.Reconnecting,reason);
        ResetPredictionForRecovery();CloseTransport();nextConnect=Math.Max(nextConnect,NetNow);
    }
    void CloseTransport()
    {
        transportClosing=true;var old=peer;old?.Close();peer=null;Multiplayer.MultiplayerPeer=new OfflineMultiplayerPeer();old?.Dispose();transportClosing=false;
    }
    void DisconnectedRecovery(string reason)
    {
        if(transportClosing||leaving||!running)return;
        if((smoke||SculptStress)&&reported&&!args.ContainsKey("recovery-test")){QuitGracefully();return;}
        BeginClientRecovery(reason);
    }
    void ResetPredictionForRecovery()
    {
        predictedInputs.Clear();pendingMotion=null;motionBuffer.Clear();hud.LiveMotion=null;
        inputSequence=lastMotionApplied=0;predictedImpulse=cameraOffset=default;cameraReady=false;incoming=null;
    }
    void FailRecovery(string reason)
    {
        failureReason=reason;clientReady=false;CloseTransport();SetHealth(ConnectionHealth.Failed,reason);
        NetEvent("recovery_failed",new{reason});
    }
    void RetryNetwork()
    {
        if(authority||leaving)return;
        if(health is ConnectionHealth.Reconnecting or ConnectionHealth.Synchronizing)
        {
            NetEvent("manual_retry_requested",new{deadline=clientDeadline});
            BeginClientRecovery("manual_retry");nextConnect=NetNow;return;
        }
        if(health!=ConnectionHealth.Failed)return;
        // A failed reservation cannot claim an expired actor; retry is a normal join.
        resumeToken=roomId="";connectionEpoch=0;clientDeadline=NetNow+30;nextConnect=NetNow;retryIndex=0;
        SetHealth(ConnectionHealth.Reconnecting,"manual_retry");
    }
    public override void _Notification(int what)
    {
        if(what!=NotificationWMCloseRequest)return;
        if(running&&Networked){quitAfterLeave=true;RequestLeave();}else QuitGracefully();
    }
    void RequestLeave()
    {
        if(!Networked){Stop("Session closed");return;}if(leaving)return;
        leaving=true;leaveAt=NetNow+.35;SetHealth(ConnectionHealth.Leaving,"user_leave");
        if(authority)foreach(var l in links.Values.Where(l=>l.Identity.Peer>0))NetRpc(l.Identity.Peer,MethodName.RecoveryClosed);
        else NetRpc(1,MethodName.RecoveryLeave,connectionEpoch);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel=5)]
    public void RecoveryLeave(int epoch)
    {
        if(!authority||!links.TryGetValue(SenderActor(),out var l)||l.Identity.Epoch!=epoch)return;
        NetEvent("peer_left_intentionally",new{actor=l.Identity.Actor});RemoveLink(l);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel=5)]
    public void RecoveryClosed(){if(!authority)FailRecovery("room_closed");}
    void RemoveLink(Link l)
    {
        int actor=l.Identity.Actor;links.Remove(actor);simulation.RemovePlayer(actor);inputTimelines.Remove(actor);pendingWorld.Remove(actor);
        if(l.Identity.Peer>0)DisconnectTransport(l.Identity.Peer,"seat_removed");GD.Print($"PEER_LEFT {actor}");
    }
    void DisconnectTransport(int remote,string reason)
    {NetEvent("transport_close_requested",new{peer=remote,reason});((SceneMultiplayer)Multiplayer).DisconnectPeer(remote);}
    void SweepUnfinishedHandshakes(double now)
    {
        if(peer==null||now-transportSweep<1)return;transportSweep=now;
        var transports=peer.Host.GetPeers();var seen=new HashSet<ulong>();
        foreach(var transport in transports)
        {
            ulong id=transport.GetInstanceId();seen.Add(id);var state=transport.GetState();
            // These peers have never entered ENetMultiplayerPeer's connected-peer map.
            // Registered peers must always be removed through SceneMultiplayer instead.
            if(state is ENetPacketPeer.PeerState.Connecting or ENetPacketPeer.PeerState.AcknowledgingConnect)
            {
                if(!transientTransports.TryGetValue(id,out double began))transientTransports[id]=now;
                else if(now-began>=8){transport.PeerDisconnectNow();transientTransports.Remove(id);NetEvent("unfinished_handshake_released",new{state=state.ToString(),age=now-began});}
            }
            else transientTransports.Remove(id);
        }
        foreach(ulong id in transientTransports.Keys.Where(id=>!seen.Contains(id)).ToArray())transientTransports.Remove(id);
    }
    void RecoveryTick()
    {
        if(!Networked)return;double now=NetNow;
        if(leaving){if(now>=leaveAt){Stop("Session closed");if(quitAfterLeave)QuitGracefully();}return;}
        if(authority)
        {
            SweepUnfinishedHandshakes(now);
            foreach(var remote in pendingPeers.Where(p=>now-p.Value>8).Select(p=>p.Key).ToArray()){pendingPeers.Remove(remote);if(!links.Values.Any(l=>l.Identity.Peer==remote))DisconnectTransport(remote,"hello_timeout");}
            foreach(var l in links.Values.ToArray())
            {
                if(l.Identity.Peer>0&&(now-l.Heard>3||l.Transfer!=null&&now-l.Progress>3))
                {
                    if(!l.Lost)LoseLink(l,"transport_or_state_stalled");
                    else{int stalled=l.Identity.Peer;l.Identity.Peer=0;l.Transfer=null;DisconnectTransport(stalled,"resync_progress_timeout");}
                }
                if(l.Lost&&now>=l.Deadline){NetEvent("reservation_expired",new{actor=l.Identity.Actor});RemoveLink(l);continue;}
                if(!l.Lost&&world.Player(l.Identity.Actor) is {NetworkAway:true} p){p.NetworkAway=false;world.Revision++;}
            }
            if(recovery.Open&&(links.Values.All(l=>!l.Lost&&l.Ready)||recovery.Expired(now)))
            {NetEvent("pause_end",new{gameTime=world.Time});recovery.Finish(now);controlGeneration++;controlsReleased=false;}
            if(now-lastPulse>.25)
            {
                lastPulse=now;
                foreach(var l in links.Values.Where(l=>l.Identity.Peer>0))NetRpc(l.Identity.Peer,MethodName.RecoveryStatus,l.Identity.Epoch,recovery.Paused,Math.Max(0,recovery.Deadline-now),l.Ready,controlGeneration);
            }
            PumpChunks();
        }
        else if(health!=ConnectionHealth.Failed)
        {
            if(clientDeadline>0&&now>=clientDeadline){FailRecovery("timeout");}
            else if(peer==null&&now>=nextConnect)
            {
                clientAttempt++;peer=new ENetMultiplayerPeer();var err=peer.CreateClient(joinAddress,joinPort);attemptStarted=lastServer=lastState=lastProgress=lastRawMotion=now;connectionEpoch=0;
                if(err==Error.Ok)Multiplayer.MultiplayerPeer=peer;else CloseTransport();
                double delay=new[]{1,2,4,5}[Math.Min(retryIndex++,3)];nextConnect=now+delay+GD.RandRange(0,.15);
                NetEvent("reconnect_attempt",new{attempt=clientAttempt,backoffStep=retryIndex,error=err.ToString()});
            }
            else if(peer!=null)
            {
                if(peer.GetConnectionStatus()==MultiplayerPeer.ConnectionStatus.Connected)
                {
                    if(!clientReady&&now-lastHello>.5)SendHello();
                    if(connectionEpoch>0&&now-lastPulse>.25)
                    {
                        lastPulse=now;
                        // A compact rolling ack window stays below the control datagram budget.
                        NetRpc(1,MethodName.RecoveryPulse,connectionEpoch,lastAppliedTransfer,lastAppliedRevision,
                            now-lastProgress>3&&now-lastState>3||clientReady&&!serverPaused&&now-lastRawMotion>3,
                            incoming?.Id??0,incoming?.AckWindow()??Array.Empty<int>());
                    }
                    bool stalled=now-lastServer>3||now-lastProgress>3&&now-lastState>3||clientReady&&!serverPaused&&now-lastRawMotion>3;
                    if(stalled)BeginClientRecovery("no_progress");
                    else if(clientReady&&!serverPaused)
                        SetHealth(now-lastRawMotion>1.5||now-lastState>2?ConnectionHealth.Unstable:ConnectionHealth.Normal,"health_sample");
                }
                else if(now-attemptStarted>=Math.Max(1,nextConnect-attemptStarted)){CloseTransport();}
            }
        }
        UpdateRecoveryHud();RecoveryFixtureTick();
    }
    void UpdateRecoveryHud()
    {
        var state=authority?(recovery.Paused?ConnectionHealth.Waiting:ConnectionHealth.Normal):health;
        double left=authority?Math.Max(0,recovery.Deadline-NetNow):clientDeadline>0?Math.Max(0,clientDeadline-NetNow):serverPauseLeft;
        hud.ShowNetworkRecovery(state,(int)Math.Ceiling(left),authority,failureReason);
        if(NetworkFrozen)Input.MouseMode=Input.MouseModeEnum.Visible;
        string signature=$"{state}:{(NetworkFrozen?1:0)}";
        if(signature!=recoverySignature){if(state==ConnectionHealth.Normal&&recoverySignature.Length>0)Input.MouseMode=Input.MouseModeEnum.Captured;recoverySignature=signature;}
    }
}


