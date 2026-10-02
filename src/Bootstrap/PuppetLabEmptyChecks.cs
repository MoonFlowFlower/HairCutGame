using Godot;
using Hairball.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hairball;

public partial class PuppetLab
{
    async void RunEmptyHairChecks()
    {
        capture=false;
        try { await CheckEmptyHairSurfaces();GD.Print("PUPPET_EMPTY_CHECKS_PASS");QuitCleanly(); }
        catch(Exception e){GD.PushError("PUPPET_EMPTY_CHECKS_FAIL "+e);GetTree().Quit(1);}
    }

    async Task CheckEmptyHairSurfaces()
    {
        void Require(bool okay,string name)
        {if(!okay)throw new InvalidOperationException(name);GD.Print("PUPPET_EMPTY_CHECK_PASS "+name);}
        async Task KeyPress(Key key)
        {
            Input.ParseInputEvent(new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=true});
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Input.ParseInputEvent(new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=false});
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        }
        async Task MouseCut()
        {
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true});
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false});
            var wait=Stopwatch.StartNew();
            while(hair.IsFuzzBuilding&&wait.Elapsed.TotalSeconds<10)
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            if(hair.IsFuzzBuilding)throw new InvalidOperationException("Empty-hair cut rebuild timed out");
        }
        async Task Screenshot(string name)
        {
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using var image=GetViewport().GetTexture().GetImage();
            if(image.SavePng(Path.Combine(output,name+".png"))!=Error.Ok)throw new IOException(name);
        }

        // Repeated native input on the normal head, without injecting an empty
        // volume or bypassing Cut/DetachUnsupported/the asynchronous renderer.
        await KeyPress(Key.Tab);string original=hair.DensityHash;
        if(furInteractive)await KeyPress(Key.F8);
        // Walkable close FPS position keeps the final back surface within the
        // existing tool ray range as the front is removed. Eye height stays 1.7 m.
        camera.Position=new(0,1.7f,1.10f);
        await Screenshot("empty-check-before");int cuts=0;
        while(hair.Triangles>0&&cuts<256)
        {
            var target=hair.Volume.Samples(1).OrderByDescending(p=>p.Z).ThenByDescending(p=>p.Y).First();
            camera.LookAt(hair.Root.ToGlobal(new Vector3(target.X,target.Y,target.Z)/PuppetLabHair.Scale));
            yaw=camera.Rotation.Y;pitch=camera.Rotation.X;
            if(!hair.Raycast(camera.Position,-camera.Basis.Z,out _,out _))throw new InvalidOperationException("Native shave target is not raycastable");
            string before=hair.DensityHash;await MouseCut();
            if(hair.DensityHash==before)throw new InvalidOperationException("Native shave made no progress");
            cuts++;
            if(cuts%10==0||hair.Triangles==0)GD.Print($"PUPPET_EMPTY_SHAVE cuts={cuts} coreTriangles={hair.Triangles} fibers={hair.Fibers}");
        }
        Require(hair.Triangles==0&&hair.Core.Mesh.GetSurfaceCount()==0,"native repeated cuts reach a legal empty core mesh");
        Require(hair.Fuzz==null&&hair.Fibers==0&&hair.FiberTriangles==0&&hair.ShortFibers==0,"empty asynchronous result removes the previous fuzz renderer");
        Require(hair.ShellCount==0&&hair.ShellLayers.Count==0&&hair.Root.GetNodeOrNull("PersistentVolumeCurveFins")==null,"empty head has no shell or stale volume-fin renderer");
        if(furInteractive){
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            Require(furMotion[0].ActiveGuides==0&&furMotion[0].Finite&&furMotion[0].MaxTipOffset==0,"empty hair disables all guides during active wind without invalid resources");
        }
        Require(!hair.Raycast(camera.Position,-camera.Basis.Z,out _,out _),"empty head no longer accepts tool hits");
        await Screenshot("empty-check-shaved");
        string empty=hair.DensityHash;await MouseCut();await KeyPress(Key.F4);await KeyPress(Key.F4);await KeyPress(Key.F5);await KeyPress(Key.F5);
        Require(hair.DensityHash==empty&&hair.Core.Mesh.GetSurfaceCount()==0&&hair.Fuzz==null,"empty input and material toggles keep empty geometry valid");
        await KeyPress(Key.R);
        Require(hair.DensityHash==original&&hair.Triangles>0&&(hair.Settings.VolumeShells?hair.ShellCount>0:hair.Fibers>0),"native R restores full hair after complete shaving");
        await Screenshot("empty-check-reset");
        await KeyPress(Key.Tab);camera.LookAt(hair.Root.ToGlobal(new Vector3(-.12f,.32f,.20f)));yaw=camera.Rotation.Y;pitch=camera.Rotation.X;
        await MouseCut();
        Require(hair.DensityHash!=original&&(hair.Settings.VolumeShells?hair.VolumeFur!=null&&hair.VolumeFur.Uploads>1:hair.Fuzz!=null&&hair.ShortFibers>0),"native cutting works again after an empty-head reset");

        // A zero-fiber result can also occur on nonempty core geometry below
        // the pile cutoff; it must not reuse stale fuzz or upload an empty surface.
        var low=new HairVolume();low.Fill(p=>HairVolume.Ellipsoid(p/PuppetLabHair.Scale,new(0,-.16f,0),new(.05f,.014f,.05f))*PuppetLabHair.Scale);
        var lowHair=new PuppetLabHair(this,0,low);lowHair.SetLook(round);
        Require(lowHair.Triangles>0&&lowHair.Fibers==0&&lowHair.Fuzz==null,"nonempty core can legitimately generate zero fibers");lowHair.Root.Free();
        var blank=new PuppetLabHair(this,0,new HairVolume(),PuppetHairSettings.Current with{Shells=4});blank.SetLook(round);
        Require(blank.Core.Mesh.GetSurfaceCount()==0&&blank.Fuzz==null&&blank.ShellCount==0,"empty initial volume remains valid with optional shells enabled");blank.Root.Free();
        File.WriteAllText(Path.Combine(output,"empty-check.json"),JsonSerializer.Serialize(new{pass=true,nativeCuts=cuts,originalDensity=original,emptyDensity=empty,fov=73,eyeHeight=1.7,resetAndRecut=true}));
    }
}
