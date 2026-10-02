using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using V=System.Numerics.Vector3;

namespace Hairball;
public partial class Main
{
    bool SculptStress=>args.ContainsKey("sculpt-stress");
    float stressAllFour=-1,stressSample;
    bool stressSent;
    int stressCycle=-1,stressFrames;
    double stressMilliseconds;
    readonly Dictionary<int,int> stressTools=new();
    void StressFixtures()
    {
        if(!authority||world.Phase!=Phase.Build)return;
        if(world.Players.Count(p=>p.Active)==4&&stressAllFour<0)stressAllFour=world.Time;
        // Load-test fixture: repeated fresh customers keep brush work running for ten
        // minutes instead of ending once a finite hairstyle has been shaved bald.
        int cycle=(int)(world.Time/30);
        if(cycle!=stressCycle){stressCycle=cycle;foreach(var h in world.Heads.Where(h=>!h.Barber&&!h.Loose&&!h.Facial))h.Volume=HairVolume.Create();}
        int definition=(int)(world.Time/4)%2==0?1:0;
        foreach(var p in world.Players.Where(p=>p.Active))
        {
            world.SharedHead.Locked=false;world.Barber(p.Slot).Locked=false;
            int id=200+p.Slot;
            var t=world.Tools.FirstOrDefault(t=>t.Id==id);
            if(t==null){t=new(){Id=id,Definition=definition,Holder=p.Id,Charge=-1};world.Tools.Add(t);}
            if(t.Definition!=definition){simulation.EndStroke(p.Id);t.Definition=definition;}
            t.Holder=p.Id;p.Held=id;
        }
    }
    void StressInput(PlayerState player,ref Vector2 movement,ref Buttons buttons)
    {
        var target=world.SharedHead.Position+new V(MathF.Sin(world.Time*.6f)*.18f,.5f,0);
        var d=target-Session.Eye(player);yaw=MathF.Atan2(-d.X,-d.Z);pitch=MathF.Asin(d.Y/d.Length());movement=Vector2.Zero;
        bool cutting=world.Tools.FirstOrDefault(t=>t.Id==player.Held)?.Definition==0;
        buttons=cutting&&world.Time%.5f>.32f?Buttons.None:((int)(world.Time/12)%2==0?Buttons.Primary:Buttons.Secondary);
        int size=(int)(world.Time/10)%3;
        if(player.BrushSize!=size){player.BrushSize=size;if(authority)simulation.SetBrushSize(player.Id,size);else RpcId(1,MethodName.BrushSizeRequest,size,inputGeneration);}
    }
    void StressCheck(double frameMs)
    {
        stressFrames++;stressMilliseconds+=frameMs;
        if(elapsed-stressSample>=10)
        {
            stressSample=elapsed;
            var mem=System.Diagnostics.Process.GetCurrentProcess().WorkingSet64/1048576;
            GD.Print($"SCULPT_STRESS_SAMPLE time={elapsed:0} piles={world.Debris.Piles.Count} flights={world.Debris.Flights.Count} fragments={world.Heads.Count(h=>h.Fragment&&h.AttachedTo<0)} mass={world.Debris.Mass:0.000} physics_ms={stressMilliseconds/Math.Max(1,stressFrames):0.00} memory_mb={mem} nodes={GetTree().GetNodeCount()} visual={salon!.Debris.RestingCount+salon.Debris.FlyingCount} impacts={(authority?simulation.Ledger.Events.Count:0)} crises={(authority?simulation.Ledger.Crises.Count:0)}");
            stressFrames=0;stressMilliseconds=0;
        }
        if(!authority||stressAllFour<0||world.Time-stressAllFour<float.Parse(args.GetValueOrDefault("duration","600")))return;
        bool good=world.Debris.Mass>.1f&&world.Debris.Piles.Count<=DebrisState.MaxPiles&&world.Debris.Flights.Count<=64&&receivedCount.Count>=3&&world.Players.Count(p=>p.Active)==4;
        good&=simulation.Ledger.Events.Count<=2048&&simulation.Ledger.Crises.Count<=128&&simulation.Ledger.Events.Any(e=>e.Actor!=0);
        Report(good,"four-player sculpt/debris/impact-ledger sustained load");if(!good)return;
        world.Phase=Phase.Complete;var data=Wire.Encode(world);GD.Print("SCULPT_STRESS_FINAL_HASH "+Convert.ToHexString(SHA256.HashData(data))[..16]);Rpc(MethodName.StressDone,data);stressSent=true;
    }
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel=1)]
    public void StressDone(byte[] data)
    {
        if(!SculptStress||authority)return;world=Wire.Decode<WorldState>(data);stressSent=true;
        GD.Print("SCULPT_STRESS_CLIENT_HASH "+Convert.ToHexString(SHA256.HashData(data))[..16]);
        Report(world.Debris.Mass>.1f&&world.Debris.Flights.Count<=64&&world.Debris.Piles.Count<=2048,"received final debris facts");RpcId(1,MethodName.CompletionAck);
    }
    void StressDrain()
    {
        if(authority&&Multiplayer.GetPeers().All(id=>completionAcks.Contains(id)))
        {GD.Print("SCULPT_STRESS_ALL_ACKNOWLEDGED");Rpc(MethodName.SmokeRelease);smokeReleaseTime=elapsed;}
    }
}
