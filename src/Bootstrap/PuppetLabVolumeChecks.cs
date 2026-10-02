using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
namespace Hairball;

public partial class PuppetLab
{
    async void RunVolumeChecks()
    {
        capture=false;busy=true;int count=0;
        void Require(bool okay,string label){if(!okay)throw new InvalidOperationException(label);count++;GD.Print("VOLUME_CHECK_PASS "+label);}
        async Task Press(Key key){Input.ParseInputEvent(new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=true});await DrawFrames(1);Input.ParseInputEvent(new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=false});await DrawFrames(1);}
        try{
            foreach(int state in new[]{0,1,5}){
                var control=new PuppetLabHair(this,state,null,PuppetHairSettings.Current with{VisualMode="R6_FIBERS",Shells=0});
                var before=control.Core.Mesh.SurfaceGetArrays(0);
                foreach(int n in PuppetHairSettings.Current.TuftHybrid?new[]{4,8,12,16}:new[]{4,8,12,16,64,128}){
                    var test=new PuppetLabHair(this,state,null,PuppetHairSettings.Current with{Shells=n});test.SetLook(round);
                    var after=test.Core.Mesh.SurfaceGetArrays(0);
                    Require(test.DensityHash==control.DensityHash&&before[(int)Mesh.ArrayType.Vertex].AsVector3Array().SequenceEqual(after[(int)Mesh.ArrayType.Vertex].AsVector3Array())&&before[(int)Mesh.ArrayType.Index].AsInt32Array().SequenceEqual(after[(int)Mesh.ArrayType.Index].AsInt32Array()),$"state={state} shells={n}: exact authority and extracted surface preserved");
                    Require(!test.Core.Visible&&test.Fuzz==null&&test.ShellLayers.Count==(test.Settings.TuftHybrid?2:1)&&test.VolumeFur?.Count==n&&(!test.Settings.TuftHybrid||test.VolumeFur.FinTriangles>0),$"state={state} shells={n}: beauty contains persistent volume, optional fixed fins and no opaque core/surface fuzz");
                    test.Root.Free();
                }
                control.Root.Free();
            }
            await Press(Key.Tab);string density=hair.DensityHash;var renderer=hair.VolumeFur!;ulong identity=renderer.Node.GetInstanceId();
            ulong fins=renderer.Fins?.GetInstanceId()??0,finMesh=renderer.Fins?.Mesh.GetInstanceId()??0;
            await Press(Key.F4);Require(hair.Core.Visible&&hair.ShellLayers.All(s=>!s.Visible)&&hair.DensityHash==density,"native F4 shows authority without changing density or leaving fins visible");
            await Press(Key.F4);Require(!hair.Core.Visible&&hair.ShellLayers.All(s=>s.Visible)&&hair.VolumeFur==renderer,"native F4 restores same volume renderer and roots");
            await Press(Key.F5);Require(hair.Core.Visible&&hair.ShellLayers.All(s=>!s.Visible),"gray view uses authoritative core only");
            await Press(Key.F5);Require(!hair.Core.Visible&&hair.ShellLayers.All(s=>s.Visible),"gray exit does not reintroduce an opaque purple core");
            camera.LookAt(hair.Root.ToGlobal(new Vector3(-.12f,.32f,.20f)));yaw=camera.Rotation.Y;pitch=camera.Rotation.X;
            for(int i=0;i<3;i++){
                string previous=hair.DensityHash;int uploads=renderer.Uploads;
                Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true});await DrawFrames(1);
                Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false});await DrawFrames(1);
                Require(hair.DensityHash!=previous&&renderer.Node.GetInstanceId()==identity&&(renderer.Fins?.GetInstanceId()??0)==fins&&(renderer.Fins?.Mesh.GetInstanceId()??0)==finMesh&&hair.VolumeFur==renderer&&renderer.Uploads==uploads+1&&renderer.UploadedRevision==hair.Volume.Revision&&!hair.Core.Visible&&hair.Fuzz==null,$"native cut {i+1}: clips persistent follicles/fins, updates texture and never regenerates roots");
            }
            await SaveShellPng("volume-check-native-depth");
            var pose=camera.Transform;await Press(Key.Key2);Require(hair.TrimVertices>0&&camera.Transform==pose,"trim selector retains FPS pose");
            await Press(Key.Key6);Require(hair.InteriorVertices>0&&camera.Transform==pose,"carve selector retains FPS pose");
            await Press(Key.Space);await Press(Key.Key3);Require(!performing&&state==5,"later material states remain gated");
            await CheckEmptyHairSurfaces();
            File.WriteAllText(Path.Combine(output,"volume-check.json"),JsonSerializer.Serialize(new{pass=true,checks=count,emptyChecks=9,mode=PuppetHairSettings.Current.VisualMode,shells=PuppetHairSettings.Current.Shells,visualAcceptance=false}));
            GD.Print("VOLUME_CHECKS_COMPLETE");QuitCleanly();
        }catch(Exception e){GD.PushError("VOLUME_CHECKS_FAIL "+e);GetTree().Quit(1);}
    }
}
