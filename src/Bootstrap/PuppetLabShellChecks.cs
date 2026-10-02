using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
namespace Hairball;

public partial class PuppetLab
{
    async void RunShellChecks()
    {
        capture=false;busy=true;int count=0;
        void Require(bool okay,string label){if(!okay)throw new InvalidOperationException(label);count++;GD.Print("R7_CHECK_PASS "+label);}
        async Task Press(Key key){Input.ParseInputEvent(new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=true});await DrawFrames(1);Input.ParseInputEvent(new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=false});await DrawFrames(1);}
        try{
            // Cross-mode invariance tests use the actual generated core arrays,
            // not a second implementation of density editing in the fixture.
            foreach(int state in new[]{0,1,5}){
                var baseline=new PuppetLabHair(this,state,null,PuppetHairSettings.Current with{VisualMode="R6_FIBERS",Shells=0});
                var arrays=baseline.Core.Mesh.SurfaceGetArrays(0);var positions=arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();var indices=arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
                foreach(int layers in new[]{4,8,12,16}){
                    var sample=new PuppetLabHair(this,state,null,PuppetHairSettings.Current with{VisualMode="R7_SHELL",Shells=layers});sample.SetLook(round);
                    var actual=sample.Core.Mesh.SurfaceGetArrays(0);
                    Require(sample.DensityHash==baseline.DensityHash&&positions.SequenceEqual(actual[(int)Mesh.ArrayType.Vertex].AsVector3Array())&&indices.SequenceEqual(actual[(int)Mesh.ArrayType.Index].AsInt32Array())&&sample.TrimVertices==baseline.TrimVertices&&sample.InteriorVertices==baseline.InteriorVertices,$"state {state}, {layers} shells preserve exact density, core and classification");
                    // The owner's 00:44 target explicitly permits a thicker
                    // uncut coat. Exposed cut pile keeps its own tight bound.
                    float limit=sample.Settings.PlushTufts?.060f:.03f;
                    Require(sample.ShellCount==layers&&sample.MaximumFiberOffset*sample.Root.Scale.X<limit&&sample.MaximumShortFiberOffset*sample.Root.Scale.X<.017f,$"state {state}, {layers} shells bounded outer and short cut geometry");sample.Root.Free();
                }
                baseline.Root.Free();
            }
            await Press(Key.Tab);string initial=hair.DensityHash;int expected=hair.ShellCount;
            await Press(Key.F4);Require(hair.ShellCount==0&&hair.DensityHash==initial,"native F4 removes shell geometry without editing density");
            await Press(Key.F4);Require(hair.ShellCount==expected&&hair.DensityHash==initial,"native F4 restores selected shell count");
            await Press(Key.F5);Require(hair.ShellLayers.All(s=>!s.Visible),"gray core view excludes shells");await Press(Key.F5);Require(hair.ShellLayers.All(s=>s.Visible),"gray restore recovers shell visibility");
            var pose=camera.Transform;await Press(Key.Key2);Require(hair.TrimVertices>0&&camera.Transform==pose,"trim selector retains FPS pose");await Press(Key.Key6);Require(hair.InteriorVertices>0&&camera.Transform==pose,"carve selector retains FPS pose");
            await Press(Key.Space);await Press(Key.Key3);Require(!performing&&state==5,"R7.1 does not activate unaccepted material states or performance");
            SetShellReviewShot("r7-native-after");await NativeShellCut();Require(hair.DensityHash!=initial&&hair.ShellCount==expected&&hair.TrimVertices>0,"native LMB updates real core and all shell layers");await SaveShellPng("r7-check-native-after");
            await CheckEmptyHairSurfaces();
            File.WriteAllText(Path.Combine(output,"r7-check.json"),JsonSerializer.Serialize(new{pass=true,checks=count,emptyChecks=9,mode=PuppetHairSettings.Current.VisualMode,shells=PuppetHairSettings.Current.Shells}));
            GD.Print("R7_CHECKS_COMPLETE");QuitCleanly();
        }catch(Exception e){GD.PushError("R7_CHECKS_FAIL "+e);GetTree().Quit(1);}
    }
}
