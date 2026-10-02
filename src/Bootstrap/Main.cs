using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using NVector=System.Numerics.Vector3;
using NVector2=System.Numerics.Vector2;

namespace Hairball;
public partial class Main : Node3D
{
    Session simulation=new();
    WorldState world=new();
    SalonView? salon;
    Hud hud=null!;
    ENetMultiplayerPeer? peer;
    bool running,authority,lab,smoke,fullSmoke,capture,autoStart;
    int localId=1,expected=1,packetBytes,lastEvent=-1,fixtureRound=-1;
    float yaw,pitch,sendClock,snapshotClock,uiClock,elapsed,phaseClock,lastCapture=-1;
    Phase lastPhase=Phase.Lobby;
    string mode="",capturePath="",reportPath="";
    Dictionary<string,string> args=new();
    readonly Dictionary<int,float> inputReceived=new();
    readonly Dictionary<int,int> receivedCount=new();
    readonly Dictionary<int,float> initialMass=new();
    readonly HashSet<int> grew=new();
    readonly HashSet<int> moved=new();
    readonly HashSet<int> jumped=new(),chairsAdjusted=new();
    bool ladderCarried,ladderPlaced,beardEdited;
    float initialBeardMass;
    int beardDiagnosticRound=-1;
    readonly HashSet<int> completionAcks=new();
    float smokeReleaseTime=-1;
    readonly Dictionary<int,NVector> spawnPositions=new();
    readonly ReplayRecorder clientReplay=new();
    List<ReplayFrame> playback=new();
    bool checkedLocks,checkedRecovery,reported,integrationRan;
    int snapshotsReceived;
    int walkthroughStep;
    float walkthroughMass;
    public override void _Ready()
    {
        GetTree().AutoAcceptQuit=false;
        var raw=OS.GetCmdlineUserArgs();
        for(int i=0;i<raw.Length;i++)if(raw[i].StartsWith("--"))args[raw[i][2..]]=i+1<raw.Length && !raw[i+1].StartsWith("--")?raw[++i]:"true";
        LanguageSettings.Load();
        VisualQuality.Configure(args);
        PuppetTuftVolume.BakeMainlineCache=args.ContainsKey("b-art-bake");
        if(VisualQuality.Puppet){GetViewport().Msaa3D=Viewport.Msaa.Msaa4X;GetViewport().UseTaa=true;GD.Print("B_PUPPET_ART accepted volume fur / fabric / cut response");}
        if(args.TryGetValue("language",out var language))L.SetLocale(language);
        if(args.ContainsKey("visual-target-lab")){AddChild(new VisualTargetLab{Options=args,Finished=QuitGracefully});return;}
        if(args.ContainsKey("visual-lookdev")){AddChild(new VisualLookDev{Options=args,Finished=QuitGracefully});return;}
        smoke=args.ContainsKey("smoke");fullSmoke=args.ContainsKey("full-smoke");smoke|=fullSmoke;if(args.ContainsKey("recovery-test")){smoke=true;fullSmoke=true;}
        HeadView.HeadlessSimulationOnly=DisplayServer.GetName()=="headless"&&(smoke||SculptStress);
        capture=args.TryGetValue("capture",out capturePath!);reportPath=args.GetValueOrDefault("report","");
        if(args.ContainsKey("hair-review")){HairShellChecks.Gallery(this);Input.MouseMode=Input.MouseModeEnum.Visible;return;}
        expected=int.Parse(args.GetValueOrDefault("expected","1"));autoStart=args.ContainsKey("auto-start")||smoke||SculptStress;
        hud=new Hud{BPlaytest=DefaultVariantB};AddChild(hud);
        InitVoice();
        hud.Start+=Start;hud.Begin+=()=>{if(authority){StartNewMatch();Input.MouseMode=Input.MouseModeEnum.Captured;}};
        hud.ReturnMenu+=RequestLeave;hud.Choose+=Choose;hud.ResetLab+=()=>{if(authority&&lab)StartNewMatch();};
        Multiplayer.PeerConnected+=OnPeerConnected;Multiplayer.PeerDisconnected+=OnPeerDisconnected;
        Multiplayer.ConnectedToServer+=ConnectedRecovery;
        Multiplayer.ConnectionFailed+=()=>DisconnectedRecovery("connection_failed");
        Multiplayer.ServerDisconnected+=()=>DisconnectedRecovery("server_disconnected_unknown_cause");
        if(args.ContainsKey("material-lab"))Start("lab","",7777);
        else if(args.ContainsKey("menu")) { }
        else if(args.ContainsKey("laundry-lab")||args.ContainsKey("laundry-check"))Start("laundry","",7777);
        else if(args.ContainsKey("host"))Start("host","",int.Parse(args.GetValueOrDefault("port","7777")));
        else if(args.TryGetValue("join",out var address))Start("join",address,int.Parse(args.GetValueOrDefault("port","7777")));
        else if(args.ContainsKey("solo")||args.ContainsKey("recovery-ui-check")||args.ContainsKey("sculpt-check")||args.ContainsKey("access-check")||args.ContainsKey("localization-check")||args.ContainsKey("walkthrough")||args.ContainsKey("integration")||smoke||capture)Start(args.ContainsKey("lab")?"lab":"solo","",7777);
        else if(args.ContainsKey("lab") || GetTree().CurrentScene.SceneFilePath.Contains("TestHairLab"))Start("lab","",7777);
        GD.Print("HAIRBALL_BOOT_OK Godot="+Engine.GetVersionInfo()["string"]);
    }
    void Start(string selected,string address,int port)
    {
        voicePanel.CancelPreview();voicePanel.ShowSettings(false);
        if(running)Stop("");
        mode=selected;lab=selected is "lab" or "laundry";authority=selected!="join";localId=1;elapsed=0;lastEvent=-1;ResetNetworkMotion();
        targetRecipients.Clear();hud.PrivateTarget=null;privateRecipients.Clear();secretBuildLeak=targetBuildLeak=false;hud.OwnSecret=null;secretClock=0;accidentObserved.Clear();accidentStage=0;accidentStageAt=-1;
        simulation=new Session{Lab=lab||SculptStress,MoldsEnabled=true,ForceAI=args.ContainsKey("force-ai-customer")||args.ContainsKey("party-body-smoke")||args.ContainsKey("target-card-check")||args.ContainsKey("expansion-feel-check")||args.ContainsKey("expansion-body-check"),DebugCustomerActor=int.Parse(args.GetValueOrDefault("qa-customer","0"))};world=simulation.State;ConfigureExperiment();
        if(smoke && !fullSmoke){simulation.ArrivalSeconds=1;simulation.PreviewSeconds=1;simulation.ChoiceSeconds=2;simulation.BuildSeconds=10;simulation.ValidationSeconds=9;simulation.ResultsSeconds=1;simulation.HighlightSeconds=1;}
        if(EgoSmoke&&!fullSmoke){simulation.BuildSeconds=16;simulation.ResultsSeconds=4;}
        ConfigureBPartySmoke();
        salon=new SalonView{Name="Salon"};AddChild(salon);
        simulation.ObstructionDistance=(origin,dir,range)=>
        {
            var query=PhysicsRayQueryParameters3D.Create(Art.V(origin),Art.V(origin+dir*range),17);
            var hit=GetWorld3D().DirectSpaceState.IntersectRay(query);
            return hit.Count>0?Math.Min(range,Art.V(origin).DistanceTo(hit["position"].AsVector3())):range;
        };
        simulation.ModelCast=(from,to)=>
        {
            if(NVector.DistanceSquared(from,to)<.0000001f)return null;
            var hit=GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(Art.V(from),Art.V(to),49));
            return hit.Count>0?(Art.N(hit["position"].AsVector3()),Art.N(hit["normal"].AsVector3())):null;
        };
        simulation.CanPlaceLadder=salon.CanPlaceLadder;
        simulation.DebrisCast=(from,to)=>
        {
            if(NVector.DistanceSquared(from,to)<.0000001f)return null;
            var result=GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(Art.V(from),Art.V(to),49));
            // Only upward-facing supports can hold a pile; walls must never turn
            // airborne chips into a hovering pile halfway up the shop.
            return result.Count>0&&result["normal"].AsVector3().Y>.5f?Art.N(result["position"].AsVector3())+NVector.UnitY*.012f:null;
        };
        if(authority)simulation.AddPlayer(1,"Barber 1");
        if(VisualQuality.Puppet&&!HeadView.HeadlessSimulationOnly&&selected is "host" or "join")
        {
            // Load the canonical fur and draw its first pipelines before opening ENet.
            // Otherwise first-use CPU/GPU work can starve the welcome exchange and
            // turn abandoned client attempts into reserved logical seats.
            var loadClock=System.Diagnostics.Stopwatch.StartNew();
            if(authority)salon.Sync(world,localId,0);
            else {
                // Local presentation preview only; it never enters the Session or wire.
                var preview=new Session();preview.State.Experiment.Variant=world.Experiment.Variant;
                preview.AddPlayer(1,"Barber 1");salon.Sync(preview.State,1,0);
            }
            RenderingServer.ForceDraw(false,0);
            GD.Print($"B_ART_NETWORK_PREPARED ms={loadClock.Elapsed.TotalMilliseconds:0.0}");
        }
        if(selected is "host" or "join")
        {
            peer=new ENetMultiplayerPeer();
            // Temporary ENet handshakes are not logical seats. Three reconnecting clients
            // can have several abandoned handshakes each before transport timeout.
            var error=selected=="host"?peer.CreateServer(port,32):peer.CreateClient(address,port);
            if(error!=Error.Ok){Stop($"ENet failed: {error}");return;}
            Multiplayer.MultiplayerPeer=peer;((SceneMultiplayer)Multiplayer).ServerRelay=false;
        }
        else Multiplayer.MultiplayerPeer=new OfflineMultiplayerPeer();
        if(authority)
        {
            if(selected is "solo" or "lab" or "laundry")StartNewMatch();
        }
        InitRecovery(address,port);running=true;BeginVoice();hud.ShowMenu(false);
        Input.MouseMode=selected=="host"?Input.MouseModeEnum.Visible:Input.MouseModeEnum.Captured;
        GD.Print($"SESSION_STARTED mode={selected} port={port}");
    }
    void Stop(string message)
    {
        running=false;
        EndVoice();
        NetEvent("session_stop",new{message});hud.HideNetworkRecovery();Input.MouseMode=Input.MouseModeEnum.Visible;
        if(peer!=null){peer.Close();peer=null;}
        Multiplayer.MultiplayerPeer=new OfflineMultiplayerPeer();
        if(salon!=null){salon.QueueFree();salon=null;}
        hud.ShowMenu(true,message);
        if(smoke&&!reported){GD.Print("SMOKE_FAIL "+message);QuitGracefully(1);}
    }
    void StartNewMatch()
    {
        simulation.CatEnabled=hud.CatOn;simulation.ReturningEnabled=hud.ReturningRounds>0;simulation.TwistsEnabled=hud.TwistsOn;simulation.MatchRounds=smoke||lab||args.Keys.Any(k=>k.Contains("check")||k.Contains("review"))?1:hud.BPlaytest?(hud.ReturningRounds>0?hud.ReturningRounds:3):1;
        if(args.TryGetValue("qa-returning",out var rounds)){simulation.ReturningEnabled=true;simulation.MatchRounds=Math.Clamp(int.Parse(rounds),3,4);simulation.DebugCustomerActor=0;simulation.TwistsEnabled=false;}
        if(args.ContainsKey("qa-cat"))simulation.CatPicker=_=>true;
        simulation.ReturnCutEnabled=hud.ReturnCutOn;simulation.SecretTasksEnabled=hud.SecretTasksOn;hud.OwnSecret=null;privateRecipients.Clear();secretBuildLeak=false;
        simulation.StartMatch();
        if(args.TryGetValue("qa-twist",out var forcedTwist)&&Enum.TryParse<TwistKind>(forcedTwist,out var twist)){foreach(var p in world.Players)p.AllowViewChanges=true;simulation.TwistPicker=_=>twist;int round=world.Round;world.Round=2;simulation.RestageTwistForQA();world.Round=round;}
        if(world.Player(localId) is {} owner){yaw=owner.Yaw;pitch=owner.Pitch;}
        if(args.ContainsKey("party-mold-lab"))MoldLabStart();
        if(mode=="laundry")
        {
            simulation.StageLaundry();var player=world.Player(1)!;yaw=player.Yaw;pitch=player.Pitch;
            if(salon!=null&&salon.Bodies.TryGetValue(1,out var body)){body.Position=Art.V(player.Position);body.Velocity=Vector3.Zero;}
        }
        StageMaterialLab();
        StageExperimentReview();
        if(args.TryGetValue("shared-review",out var review))
        {
            if(review=="llama")
            {
                world.Phase=Phase.Build;world.Remaining=75;world.Customers[0].Profile="llama";
                foreach(var head in world.Heads.Where(h=>!h.Barber&&!h.Loose))head.Material=HeadMaterialKind.Wool;
                simulation.Stimulate(.65f,1,CustomerStimulus.Noise);
            }
            else if(review is "landing" or "failure")
            {
                world.Job.Goal=0;if(review=="landing"){world.SharedHead.Volume.Fill(p=>Math.Max(Math.Min(.68f-Math.Abs(p.X),Math.Min(.18f-Math.Abs(p.Y-.7f),.5f-Math.Abs(p.Z))),Math.Min(.45f-new NVector2(p.X,p.Z).Length(),.3f-Math.Abs(p.Y-.35f))));foreach(var patch in world.SharedHead.Patches)patch.Glue=1;}else world.SharedHead.Volume.Fill(_=>-1);
                foreach(var head in world.Heads)head.Locked=true;
                world.Phase=Phase.Validation;world.Remaining=simulation.ValidationSeconds;Validation.Begin(world);
            }
        }
    }
    void OnPeerConnected(long id)
    {if(authority&&!links.Values.Any(l=>l.Identity.Peer==id))pendingPeers[(int)id]=NetNow;}
    void OnPeerDisconnected(long id)
    {
        if(!authority)return;NetEvent("transport_disconnected",new{peer=id});pendingPeers.Remove((int)id);
        var link=links.Values.FirstOrDefault(l=>l.Identity.Peer==id);
        if(link!=null){link.Identity.Peer=0;link.Transfer=null;if(!fixtureSent)LoseLink(link,"peer_disconnected_unknown_cause");}
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.UnreliableOrdered,TransferChannel=0)]
    public void InputRequest(float x,float z,float lookYaw,float lookPitch,int buttons,int generation)
    {
        if(!authority)return;int sender=SenderActor();
        if(!CanAct(sender,generation)||world.Player(sender)==null || !float.IsFinite(x)||!float.IsFinite(z)||!float.IsFinite(lookYaw)||!float.IsFinite(lookPitch))return;
        if(inputReceived.TryGetValue(sender,out float last)&&elapsed-last<.025f)return;
        inputReceived[sender]=elapsed;receivedCount[sender]=receivedCount.GetValueOrDefault(sender)+1;
        simulation.Inputs[sender]=(new NVector2(Math.Clamp(x,-1,1),Math.Clamp(z,-1,1)),lookYaw,Math.Clamp(lookPitch,-1.35f,1.35f),(Buttons)(buttons&2047));
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable)]
    public void GoalRequest(int choice,int generation)
    {if(authority&&CanAct(SenderActor(),generation))simulation.Choose(SenderActor(),choice);}
    void Choose(int choice){if(NetworkFrozen)return;if(authority)simulation.Choose(localId,choice);else NetRpc(1,MethodName.GoalRequest,choice,inputGeneration);}
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable)]
    public void BrushSizeRequest(int size,int generation){if(authority&&CanAct(SenderActor(),generation))simulation.SetBrushSize(SenderActor(),size);}
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable)]
    public void StrokeReleaseRequest(int generation){if(authority&&CanAct(SenderActor(),generation))simulation.EndStroke(SenderActor());}
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel=1)]
    public void Snapshot(byte[] data) { if(LegacyNet)ApplySnapshot(data); }
    void ApplySnapshot(byte[] data)
    {
        if(authority)return;
        var next=Wire.Decode<WorldState>(data);packetBytes=data.Length;snapshotsReceived++;
        stateBytes+=data.Length;
        if(next.Revision<world.Revision)return;
        if(SculptStress&&snapshotsReceived==1)GD.Print($"SCULPT_STRESS_JOIN_SNAPSHOT mass={next.Debris.Mass:0.000} piles={next.Debris.Piles.Count}");
        bool changedRound=next.Round!=world.Round;
        // Reuse identical immutable density buffers; decoding a snapshot must not
        // invalidate mass/mesh caches for every unedited head.
        foreach(var h in next.Heads)if(world.Heads.FirstOrDefault(p=>p.Id==h.Id) is {} old&&old.Volume.Revision==h.Volume.Revision&&old.Volume.Data.AsSpan().SequenceEqual(h.Volume.Data))h.Volume=old.Volume;
        world=next;
        ObserveExperiment();
        BPartyNetworkObserve();
        if(snapshotsReceived==1&&world.Player(localId) is {} joined){yaw=joined.Yaw;pitch=joined.Pitch;}
        if(changedRound)clientReplay.Clear();
        if(world.Phase==Phase.Build)clientReplay.Record(world,.2f);
        if(smoke && !args.ContainsKey("recovery-test") && world.Phase==Phase.Complete && !reported)
        {GD.Print($"CLIENT_FINAL round={world.Round} players={world.Players.Count(p=>p.Active)} snapshots={snapshotsReceived} hash={Convert.ToHexString(SHA256.HashData(data))[..16]}");Report((!BPartySmoke||BPartySmokeGood())&&SecretSmokeGood()&&BodySmokeGood()&&CustomerPrivacyGood(),"client received authoritative match completion");NetRpc(1,MethodName.CompletionAck);}
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel=1)]
    public void CompletionAck()
    {if((smoke||SculptStress)&&authority&&world.Phase==Phase.Complete&&world.Player(SenderActor())!=null)completionAcks.Add(SenderActor());}
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel=1)]
    public void SmokeRelease()
    {if((smoke||SculptStress)&&!authority&&reported){GD.Print("SMOKE_COMPLETION_ACKNOWLEDGED");smokeReleaseTime=elapsed;}}
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel=2)]
    public void ReplayData(byte[] data){playback=Wire.Decode<List<ReplayFrame>>(data);GD.Print($"REPLAY_RECEIVED frames={playback.Count} bytes={data.Length} phase={world.Phase}");}
    void SendSnapshot()
    {
        if(mode!="host"||peer==null||Multiplayer.GetPeers().Length==0)return;
        foreach(var link in links.Values.Where(l=>l.Ready&&l.Transfer==null&&!l.Lost))QueueWorld(link);
    }
    public override void _Input(InputEvent e)
    {
        // Read look before the recovery overlay consumes GUI events. The overlay still
        // blocks clicks from reaching gameplay/menu buttons underneath it.
        if(running&&NetworkFrozen&&e is InputEventMouseMotion look)
        {yaw-=look.Relative.X*.0023f;pitch=Math.Clamp(pitch-look.Relative.Y*.0023f,-1.35f,1.35f);}
    }
    public override void _UnhandledInput(InputEvent e)
    {
        if(!running)return;
        if(NetworkFrozen)return;
        if(e is InputEventMouseButton release&&!release.Pressed&&release.ButtonIndex is MouseButton.Left or MouseButton.Right)
        {if(authority)simulation.EndStroke(localId);else if(peer?.GetConnectionStatus()==MultiplayerPeer.ConnectionStatus.Connected)NetRpc(1,MethodName.StrokeReleaseRequest,inputGeneration);}
        if(e is InputEventMouseButton wheel&&wheel.Pressed&&Input.MouseMode==Input.MouseModeEnum.Captured&&world.Player(localId) is {} sculptor&&world.Tools.Any(t=>t.Id==sculptor.Held&&t.Definition is 0 or 1)&&wheel.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
        {
            int size=Math.Clamp(sculptor.BrushSize+(wheel.ButtonIndex==MouseButton.WheelUp?1:-1),0,2);
            sculptor.BrushSize=size;if(authority)simulation.SetBrushSize(localId,size);else NetRpc(1,MethodName.BrushSizeRequest,size,inputGeneration);
        }
        if(e is InputEventKey key && key.Pressed && !key.Echo)
        {
            if(key.Keycode==Key.Escape)Input.MouseMode=Input.MouseMode==Input.MouseModeEnum.Captured?Input.MouseModeEnum.Visible:Input.MouseModeEnum.Captured;
            if(key.Keycode==Key.F3)hud.DebugVisible=!hud.DebugVisible;
            if(world.Twist.Kind==TwistKind.Reverse&&world.Player(localId)?.Customer==true&&key.Keycode>=Key.Key1&&key.Keycode<=Key.Key4)GuessInput((int)(key.Keycode-Key.Key1));
            if(key.Keycode==Key.F5&&lab&&authority)StartNewMatch();
            if(key.Keycode==Key.F6&&lab&&authority&&world.Player(localId) is {} wearer)simulation.SpawnWig(wearer.Position+Session.Aim(wearer),world.NextHead%4);
            if(key.Keycode==Key.F7&&lab&&authority&&world.Player(localId) is {} tester){world.Job.Goal=(world.Job.Goal+1)%Goals.All.Length;PhysicalProps.Stage(world);}
            if(key.Keycode==Key.Enter&&authority&&world.Phase is Phase.Lobby or Phase.Complete){StartNewMatch();Input.MouseMode=Input.MouseModeEnum.Captured;}
            if(world.Phase==Phase.Choice&&world.Player(localId) is {} p)
            {int index=key.Keycode switch{Key.Key1=>0,Key.Key2=>1,Key.Key3=>2,_=>-1};if(index>=0)Choose(world.Job.Choices[index]);}
        }
        if(e is InputEventMouseMotion motion && Input.MouseMode==Input.MouseModeEnum.Captured)
        {yaw-=motion.Relative.X*.0023f;pitch=Math.Clamp(pitch-motion.Relative.Y*.0023f,-1.35f,1.35f);if(world.Player(localId) is {Customer:true,Standing:false}){var eyes=HumanCustomer.Eyes(world,yaw,pitch);yaw=eyes.Yaw;pitch=eyes.Pitch;}}
    }
    public override void _PhysicsProcess(double delta)
    {
        if(quitting)return;
        if(!running||salon==null)return;
        long frameStart=System.Diagnostics.Stopwatch.GetTimestamp();
        float dt=(float)delta;elapsed+=dt;
        RecoveryTick();if(!running)return;
        if(args.TryGetValue("quit-after-seconds",out var wallLimit)&&elapsed>float.Parse(wallLimit)){QuitGracefully();return;}
        if(NetworkFrozen){if(authority)SendMotion(dt);NetworkTelemetry(dt);return;}
        phaseClock+=dt;
        if(args.ContainsKey("recovery-ui-check")&&elapsed>.3f&&!integrationRan)
        {integrationRan=true;try{hud.VerifyRecoveryUi();QuitGracefully();}catch(Exception e){GD.PushError(e.ToString());QuitGracefully(1);}return;}
        if(args.ContainsKey("voice-check")&&elapsed>.3f&&!integrationRan){integrationRan=true;VoiceChecks();return;}
        if(args.ContainsKey("expansion-feel-check")){if(elapsed>.3f&&!integrationRan){integrationRan=true;ExpansionFeelChecks();}return;}
        if(args.ContainsKey("world-check")){if(elapsed>.3f&&!integrationRan){integrationRan=true;WorldChecks();}return;}
        if(args.ContainsKey("b-art-check")){if(elapsed>.3f&&!integrationRan){integrationRan=true;MainlineArtChecks();}return;}
        if(args.ContainsKey("twist-check")){if(elapsed>.3f&&!integrationRan){integrationRan=true;TwistChecks();}return;}
        if(args.ContainsKey("human-customer-check")){if(elapsed>.3f&&!integrationRan){integrationRan=true;HumanCustomerChecks();}return;}
        if(args.ContainsKey("target-card-check")){if(elapsed>.3f&&!integrationRan){integrationRan=true;TargetCardChecks();}return;}
        if(args.ContainsKey("party-body-check")){if(elapsed>.3f&&!integrationRan){integrationRan=true;ExpansionBodyChecks();}return;}
        if(args.ContainsKey("party-gallery-check")){if(elapsed>.3f&&!integrationRan){integrationRan=true;ExpansionFeelChecks();}return;}
        if(args.ContainsKey("party-safety-review")){if(elapsed>.3f&&!integrationRan){integrationRan=true;PartySafetyReview();}return;}
        if(args.ContainsKey("v06-check")&&elapsed>.3f)
        {if(!integrationRan){integrationRan=true;ExperimentChecks();}return;}
        // Once all final snapshots are acknowledged, drain reliable packets before the host
        // closes ENet. Clients stop sending input and exit on ServerDisconnected.
        if(smokeReleaseTime>=0){if(elapsed-smokeReleaseTime>1)QuitGracefully();return;}
        if(args.ContainsKey("sculpt-check")&&elapsed>.25f&&!integrationRan){integrationRan=true;try{SculptChecks.Run(this,salon,simulation);}catch(Exception e){GD.PushError("SCULPT_CHECKS_FAIL "+e);QuitGracefully(1);}return;}
        if(args.ContainsKey("laundry-check")&&elapsed>.35f&&!integrationRan){integrationRan=true;try{LaundryChecks();}catch(Exception e){GD.PushError("LAUNDRY_CHECKS_FAIL "+e);QuitGracefully(1);}return;}
        if(SculptStress&&stressSent){StressDrain();return;}
        if(args.ContainsKey("access-check")&&elapsed>.25f){if(!integrationRan){integrationRan=true;AccessChecks.Run(this,salon);}return;}
        if(args.ContainsKey("shared-check")&&elapsed>.25f){if(!integrationRan){integrationRan=true;SharedChecks.Run(this,salon);}return;}
        if(args.ContainsKey("ego-check")&&elapsed>.25f)
        {
            if(!integrationRan)
            {
                integrationRan=true;
                try{EgoChecks.Run(this,salon,simulation,hud);
                    if(args.ContainsKey("ego-order-view")){foreach(var body in salon.Bodies.Values)body.Position=new Vector3(0,0,4);salon.Sync(world,localId,.1f);hud.Visible=false;salon.Camera.Position=new(3,2.1f,-2.7f);salon.Camera.LookAt(new Vector3(3,1.3f,-4.5f));}GetTree().CreateTimer(.25).Timeout+=()=>{if(capture)SaveCapture();QuitGracefully();};}
                catch(Exception e){GD.PushError("EGO_CHECKS_FAIL "+e);QuitGracefully(1);}
            }
            return;
        }
        if(args.ContainsKey("localization-check")&&elapsed>.25f&&!integrationRan)
        {
            integrationRan=true;
            try{hud.VerifyLanguageSwitch(world,localId,salon.Mirror);QuitGracefully();}
            catch(Exception e){GD.PushError("LOCALIZATION_CHECK_FAIL "+e);QuitGracefully(1);}
            return;
        }
        if(args.ContainsKey("integration")&&elapsed>.25f&&!integrationRan)
        {
            integrationRan=true;
            try{EngineChecks.Run(this,salon,simulation.ObstructionDistance);QuitGracefully();}
            catch(Exception e){GD.PushError("ENGINE_INTEGRATION_FAIL "+e);QuitGracefully(1);}
            return;
        }
        if(world.Phase!=lastPhase)
        {
            phaseClock=0;GD.Print($"PHASE round={world.Round} phase={world.Phase} peers={world.Players.Count(p=>p.Active)}");lastPhase=world.Phase;
            if(world.Phase==Phase.Validation&&world.Player(localId) is {} observer)
            {var target=world.SharedHead.Position+new NVector(0,.7f,0)-Session.Eye(observer);yaw=MathF.Atan2(-target.X,-target.Z);pitch=MathF.Asin(target.Y/target.Length());}
        }
        if(authority&&autoStart&&world.Phase==Phase.Lobby&&world.Players.Count(p=>p.Active)>=expected){StartNewMatch();Input.MouseMode=Input.MouseModeEnum.Captured;}
        if(smoke&&authority)SmokeFixtures();
        UpdateBodyPreferences();BPartyNetworkFixtures();BodyNetworkFixtures();
        if(SculptStress)StressFixtures();
        if(EgoSmoke&&authority)EgoNetworkFixtures();
        Vector2 movement=Vector2.Zero;Buttons buttons=Buttons.None;
        if(!controlsReleased&&!Input.IsMouseButtonPressed(MouseButton.Left)&&!Input.IsMouseButtonPressed(MouseButton.Right)&&!Input.IsPhysicalKeyPressed(Key.E)&&!Input.IsPhysicalKeyPressed(Key.G)&&!Input.IsPhysicalKeyPressed(Key.Space))controlsReleased=true;
        if(Input.MouseMode==Input.MouseModeEnum.Captured&&!smoke&&controlsReleased)
        {
            movement=new((Input.IsPhysicalKeyPressed(Key.D)?1:0)-(Input.IsPhysicalKeyPressed(Key.A)?1:0),(Input.IsPhysicalKeyPressed(Key.S)?1:0)-(Input.IsPhysicalKeyPressed(Key.W)?1:0));
            if(Input.IsMouseButtonPressed(MouseButton.Left))buttons|=Buttons.Primary;
            if(Input.IsMouseButtonPressed(MouseButton.Right))buttons|=Buttons.Secondary;
            if(Input.IsPhysicalKeyPressed(Key.E))buttons|=Buttons.Interact;
            if(Input.IsPhysicalKeyPressed(Key.Tab))buttons|=Buttons.Reference;
            if(Input.IsPhysicalKeyPressed(Key.G))buttons|=Buttons.Drop;
            if(Input.IsPhysicalKeyPressed(Key.Space))buttons|=Buttons.Jump;
            if(Input.IsPhysicalKeyPressed(Key.W))buttons|=Buttons.Stand;
            if(Input.IsPhysicalKeyPressed(Key.R))buttons|=Buttons.ChairUp;
            if(Input.IsPhysicalKeyPressed(Key.F))buttons|=Buttons.ChairDown;
        }
        if(world.Player(localId) is {} chairUser&&!chairUser.Customer&&PartyBodies.Empty(chairUser)&&Session.LookingAt(chairUser,Session.ChairControl(0),.5f,3)){if(buttons.HasFlag(Buttons.Primary))buttons=(buttons&~Buttons.Primary)|Buttons.ChairLeft;if(buttons.HasFlag(Buttons.Secondary))buttons=(buttons&~Buttons.Secondary)|Buttons.ChairRight;}
        if(smoke&&world.Player(localId) is {} bot)
        {
            if(world.Phase==Phase.Choice)Choose(world.Job.Choices[0]);
            var target=world.SharedHead.Position+new NVector(0,.4f,0);
            if(bot.Held<0&&world.Phase is Phase.Choice or Phase.Preview or Phase.Build)target=world.Tools.First(t=>t.Id==SmokeToolId(bot)).Position;
            var direction=target-Session.Eye(bot);yaw=MathF.Atan2(-direction.X,-direction.Z);pitch=MathF.Asin(direction.Y/direction.Length());
            buttons=bot.Held<0?(phaseClock%1<.7f?Buttons.Interact:Buttons.None):world.Phase==Phase.Build?Buttons.Primary:Buttons.None;
            if(world.Phase==Phase.Build&&phaseClock>.25f&&phaseClock<1.25f)movement.X=phaseClock<.75f?.7f:-.7f;
            if(world.Experiment.IsB&&bot.Slot>0&&world.Phase==Phase.Build&&world.Remaining<(fullSmoke?20:2.66f))
            {
                var d=world.SharedHead.Position-Session.Eye(bot);yaw=MathF.Atan2(-d.X,-d.Z);pitch=MathF.Asin(d.Y/d.Length());
                movement=new(0,new NVector2(bot.Position.X-world.SharedHead.Position.X,bot.Position.Z-world.SharedHead.Position.Z).Length()>1.25f?-.8f:0);buttons=Buttons.Interact;
            }
        }
        if(args.ContainsKey("walkthrough")&&world.Player(localId) is {} walker)
        {
            // Navigate the unchanged shop, aim at the actual rack item, then return and use it.
            NVector waypoint=walkthroughStep switch{0=>new(-1.65f,0,2.6f),1=>new(-1.65f,0,1.65f),3=>new(-1.65f,0,2.6f),4=>new(0,0,2.6f),_=>walker.Position};
            if(walkthroughStep is 0 or 1 or 3 or 4)
            {
                var d=waypoint-walker.Position;d.Y=0;yaw=0;pitch=0;
                if(d.Length()<.08f)walkthroughStep++;else movement=new Vector2(d.X,d.Z).Normalized();
            }
            else if(walkthroughStep==2)
            {
                var target=world.Tools.First(t=>t.Id==1).Position-Session.Eye(walker);yaw=MathF.Atan2(-target.X,-target.Z);pitch=MathF.Asin(target.Y/target.Length());
                buttons=elapsed%1<.5f?Buttons.Interact:Buttons.None;if(walker.Held==1){GD.Print("WALKTHROUGH_RACK_PICKUP_OK");walkthroughStep++;}
            }
            else if(walkthroughStep==5)
            {
                yaw=0;pitch=.1f;if(world.Phase==Phase.Build){walkthroughMass=world.SharedHead.Mass;walkthroughStep++;}
            }
            else if(walkthroughStep==6)
            {
                buttons=Buttons.Primary;
                if(world.SharedHead.Mass>walkthroughMass+.5f){GD.Print("WALKTHROUGH_AIM_GROWTH_OK");buttons=Buttons.Drop;walkthroughStep++;}
            }
            else if(walkthroughStep==7&&walker.Held==-1)
            {GD.Print("WALKTHROUGH_DROP_OK");Report(true,"normal shop navigation / physical rack pickup / aim / use / drop");QuitGracefully();}
            if(elapsed>25){Report(false,"normal shop walkthrough timed out at step "+walkthroughStep);QuitGracefully(1);}
        }
        if(SculptStress&&world.Player(localId) is {} stressPlayer)StressInput(stressPlayer,ref movement,ref buttons);
        LaundryReviewInput(ref movement,ref buttons);
        ExperimentReviewInput(ref movement,ref buttons);
        if(EgoSmoke&&world.Player(localId) is {} egoPlayer)EgoNetworkInput(egoPlayer,ref movement,ref buttons);
        NetWalk(ref movement,ref buttons);CustomerBot(ref movement,ref buttons);
        if(world.Player(localId) is {} callPlayer)BPartyNetworkInput(callPlayer,ref movement,ref buttons);
        if(world.Player(localId) is {} bodyPlayer)BodyNetworkInput(bodyPlayer,ref movement,ref buttons);
        SyncRemoteColliders();
        if(world.Player(localId) is {} local)
        {
            if(authority)simulation.Inputs[localId]=(new(movement.X,movement.Y),yaw,pitch,buttons);
            else
            {
                if(!LegacyNet&&salon.Bodies.TryGetValue(localId,out var predictedBody))PredictLocal(predictedBody,local,movement,buttons,dt);
                else
                {
                sendClock+=dt;if(sendClock>=.05f&&peer?.GetConnectionStatus()==MultiplayerPeer.ConnectionStatus.Connected){sendClock=0;NetRpc(1,MethodName.InputRequest,movement.X,movement.Y,yaw,pitch,(int)buttons,inputGeneration);}
                if(salon.Bodies.TryGetValue(localId,out var ownBody))
                {
                    PartyBodyMove(ownBody,local,movement,yaw,delta,buttons.HasFlag(Buttons.Jump),Art.V(local.Impulse),world.Time);
                    float distance=ownBody.Position.DistanceTo(Art.V(local.Position));
                    if(distance>.12f)corrections.Add(distance>1.5f?distance:distance*.12f);if(distance>1.5f)hardCorrections++;
                    if(distance>1.5f)ownBody.Position=Art.V(local.Position);
                    else if(distance>.12f){var corrected=ownBody.Position.Lerp(Art.V(local.Position),.12f);corrected.Y=Math.Abs(ownBody.Position.Y-local.Position.Y)>.65f?local.Position.Y:ownBody.Position.Y;ownBody.Position=corrected;}
                }
                }
            }
            if(salon.Bodies.TryGetValue(localId,out var body))
            {salon.Camera.Position=body.Position+new Vector3(0,1.7f,0);salon.Camera.Rotation=new(pitch,yaw,0);salon.Camera.Fov=local.Held>=0 && world.Tools.First(t=>t.Id==local.Held).Definition==7 && buttons.HasFlag(Buttons.Secondary)?35:73;}
        }
        if(authority)
        {
            foreach(var p in world.Players.Where(p=>p.Active&&!p.NetworkAway))
            {
                if(!salon.Bodies.TryGetValue(p.Id,out var body))continue;
                if(!LegacyNet&&p.Id!=localId){MoveRemoteAuthority(p,body,dt);continue;}
                if(p.Id!=localId && inputReceived.TryGetValue(p.Id,out var last)&&elapsed-last>.3f)simulation.Inputs.Remove(p.Id);
                if(simulation.Inputs.TryGetValue(p.Id,out var input))PartyBodyMove(body,p,new(input.Move.X,input.Move.Y),input.Yaw,delta,input.Buttons.HasFlag(Buttons.Jump),Art.V(p.Impulse),world.Time);
                else PartyBodyMove(body,p,Vector2.Zero,p.Yaw,delta,false,Art.V(p.Impulse),world.Time);
                p.Position=Art.N(body.Position);
            }
            salon.PhysicsCustomers(world);
            Phase before=world.Phase;simulation.Tick(dt);
            if(before!=Phase.Results&&world.Phase==Phase.Results)
            {
                playback=simulation.Replay.Best;
                if(mode=="host"&&Multiplayer.GetPeers().Length>0)BeginReplayTransfer();
            }
            snapshotClock+=dt;if(snapshotClock>=.2f){snapshotClock=0;SendSnapshot();}
            SendMotion(dt);
            PumpReplayTransfer();
        }
        else if(LegacyNet)foreach(var p in world.Players.Where(p=>p.Active&&p.Id!=localId))if(salon.Bodies.TryGetValue(p.Id,out var body))
        {body.Position=body.Position.Lerp(Art.V(p.Position),Math.Min(1,dt*12));body.Model.Rotation=new(0,p.Yaw+Mathf.Pi,0);}
        CameraPhysics(movement,dt);
        UpdateSecrets(dt);
        ReplayFrame? replay=null;
        if(world.Phase==Phase.Highlight&&playback.Count>0)
        {float span=playback[^1].Time-playback[0].Time;float time=playback[0].Time+Math.Min(1,phaseClock/(smoke&&!fullSmoke?1:6))*span;replay=playback.LastOrDefault(f=>f.Time<=time)??playback[0];}
        // Explicit visual regression fixture; ordinary play always uses the FPS camera.
        if(args.TryGetValue("reference-view",out var referenceView)&&world.Player(localId) is {} referencePlayer)
        {
            world.Job.Goal=int.Parse(args.GetValueOrDefault("reference-goal","6"));
            var focus=Art.V(world.SharedHead.Position)+Vector3.Up*.75f;
            var offset=referenceView switch{"side"=>new Vector3(3.5f,0,0),"top"=>new Vector3(0,3.5f,0),"oblique"=>new Vector3(2.6f,1.3f,2.6f),_=>new Vector3(0,0,3.5f)};
            salon.Camera.Position=focus+offset;salon.Camera.LookAt(focus,referenceView=="top"?Vector3.Forward:Vector3.Up);
        }
        if(args.ContainsKey("sculpt-floor-view")&&world.Player(localId) is {} floorViewer)
        {
            var station=Art.V(Session.WorkCenter);salon.Camera.Position=station+new Vector3(1.1f,1.7f,2.4f);salon.Camera.LookAt(station+new Vector3(0,.15f,.45f));
        }
        if(args.ContainsKey("material-review")){salon.Camera.Position=new(0,4.5f,-4.85f);salon.Camera.LookAt(new Vector3(0,1.65f,-2.1f));}
        if(args.ContainsKey("miniature-review")){salon.Camera.Position=new(-1.5f,2.9f,-1.5f);salon.Camera.LookAt(new Vector3(-3,2.25f,-3.6f));}
        if(args.ContainsKey("board-review")&&world.Player(localId) is {} reader)reader.ReferenceUp=true;
        salon.Sync(world,localId,dt,replay);
        ObserveExperiment();
        BPartyNetworkObserve();
        foreach(var e in world.Events.Where(e=>e.Id>lastEvent))
        {
            if(world.Player(e.Source) is {} source && e.Text.Contains("→"))
                {int tool=world.Tools.FirstOrDefault(t=>t.Id==source.Held)?.Definition??0;if(tool is not (1 or 3 or 4))salon.Beam(Art.V(Session.Eye(source)+Session.Aim(source)*.3f),Art.V(e.Position),Art.ToolColors[tool]);}
            lastEvent=e.Id;
        }
        uiClock+=dt;if(uiClock>.12f){uiClock=0;UpdateCustomer(dt,buttons);hud.Update(world,localId,authority,lab,salon.Mirror,packetBytes);}
        if(EgoSmoke)EgoNetworkCheck();
        if(smoke&&!args.ContainsKey("recovery-test"))SmokeCheck();
        if(SculptStress)StressCheck(System.Diagnostics.Stopwatch.GetElapsedTime(frameStart).TotalMilliseconds);
        if(physicsCosts.Count<36000)physicsCosts.Add(System.Diagnostics.Stopwatch.GetElapsedTime(frameStart).TotalMilliseconds);
        NetworkTelemetry(dt);
        bool captureReady=args.TryGetValue("capture-phase",out var phase)?world.Phase==lastPhase&&world.Phase.ToString().Equals(phase,StringComparison.OrdinalIgnoreCase)&&phaseClock>float.Parse(args.GetValueOrDefault("capture-delay","2")):elapsed>3;
        if(capture&&captureReady && lastCapture<0){lastCapture=elapsed;CallDeferred(MethodName.SaveCapture);}
        if(args.TryGetValue("quit-after-seconds",out var seconds)&&elapsed>float.Parse(seconds))QuitGracefully();
    }
    int SmokeToolId(PlayerState p)
    {
        // Exercise growth through an eligible barber when the original bottle owner becomes a customer or family member.
        var grower=world.Players.Where(q=>q.Active&&!q.NetworkAway&&!q.Customer&&q.Id!=world.Twist.FamilyActor).OrderBy(q=>q.Slot).FirstOrDefault();
        if(HumanSmoke&&grower?.Id==p.Id&&p.Slot!=0)return world.Tools.First(t=>t.Definition==1&&t.Id!=1&&t.Id!=11).Id;
        return p.Slot switch{0=>1,1=>11,2=>0,_=>4};
    }
    void SmokeFixtures()
    {
        if(world.Round!=fixtureRound&&world.Round>0)
        {
            fixtureRound=world.Round;
            foreach(var p in world.Players.Where(p=>p.Active&&!p.NetworkAway))
            {
                int toolId=SmokeToolId(p);
                world.Tools.First(t=>t.Id==toolId).Position=p.Position+Session.Rotate(new(0,1.05f,-.9f),p.Yaw);
                initialMass[p.Id]=world.SharedHead.Mass;spawnPositions[p.Id]=p.Position;
            }
        }
        foreach(var p in world.Players.Where(p=>p.Active))
        {
            if(p.Added>.001f)grew.Add(p.Id);
            if(spawnPositions.TryGetValue(p.Id,out var start)&&NVector.Distance(p.Position,start)>.10f)moved.Add(p.Id);
        }
        if(world.Phase==Phase.Build&&phaseClock>2&&!checkedRecovery){if(world.Experiment.IsB)simulation.Hear(new(3,2,0),1,"qa_tool_motor");else simulation.Stimulate(.6f,1,CustomerStimulus.Noise);checkedRecovery=true;}
        if((world.Phase==Phase.Validation||world.Experiment.IsB&&world.Phase==Phase.Results)&&!checkedLocks)
        {
            var h=world.SharedHead;float before=h.Mass;HairSystem.Apply(h,h.Patches[0],new(EffectKind.RemoveHair,3));
            checkedLocks=Math.Abs(before-h.Mass)<.0001f&&(HumanSmoke||world.Barber(0).Locked);
        }
    }
    void SmokeCheck()
    {
        if(elapsed>400){Report(false,"smoke deadline exceeded");QuitGracefully(1);return;}
        if(authority&&world.Phase==Phase.Complete&&!reported)
        {
            int finalExpected=int.Parse(args.GetValueOrDefault("expected-final",expected.ToString()));
            bool good=world.Customers.Count==1&&world.Heads.Count(h=>!h.Barber&&!h.Loose&&!h.Facial)==1&&world.Job.Settled&&world.Round==(world.Experiment.Variant==ExperimentVariant.Off?3:world.MatchRounds)&&checkedLocks&&grew.Count>=(HumanSmoke?1:Math.Min(2,expected))&&world.Players.Count(p=>p.Active)==finalExpected;
            good&=expected==1||receivedCount.Count>=expected-1;
            good&=moved.Count>=expected;
            if(EgoSmoke)good&=egoResults==3;

            GD.Print($"SMOKE_METRICS grew={grew.Count} moved={moved.Count} peersSending={receivedCount.Count} locked={checkedLocks} replayFrames={playback.Count} heads={world.Heads.Count}");
            if(BPartySmoke)good=BPartySmokeGood();
            else if(BodySmoke)good=BodySmokeGood()&&world.Job.Settled&&world.Experiment.Resolved&&checkedLocks&&receivedCount.Count>=expected-1;
            else if(HumanSmoke){good&=world.Experiment.ResultTarget!=null&&world.Experiment.ResultTasks.All(t=>t.Actor!=world.Experiment.CustomerActor);}
            else if(world.Experiment.IsB){good&=experimentFacts.Contains("leave:ToMirror")&&experimentFacts.Contains("leave:Mirror")&&experimentFacts.Contains("leave:ToDoor");if(expected>1)good&=experimentFacts.Contains("brace");}
            good&=SecretSmokeGood()&&BodySmokeGood()&&CustomerPrivacyGood();
            if(world.Twist.Kind==TwistKind.Family&&world.Player(world.Twist.FamilyActor) is {} family)good&=family.Added==0&&family.Removed==0;
            SendSnapshot();Report(good,"authority / input / hair / resolution / replay");
        }
        if(reported&&authority)
        {
            if(mode!="host"){if(phaseClock>2)QuitGracefully();return;}
            if(smokeReleaseTime<0&&links.Values.All(l=>completionAcks.Contains(l.Identity.Actor)))
            {GD.Print($"SMOKE_ALL_CLIENTS_ACKNOWLEDGED {completionAcks.Count}");Rpc(MethodName.SmokeRelease);smokeReleaseTime=elapsed;}
            if(smokeReleaseTime>=0&&elapsed-smokeReleaseTime>1)QuitGracefully();
        }
    }
    void Report(bool good,string reason)
    {
        good&=VoiceSmokeGood();WriteVoiceReport();
        reported=true;GD.Print((good?"SMOKE_OK ":"SMOKE_FAIL ")+reason);
        if(reportPath!="")
        {Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath))!);File.WriteAllText(reportPath,System.Text.Json.JsonSerializer.Serialize(new{success=good,reason,mode,variant=world.Experiment.Variant.ToString(),experimentFacts=experimentFacts.OrderBy(x=>x).ToArray(),flight=world.Experiment.Flight.ToString(),liveResolved=world.Experiment.Resolved,players=world.Players.Count(p=>p.Active),round=world.Round,snapshotsReceived,grew=grew.Count,moved=moved.Count,inputPeers=receivedCount.Count,checkedLocks,elapsed,party=BPartyNetworkReport(),body=BodyNetworkReport(),secrets=SecretReport()}));}
        WriteNetReport();if(!good)QuitGracefully(1);
    }
    public void SaveCapture()
    {
        string path=Path.GetFullPath(capturePath);Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image=GetViewport().GetTexture().GetImage();image.SavePng(path);GD.Print("CAPTURE_SAVED "+path);
    }
    public override void _Process(double delta)
    {
        TickVoice(delta);
        if(quitting)return;
        if(args.ContainsKey("b-art-check"))return;
        if(running&&renderCosts.Count<36000)renderCosts.Add(delta*1000);
        if(!NetworkFrozen)RenderNetwork((float)delta);else if(salon!=null)salon.Camera.Rotation=new(pitch,yaw,0);
        if(running&&salon!=null&&world.Player(localId) is {} player)
        {
            bool captured=!NetworkFrozen&&Input.MouseMode==Input.MouseModeEnum.Captured;
            salon.Brush.Sync(world,player,salon.Camera,captured&&(Input.IsMouseButtonPressed(MouseButton.Left)||Input.IsMouseButtonPressed(MouseButton.Right)),captured&&Input.IsMouseButtonPressed(MouseButton.Right)&&!Input.IsMouseButtonPressed(MouseButton.Left),authority?simulation.Stroke(localId):null,simulation.ObstructionDistance);
        }
        if(running||(!args.ContainsKey("menu")&&!args.ContainsKey("hair-review")))return;
        elapsed+=(float)delta;
        if(capture&&elapsed>3&&lastCapture<0){lastCapture=elapsed;CallDeferred(MethodName.SaveCapture);}
        if(args.TryGetValue("quit-after-seconds",out var seconds)&&elapsed>float.Parse(seconds))QuitGracefully();
    }
}



