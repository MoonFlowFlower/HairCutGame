using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
namespace Hairball;

public partial class PuppetLab
{
    readonly List<PuppetFurMotion> furMotion=new();
    readonly List<object> furMotionRows=new();
    StaticBody3D? furContact;
    MeshInstance3D? furContactVisual;
    const float FurContactRadius=.10f;
    float FurContactDepth=>args.ContainsKey("fur-contact-deep")?.050f:.025f;
    Vector3 furContactTarget,furContactNormal;
    float furCutExposureDepth;
    double furMotionStart,furPhysicsTime;
    double furContactCommandTime;
    bool furContactApplied;
    bool furMotionRunning,furMotionPerf;
    bool furInteractive,furInteractiveWind,furInteractiveContact;
    float furInteractiveApproach;
    Label? furInteractiveLabel;
    int furGuide,furPhysicsFrame,furContactTicks;
    string FurMotionPhase(double t)=>t<1?"rest":t<2.2?"wind":t<4.7?"recover_wind":t<5.5?"contact_enter":t<6.5?"contact_hold":t<7.3?"contact_leave":t<9.8?"recover_contact":t<11.4?"reverse_pulses":t<14.4?"recover_pulses":t<15.4?"wind_cut":"recover_cut";
    Vector3 FurWind(double t)=>t>=1&&t<2.2?new(.9f,0,.2f):t>=9.8&&t<11.4?new(((int)((t-9.8)/.2)%2==0?1:-1)*.85f,0,.1f):t>=14.4&&t<15.4?new(-.75f,0,.2f):Vector3.Zero;
    void StopFurMotion()
    {
        furMotionRunning=false;
        foreach(var motion in furMotion)motion.Dispose();furMotion.Clear();
        furContact?.Free();furContact=null;
        furContactVisual?.Free();furContactVisual=null;
    }
    void PrepareFurMotion(bool perf)
    {
        if(!hair.Settings.VolumeShells)throw new InvalidOperationException("Fur dynamics require the persistent volume renderer");
        if(hair.Settings.DiagnosticNonShell)throw new InvalidOperationException("Reference geometry modes are static sampling diagnostics");
        furMotionPerf=perf;furMotionRows.Clear();furPhysicsFrame=0;furPhysicsTime=0;furContactTicks=0;
        foreach(var head in ShellReviewHeads())furMotion.Add(new PuppetFurMotion(head){ResponseEnabled=!args.ContainsKey("fur-no-response"),CutFeedbackEnabled=!args.ContainsKey("fur-no-cut-feedback")});
        furGuide=furMotion[0].NearestGuide(new Vector3(.23f,.32f,.30f));
        if(furGuide<0)throw new InvalidOperationException("No occupied hair guide for contact");
        furContactTarget=furMotion[0].TipWorld(furGuide);
        furContactNormal=(hair.Root.GlobalBasis*(furMotion[0].RestTips[furGuide]-PuppetVolumeFur.RootCenter)).Normalized();
        furCutExposureDepth=0;
        if(args.ContainsKey("fur-contact-deep")){
            // The conservative guide envelope can sit beyond the visible
            // tufts. Ground this stronger diagnostic in an actual FPS hit,
            // so a correct guide number cannot substitute for visible contact.
            var screen=GetViewport().GetVisibleRect().Size*new Vector2(.58f,.32f);
            if(args.ContainsKey("fur-contact-cut")){
                float best=-1;
                for(float y=.12f;y<=.48f;y+=.02f)for(float x=.25f;x<=.72f;x+=.02f){
                    var test=GetViewport().GetVisibleRect().Size*new Vector2(x,y);
                    if(!hair.Raycast(camera.GlobalPosition,camera.ProjectRayNormal(test),out var hit,out _))continue;
                    var p=hair.Root.ToLocal(hit);
                    if(!hair.VolumeFur!.WasOriginalInterior(p))continue;
                    var radial=p-PuppetVolumeFur.RootCenter;float r=(radial/new Vector3(.445f,.45f,.322f)).Length();
                    var ray=radial/r;float scale=hair.Root.GlobalBasis.Scale.X;
                    if((r-.98f)*ray.Length()*scale<.09f)continue;
                    float depth=(hair.VolumeFur.OriginalEnd(ray)-r)*ray.Length()*scale;
                    if(depth<=.04f||depth<=best)continue;
                    best=depth;screen=test;furCutExposureDepth=depth;
                }
                if(best<0)throw new InvalidOperationException("Fixed FPS camera has no visible exposed cut wall with root clearance");
            }else if(args.ContainsKey("fur-contact-edge")){
                bool found=false;
                for(float x=.82f;x>=.55f;x-=.01f){
                    var test=GetViewport().GetVisibleRect().Size*new Vector2(x,.28f);
                    if(!hair.Raycast(camera.GlobalPosition,camera.ProjectRayNormal(test),out _,out _))continue;
                    screen=GetViewport().GetVisibleRect().Size*new Vector2(x-.025f,.28f);found=true;break;
                }
                if(!found)throw new InvalidOperationException("Edge contact has no visible hair silhouette ray");
            }
            if(!hair.Raycast(camera.GlobalPosition,camera.ProjectRayNormal(screen),out furContactTarget,out _))throw new InvalidOperationException("Deep contact has no visible-volume ray hit");
            var local=hair.Root.ToLocal(furContactTarget);furGuide=furMotion[0].NearestGuide(local);
            var n=hair.Volume.Normal(new System.Numerics.Vector3(local.X,local.Y,local.Z)*PuppetLabHair.Scale);
            furContactNormal=(hair.Root.GlobalBasis*new Vector3(n.X,n.Y,n.Z)).Normalized();
        }
        furContact=new StaticBody3D{Name="DiagnosticFurContact",CollisionLayer=PuppetFurMotion.ContactLayer,CollisionMask=0};AddChild(furContact);
        furContact.AddChild(new CollisionShape3D{Shape=new SphereShape3D{Radius=FurContactRadius}});
        furContactVisual=new MeshInstance3D{Mesh=new SphereMesh{Radius=FurContactRadius,Height=FurContactRadius*2,RadialSegments=32,Rings=16},
            MaterialOverride=new StandardMaterial3D{AlbedoColor=new Color("e4a447"),Roughness=.75f},Layers=2,
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,GIMode=GeometryInstance3D.GIModeEnum.Disabled};AddChild(furContactVisual);
        furContact.Position=furContactTarget+furContactNormal*(FurContactRadius+(perf?-FurContactDepth:.08f));
        furContactVisual.Position=furContact.Position;furContactVisual.Visible=perf;
        furContactApplied=perf;furContactCommandTime=0;
    }
    void BeginInteractiveFur()
    {
        PrepareFurMotion(false);furInteractiveWind=furInteractiveContact=false;furInteractiveApproach=0;
        furMotionStart=clock.Elapsed.TotalSeconds;furMotionRunning=true;
        if(furInteractiveLabel==null){
            var canvas=new CanvasLayer();AddChild(canvas);
            furInteractiveLabel=new Label{Position=new Vector2(14,14)};canvas.AddChild(furInteractiveLabel);
        }
        UpdateFurLabel();
    }
    void UpdateFurLabel()
    {
        if(furInteractiveLabel!=null)furInteractiveLabel.Text=$"F8 wind {(furInteractiveWind?"ON":"OFF")} | F9 contact {(furInteractiveContact?"ON":"OFF")} | F10 response {(furMotion[0].ResponseEnabled?"ON":"OFF")}\n1 normal / 2 trimmed / 6 carved | LMB cut + bounce | R reset | TAB capture / ESC release";
    }
    bool HandleFurKey(Key key)
    {
        if(!furInteractive)return false;
        if(key==Key.F8)furInteractiveWind=!furInteractiveWind;
        else if(key==Key.F9)furInteractiveContact=!furInteractiveContact;
        else if(key==Key.F10)foreach(var motion in furMotion)motion.ResponseEnabled=!motion.ResponseEnabled;
        else return false;
        UpdateFurLabel();return true;
    }
    public override void _PhysicsProcess(double delta)
    {
        if(!furMotionRunning)return;
        double t=clock.Elapsed.TotalSeconds-furMotionStart;furPhysicsTime+=delta;furPhysicsFrame++;
        var wind=args.ContainsKey("fur-cut-review")?Vector3.Zero:furInteractive?(furInteractiveWind?new Vector3(.9f,0,.2f):Vector3.Zero):furMotionPerf?new Vector3(.8f,0,.2f):FurWind(t);
        // The server broadphase consumes last tick's body transform. Query
        // that pose and show exactly that pose; stage the next command only
        // after the query. Updating the node just before GetRestInfo produced
        // a one-tick visual/query mismatch (retained in dynamic-pilot-*).
        bool contact=furContactApplied;
        if(furContactVisual!=null&&furContact!=null){furContactVisual.GlobalTransform=furContact.GlobalTransform;furContactVisual.Visible=contact;}
        var space=GetWorld3D().DirectSpaceState;
        foreach(var motion in furMotion)motion.Step((float)delta,wind,space,furContact?.GlobalPosition,FurContactRadius,contact);
        var main=furMotion[0];float penetration=0;if(main.Contacts>0)furContactTicks++;
        if(contact&&furContact!=null)for(int i=0;i<main.Active.Length;i++)foreach(float along in PuppetFurMotion.ContactSamples)if(main.ProbeActive(i,along))
            penetration=Math.Max(penetration,FurContactRadius+PuppetFurMotion.ProbeRadius-main.ProbeWorld(i,along).DistanceTo(furContact.GlobalPosition));
        if(!furInteractive)furMotionRows.Add(new{
            wallTime=t,physicsTime=furPhysicsTime,physicsFrame=furPhysicsFrame,delta,phase=furMotionPerf?"steady_wind_contact":FurMotionPhase(t),
            inputWind=V(wind),response=main.ResponseEnabled,root=V(hair.Root.GlobalPosition),guide=furGuide,
            guideRest=V(main.RestTips[furGuide]),guideTipWorld=V(main.TipWorld(furGuide)),guideOffsetLocal=V(main.Offsets[furGuide]),
            maxTipOffsetWorld=furMotion.Max(m=>m.MaxTipOffset),rmsTipOffsetWorld=main.RmsTipOffset,maxTipSpeedWorld=furMotion.Max(m=>m.MaxTipSpeed),
            cutEvents=main.CutEvents,cutImpulses=main.CutImpulses,cutFeedback=main.CutFeedbackEnabled,cutAge=main.CutAge,
            cutGuide=main.LastCutGuide,cutPoint=V(main.LastCutWorldPoint),cutDirection=V(main.LastCutWorldDirection),cutRemovedMass=main.LastCutRemovedMass,cutLocalGuides=main.LastCutLocalGuides,
            cutGuideSignedWorld=main.LastCutGuide>=0?(hair.Root.GlobalBasis*main.Offsets[main.LastCutGuide]).Dot(main.LastCutWorldDirection):0,
            contact,contactBodyId=furContact?.GetInstanceId(),hitColliderId=main.LastCollider,contactCenter=furContact==null?null:V(furContact.GlobalPosition),
            colliderCommandTime=furContactCommandTime,colliderCommandDelay=t-furContactCommandTime,
            contacts=furMotion.Sum(m=>m.Contacts),queries=furMotion.Sum(m=>m.Queries),activeGuides=furMotion.Sum(m=>m.ActiveGuides),
            contactPenetrationWorld=Math.Max(0,penetration),maxContactCorrectionWorld=main.MaxContactCorrection,
            boundedContacts=furMotion.Sum(m=>m.BoundedContacts),infeasibleContactPlanes=furMotion.Sum(m=>m.InfeasibleContactPlanes),maxRequiredContactOffsetLocal=furMotion.Max(m=>m.MaxRequiredContactOffset),
            updateMs=furMotion.Sum(m=>m.LastStepMs),probeRefreshMs=furMotion.Sum(m=>m.LastProbeRefreshMs),finite=furMotion.All(m=>m.Finite),
            fullMotionBoundsContained=furMotion.All(m=>m.BoundsContainFullMotion),
            densityRevision=hair.Volume.Revision,rendererId=hair.VolumeFur!.Node.GetInstanceId()
        });
        if(furContact!=null){
            furInteractiveApproach=Mathf.MoveToward(furInteractiveApproach,furInteractiveContact?1:0,(float)delta/.8f);
            furContactApplied=args.ContainsKey("fur-cut-review")?false:furInteractive?furInteractiveContact||furInteractiveApproach>0:furMotionPerf||t>=4.7&&t<7.3;
            float approach=furInteractive?furInteractiveApproach:furMotionPerf?1:t<5.5?(float)((t-4.7)/.8):t<6.5?1:1-(float)((t-6.5)/.8);
            approach=Math.Clamp(approach,0,1);approach=approach*approach*(3-2*approach);
            furContact.Position=furContactTarget+furContactNormal*(FurContactRadius+Mathf.Lerp(.08f,-FurContactDepth,approach));
            furContactCommandTime=t;
        }
    }
    async void RunFurMotionReview()
    {
        if(args.ContainsKey("fur-cut-review")){RunFurCutReview();return;}
        var captured=new List<(Image Image,double Time,int Frame,string? Checkpoint)>();
        try{
            capture=false;busy=true;
            string label=args.ContainsKey("fur-dynamic-trimmed")?"trimmed":args.ContainsKey("fur-dynamic-carved")?"carved":"normal";
            int heads=args.ContainsKey("fur-heads-4")?4:args.ContainsKey("fur-heads-2")?2:1;
            bool perf=args.ContainsKey("fur-dynamic-perf");
            bool contactPixels=args.ContainsKey("fur-contact-pixels");
            if(contactPixels&&(!perf||heads!=1))throw new InvalidOperationException("Contact pixel diagnostics require a single performance head");
            SetShellReviewShot(perf&&!contactPixels?"r7-perf-"+(heads==4?"four":heads==2?"two":"one"):"r7-"+label+"-close");
            if(args.ContainsKey("interactive")){
                furInteractive=true;BeginInteractiveFur();free=true;Input.MouseMode=Input.MouseModeEnum.Captured;
                if(args.ContainsKey("puppet-check"))RunInteractiveFurCheck();return;
            }
            PrepareFurMotion(perf);
            string before=hair.DensityHash;ulong renderer=hair.VolumeFur!.Node.GetInstanceId();
            ulong fins=hair.VolumeFur.Fins?.GetInstanceId()??0,finMesh=hair.VolumeFur.Fins?.Mesh.GetInstanceId()??0;
            string fieldName=hair.Settings.TuftVolume?"tuft_volume":hair.Settings.BundleField?"bundle_pattern":"root_pattern";
            var initialMaterial=(ShaderMaterial)hair.VolumeFur.Node.MaterialOverride;
            ulong roots=initialMaterial.GetShaderParameter(fieldName).AsGodotObject().GetInstanceId();
            ulong supports=hair.Settings.TuftVolume?initialMaterial.GetShaderParameter("tuft_roots").AsGodotObject().GetInstanceId():0;
            var pose=camera.GlobalTransform;var headPose=hair.Root.GlobalTransform;
            double warm=clock.Elapsed.TotalSeconds;while(clock.Elapsed.TotalSeconds-warm<4)await DrawFrames(1);
            furMotionStart=clock.Elapsed.TotalSeconds;furMotionRunning=true;
            if(perf){await CaptureFurMotionPerf(before,renderer,pose);StopFurMotion();GD.Print("FUR_MOTION_PERF_COMPLETE");QuitCleanly();return;}
            bool record=args.ContainsKey("fur-record"),cut=false,released=false;string after="";double cutTime=0,cutDrawTime=0;
            var frameTimes=new List<object>();int frame=0;double nextFrame=0;
            var checkpoints=new (double Time,string Name)[]{(.6,"rest"),(1.9,"wind"),(4.5,"wind-recovered"),(6.0,"contact"),(9.5,"contact-recovered"),(10.7,"reverse"),(14.1,"pulses-recovered"),(14.6,"cut-before"),(15.2,"cut-active"),(18.2,"cut-recovered")};
            int checkpoint=0;Directory.CreateDirectory(Path.Combine(output,"motion"));
            while(clock.Elapsed.TotalSeconds-furMotionStart<18.5){
                await DrawFrames(1);double t=clock.Elapsed.TotalSeconds-furMotionStart;
                if(camera.GlobalTransform!=pose||hair.Root.GlobalTransform!=headPose)throw new InvalidOperationException("Dynamic camera/head pose moved");
                if(t>=14.7&&!cut){
                    if(hair.DensityHash!=before)throw new InvalidOperationException("Presentation motion modified authoritative density");
                    if(!hair.Raycast(camera.GlobalPosition,-camera.GlobalBasis.Z,out _,out _))throw new InvalidOperationException("Fixed FPS camera has no native cutting hit");
                    free=true;Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true});cut=true;cutTime=t;
                }else if(cut&&!released){
                    Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false});free=false;released=true;after=hair.DensityHash;
                    cutDrawTime=t;
                    if(after==before)throw new InvalidOperationException("Dynamic native LMB did not edit density");
                }
                if(record&&t>=nextFrame||checkpoint<checkpoints.Length&&t>=checkpoints[checkpoint].Time){
                    int number=-1;string? name=null;
                    if(record&&t>=nextFrame){number=frame;frameTimes.Add(new{frame,time=t,phase=FurMotionPhase(t),physicsFrame=furPhysicsFrame});frame++;nextFrame=t+1.0/20;}
                    if(checkpoint<checkpoints.Length&&t>=checkpoints[checkpoint].Time){name=checkpoints[checkpoint].Name;checkpoint++;}
                    captured.Add((GetViewport().GetTexture().GetImage(),t,number,name));
                }
            }
            furMotionRunning=false;free=false;
            double sequenceDuration=clock.Elapsed.TotalSeconds-furMotionStart;
            if(after.Length==0||hair.DensityHash!=after||hair.VolumeFur!.Node.GetInstanceId()!=renderer)throw new InvalidOperationException("Cut regrew or renderer identity changed");
            if((hair.VolumeFur.Fins?.GetInstanceId()??0)!=fins||(hair.VolumeFur.Fins?.Mesh.GetInstanceId()??0)!=finMesh)throw new InvalidOperationException("Existing volume fins regenerated during motion/cut");
            var material=(ShaderMaterial)hair.VolumeFur.Node.MaterialOverride;
            if(material.GetShaderParameter(fieldName).AsGodotObject().GetInstanceId()!=roots||hair.Settings.TuftVolume&&material.GetShaderParameter("tuft_roots").AsGodotObject().GetInstanceId()!=supports)throw new InvalidOperationException("Follicle field regenerated during motion/cut");
            if(furMotion.Any(m=>!m.Finite||!m.BoundsContainFullMotion))throw new InvalidOperationException("Non-finite or unbounded fur response");
            // Store bounded raw readbacks during the live sequence. PNG
            // compression here cannot steal physics time from the experiment.
            GD.Print($"FUR_MOTION_CAPTURED frames={frame} physics={furPhysicsFrame} wall={sequenceDuration:F3} sim={furPhysicsTime:F3}");
            var concat=new StringBuilder("ffconcat version 1.0\n");
            var video=captured.Where(x=>x.Frame>=0).ToArray();
            await Task.Run(()=>Parallel.ForEach(captured,new ParallelOptions{MaxDegreeOfParallelism=4},picture=>{
                if(picture.Frame>=0)picture.Image.SavePng(Path.Combine(output,"motion",$"frame{picture.Frame:D4}.png"));
                if(picture.Checkpoint!=null)picture.Image.SavePng(Path.Combine(output,picture.Checkpoint+".png"));
            }));
            for(int i=0;i<video.Length;i++){
                double duration=i+1<video.Length?video[i+1].Time-video[i].Time:1.0/20;
                concat.Append($"file 'motion/frame{video[i].Frame:D4}.png'\nduration {duration.ToString("F6",CultureInfo.InvariantCulture)}\n");
            }
            if(video.Length>0){concat.Append($"file 'motion/frame{video[^1].Frame:D4}.png'\n");File.WriteAllText(Path.Combine(output,"motion.ffconcat"),concat.ToString());}
            await SaveShellPng("r7-dynamic-after");var silhouette=await ShellSilhouette("r7-dynamic-after");
            File.WriteAllText(Path.Combine(output,"dynamics.json"),JsonSerializer.Serialize(new{
                state=label,settings=hair.Settings,response=!args.ContainsKey("fur-no-response"),firstPerson=true,camera=V(camera.GlobalPosition),fov=camera.Fov,eyeHeight=camera.Position.Y,
                simulation="Bounded cosmetic spring guides + Godot GetRestInfo against a real diagnostic StaticBody3D sphere; not production tool/ApplyForce integration",
                coordinates="Root-fixed rest-space cut mask. Occupied guide fractions .5/.75/1 query the actual sphere. Post-guide GPU vertices also project against that same sphere, with root fade and a bounded extra correction. Guide metrics do not measure all GPU displacements; separate contact-pixel masks test rendered intersections",
                input="Diagnostic wind and collider path; cut uses native LMB and existing authoritative rest-volume raycast",
                beforeDensity=before,afterDensity=after,finalDensity=hair.DensityHash,nativeCutTime=cutTime,firstDrawAfterCutTime=cutDrawTime,cutToDrawMs=(cutDrawTime-cutTime)*1000,hair.LastCutBuildMs,hair.LastCutReadyMs,
                rootFieldId=roots,rootSupportFieldId=supports,fieldName,rendererId=renderer,finRendererId=fins,finMeshId=finMesh,rootAndRendererRetained=true,finsRetained=true,poseFixed=true,performanceValid=false,performanceNote="PNG readback/encoding excluded from controlled perf runs",
                maxOffsetLocal=PuppetFurMotion.MaxOffset,maxGpuContactPushWorld=PuppetFurMotion.MaxContactPushWorld,contactRadius=FurContactRadius,contactDepthWorld=FurContactDepth,contactTarget=V(furContactTarget),contactTargetSource=args.ContainsKey("fur-contact-cut")?"native FPS ray on exposed original interior; radial exposure is not SDF distance":args.ContainsKey("fur-contact-deep")?"native rest-volume FPS ray hit":"conservative guide envelope",cutExposureDepthWorld=furCutExposureDepth,guideProbeRadius=PuppetFurMotion.ProbeRadius,rows=furMotionRows,silhouette
            },new JsonSerializerOptions{WriteIndented=true}));
            File.WriteAllText(Path.Combine(output,"motion.json"),JsonSerializer.Serialize(new{actualRealtime=true,width=1280,height=800,frames=frame,duration=sequenceDuration,physicsTime=furPhysicsTime,physicsFrames=furPhysicsFrame,captureTargetHz=20,encodingAfterSequence=true,times=frameTimes}));
            if(furContactTicks==0)throw new InvalidOperationException("Recorded sequence never made actual contact with the diagnostic body");
            StopFurMotion();GD.Print("FUR_MOTION_COMPLETE");QuitCleanly();
        }catch(Exception e){GD.PushError("FUR_MOTION_FAIL "+e);GetTree().Quit(1);}
        finally{foreach(var picture in captured)picture.Image.Dispose();}
    }
    async Task CaptureFurMotionPerf(string density,ulong renderer,Transform3D pose)
    {
        var ft=new List<double>();var gpuTimes=new List<double>();var cpuTimes=new List<double>();double begin=clock.Elapsed.TotalSeconds,last=begin;
        while(clock.Elapsed.TotalSeconds-begin<8||ft.Count<240){
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);double now=clock.Elapsed.TotalSeconds;
            if(now-begin>4){ft.Add((now-last)*1000);gpuTimes.Add(RenderingServer.ViewportGetMeasuredRenderTimeGpu(GetViewport().GetViewportRid()));cpuTimes.Add(RenderingServer.ViewportGetMeasuredRenderTimeCpu(GetViewport().GetViewportRid())+RenderingServer.GetFrameSetupTimeCpu());}last=now;
        }
        furMotionRunning=false;
        if(hair.DensityHash!=density||camera.GlobalTransform!=pose)throw new InvalidOperationException("Perf pose/density changed");
        await SaveShellPng("r7-dynamic-perf");var silhouette=await ShellSilhouette("r7-dynamic-perf");var ordered=ft.Order().ToArray();
        File.WriteAllText(Path.Combine(output,"dynamics-perf.json"),JsonSerializer.Serialize(new{heads=furMotion.Count,settings=hair.Settings,response=!args.ContainsKey("fur-no-response"),
            gpuMs=gpuTimes.Average(),renderCpuMs=cpuTimes.Average(),frameMs=ft.Average(),p95Ms=ordered[(int)((ordered.Length-1)*.95)],p99Ms=ordered[(int)((ordered.Length-1)*.99)],samples=ft.Count,
            densityHash=density,rendererId=renderer,camera=V(camera.GlobalPosition),fov=camera.Fov,eyeHeight=camera.Position.Y,contactDepthWorld=FurContactDepth,cutExposureDepthWorld=furCutExposureDepth,rows=furMotionRows,silhouette,
            note="4 s warmup + >=4 s/240 frames without screenshot readback. CPU render time excludes spring/query update; updateMs recorded per physics tick. Stress heads are hair render instances, not multiplayer."
        },new JsonSerializerOptions{WriteIndented=true}));
        if(args.ContainsKey("fur-contact-pixels"))await CaptureFurContactPixels();
    }
    async void RunInteractiveFurCheck()
    {
        int checks=0;
        void Require(bool value,string label){if(!value)throw new InvalidOperationException(label);checks++;GD.Print("FUR_INPUT_PASS "+label);}
        async Task Press(Key key){Input.ParseInputEvent(new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=true});await DrawFrames(1);Input.ParseInputEvent(new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=false});await DrawFrames(1);}
        async Task Settle(double seconds){double start=clock.Elapsed.TotalSeconds;while(clock.Elapsed.TotalSeconds-start<seconds)await DrawFrames(1);}
        try{
            string before=hair.DensityHash;
            await Press(Key.F8);await Settle(1);
            Require(furMotion[0].MaxTipOffset>.005f&&hair.DensityHash==before,"native F8 bends existing hair without density edits");
            await Press(Key.F8);await Settle(1);
            Require(furMotion[0].MaxTipOffset<.0005f,"native F8 release recovers");
            await Press(Key.F9);await Settle(1.2);
            Require(furMotion[0].Contacts>0&&furContactVisual!.Visible,"native F9 drives real visible-body contact");
            await Press(Key.F10);await Settle(.1);
            Require(!furMotion[0].ResponseEnabled&&furMotion[0].MaxTipOffset==0,"native F10 disables displacement");
            await Press(Key.F10);await Press(Key.F9);await Settle(1.2);
            foreach(var key in new[]{Key.Key2,Key.Key6,Key.R}){
                ulong old=hair.VolumeFur!.Node.GetInstanceId();await Press(key);await Press(Key.F8);await Settle(.7);
                Require(hair.VolumeFur!.Node.GetInstanceId()!=old&&furMotion[0].Hair==hair&&furMotion[0].MaxTipOffset>.001f,$"native {key} replaces only reset/state fixture and reconnects live response");
            }
            await CheckEmptyHairSurfaces();
            await Press(Key.F8);await Settle(.7);
            Require(furMotion[0].Hair==hair&&furMotion[0].ActiveGuides>0&&furMotion[0].MaxTipOffset>.001f,"response survives complete native shaving, reset and recut");
            await SaveShellPng("fur-interactive-check");
            File.WriteAllText(Path.Combine(output,"fur-input-check.json"),JsonSerializer.Serialize(new{pass=true,checks,emptyChecks=9,emptyDynamicCheck=true,diagnosticOnly=true,visualAcceptance=false}));
            furInteractive=false;StopFurMotion();GD.Print("FUR_INPUT_CHECKS_COMPLETE");QuitCleanly();
        }catch(Exception e){GD.PushError("FUR_INPUT_FAIL "+e);GetTree().Quit(1);}
    }
}
