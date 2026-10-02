using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
namespace Hairball;

public partial class PuppetLab
{
    // Fixed real FPS camera, native LMB only. No wind/contact injection is
    // allowed to masquerade as the haircut response being reviewed here.
    async void RunFurCutReview()
    {
        var pictures=new List<(Image Image,double Time,int Frame)>();
        try{
            string label=args.ContainsKey("fur-dynamic-trimmed")?"trimmed":args.ContainsKey("fur-dynamic-carved")?"carved":"normal";
            SetShellReviewShot(args.GetValueOrDefault("shot","r7-"+label+"-close"));
            PrepareFurMotion(false);
            var motion=furMotion[0];var pose=camera.GlobalTransform;var headPose=hair.Root.GlobalTransform;
            var volume=hair.VolumeFur!;ulong renderer=volume.Node.GetInstanceId(),finMesh=volume.Fins?.Mesh.GetInstanceId()??0;
            var material=(ShaderMaterial)volume.Node.MaterialOverride;
            if(args.ContainsKey("fur-finite-debug"))foreach(var m in volume.MotionMaterials)m.SetShaderParameter("nonfinite_debug",true);
            ulong roots=material.GetShaderParameter("tuft_roots").AsGodotObject().GetInstanceId();
            string before=hair.DensityHash,after=before;
            var events=new List<object>();var times=new List<object>();
            await SaveShellPng("cut-before");await ShellSilhouette("cut-before");
            double warm=clock.Elapsed.TotalSeconds;while(clock.Elapsed.TotalSeconds-warm<2)await DrawFrames(1);
            double[] schedule={1.0,3.0,3.18,3.36,3.54,3.72};int nextCut=0,frame=0;
            bool release=false;double nextFrame=0,pendingTime=0;string pendingBefore=before;int pendingCount=0;bool record=args.ContainsKey("fur-record");
            Directory.CreateDirectory(Path.Combine(output,"motion"));
            furMotionStart=clock.Elapsed.TotalSeconds;furMotionRunning=true;
            while(clock.Elapsed.TotalSeconds-furMotionStart<7.5){
                await DrawFrames(1);double t=clock.Elapsed.TotalSeconds-furMotionStart;
                if(camera.GlobalTransform!=pose||hair.Root.GlobalTransform!=headPose)throw new InvalidOperationException("Cut camera/head moved");
                if(release){
                    Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false});release=false;free=false;
                    after=hair.DensityHash;
                    events.Add(new{attempt=nextCut,time=pendingTime,firstDrawTime=t,committed=motion.CutEvents>pendingCount,before=pendingBefore,after,hair.LastCutReadyMs,hair.LastCutBuildMs,
                        impulses=motion.CutImpulses,localGuides=motion.LastCutLocalGuides,point=V(motion.LastCutWorldPoint),removedMass=motion.LastCutRemovedMass});
                    GD.Print($"FUR_CUT_NATIVE attempt={nextCut} committed={motion.CutEvents>pendingCount} total={motion.CutEvents} impulses={motion.CutImpulses}");
                }
                if(nextCut<schedule.Length&&t>=schedule[nextCut]){
                    pendingBefore=hair.DensityHash;pendingCount=motion.CutEvents;pendingTime=t;
                    free=true;Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true});release=true;
                    nextCut++;
                }
                if(record&&t>=nextFrame){
                    times.Add(new{frame,time=t,physicsFrame=furPhysicsFrame});pictures.Add((GetViewport().GetTexture().GetImage(),t,frame++));nextFrame=t+1.0/30;
                }
            }
            furMotionRunning=false;free=false;
            double duration=clock.Elapsed.TotalSeconds-furMotionStart;
            File.WriteAllText(Path.Combine(output,"cut-raw.json"),JsonSerializer.Serialize(new{events,rows=furMotionRows,before,after,final=hair.DensityHash,motion.CutEvents,motion.CutImpulses}));
            if(motion.CutEvents<2||after==before||hair.DensityHash!=after)throw new InvalidOperationException($"Native repeated cuts missing or material regrew: events={motion.CutEvents}, changed={after!=before}, finalEqual={hair.DensityHash==after}");
            if(!motion.Finite||!motion.BoundsContainFullMotion||motion.MaxTipOffset>.0005f)throw new InvalidOperationException("Cut response unbounded or failed to settle");
            if(volume.Node.GetInstanceId()!=renderer||(volume.Fins?.Mesh.GetInstanceId()??0)!=finMesh||material.GetShaderParameter("tuft_roots").AsGodotObject().GetInstanceId()!=roots)throw new InvalidOperationException("Cut replanted original hair");
            await SaveShellPng("cut-after");var silhouette=await ShellSilhouette("cut-after");
            // A real click aimed away must produce neither edit nor impulse.
            camera.LookAt(camera.GlobalPosition+Vector3.Back);free=true;int missCount=motion.CutEvents;
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true});await DrawFrames(1);
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false});free=false;camera.GlobalTransform=pose;
            if(motion.CutEvents!=missCount||hair.DensityHash!=after)throw new InvalidOperationException("Empty click emitted a haircut response");
            GD.Print($"FUR_CUT_CAPTURED state={label} events={motion.CutEvents} impulses={motion.CutImpulses} frames={frame} wall={duration:F3} sim={furPhysicsTime:F3}");
            await Task.Run(()=>Parallel.ForEach(pictures,new ParallelOptions{MaxDegreeOfParallelism=2},p=>p.Image.SavePng(Path.Combine(output,"motion",$"frame{p.Frame:D4}.png"))));
            var concat=new StringBuilder("ffconcat version 1.0\n");
            for(int i=0;i<pictures.Count;i++){
                double dt=i+1<pictures.Count?pictures[i+1].Time-pictures[i].Time:1.0/30;
                concat.Append($"file 'motion/frame{i:D4}.png'\noption framerate 1000\nduration {dt.ToString("F6",CultureInfo.InvariantCulture)}\n");
            }
            if(pictures.Count>0){concat.Append($"file 'motion/frame{pictures.Count-1:D4}.png'\noption framerate 1000\n");File.WriteAllText(Path.Combine(output,"motion.ffconcat"),concat.ToString());}
            File.WriteAllText(Path.Combine(output,"cut-feedback.json"),JsonSerializer.Serialize(new{
                state=label,firstPerson=true,fov=camera.Fov,eyeHeight=camera.Position.Y,camera=V(camera.GlobalPosition),response=motion.ResponseEnabled,cutFeedback=motion.CutFeedbackEnabled,
                beforeDensity=before,afterDensity=after,finalDensity=hair.DensityHash,originalRootFieldId=roots,finMeshId=finMesh,rootFieldAndFinsRetained=true,
                motion.CutEvents,motion.CutImpulses,emptyClickNoResponse=true,settledBelowHalfMm=motion.MaxTipOffset<.0005f,
                nativeInputOnly=true,windInjected=false,contactInjected=false,diagnosticProductionBoundary="Native PuppetLab LMB cutting now emits a presentation impulse; formal B renderer/tools unchanged",
                duration,physicsTime=furPhysicsTime,physicsFrames=furPhysicsFrame,events,rows=furMotionRows,silhouette,
                performanceValid=false,note="Actual realtime capture under monitored shared load, not controlled GPU/latency acceptance; fixed camera/head; no frame interpolation."
            },new JsonSerializerOptions{WriteIndented=true}));
            File.WriteAllText(Path.Combine(output,"motion.json"),JsonSerializer.Serialize(new{actualRealtime=true,width=1280,height=800,frames=frame,duration,physicsTime=furPhysicsTime,physicsFrames=furPhysicsFrame,times}));
            StopFurMotion();GD.Print("FUR_CUT_REVIEW_COMPLETE");QuitCleanly();
        }catch(Exception e){GD.PushError("FUR_CUT_REVIEW_FAIL "+e);GetTree().Quit(1);}
        finally{foreach(var p in pictures)p.Image.Dispose();}
    }
}
