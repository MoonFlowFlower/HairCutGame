using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using Hairball.Core;
namespace Hairball;

public partial class PuppetLab
{
    PuppetHairFragments? fragments;
    readonly List<PuppetLabHair> stressHairs=new();
    void AttachHairPresentation()
    {
        if(PuppetHairSettings.Current.Round<3)return;
        fragments??=new PuppetHairFragments(this,PuppetHairSettings.Current);
        hair.Detached+=(pieces,pose)=>fragments.Emit(pieces,pose);
    }
    void ClearHairPresentation(){fragments?.Clear();stressHairs.Clear();}
    void TickHairPresentation(float dt)=>fragments?.Tick(dt);
    static readonly string[] OptimizationShots = {
        "opt-normal-gameplay", "opt-trimmed-gameplay", "opt-carved-gameplay", "opt-burnt-gameplay", "opt-wet-gameplay", "opt-frozen-gameplay",
        "opt-normal-close", "opt-trimmed-close", "opt-carved-close", "opt-burnt-close", "opt-wet-close", "opt-frozen-close",
        "opt-native-trim", "opt-detach-warning", "opt-detach-fall"
    };
    object OptimizationMetrics() => new {
        settings=hair.Settings, activeHair=stressHairs.Count>0?stressHairs.Count:shot=="four"?4:1, shellLayers=hair.ShellCount,
        allShellLayers=stressHairs.Count>0?stressHairs.Sum(h=>h.ShellCount):hair.ShellCount,
        allFiberTriangles=stressHairs.Count>0?stressHairs.Sum(h=>h.FiberTriangles):hair.FiberTriangles,
        debris=fragments?.Count??0,debrisTriangles=fragments?.Triangles??0,
        hair.TrimVertices, hair.InteriorVertices, hair.ShortFibers,
        cutBuildMs=hair.LastCutBuildMs,cutReadyMs=hair.LastCutReadyMs,
        warningImplemented=hair.Settings.Round>=3,warningCells=hair.Warning.Cells,warningMaxLocalOffset=.007f,
        prefallSeconds=fragments?.BeatSeconds??0,firstPerson=held.Visible&&Math.Abs(camera.Fov-73)<.001f&&Math.Abs(camera.Position.Y-1.7f)<.001f
    };
    void SetOptimizationShot(string name)
    {
        SetShot("gameplay");shot=name;
        int next=name.Contains("trimmed")?1:name.Contains("burnt")?2:name.Contains("wet")?3:name.Contains("frozen")?4:name.Contains("carved")?5:0;
        RebuildHair(next);effects.SetState(next);effects.Visible=false;
        if(name.StartsWith("opt-perf-"))SetupHairLoad(name);
        if(name.StartsWith("opt-detach-")&&PuppetHairSettings.Current.Round>=3)
        {
            hair.Root.Free();hair=new PuppetLabHair(this,0,PuppetLabHair.WarningFixture());hair.SetLook(round);AttachHairPresentation();
            camera.LookAt(new(.05f,1.94f,0));
        }
        // Every gameplay shot uses the existing 73-degree, 1.7 m FPS camera and hands.
        if(name.EndsWith("close")){held.Visible=false;camera.Position=new(.14f,2.25f,.96f);camera.LookAt(new(0,2.17f,0));}
        if(camera.Attributes is CameraAttributesPractical lens)lens.DofBlurFarEnabled=false;
        yaw=camera.Rotation.Y;pitch=camera.Rotation.X;
        effects.GlobalTransform=hair.Root.GlobalTransform;
        if(name=="opt-native-trim")CaptureNativeTrim();
        if(name=="opt-detach-fall"&&PuppetHairSettings.Current.Round>=3)CaptureDetachBeat();
    }
    void SetupHairLoad(string name)
    {
        int count=name.Contains("four")?4:1;
        if(count==1)return;
        subject.Visible=false;friend.Visible=false;held.Visible=false;hair.Root.Visible=false;
        stress=new Node3D();AddChild(stress);
        for(int i=0;i<4;i++){
            var h=new PuppetLabHair(stress,0,null,PuppetHairSettings.Current with{Lod=i==0?0:1});h.SetLook(round);
            h.Root.Position=new((i%2-.5f)*1.20f,(i/2)*1.05f+1.25f,0);stressHairs.Add(h);
        }
        camera.Position=new(0,1.7f,3.1f);camera.LookAt(new(0,1.73f,0));
        int debris=name.EndsWith("max")?12:name.EndsWith("six")?6:0;
        var piece=new HairVolume();piece.Fill(p=>HairVolume.Ellipsoid(p/PuppetLabHair.Scale,new(.02f,.16f,0),new(.22f,.16f,.18f))*PuppetLabHair.Scale);
        for(int i=0;i<debris;i++)fragments!.Emit(new[]{piece.Clone()},new Transform3D(Basis.Identity.Scaled(Vector3.One*.9f),new Vector3((i%6-2.5f)*.50f,.36f+(i/6)*.25f,.85f)));
        fragments!.FreezeAge=0;fragments.Tick(0);
    }
    async void CaptureDetachBeat()
    {
        busy=true;
        try {
            for(int i=0;i<8;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            string before=hair.DensityHash;int warning=hair.Warning.Cells;
            hair.SeverWarningFixture();string after=hair.DensityHash;
            if(fragments!.Count==0||before==after)throw new InvalidOperationException("Fixture did not actually detach");
            double begin=clock.Elapsed.TotalSeconds;var times=new List<object>();var captures=new List<(Image Image,float Target)>();
            foreach(float target in new[]{0f,.16f,.31f,.42f,.60f}){
                while(clock.Elapsed.TotalSeconds-begin<target)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                double actual=clock.Elapsed.TotalSeconds-begin;
                captures.Add((GetViewport().GetTexture().GetImage(),target));
                times.Add(new{target,actual,fragmentAge=fragments.OldestAge,drop=fragments.GreatestDrop,mainDensity=hair.DensityHash});
            }
            fragments.FreezeAge=.60f;fragments.Tick(0);
            foreach(var item in captures){item.Image.SavePng(Path.Combine(output,$"detach-beat-{item.Target:F2}.png"));item.Image.Dispose();}
            File.WriteAllText(Path.Combine(output,"detach-beat.json"),JsonSerializer.Serialize(new{before,after,warning,beat=fragments.BeatSeconds,logicalDetachedImmediately=true,fragments=fragments.Count,times}));
            captureFrames=0;start=last=clock.Elapsed.TotalSeconds;frames.Clear();gpu.Clear();draws.Clear();busy=false;
        } catch(Exception e){GD.PushError("HAIR_DETACH_FAIL "+e);GetTree().Quit(1);}
    }
    void CheckOptimization()
    {
        if(!PuppetHairSettings.Current.Enabled)return;
        void Require(bool okay,string name){if(!okay)throw new InvalidOperationException(name);GD.Print("PUPPET_OPT_CHECK_PASS "+name);}
        var normal=new HairVolume();normal.Fill(p=>PuppetLabHair.Field(p/PuppetLabHair.Scale)*PuppetLabHair.Scale);
        var before=(byte[])normal.Data.Clone();var stable=PuppetHairWarning.Evaluate(normal,PuppetLabHair.Supported);
        Require(before.AsSpan().SequenceEqual(normal.Data),"warning estimator never changes density bytes");
        Require(stable.Cells==0,"uncut authored head has no structural warning");
        var weak=PuppetLabHair.WarningFixture();var redundant=PuppetLabHair.WarningFixture(true);
        Require(weak.Clone().DetachUnsupported(PuppetLabHair.Supported,false).Count==0,"thin neck fixture is really connected before the last cut");
        var warning=PuppetHairWarning.Evaluate(weak,PuppetLabHair.Supported);
        Require(warning.Cells>=64,"one-cell articulation with a large outboard volume is warned");
        Require(PuppetHairWarning.Evaluate(redundant,PuppetLabHair.Supported).Cells==0,"redundant wide support suppresses warning");
        Require(PuppetHairWarning.Evaluate(weak,p=>PuppetLabHair.Supported(p)||p.X/PuppetLabHair.Scale>.40f).Cells==0,"additional anchor on the load suppresses warning");
        var saved=(byte[])weak.Data.Clone();weak.Brush(new HairEffect(EffectKind.RemoveHair,0,System.Numerics.Vector3.UnitY),warning.Neck,.065f*PuppetLabHair.Scale);
        var pieces=weak.DetachUnsupported(PuppetLabHair.Supported,false);
        Require(pieces.Count>0&&!saved.AsSpan().SequenceEqual(weak.Data),"existing authoritative connectivity removes disconnected material immediately");
        var dataAfter=(byte[])weak.Data.Clone();fragments??=new PuppetHairFragments(this,PuppetHairSettings.Current);fragments.Clear();
        fragments.Emit(pieces,new Transform3D(Basis.Identity.Scaled(Vector3.One*.9f),PuppetLabHair.Origin));
        fragments.FreezeAge=.16f;fragments.Tick(0);float beforeRelease=fragments.GreatestDrop;
        fragments.FreezeAge=.60f;fragments.Tick(0);
        Require(beforeRelease<.011f&&fragments.GreatestDrop>.20f,"visual prefall holds then releases a detached piece");
        Require(dataAfter.AsSpan().SequenceEqual(weak.Data),"prefall presentation cannot put material back into the head");
        for(int i=0;i<14;i++)fragments.Emit(pieces,new Transform3D(Basis.Identity,PuppetLabHair.Origin));
        Require(fragments.Count==PuppetHairFragments.Maximum,"large cosmetic fragments stay capped at twelve");fragments.Clear();
        var a=new PuppetLabHair(this,5);a.SetLook(round);
        var b=new PuppetLabHair(this,5,null,PuppetHairSettings.Current with{Smooth=false,Fuzz=false,Trim=false,Carve=false});b.SetLook(round);
        Require(a.InteriorVertices>0&&a.ShortFibers>0,"carve wall has actual classified vertices and short fibers");
        Require(a.DensityHash==b.DensityHash&&a.Core.Mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array().SequenceEqual(b.Core.Mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array()),"visual A/B flags preserve every core position and density byte");
        Require(b.Fuzz?.Visible==false,"fuzz OFF hides the actual fiber renderer");a.Root.Free();b.Root.Free();
        File.WriteAllText(Path.Combine(output,"optimization-check.json"),JsonSerializer.Serialize(new{pass=true,stableWarningCells=stable.Cells,weakWarningCells=warning.Cells,fragmentLimit=PuppetHairFragments.Maximum,warningMaxLocalOffset=.007f,corePositionsUnchanged=true}));
    }
    async void CaptureNativeTrim()
    {
        busy=true;
        try {
            // Dispatch through the same native input handler used by an interactive player.
            free=true;camera.LookAt(hair.Root.ToGlobal(new Vector3(-.12f,.32f,.20f)));yaw=camera.Rotation.Y;pitch=camera.Rotation.X;
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using(var image=GetViewport().GetTexture().GetImage())image.SavePng(Path.Combine(output,"native-input-before.png"));
            string before=hair.DensityHash;float mass=hair.Volume.Mass;
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true});
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false});
            int progressed=0;double begin=clock.Elapsed.TotalSeconds;
            while(hair.IsFuzzBuilding&&clock.Elapsed.TotalSeconds-begin<10){progressed++;await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
            if(hair.DensityHash==before||hair.IsFuzzBuilding)throw new InvalidOperationException("Native cut did not finish");
            File.WriteAllText(Path.Combine(output,"native-input.json"),JsonSerializer.Serialize(new{before,after=hair.DensityHash,massBefore=mass,massAfter=hair.Volume.Mass,progressed,hair.LastCutBuildMs,hair.LastCutReadyMs,hair.TrimVertices,hair.InteriorVertices,hair.ShortFibers}));
            free=false;captureFrames=0;start=last=clock.Elapsed.TotalSeconds;frames.Clear();gpu.Clear();draws.Clear();busy=false;
        }
        catch(Exception e){GD.PushError("HAIR_REVIEW_FAIL "+e);GetTree().Quit(1);}
    }
}
