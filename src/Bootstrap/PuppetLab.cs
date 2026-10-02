using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using static Hairball.TargetLabGeometry;
using NV=System.Numerics.Vector3;
namespace Hairball;

// Standalone scene entry: never instantiates Main, Session, networking or microphone.
public partial class PuppetLab : Node3D
{
    readonly Camera3D camera=new(){Near=.035f,Far=30,Current=true,Fov=73};
    readonly Dictionary<string,string> args=new();readonly List<object> metrics=new();readonly List<double> frames=new(),gpu=new();readonly List<int> draws=new();
    readonly Stopwatch clock=new();Node3D subject=null!,held=null!,friend=null!,room=null!;PuppetLabHair hair=null!;Node3D? stress;
    string output="",shot="beauty";int round=1,state,index,expression,captureFrames;double start,last;bool capture,busy,gray,baseline,free;float yaw,pitch;
    PuppetLabEffects effects=null!;
    readonly List<string> shots=["beauty","gameplay","state-normal","state-trimmed","state-burnt","state-wet","state-frozen","gray","bald","baseline","micro","hair-fuzz","close","four","burn-particles","freeze-particles","expr-happy","expr-panic","expr-angry","expr-dazed"];
    readonly Dictionary<MeshInstance3D,Material?> prior=new();readonly PuppetLabSurface surface=new();
    public override void _Ready()
    {
        var raw=OS.GetCmdlineUserArgs();for(int i=0;i<raw.Length;i++)if(raw[i].StartsWith("--"))args[raw[i][2..]]=i+1<raw.Length&&!raw[i+1].StartsWith("--")?raw[++i]:"true";
        round=int.Parse(args.GetValueOrDefault("puppet-round","1"));output=Path.GetFullPath(args.GetValueOrDefault("puppet-output","artifacts/b2-puppet/round1"));Directory.CreateDirectory(output);
        ConfigureLightingStudy();
        PuppetHairSettings.Configure(args);
        PuppetLabModels.FineSurfaceSampling=round>=6;
        File.WriteAllText(Path.Combine(output,"run-args.json"),JsonSerializer.Serialize(args,new JsonSerializerOptions{WriteIndented=true}));
        GetViewport().Msaa3D=Viewport.Msaa.Msaa4X;GetViewport().PositionalShadowAtlasSize=4096;
        GetViewport().UseTaa=round>=4&&RenderingServer.GetCurrentRenderingMethod()=="forward_plus";
        room=new Node3D{Name="B2HeroStation"};AddChild(room);PuppetLabRoom.Build(room,round);
        if(round==3){PuppetLabModels.Skin.AlbedoColor=new("c29576");PuppetLabModels.Cream.AlbedoColor=new("bcb29e");PuppetLabModels.Brow.AlbedoColor=new("524051");}
        subject=PuppetLabModels.Customer(this);subject.Scale=Vector3.One*.9f;friend=new Node3D();AddChild(friend);PuppetLabModels.Friend(friend);
        hair=new PuppetLabHair(this);AddChild(camera);held=PuppetLabModels.Hands(camera,round);
        surface.Collect(subject);surface.Collect(friend);surface.Collect(held);
        if(round>=4)surface.Collect(room,true);
        foreach(var m in Descendants(subject).Concat(Descendants(held)).OfType<MeshInstance3D>())m.Layers=2;
        foreach(var m in Descendants(friend).OfType<MeshInstance3D>())m.Layers=4;
        effects=new PuppetLabEffects();AddChild(effects);
        RenderingServer.ViewportSetMeasureRenderTime(GetViewport().GetViewportRid(),true);
        if(args.ContainsKey("probe")){shots.Clear();shots.AddRange(["beauty","gameplay","gray","bald","close"]);}
        if(args.ContainsKey("hair-review")){shots.Clear();shots.AddRange(OptimizationShots);}
        if(args.TryGetValue("shot",out var singleShot)){shots.Clear();shots.Add(singleShot);}
        clock.Start();capture=!args.ContainsKey("interactive")&&!args.ContainsKey("puppet-check")&&!args.ContainsKey("reel");SetShot(capture?shots[0]:"gameplay");
        if(args.ContainsKey("interactive")&&!args.ContainsKey("r7-review"))PreparePerformance();
        GD.Print($"PUPPET_READY round={round} renderer={RenderingServer.GetCurrentRenderingMethod()} output={output}");
        if(args.ContainsKey("fur-appearance-audit")){capture=false;RunFurAppearanceAudit();}
        else if(args.ContainsKey("fur-fragments")){capture=false;RunFurFragmentReview();}
        else if(args.ContainsKey("fur-dynamics")){capture=false;RunFurMotionReview();}
        else if(args.ContainsKey("r7-review")&&args.ContainsKey("puppet-check")){capture=false;if(PuppetHairSettings.Current.VolumeShells)RunVolumeChecks();else RunShellChecks();}
        else if(args.ContainsKey("r7-review")&&!args.ContainsKey("interactive")){capture=false;RunShellReview();}
        else if(args.ContainsKey("hair-empty-check"))RunEmptyHairChecks();
        else if(args.ContainsKey("puppet-check"))RunChecks();
        if(args.ContainsKey("reel"))StartPerformance(true);
    }
    void RebuildHair(int next)
    {if(furMotion.Count>0)StopFurMotion();hair.Root.Free();hair=new PuppetLabHair(this,next);hair.SetLook(round);state=next;AttachHairPresentation();if(furInteractive)BeginInteractiveFur();}
    void Gray(bool enabled)
    {
        if(!enabled){foreach(var kv in prior)if(IsInstanceValid(kv.Key))kv.Key.MaterialOverride=kv.Value;prior.Clear();if(hair.Fuzz!=null)hair.Fuzz.Visible=round>=2&&!baseline;foreach(var shell in hair.ShellLayers)shell.Visible=!baseline;hair.Core.Visible=!hair.Settings.VolumeShells||baseline;return;}
        hair.Core.Visible=true;
        foreach(var root in new[]{subject,hair.Root})foreach(var mesh in Descendants(root).OfType<MeshInstance3D>())
        {prior[mesh]=mesh.MaterialOverride;var material=Mat("96958e",.95f);material.CullMode=BaseMaterial3D.CullModeEnum.Disabled;mesh.MaterialOverride=material;}if(hair.Fuzz!=null)hair.Fuzz.Visible=false;foreach(var shell in hair.ShellLayers)shell.Visible=false;
    }
    static IEnumerable<Node> Descendants(Node p){yield return p;foreach(var c in p.GetChildren())foreach(var n in Descendants(c))yield return n;}
    void Expression(int value)
    {
        surface.Enable(false);expression=value;subject.GetNode<Node3D>("OriginalPuppetFace").Free();var face=PuppetLabModels.Face(subject,new(0,1.97f,.035f),1,value);
        foreach(var m in Descendants(face).OfType<MeshInstance3D>()){m.Layers=2;m.GIMode=GeometryInstance3D.GIModeEnum.Disabled;}
        surface.Clear();surface.Collect(subject);surface.Collect(friend);surface.Collect(held);if(round>=4)surface.Collect(room,true);surface.Enable(round>=2&&!baseline);
        surface.SetTransport(PuppetLabSurface.TransportEnabled&&!baseline);
    }
    void ToggleBaseline()
    {
        Gray(false);gray=false;baseline=!baseline;surface.Enable(round>=2&&!baseline);hair.SetLook(round,!baseline);PuppetLabRoom.SetLighting(room,round,!baseline);effects.Visible=!baseline&&state is 2 or 4;
        ApplyLightingStudy();
        if(camera.Attributes is CameraAttributesPractical lens)lens.DofBlurFarEnabled=!baseline&&shot=="beauty";
        GetViewport().UseTaa=!baseline&&round>=4&&RenderingServer.GetCurrentRenderingMethod()=="forward_plus";
    }
    void SetShot(string name)
    {
        if(name.StartsWith("opt-")){SetOptimizationShot(name);return;}
        StopPerformance();
        ClearHairPresentation();
        Gray(false);gray=false;baseline=false;shot=name;free=false;Input.MouseMode=Input.MouseModeEnum.Visible;
        GetViewport().UseTaa=name!="baseline"&&round>=4&&RenderingServer.GetCurrentRenderingMethod()=="forward_plus";
        if(stress!=null){stress.Free();stress=null;}
        int next=name switch{"state-trimmed"=>1,"state-burnt" or "burn-particles"=>2,"state-wet"=>3,"state-frozen" or "freeze-particles"=>4,_=>0};RebuildHair(next);
        effects.SetState(next);effects.Visible=round>=3&&name.EndsWith("particles");
        if(expression!=0||name.StartsWith("expr-"))Expression(name switch{"expr-happy"=>1,"expr-panic"=>2,"expr-angry"=>3,"expr-dazed"=>4,_=>0});
        PuppetLabRoom.SetLighting(room,round,name is not ("baseline" or "micro" or "hair-fuzz"));
        held.Visible=name=="gameplay";subject.Visible=true;friend.Visible=true;hair.Root.Visible=name!="bald";
        camera.Fov=name=="gameplay"?73:43;camera.Position=name=="gameplay"?new(0,1.7f,round>=4?1.70f:1.95f):round>=4?new(.32f,2.17f,3.15f):new(.38f,2.13f,3.82f);
        camera.LookAt(name=="gameplay"?new(0,1.86f,.0f):new(0,round>=4?1.76f:1.67f,0));
        if(name.StartsWith("state-")){camera.Position=new(.25f,2.03f,2.9f);camera.Fov=41;camera.LookAt(new(0,1.90f,0));}
        if(name.StartsWith("expr-")){camera.Position=new(.1f,1.88f,2.50f);camera.Fov=41;camera.LookAt(new(0,1.85f,0));}
        if(round>=4){camera.Attributes=new CameraAttributesPractical{DofBlurFarEnabled=name=="beauty",DofBlurFarDistance=3.45f,DofBlurFarTransition=1.8f,DofBlurAmount=.07f};}
        surface.Enable(round>=2&&name!="baseline");
        if(name is "baseline" or "micro"){hair.SetLook(round,false);baseline=name=="baseline";}
        if(name=="close"){camera.Position=new(.0f,2.23f,.80f);camera.Fov=73;camera.LookAt(new(0,2.14f,0));}
        if(name=="four")
        {
            subject.Visible=false;friend.Visible=false;hair.Root.Visible=false;stress=new Node3D();AddChild(stress);
            for(int i=0;i<4;i++){var h=new PuppetLabHair(stress);h.SetLook(round);h.Root.Position=new((i%2-.5f)*1.20f,(i/2)*1.12f+1.25f,0);}
            camera.Position=new(0,2.0f,3.6f);camera.Fov=53;camera.LookAt(new(0,2.0f,0));
        }
        ApplyLightingStudy();
        if(name=="gray"){Gray(true);gray=true;}
        captureFrames=0;start=last=clock.Elapsed.TotalSeconds;frames.Clear();gpu.Clear();draws.Clear();yaw=camera.Rotation.Y;pitch=camera.Rotation.X;
        effects.GlobalTransform=hair.Root.GlobalTransform;
    }
    public override void _Process(double dt)
    {
        AnimatePerformance((float)dt);
        TickHairPresentation((float)dt);
        if(free){var move=new Vector3((Input.IsPhysicalKeyPressed(Key.D)?1:0)-(Input.IsPhysicalKeyPressed(Key.A)?1:0),0,(Input.IsPhysicalKeyPressed(Key.S)?1:0)-(Input.IsPhysicalKeyPressed(Key.W)?1:0));var position=(camera.Position+move.Rotated(Vector3.Up,yaw)*(float)dt*1.35f).Clamp(new Vector3(-3,1.7f,args.ContainsKey("r7-review")?-.8f:.8f),new Vector3(3,1.7f,5));if(args.ContainsKey("r7-review")){var planar=new Vector2(position.X,position.Z);if(planar.Length()<.55f){planar=planar.LengthSquared()<.001f?Vector2.Down*.55f:planar.Normalized()*.55f;position=new(planar.X,1.7f,planar.Y);}}camera.Position=position;}
        if(!capture||busy)return;double now=clock.Elapsed.TotalSeconds;
        // Shader/pipeline compilation can stall the first frames; it is not a steady render sample.
        if(captureFrames++<10){start=last=now;return;}
        if(now-start>CaptureWarmup){frames.Add((now-last)*1000);gpu.Add(RenderingServer.ViewportGetMeasuredRenderTimeGpu(GetViewport().GetViewportRid()));draws.Add(GetViewport().GetRenderInfo(Viewport.RenderInfoType.Visible,Viewport.RenderInfo.DrawCallsInFrame));}
        last=now;if(now-start>CaptureWarmup+1.5&&frames.Count>=60)SaveAndAdvance();
    }
    async void SaveAndAdvance()
    {
        busy=true;await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        string filename=shot switch{"beauty"=>$"beauty_round_{round}","gameplay"=>$"gameplay_round_{round}","gray"=>$"gray_silhouette_round_{round}",_=>shot};
        using var img=GetViewport().GetTexture().GetImage();img.SavePng(Path.Combine(output,filename+".png"));
        if(shot=="gameplay"){img.Resize(400,250,Image.Interpolation.Lanczos);img.SavePng(Path.Combine(output,$"thumbnail_400_round_{round}.png"));}
        if(frames.Count>0)
        {
            var sorted=frames.Order().ToArray();bool hit=hair.Raycast(camera.Position,-camera.Basis.Z,out _,out var distance);
            var env=room.GetChildren().OfType<WorldEnvironment>().Single().Environment;
            metrics.Add(new{shot,round,lightingProfile,optimization=OptimizationMetrics(),lightRig=args.GetValueOrDefault("light-rig","spot"),spatialGi=env.SdfgiEnabled,ssao=env.SsaoEnabled,ssil=env.SsilEnabled,shadowLights=room.GetChildren().OfType<Light3D>().Count(l=>l.ShadowEnabled),fineSampling=PuppetLabModels.FineSurfaceSampling,warmupSeconds=CaptureWarmup,state,gray,baseline,frameMs=frames.Average(),fps=1000/frames.Average(),p99Ms=sorted[(int)((sorted.Length-1)*.99)],gpuMs=gpu.Average(),drawCalls=draws.Average(),samples=frames.Count,hairTriangles=hair.Triangles,fibers=hair.Fibers,fiberTriangles=hair.FiberTriangles,maskCoverage=hair.MaskCoverage,particles=effects.Visible?effects.Count:0,densityHash=hair.DensityHash,fov=camera.Fov,eyeHeight=camera.Position.Y,centerRayHit=hit,hitMeters=hit?(float?)distance:null});
        }
        GD.Print("PUPPET_CAPTURE "+filename);index++;
        if(index<shots.Count){busy=false;SetShot(shots[index]);return;}
        File.WriteAllText(Path.Combine(output,"metrics.json"),JsonSerializer.Serialize(new{engine=Engine.GetVersionInfo()["string"].AsString(),renderer=RenderingServer.GetCurrentRenderingMethod(),device=RenderingServer.GetVideoAdapterName(),width=GetViewport().GetVisibleRect().Size.X,height=GetViewport().GetVisibleRect().Size.Y,simulation=false,shellLayers=PuppetHairSettings.Current.Shells,shots=metrics},new JsonSerializerOptions{WriteIndented=true}));
        if(args.ContainsKey("hair-motion"))await CaptureHairSamplingMotion();
        GD.Print("PUPPET_CAPTURE_COMPLETE "+output);QuitCleanly();
    }
    public override void _UnhandledInput(InputEvent e)
    {
        if(e is InputEventMouseMotion m&&free){yaw-=m.Relative.X*.0024f;pitch=Math.Clamp(pitch-m.Relative.Y*.0024f,-1.15f,1.15f);camera.Rotation=new(pitch,yaw,0);}
        if(e is InputEventMouseButton{Pressed:true,ButtonIndex:MouseButton.Left}&&free)
        {
            var d=-camera.Basis.Z;
            if(hair.Raycast(camera.Position,d,out var p,out _))hair.Cut(p,-d);
        }
        if(e is not InputEventKey{Pressed:true,Echo:false} k)return;
        if(HandleFurKey(k.Keycode))return;
        // R7.1 has not passed its gate for performances or material states.
        if(args.ContainsKey("r7-review")&&k.Keycode is Key.Space or Key.Key3 or Key.Key4 or Key.Key5)return;
        if(args.ContainsKey("r7-review")&&k.Keycode is Key.Key1 or Key.Key2 or Key.Key6){Gray(false);gray=false;RebuildHair(k.Keycode==Key.Key2?1:k.Keycode==Key.Key6?5:0);effects.Visible=false;return;}
        if(k.Keycode==Key.Space){if(performing)SetShot("gameplay");else StartPerformance();}
        if(k.Keycode==Key.F1)SetShot("beauty");if(k.Keycode==Key.F2)SetShot("gameplay");
        if(k.Keycode>=Key.Key1&&k.Keycode<=Key.Key5)SetShot(new[]{"state-normal","state-trimmed","state-burnt","state-wet","state-frozen"}[(int)(k.Keycode-Key.Key1)]);
        if(k.Keycode==Key.F5){gray=!gray;Gray(gray);}if(k.Keycode==Key.F6)SetShot("bald");
        if(k.Keycode==Key.F4)ToggleBaseline();
        if(k.Keycode==Key.F7)effects.Visible=!effects.Visible;
        if(k.Keycode==Key.Q){StopPerformance();Gray(false);gray=false;Expression((expression+1)%5);}
        if(k.Keycode==Key.Tab){SetShot("gameplay");free=true;Input.MouseMode=Input.MouseModeEnum.Captured;}
        if(k.Keycode==Key.Escape){free=false;Input.MouseMode=Input.MouseModeEnum.Visible;}
        if(k.Keycode==Key.R)SetShot("gameplay");
        if(PuppetHairSettings.Current.Enabled){if(k.Keycode==Key.Key6)SetOptimizationShot("opt-carved-gameplay");if(k.Keycode==Key.F11)SetOptimizationShot("opt-detach-warning");if(k.Keycode==Key.F12&&shot.StartsWith("opt-detach-"))hair.SeverWarningFixture();}
    }
    async void RunChecks()
    {
        try
        {
            for(int i=0;i<4;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            async System.Threading.Tasks.Task KeyPress(Key key)
            {Input.ParseInputEvent(new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=true});await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);Input.ParseInputEvent(new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=false});await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
            void Require(bool okay,string name){if(!okay)throw new InvalidOperationException(name);GD.Print("PUPPET_CHECK_PASS "+name);}
            string density=hair.DensityHash;var vertices=hair.Core.Mesh.SurfaceGetArrays(0)[(int)Godot.Mesh.ArrayType.Vertex].AsVector3Array();
            await KeyPress(Key.F4);Require(baseline&&hair.Core.MaterialOverride is StandardMaterial3D&&hair.Fuzz?.Visible==false,"native F4 disables core shader and fuzz");
            Require(room.GetChildren().OfType<WorldEnvironment>().Single().Environment.SsaoEnabled==false,"baseline disables hero SSAO");
            Require(!room.GetChildren().OfType<WorldEnvironment>().Single().Environment.SdfgiEnabled,"baseline disables spatial GI");
            Require(!GetViewport().UseTaa&&(!(camera.Attributes is CameraAttributesPractical lens)||!lens.DofBlurFarEnabled),"baseline disables TAA and camera blur");
            Require(hair.DensityHash==density,"visual toggle preserves density");
            await KeyPress(Key.F4);Require(!baseline&&hair.Core.MaterialOverride is ShaderMaterial&&hair.Fuzz?.Visible==true,"native F4 restores candidate");
            Require(room.GetChildren().OfType<WorldEnvironment>().Single().Environment.SdfgiEnabled==SpatialGi,"F4 restores the selected GI profile");
            if(SpatialGi)Require(new[]{subject,friend,held,hair.Root}.SelectMany(Descendants).OfType<GeometryInstance3D>().All(m=>m.GIMode==GeometryInstance3D.GIModeEnum.Disabled),"deforming actors receive but never inject stale SDFGI silhouettes");
            Require(vertices.SequenceEqual(hair.Core.Mesh.SurfaceGetArrays(0)[(int)Godot.Mesh.ArrayType.Vertex].AsVector3Array()),"toggle preserves actual mesh positions");
            var before=Descendants(subject).OfType<MeshInstance3D>().ToDictionary(m=>m,m=>m.MaterialOverride);
            await KeyPress(Key.F5);Require(gray&&hair.Fuzz?.Visible==false,"gray disables fuzz");await KeyPress(Key.F5);
            Require(before.All(kv=>kv.Key.MaterialOverride==kv.Value),"gray restores all overrides including imported null overrides");
            await KeyPress(Key.Q);await KeyPress(Key.F4);
            Require(Descendants(subject).OfType<MeshInstance3D>().All(m=>m.MaterialOverride is not ShaderMaterial),"expression then baseline restores all body materials");await KeyPress(Key.F4);
            await KeyPress(Key.Tab);Require(free&&Math.Abs(camera.Fov-73)<.001f&&Math.Abs(camera.Position.Y-1.7f)<.001f,"native Tab activates fixed-height real FPS");
            camera.LookAt(hair.Root.ToGlobal(new Vector3(-.12f,.32f,.20f)));yaw=camera.Rotation.Y;pitch=camera.Rotation.X;
            float mass=hair.Volume.Mass;
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using(var img=GetViewport().GetTexture().GetImage())img.SavePng(Path.Combine(output,"native-cut-before.png"));
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true});await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false});await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Require(hair.Volume.Mass<mass&&hair.DensityHash!=density,"actual mouse input cuts the local density");
            int backgroundFrames=0;var cutWait=Stopwatch.StartNew();while(hair.IsFuzzBuilding&&cutWait.Elapsed.TotalSeconds<3){backgroundFrames++;await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
            Require(!hair.IsFuzzBuilding&&backgroundFrames>0,"fuzz rebuilt in background while native frames continued");
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using(var img=GetViewport().GetTexture().GetImage())img.SavePng(Path.Combine(output,"native-cut-after.png"));
            Require(hair.Fuzz!=null&&hair.Fibers>0&&hair.ShortFibers>0,"cut regenerates short nap on the actual cut plane");
            double cutBuildMs=hair.LastCutBuildMs,cutReadyMs=hair.LastCutReadyMs;float cutMassAfter=hair.Volume.Mass;int shortFibers=hair.ShortFibers;
            GD.Print($"PUPPET_CUT_BUILD_MS {cutBuildMs:F3}");
            await KeyPress(Key.R);Require(hair.DensityHash==density,"native R restores fixture density");
            await KeyPress(Key.Tab);camera.LookAt(hair.Root.ToGlobal(new Vector3(-.12f,.32f,.20f)));var retiredHair=hair;
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true});await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false});
            Require(retiredHair.IsFuzzBuilding,"second native cut starts asynchronous geometry work");await KeyPress(Key.R);
            var retireWait=Stopwatch.StartNew();while(retiredHair.IsFuzzBuilding&&retireWait.Elapsed.TotalSeconds<3)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Require(!retiredHair.IsFuzzBuilding&&hair.DensityHash==density&&hair.Root.IsInsideTree(),"reset during rebuild safely discards retired hair result");
            await KeyPress(Key.Space);Require(performing&&activePerformanceFace!=null&&performanceHair.Count==5,"native Space starts cached puppet performance");
            var performanceWait=Stopwatch.StartNew();while(lastBeat<2&&performanceWait.Elapsed.TotalSeconds<8)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Require(lastBeat==2&&state==3&&effects.Spraying&&performanceFaces.Values.Count(f=>f.Visible)==1,"live rescue beat has one face and visible spray");
            await KeyPress(Key.F4);Require(baseline&&!effects.Visible&&!GetViewport().UseTaa,"baseline disables effects during live performance");
            await KeyPress(Key.R);Require(!performing&&performanceHair.Values.All(h=>!h.Root.Visible)&&subject.GetNode<Node3D>("OriginalPuppetFace").Visible&&GetViewport().UseTaa,"native R leaves performance and restores FPS candidate");
            await KeyPress(Key.F1);Require(camera.Attributes is CameraAttributesPractical beautyLens&&beautyLens.DofBlurFarEnabled,"beauty camera actually enables background depth of field");
            await KeyPress(Key.F4);Require(camera.Attributes is CameraAttributesPractical basicLens&&!basicLens.DofBlurFarEnabled&&Descendants(room).OfType<MeshInstance3D>().All(m=>m.MaterialOverride is not ShaderMaterial),"baseline removes beauty blur and every room shader including window");
            CheckOptimization();
            await CheckEmptyHairSurfaces();
            File.WriteAllText(Path.Combine(output,"native-check.json"),JsonSerializer.Serialize(new{pass=true,density,cutMassBefore=mass,cutMassAfter,cutBuildMs,cutReadyMs,backgroundFrames,shortFibers,round,renderer=RenderingServer.GetCurrentRenderingMethod()}));
            GD.Print("PUPPET_NATIVE_CHECKS_PASS");QuitCleanly();
        }
        catch(Exception e){GD.PushError("PUPPET_NATIVE_CHECKS_FAIL "+e);GetTree().Quit(1);}
    }
}
