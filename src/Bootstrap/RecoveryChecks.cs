using Godot;
using Hairball.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;

namespace Hairball;
public partial class Main
{
    bool fixtureFinishing,fixtureSent;
    double fixtureFinishAt;
    double fixtureEndSend;
    string fixtureHash="";
    int fixtureMatches;
    double fixturePeakMemory;
    byte[]? frozenFacts;
    bool pauseVerified=true;
    bool testedCredential,testedEpoch,testRetried,testedActiveRetry;
    double testRetryAt;
    readonly System.Collections.Generic.HashSet<ConnectionHealth> capturedRecoveryStates=new();
    void RecoveryTelemetry()
    {
        if(!Networked)return;
        var peers=links.Values.Select(l=>{
            var transport=l.Identity.Peer>0&&Multiplayer.GetPeers().Contains(l.Identity.Peer)?peer?.GetPeer(l.Identity.Peer):null;
            return new{actor=l.Identity.Actor,peer=l.Identity.Peer,epoch=l.Identity.Epoch,l.Ready,l.Lost,heardAge=NetNow-l.Heard,progressAge=NetNow-l.Progress,stateAge=NetNow-l.Applied,inputArrivalAge=l.InputArrival>0?(double?)(NetNow-l.InputArrival):null,inputAcceptedAge=l.InputAccepted>0?(double?)(NetNow-l.InputAccepted):null,l.InputPackets,l.AcceptedInputPackets,inputAck=inputTimelines.GetValueOrDefault(l.Identity.Actor)?.Ack,pending=l.Transfer?.Id??0,outstanding=l.Transfer?.Outstanding??0,bytes=l.Transfer?.Data.Length??0,rttStale=transport==null||NetNow-l.Heard>2,rtt=transport?.GetStatistic(ENetPacketPeer.PeerStatistic.RoundTripTime),loss=transport?.GetStatistic(ENetPacketPeer.PeerStatistic.PacketLoss)/65536.0};
        }).ToArray();
        var transportStates=authority&&peer!=null?peer.Host.GetPeers().GroupBy(p=>p.GetState().ToString()).ToDictionary(g=>g.Key,g=>g.Count()):null;
        try{networkLog?.WriteLine(JsonSerializer.Serialize(new{kind="health",utc=DateTime.UtcNow.ToString("O"),monotonicSeconds=NetNow,health=health.ToString(),paused=recovery.Paused||serverPaused,rawMotionAge=NetNow-lastRawMotion,acceptedMotionAge=lastMotionArrival>0?(double?)(elapsed-lastMotionArrival):null,acceptedMotionSequence=pendingMotion?.Sequence,appliedMotionSequence=lastMotionApplied,decodedStateAge=NetNow-lastState,receiveProgressAge=NetNow-lastProgress,rttStale=!authority&&NetNow-lastServer>2,transportStates,peers}));}catch(IOException){}
    }
    void RecoveryFixtureTick()
    {
        if(!args.ContainsKey("recovery-test"))return;
        if(DisplayServer.GetName()!="headless"&&hud.RecoveryVisible&&capturedRecoveryStates.Add(health)&&reportPath.Length>0)
        {
            string screenshot=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(reportPath))!,"recovery-"+health+"-"+L.Locale+".png");
            CaptureRecoveryAfterDraw(screenshot);
        }
        using(var process=System.Diagnostics.Process.GetCurrentProcess())fixturePeakMemory=Math.Max(fixturePeakMemory,process.WorkingSet64);
        if(authority)
        {
            if(args.ContainsKey("test-host-close")&&elapsed>25&&!leaving){RecoveryFixtureReport(true,"host_closed","");reported=true;quitAfterLeave=true;RequestLeave();return;}
            // Compare gameplay facts, not recovery presence metadata. No simulation runs during pause.
            if(recovery.Paused)
            {
                var facts=Wire.Encode(new{world.Time,world.Remaining,world.Customers,world.Props,head=world.SharedHead,world.Job});
                if(frozenFacts!=null&&!facts.AsSpan().SequenceEqual(frozenFacts))pauseVerified=false;
                frozenFacts=facts;
                if(args.ContainsKey("test-host-continue")&&recovery.Deadline-NetNow<27)ContinueNetwork("fixture_host_continue");
            }else frozenFacts=null;
            if(world.Phase==Phase.Complete&&phaseClock>2&&!fixtureFinishing)
            {fixtureMatches++;StartNewMatch();fixtureRound=-1;checkedLocks=checkedRecovery=false;}
            if(NetNow>=double.Parse(args["recovery-test"])&&!fixtureFinishing)
            {fixtureFinishing=true;fixtureFinishAt=NetNow;NetEvent("fixture_finishing",new{revision=world.Revision});}
            if(fixtureFinishing&&!fixtureSent&&links.Count==expected-1&&links.Values.All(l=>l.Ready&&!l.Lost&&l.AppliedRevision==world.Revision))
            {
                fixtureSent=true;string hash=Convert.ToHexString(SHA256.HashData(Wire.Encode(world)));fixtureHash=hash;fixtureFinishAt=fixtureEndSend=NetNow;
                foreach(var l in links.Values)NetRpc(l.Identity.Peer,MethodName.RecoveryFixtureEnd,hash);
                RecoveryFixtureReport(pauseVerified,"host_final",hash);
            }
            if(fixtureSent&&NetNow-fixtureEndSend>.25)
            {
                fixtureEndSend=NetNow;
                foreach(var l in links.Values.Where(l=>l.Identity.Peer>0))NetRpc(l.Identity.Peer,MethodName.RecoveryFixtureEnd,fixtureHash);
            }
            if(fixtureSent&&(links.Values.All(l=>l.Identity.Peer==0)||NetNow-fixtureFinishAt>15))QuitGracefully(pauseVerified?0:1);
            else if(fixtureFinishing&&!fixtureSent&&NetNow-fixtureFinishAt>45){RecoveryFixtureReport(false,"peers_not_ready","");QuitGracefully(1);}
        }
        else
        {
            if(args.ContainsKey("test-active-retry")&&!testedActiveRetry&&health==ConnectionHealth.Reconnecting&&recoveryCount>0)
            {
                testedActiveRetry=true;double deadline=clientDeadline;RetryNetwork();
                if(clientDeadline!=deadline)throw new Exception("Manual retry extended recovery deadline");
                NetEvent("manual_retry_deadline_preserved",new{deadline});
            }
            if(clientReady&&!testedEpoch)
            {
                testedEpoch=true;int revision=world.Revision;var before=incoming;
                RecoveryChunk(connectionEpoch-1,1,0,0,1,new byte[32],0,new byte[1]);
                if(world.Revision!=revision||incoming!=before)throw new Exception("Stale connection data accepted");
                NetEvent("stale_epoch_rejected",new{actor=localId});
            }
            if(args.ContainsKey("test-bad-token")&&elapsed>25&&!testedCredential){testedCredential=true;resumeToken=new string('0',64);BeginClientRecovery("fixture_bad_credential");}
            if(args.ContainsKey("test-user-leave")&&elapsed>25&&!leaving){RecoveryFixtureReport(true,"intentional_leave","");reported=true;quitAfterLeave=true;RequestLeave();return;}
            if(health==ConnectionHealth.Failed&&args.ContainsKey("test-manual-retry")&&!testRetried)
            {
                if(testRetryAt==0)testRetryAt=NetNow+10;
                if(NetNow>=testRetryAt){testRetried=true;RetryNetwork();}
                return;
            }
            if(health==ConnectionHealth.Failed&&args.ContainsKey("test-expect-failure"))
            {RecoveryFixtureReport(true,"expected_failure:"+failureReason,"");QuitGracefully();}
            else if(health==ConnectionHealth.Failed){RecoveryFixtureReport(false,"unexpected_failure:"+failureReason,"");QuitGracefully(1);}
            if(NetNow>double.Parse(args["recovery-test"])+60){RecoveryFixtureReport(false,"client_deadline","");QuitGracefully(1);}
        }
    }
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Unreliable,TransferChannel=5)]
    public void RecoveryFixtureEnd(string hash)
    {
        if(authority||!args.ContainsKey("recovery-test")||fixtureSent)return;fixtureSent=true;
        string actual=Convert.ToHexString(SHA256.HashData(Wire.Encode(world)));
        RecoveryFixtureReport(hash==actual,"client_final",actual);GetTree().CreateTimer(.4).Timeout+=()=>QuitGracefully(hash==actual?0:1);
    }
    void RecoveryFixtureReport(bool success,string reason,string hash)
    {
        if(reason.StartsWith("expected_failure:"))success&=hud.RecoveryVisible&&!hud.MenuVisible&&health==ConnectionHealth.Failed;
        var data=new{success,reason,hash,mode,actor=localId,epoch=connectionEpoch,recoveryCount,resumeCount,pauseVerified,matches=fixtureMatches,seconds=elapsed,wallSeconds=NetNow,peakWorkingSet=fixturePeakMemory,players=world.Players.Count(p=>p.Active),outstanding=links.Values.Sum(l=>l.Transfer?.Outstanding??0)};
        if(reportPath.Length>0)File.WriteAllText(reportPath,JsonSerializer.Serialize(data));
        GD.Print("RECOVERY_TEST_"+(success?"OK ":"FAIL ")+JsonSerializer.Serialize(data));
        WriteNetReport();
    }
    async void CaptureRecoveryAfterDraw(string path)
    {
        await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        if(IsInsideTree())GetViewport().GetTexture().GetImage().SavePng(path);
    }
}
