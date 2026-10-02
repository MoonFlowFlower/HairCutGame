using Godot;
using Hairball.Core;
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
    async void RunFurFragmentReview()
    {
        var captured=new List<(Image Image,double Time)>();
        try{
            capture=false;busy=true;
            if(!PuppetHairSettings.Current.SurfaceSamples)throw new InvalidOperationException("Fragment review requires SurfaceSamples");
            SetShellReviewShot("r7-normal-close");fragments!.Clear();fragments.FreezeAge=0;
            // A bounded diagnostic fixture cuts a narrow band in ORIGINAL
            // material, leaving one real bridge for native LMB to sever.
            // It adds no material, no new roots and no production tool mode.
            var fixture=new HairVolume();fixture.Fill(raw=>{
                var p=raw/PuppetLabHair.Scale;
                float band=Math.Abs(p.Y-.29f)-.050f;
                float bridge=.050f-MathF.Sqrt((p.X+.12f)*(p.X+.12f)+(p.Z-.20f)*(p.Z-.20f));
                return Math.Min(PuppetLabHair.Field(p),Math.Max(band,bridge))*PuppetLabHair.Scale;
            });
            if(fixture.Clone().DetachUnsupported(PuppetLabHair.Supported,false).Count>0)throw new InvalidOperationException("Diagnostic bridge must still support the original crown before native cut");
            hair.Root.Free();hair=new PuppetLabHair(this,0,fixture);hair.SetLook(round);AttachHairPresentation();
            await DrawFrames(4);await SaveShellPng("fragments-before");
            string before=hair.DensityHash;int cuts=0;
            for(;cuts<8&&!fragments.Hairs.Any(p=>p.Triangles>=300);cuts++){
                camera.LookAt(hair.Root.ToGlobal(new Vector3(-.12f,.29f,.20f)));yaw=camera.Rotation.Y;pitch=camera.Rotation.X;
                free=true;Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true});await DrawFrames(1);
                Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false});free=false;await DrawFrames(1);
            }
            if(!fragments.Hairs.Any(p=>p.Triangles>=300)||hair.DensityHash==before)throw new InvalidOperationException("Native cuts did not detach a sufficiently large review piece");
            var detached=fragments.Hairs.ToArray();var after=hair.DensityHash;
            var pieceData=detached.Select(p=>p.DensityHash).ToArray();
            var pieceBounds=detached.Select(p=>new{triangles=p.Triangles,size=V(p.Core.Mesh.GetAabb().Size),mass=p.Volume.Mass}).ToArray();
            var field=PuppetTuftVolume.Field.GetInstanceId();var roots=PuppetTuftVolume.Roots.GetInstanceId();
            bool shared=detached.All(p=>p.VolumeFur!=null&&!p.Core.Visible&&p.VolumeFur.FinTriangles<=PuppetTuftVolume.FragmentFinTriangleLimit&&p.VolumeFur.Fins?.Mesh!=hair.VolumeFur?.Fins?.Mesh&&p.ShellCount==Math.Min(8,hair.Settings.Shells)
                &&((ShaderMaterial)p.VolumeFur.Node.MaterialOverride).GetShaderParameter("tuft_volume").AsGodotObject().GetInstanceId()==field
                &&((ShaderMaterial)p.VolumeFur.Node.MaterialOverride).GetShaderParameter("tuft_roots").AsGodotObject().GetInstanceId()==roots);
            if(!shared)throw new InvalidOperationException("Detached material lost the fixed field or reintroduced opaque core/full-head fins");
            await SaveShellPng("fragments-attached-pose");
            fragments.ResumeAges();double begin=clock.Elapsed.TotalSeconds,next=0;var times=new List<object>();
            while(clock.Elapsed.TotalSeconds-begin<.85){
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);double t=clock.Elapsed.TotalSeconds-begin;
                if(t>=next){captured.Add((GetViewport().GetTexture().GetImage(),t));times.Add(new{time=t,age=fragments.OldestAge,drop=fragments.GreatestDrop,headDensity=hair.DensityHash});next=t+.05;}
            }
            if(hair.DensityHash!=after||!detached.Select(p=>p.DensityHash).SequenceEqual(pieceData))throw new InvalidOperationException("Cosmetic flight changed head or fragment occupancy");
            fragments.FreezeAge=.42f;fragments.Tick(0);await DrawFrames(3);await SaveShellPng("fragments-cut-wall");
            var silhouette=await ShellSilhouette("fragments-cut-wall",detached);
            var concat=new StringBuilder("ffconcat version 1.0\n");Directory.CreateDirectory(Path.Combine(output,"motion"));
            for(int i=0;i<captured.Count;i++){
                captured[i].Image.SavePng(Path.Combine(output,"motion",$"frame{i:D4}.png"));
                double duration=i+1<captured.Count?captured[i+1].Time-captured[i].Time:.05;
                concat.Append($"file 'motion/frame{i:D4}.png'\nduration {duration.ToString("F6",CultureInfo.InvariantCulture)}\n");
            }
            concat.Append($"file 'motion/frame{captured.Count-1:D4}.png'\n");File.WriteAllText(Path.Combine(output,"motion.ffconcat"),concat.ToString());
            var sample=detached.OrderByDescending(p=>p.Volume.Mass).First().Volume.Clone();
            int originalCount=fragments.Count,originalTriangles=fragments.Triangles;
            fragments.Clear();
            for(int i=0;i<14;i++){
                var transform=hair.Root.GlobalTransform;transform.Origin+=new Vector3((i%4-1.5f)*.24f,-.32f+(i/4)*.12f,.40f);
                fragments.Emit(new[]{sample.Clone()},transform);
            }
            fragments.FreezeAge=0;fragments.Tick(0);
            if(fragments.Count!=PuppetHairFragments.Maximum||fragments.Hairs.Any(p=>p.Core.Visible||p.VolumeFur?.FinTriangles>PuppetTuftVolume.FragmentFinTriangleLimit))throw new InvalidOperationException("Fragment cap or material lifecycle failed");
            var ft=new List<double>();var gt=new List<double>();begin=clock.Elapsed.TotalSeconds;double last=begin;
            while(clock.Elapsed.TotalSeconds-begin<8||ft.Count<240){
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);double now=clock.Elapsed.TotalSeconds;
                if(now-begin>4){ft.Add((now-last)*1000);gt.Add(RenderingServer.ViewportGetMeasuredRenderTimeGpu(GetViewport().GetViewportRid()));}last=now;
            }
            await SaveShellPng("fragments-max-stress");var ordered=ft.Order().ToArray();
            File.WriteAllText(Path.Combine(output,"fragments.json"),JsonSerializer.Serialize(new{
                pass=true,settings=hair.Settings,firstPerson=true,eyeHeight=camera.Position.Y,fov=camera.Fov,cuts,before,after,finalDensity=hair.DensityHash,
                originalCount,originalTriangles,pieceBounds,sharedOriginalField=shared,fieldId=field,rootsId=roots,pieceDensityHashes=pieceData,
                opaqueCoreVisible=false,fullHeadFinCopied=false,finTrianglesPerPieceLimit=PuppetTuftVolume.FragmentFinTriangleLimit,recordedFrames=captured.Count,times,silhouette,
                nativeCutReadyMs=hair.LastCutReadyMs,stressCount=fragments.Count,stressTriangles=fragments.Triangles,gpuMs=gt.Average(),frameMs=ft.Average(),p99Ms=ordered[(int)((ordered.Length-1)*.99)],samples=ft.Count,
                fixture="Diagnostic subtraction band in the original crown, with one actual support bridge; initial support verified. Native LMB severs it. No material was added and no fur was replanted.",
                note="Fixed first-person camera and original volume coordinates. Flight is the existing bounded cosmetic prefall/gravity path, not a new rigid-body simulation. Maximum stress repeats a real detached piece. Warmup4s/sample4s, no readback in measured window. Visual acceptance remains open."
            },new JsonSerializerOptions{WriteIndented=true}));
            fragments.Clear();if(fragments.Count!=0)throw new InvalidOperationException("Fragment clear failed");
            GD.Print("FUR_FRAGMENTS_COMPLETE");QuitCleanly();
        }catch(Exception e){GD.PushError("FUR_FRAGMENTS_FAIL "+e);GetTree().Quit(1);}
        finally{foreach(var item in captured)item.Image.Dispose();}
    }
}
