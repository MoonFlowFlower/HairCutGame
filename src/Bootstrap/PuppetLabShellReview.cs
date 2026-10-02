using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
namespace Hairball;

public partial class PuppetLab
{
    static object V(Vector3 v)=>new{x=v.X,y=v.Y,z=v.Z};
    List<PuppetLabHair> ShellReviewHeads()=>stressHairs.Count>0?stressHairs:new(){hair};
    Vector3[] WorldCore(PuppetLabHair h)=>h.Core.Mesh.GetSurfaceCount()==0?Array.Empty<Vector3>():h.Core.Mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array().Select(h.Root.ToGlobal).ToArray();
    float NearestCore()=>WorldCore(hair).Min(v=>v.DistanceTo(camera.GlobalPosition));
    void SetShellReviewShot(string name)
    {
        SetShot("gameplay");shot=name;capture=false;busy=true;
        int s=name.Contains("trimmed")?1:name.Contains("carved")?5:0;
        RebuildHair(s);state=s;effects.Visible=false;
        camera.Fov=73;held.Visible=true;
        if(name.EndsWith("rear")){
            // A reachable standing first-person pose behind the customer.
            // The previous frontal matrix missed scalp seams and lamp shadows.
            camera.Position=new(.36f,1.7f,-.68f);camera.LookAt(new(0,2.10f,0));
        }else if(name.EndsWith("top")){
            camera.Position=new(.12f,2.75f,.90f);camera.LookAt(new(0,2.18f,0));held.Visible=false;
        }else{
            float az=name.EndsWith("side")?1.05f:name.EndsWith("close")?.75f:name.EndsWith("low")?-.22f:0;
            float distance=name.EndsWith("close")?.48f:name.EndsWith("low")?.50f:1.0f;
            // Solve against the actual current mesh in world units; the 0.14
            // density step is scaled /3.5 *0.9 in this scene.
            // The head's face reaches z=0.45. Stay in the same z>=0.55
            // walkable half-space as interactive R7; never solve inside it.
            float lo=.55f/MathF.Cos(az),hi=2.6f;
            for(int i=0;i<24;i++){
                float radius=(lo+hi)*.5f;
                camera.Position=new(MathF.Sin(az)*radius,1.7f,MathF.Cos(az)*radius);
                if(NearestCore()>distance)hi=radius;else lo=radius;
            }
            camera.LookAt(new(0,name.EndsWith("low")?2.09f:2.17f,.06f));
        }
        if(name.StartsWith("r7-perf-")){
            int count=name.Contains("four")?4:name.Contains("two")?2:1;
            // Keep a close foreground customer, FPS hands and the same room.
            // Neighbor samples crowd the remaining field of view; no distant
            // four-ball framing is used as a proxy for this workload.
            // Selected by the real-mask framing scan: 30.5 cm from the core,
            // no camera intersection, fixed FPS eye height and field of view.
            camera.Position=new(.59510607f,1.7f,-.083380245f);camera.LookAt(new(.16f,2.2f,0));
            if(count>1){
                // Several visible heads in the standing FPS field of view.
                // Keeping the single-head macro pose hid almost all peers.
                camera.Position=new(.08f,1.7f,1.10f);camera.LookAt(new(0,2.22f,0));
                stress=new Node3D();AddChild(stress);stressHairs.Add(hair);
                var positions=new[]{new Vector3(-.78f,1.94f,.12f),new Vector3(.81f,2.02f,.08f),new Vector3(-.07f,2.98f,-.52f)};
                for(int i=1;i<count;i++){
                    var extra=new PuppetLabHair(stress,0,null,PuppetHairSettings.Current);extra.SetLook(round);extra.Root.Position=positions[i-1];stressHairs.Add(extra);
                }
            }
        }
        if(camera.Attributes is CameraAttributesPractical lens)lens.DofBlurFarEnabled=false;
        yaw=camera.Rotation.Y;pitch=camera.Rotation.X;
    }
    async Task DrawFrames(int count=2)
    {
        for(int i=0;i<count;i++){
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        }
    }
    async Task SaveShellPng(string name)
    {
        await DrawFrames();using var image=GetViewport().GetTexture().GetImage();
        if(image.SavePng(Path.Combine(output,name+".png"))!=Error.Ok)throw new IOException(name);
    }
    async Task NativeShellCut(int repeats=1)
    {
        if(fragments!=null)fragments.FreezeAge=0;
        camera.LookAt(hair.Root.ToGlobal(new Vector3(-.12f,.32f,.20f)));yaw=camera.Rotation.Y;pitch=camera.Rotation.X;
        string prefix=repeats>1?"r7-depth":"r7-native";
        await SaveShellPng(prefix+"-before");string before=hair.DensityHash;var edits=new List<object>();
        for(int i=0;i<repeats;i++){
            string previous=hair.DensityHash;free=true;
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true});
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false});free=false;
            double begin=clock.Elapsed.TotalSeconds;
            while(hair.IsFuzzBuilding&&clock.Elapsed.TotalSeconds-begin<10)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            if(hair.IsFuzzBuilding||hair.DensityHash==previous)throw new InvalidOperationException("R7 native input did not complete a real cut");
            edits.Add(new{cut=i+1,before=previous,after=hair.DensityHash,hair.LastCutBuildMs,hair.LastCutReadyMs,hair.TrimVertices,hair.InteriorVertices});
            if(repeats>1)await SaveShellPng(prefix+"-cut-"+(i+1));
        }
        if(fragments!=null){fragments.FreezeAge=.42f;fragments.Tick(0);}
        File.WriteAllText(Path.Combine(output,prefix+"-input.json"),JsonSerializer.Serialize(new{before,after=hair.DensityHash,hair.LastCutBuildMs,hair.LastCutReadyMs,hair.TrimVertices,hair.InteriorVertices,edits,fragmentStaticAge=.42f}));
    }
    async Task<object> ShellSilhouette(string name,IReadOnlyList<PuppetLabHair>? targetHeads=null)
    {
        var heads=targetHeads??ShellReviewHeads();var meshes=heads.SelectMany(h=>new GeometryInstance3D?[]{h.Core,h.Fuzz}.Concat(h.ShellLayers)).Where(m=>m!=null).Cast<GeometryInstance3D>().ToHashSet();
        var other=Descendants(this).OfType<MeshInstance3D>().Where(m=>!meshes.Contains(m)).ToDictionary(m=>m,m=>m.MaterialOverride);
        var otherInstances=Descendants(this).OfType<MultiMeshInstance3D>().Where(m=>!meshes.Contains(m)).ToDictionary(m=>m,m=>m.MaterialOverride);
        var black=new ShaderMaterial{Shader=GD.Load<Shader>("res://shaders/puppet_mask_occluder.gdshader")};
        var worldEnv=room.GetChildren().OfType<WorldEnvironment>().Single();var previous=worldEnv.Environment;
        bool taa=GetViewport().UseTaa;var visibility=meshes.ToDictionary(m=>m,m=>m.Visible);
        bool fragmentsPaused=fragments?.Paused??false;if(fragments!=null)fragments.Paused=true;
        try{
            foreach(var m in other.Keys)m.MaterialOverride=black;
            foreach(var m in otherInstances.Keys)m.MaterialOverride=black;
            worldEnv.Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=Colors.Black,TonemapMode=Godot.Environment.ToneMapper.Linear};
            GetViewport().UseTaa=false;
            foreach(var m in meshes)if(m.MaterialOverride is ShaderMaterial material)material.SetShaderParameter("mask_pass",true);
            await SaveShellPng(name+"-tips-mask");
            if(hair.Settings.VolumeShells){
                foreach(var h in heads)if(h.VolumeFur?.Node.MaterialOverride is ShaderMaterial material)material.SetShaderParameter("coverage_pass",true);
                await SaveShellPng(name+"-volume-coverage-mask");
                foreach(var h in heads)if(h.VolumeFur?.Node.MaterialOverride is ShaderMaterial material)material.SetShaderParameter("coverage_pass",false);
            }
            if(name.StartsWith("r7-perf-")){
                var white=new ShaderMaterial{Shader=GD.Load<Shader>("res://shaders/puppet_mask_occluder.gdshader")};white.SetShaderParameter("white",true);
                var faceMeshes=Descendants(subject.GetNode<Node3D>("OriginalPuppetFace")).OfType<MeshInstance3D>().ToArray();
                foreach(var m in faceMeshes)m.MaterialOverride=white;
                await SaveShellPng(name+"-head-mask");
                foreach(var m in faceMeshes)m.MaterialOverride=black;
            }
            foreach(var h in heads){h.Core.Visible=true;if(h.Fuzz!=null)h.Fuzz.Visible=false;foreach(var layer in h.ShellLayers)layer.Visible=false;}
            await SaveShellPng(name+"-core-mask");
            foreach(var h in heads)if(h.Core.MaterialOverride is ShaderMaterial material)material.SetShaderParameter("depth_pass",true);
            await SaveShellPng(name+"-core-depth");
        }finally{
            foreach(var kv in other)kv.Key.MaterialOverride=kv.Value;
            foreach(var kv in otherInstances)kv.Key.MaterialOverride=kv.Value;
            foreach(var kv in visibility)kv.Key.Visible=kv.Value;
            foreach(var m in meshes)if(m.MaterialOverride is ShaderMaterial material)material.SetShaderParameter("mask_pass",false);
            foreach(var h in heads)if(h.VolumeFur?.Node.MaterialOverride is ShaderMaterial material)material.SetShaderParameter("coverage_pass",false);
            foreach(var h in heads)if(h.Core.MaterialOverride is ShaderMaterial material)material.SetShaderParameter("depth_pass",false);
            worldEnv.Environment=previous;GetViewport().UseTaa=taa;
            if(fragments!=null)fragments.Paused=fragmentsPaused;
        }
        var points=heads.SelectMany(WorldCore).ToArray();var depth=points.Select(p=>-camera.ToLocal(p).Z).Where(z=>z>0).ToArray();
        float worldScale=hair.Root.GlobalBasis.Scale.X;
        return new{
            actualViewportMasks=true,taaDisabledForMaskOnly=true,otherObjectsKeepDepthOcclusion=true,staticPoseAcrossPasses=true,
            wholeHeadMask=name.StartsWith("r7-perf-"),wholeHeadDefinition="Visible main customer face + hair; extra hair instances included for multi-head stress. Body/room/hands retain occlusion.",
            camera=V(camera.GlobalPosition),right=V(camera.GlobalBasis.X),up=V(camera.GlobalBasis.Y),forward=V(-camera.GlobalBasis.Z),
            minCoreDepth=depth.Min(),maxCoreDepth=depth.Max(),fov=camera.Fov,
            densityStepWorld=HairVolume.Step/PuppetLabHair.Scale*worldScale,
            shellOffsetUpperBoundMeters=hair.Settings.ShellFur?(hair.Settings.PlushTufts?PuppetShellFur.PlushPile*1.04f:PuppetShellFur.MaximumLocalOffset)*worldScale:0,
            volumeOriginalTipExtensionLimitMeters=hair.Settings.VolumeShells&&hair.Settings.BundleField ? .040f*worldScale : 0,
            cosmeticMotionGuideOffsetLimitMeters=furMotion.Count>0?PuppetFurMotion.MaxOffset*worldScale:0,
            actualShortFiberRootOffsetMaxMeters=hair.Settings.VolumeShells?(float?)null:hair.MaximumShortFiberOffset*worldScale,
            fiberCountDefinition=hair.Settings.TuftHybrid?"instanced Shell plus fixed curved ribbon fins throughout ORIGINAL volume; fins and shells counted separately":hair.Settings.TuftReference?"fixed finite volume curves rendered as geometry; NOT Shell":hair.Settings.VolumeShells?"implicit persistent follicles sampled by instanced shells; no ribbon fibers":hair.Settings.PlushTufts?"individual curved ribbons arranged in five-filament tufts":"individual crossed ribbons",
            actualFiberRootOffsetMaxMeters=hair.Settings.VolumeShells?(float?)null:hair.MaximumFiberOffset*worldScale,
            fiberRootAtMaximum=V(hair.Root.ToGlobal(hair.MaximumFiberRoot)),fiberTipAtMaximum=V(hair.Root.ToGlobal(hair.MaximumFiberTip)),
            depthImageRangeMeters=8,depthImageEncoding="linear view depth / 8 emitted into sRGB PNG; inverse sRGB before use",
            note="PNG silhouette measures visible tips with scene occlusion. Nearest core edge depth scales screen pixels to a local view-plane cm estimate, not a 3D SDF. Root displacement bounds reported separately."
        };
    }
    async void RunShellReview()
    {
        capture=false;busy=true;
        if(args.GetValueOrDefault("shot")=="r7-framing-scan"){RunShellFramingScan();return;}
        try{
            string[] names=args.TryGetValue("shot",out var single)?new[]{single}:args.ContainsKey("volume-pilot")?new[]{"r7-normal-close","r7-trimmed-close","r7-carved-close"}:
                new[]{"normal","trimmed","carved"}.SelectMany(s=>new[]{"front","close","side","top","low"}.Select(v=>$"r7-{s}-{v}")).Concat(new[]{"r7-native-after","r7-native-deep"}).ToArray();
            var results=new List<object>();
            foreach(string name in names){
                SetShellReviewShot(name);
                if(name=="r7-native-after")await NativeShellCut();
                if(name=="r7-native-deep")await NativeShellCut(3);
                await DrawFrames(3);
                double begin=clock.Elapsed.TotalSeconds,last=begin;
                var ft=new List<double>();var gt=new List<double>();var ct=new List<double>();var memory=new List<double>();var dc=new List<int>();var primitives=new List<int>();
                while(clock.Elapsed.TotalSeconds-begin<6||ft.Count<120){
                    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    double now=clock.Elapsed.TotalSeconds;
                    if(now-begin>4){ft.Add((now-last)*1000);gt.Add(RenderingServer.ViewportGetMeasuredRenderTimeGpu(GetViewport().GetViewportRid()));ct.Add(RenderingServer.ViewportGetMeasuredRenderTimeCpu(GetViewport().GetViewportRid())+RenderingServer.GetFrameSetupTimeCpu());memory.Add(Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed));dc.Add(GetViewport().GetRenderInfo(Viewport.RenderInfoType.Visible,Viewport.RenderInfo.DrawCallsInFrame));primitives.Add(GetViewport().GetRenderInfo(Viewport.RenderInfoType.Visible,Viewport.RenderInfo.PrimitivesInFrame));}
                    last=now;
                }
                await SaveShellPng(name);
                var heads=ShellReviewHeads();var sorted=ft.Order().ToArray();
                bool hit=hair.Raycast(camera.GlobalPosition,-camera.GlobalBasis.Z,out var hp,out float hm);
                var silhouette=await ShellSilhouette(name);
                results.Add(new{shot=name,state,mode=hair.Settings.VisualMode,shells=!hair.Settings.DiagnosticNonShell&&(hair.Settings.ShellFur||hair.Settings.VolumeShells)?hair.ShellCount:0,curveReference=hair.Settings.TuftReference,heads=heads.Count,
                    firstPerson=held.Visible&&Math.Abs(camera.Position.Y-1.7f)<.001f,fov=camera.Fov,eyeHeight=camera.Position.Y,nearestCoreMeters=NearestCore(),centerRayHit=hit,centerHitMeters=hit?(float?)hm:null,centerHitPoint=hit?V(hp):null,
                    gpuMs=gt.Average(),frameMs=ft.Average(),fps=1000/ft.Average(),p99Ms=sorted[(int)((sorted.Length-1)*.99)],drawCalls=dc.Average(),visiblePrimitives=primitives.Average(),samples=ft.Count,
                    renderCpuMs=ct.Average(),engineVideoMemoryMiB=memory.Average()/1048576,performanceNotes="CPU includes viewport render + frame setup only, excludes game logic. Memory is engine allocation monitor, not process residency. 4 s warmup + >=2 s sample, VSync off.",
                    totalCoreTriangles=heads.Sum(h=>h.Triangles),totalFiberTriangles=heads.Sum(h=>h.FiberTriangles),totalFibers=heads.Sum(h=>h.Fibers),shellTriangles=heads.Where(h=>!h.Settings.DiagnosticNonShell).Sum(h=>h.ShellTriangles),referenceTriangles=heads.Where(h=>h.Settings.DiagnosticNonShell).Sum(h=>h.ShellTriangles),
                    fixedVolumeFinTriangles=heads.Sum(h=>h.VolumeFur?.FinTriangles??0),
                    finCenterlineSimplificationBoundMeters=hair.Settings.TuftHybrid?(float?)(PuppetTuftVolume.FinSimplificationBound*hair.Root.GlobalBasis.Scale.X):null,
                    finMeanSegments=hair.Settings.TuftHybrid?(float?)PuppetTuftVolume.FinMeanSegments:null,
                    tuftVolumeResolution=hair.Settings.TuftVolume?(int?)PuppetTuftVolume.Size:null,
                    densityHash=hair.DensityHash,hair.TrimVertices,hair.InteriorVertices,hair.LastCutBuildMs,hair.LastCutReadyMs,silhouette});
                File.WriteAllText(Path.Combine(output,"r7-metrics.json"),JsonSerializer.Serialize(new{stage="R7.1",engine=Engine.GetVersionInfo()["string"].AsString(),renderer=RenderingServer.GetCurrentRenderingMethod(),device=RenderingServer.GetVideoAdapterName(),width=GetViewport().GetVisibleRect().Size.X,height=GetViewport().GetVisibleRect().Size.Y,settings=PuppetHairSettings.Current,shots=results},new JsonSerializerOptions{WriteIndented=true}));
                GD.Print($"R7_CAPTURE {name} gpuMs={gt.Average():F3} fps={1000/ft.Average():F1} shells={(hair.Settings.DiagnosticNonShell?0:hair.ShellCount)} mode={hair.Settings.VisualMode} nearest={NearestCore():F3}");
            }
            if(args.ContainsKey("hair-motion")){
                // Mask passes replace the environment. Restore GI/TAA history
                // before the stationary and moving sampling sequence begins.
                double settle=clock.Elapsed.TotalSeconds;
                while(clock.Elapsed.TotalSeconds-settle<4)await DrawFrames();
                File.WriteAllText(Path.Combine(output,"r7-motion-warmup.json"),JsonSerializer.Serialize(new{seconds=clock.Elapsed.TotalSeconds-settle,afterMaskEnvironmentRestored=true}));
                await CaptureHairSamplingMotion();
            }
            GD.Print("R7_REVIEW_COMPLETE");QuitCleanly();
        }catch(Exception e){GD.PushError("R7_REVIEW_FAIL "+e);GetTree().Quit(1);}
    }

    // Camera selection only, not a performance/visual acceptance run. Searches
    // real walkable FPS poses with scene occlusion, never changes head scale.
    async void RunShellFramingScan()
    {
        try{
            SetShellReviewShot("r7-normal-close");
            var meshes=new GeometryInstance3D?[]{hair.Core,hair.Fuzz}.Concat(hair.ShellLayers).Where(m=>m!=null).Cast<GeometryInstance3D>().ToHashSet();
            var other=Descendants(this).OfType<MeshInstance3D>().Where(m=>!meshes.Contains(m)).ToDictionary(m=>m,m=>m.MaterialOverride);
            var worldEnv=room.GetChildren().OfType<WorldEnvironment>().Single();var previous=worldEnv.Environment;
            var rows=new List<(float Coverage,Vector3 Position,Vector3 Target,float Distance)>();
            try{
                var black=new ShaderMaterial{Shader=GD.Load<Shader>("res://shaders/puppet_mask_occluder.gdshader")};
                foreach(var m in other.Keys)m.MaterialOverride=black;
                foreach(var m in meshes)if(m.MaterialOverride is ShaderMaterial material)material.SetShaderParameter("mask_pass",true);
                worldEnv.Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=Colors.Black,TonemapMode=Godot.Environment.ToneMapper.Linear};GetViewport().UseTaa=false;
                for(int a=0;a<24;a++){
                    float angle=(a<12?1:-1)*(.8f+(a%12)*.13f),lo=.55f,hi=2;
                    for(int i=0;i<24;i++){float r=(lo+hi)*.5f;camera.Position=new(MathF.Sin(angle)*r,1.7f,MathF.Cos(angle)*r);if(NearestCore()>.305f)hi=r;else lo=r;}
                    if(NearestCore()>.501f)continue;
                    for(int y=0;y<7;y++)for(int x=0;x<3;x++){
                        var target=new Vector3((x-1)*.16f,2.00f+y*.10f,0);camera.LookAt(target);await DrawFrames(2);
                        using var picture=GetViewport().GetTexture().GetImage();picture.Convert(Image.Format.Rgb8);var bytes=picture.GetData();int white=0;
                        for(int i=0;i<bytes.Length;i+=3)if(bytes[i]>=188&&bytes[i+1]>=188&&bytes[i+2]>=188)white++;
                        rows.Add((white*300f/bytes.Length,camera.Position,target,NearestCore()));
                    }
                }
            }finally{
                foreach(var kv in other)kv.Key.MaterialOverride=kv.Value;
                foreach(var m in meshes)if(m.MaterialOverride is ShaderMaterial material)material.SetShaderParameter("mask_pass",false);
                worldEnv.Environment=previous;GetViewport().UseTaa=true;
            }
            var sorted=rows.OrderByDescending(r=>r.Coverage).ToArray();var best=sorted[0];camera.Position=best.Position;camera.LookAt(best.Target);await DrawFrames(60);await SaveShellPng("framing-best");
            File.WriteAllText(Path.Combine(output,"framing-scan.json"),JsonSerializer.Serialize(sorted.Select(r=>new{r.Coverage,position=V(r.Position),target=V(r.Target),r.Distance}),new JsonSerializerOptions{WriteIndented=true}));
            GD.Print($"R7_FRAMING_BEST coverage={best.Coverage:F2} distance={best.Distance:F3} position={best.Position} target={best.Target}");QuitCleanly();
        }catch(Exception e){GD.PushError("R7_FRAMING_FAIL "+e);GetTree().Quit(1);}
    }
}
