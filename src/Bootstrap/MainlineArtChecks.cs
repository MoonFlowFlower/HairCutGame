using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
namespace Hairball;
public partial class Main
{
    async void MainlineArtChecks()
    {
        string output=Path.GetFullPath(args.GetValueOrDefault("qa-output","artifacts/b-puppet-mainline/check"));Directory.CreateDirectory(output);
        var records=new List<object>();var facts=new List<string>();
        try {
            void C(bool b,string message){if(!b)throw new InvalidOperationException(message);facts.Add(message);GD.Print("B_ART_PASS "+message);}
            Input.MouseMode=Input.MouseModeEnum.Visible;
            simulation.ForceAI=true;simulation.CatEnabled=false;simulation.TwistsEnabled=false;simulation.State.Seed=729;simulation.StartMatch();world.Phase=Phase.Build;world.Remaining=9999;
            var h=world.SharedHead;var p=world.Player(1)!;var initial=h.Volume.Clone();
            p.Position=new(0,0,2.15f);p.Held=0;world.Tools.First(t=>t.Id==0).Holder=1;
            salon!.Sync(world,1,.016f);hud.Visible=false;voicePanel.Visible=false;
            var view=salon.GetNode<HeadView>("Hair_0");
            void Camera(float distance=2.15f){p.Position=new(.18f,0,distance);salon.Bodies[1].Position=Art.V(p.Position);salon.Camera.Position=Art.V(Session.Eye(p));salon.Camera.LookAt(Art.V(h.Position)+new Vector3(0,.38f,0));var aim=Art.N(-salon.Camera.GlobalBasis.Z);p.Yaw=yaw=MathF.Atan2(-aim.X,-aim.Z);p.Pitch=pitch=MathF.Asin(aim.Y);}
            async Task Frame(){await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);}
            async Task Wait(int count){for(int i=0;i<count;i++)await Frame();}
            void Save(string name){using var image=GetViewport().GetTexture().GetImage();if(image.SavePng(Path.Combine(output,name+".png"))!=Error.Ok)throw new IOException(name);}
            string Hash()=>Convert.ToHexString(SHA256.HashData(h.Volume.Data));
            async Task Shot(string name,float distance=2.15f){
                salon.Sync(world,1,.016f);Camera(distance);string before=Hash();await Wait(80);
                var gpu=new List<double>();var frame=new List<double>();var timer=Stopwatch.StartNew();double prev=timer.Elapsed.TotalMilliseconds;
                for(int i=0;i<120;i++){await Frame();double now=timer.Elapsed.TotalMilliseconds;frame.Add(now-prev);prev=now;gpu.Add(RenderingServer.ViewportGetMeasuredRenderTimeGpu(GetViewport().GetViewportRid()));}
                Save(name);C(Hash()==before,"render leaves authoritative density unchanged: "+name);
                records.Add(new{name,camera=new[]{salon.Camera.Position.X,salon.Camera.Position.Y,salon.Camera.Position.Z},rotation=new[]{salon.Camera.Rotation.X,salon.Camera.Rotation.Y,salon.Camera.Rotation.Z},fov=salon.Camera.Fov,density=Hash(),mass=h.Mass,gpuMs=gpu.Average(),gpuP99=gpu.Order().ElementAt(118),frameMs=frame.Average(),frameP99=frame.Order().ElementAt(118),gpu,frame,uploads=view.PuppetFur?.Uploads,updateMs=view.PuppetFur?.LastUpdateMs,triangles=view.SurfaceTriangles,fins=view.PuppetFur?.Renderer?.FinTriangles});
                if(view.PuppetFur?.Renderer is {} renderer){
                    var priorEnv=salon.Camera.Environment;var priorMask=salon.Camera.CullMask;var env=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=Colors.Black,TonemapMode=Godot.Environment.ToneMapper.Linear};salon.Camera.Environment=env;salon.Camera.CullMask=1u<<18;
                    uint layer=renderer.Node.Layers;renderer.Node.Layers=1u<<18;if(renderer.Fins!=null)renderer.Fins.Layers=1u<<18;
                    foreach(var mat in renderer.MotionMaterials){mat.SetShaderParameter("mask_pass",true);mat.SetShaderParameter("motion_enabled",false);}await Wait(12);Save(name+"-fur-mask");
                    renderer.Node.Visible=false;if(renderer.Fins!=null)renderer.Fins.Visible=false;
                    var geo=HairShell.Build(h.Volume);var array=new Godot.Collections.Array();array.Resize((int)Mesh.ArrayType.Max);array[(int)Mesh.ArrayType.Vertex]=geo.Vertices.Select(Art.V).ToArray();array[(int)Mesh.ArrayType.Index]=geo.Indices.ToArray();
                    var mesh=new ArrayMesh();mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,array);var core=new MeshInstance3D{Mesh=mesh,Layers=1u<<18,MaterialOverride=new StandardMaterial3D{AlbedoColor=Colors.White,ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,CullMode=BaseMaterial3D.CullModeEnum.Disabled}};view.AddChild(core);await Wait(12);Save(name+"-core-mask");core.Free();mesh.Dispose();
                    foreach(var mat in renderer.MotionMaterials){mat.SetShaderParameter("mask_pass",false);mat.SetShaderParameter("motion_enabled",true);}renderer.Node.Visible=true;renderer.Node.Layers=layer;if(renderer.Fins!=null){renderer.Fins.Visible=true;renderer.Fins.Layers=layer;}salon.Camera.CullMask=priorMask;salon.Camera.Environment=priorEnv;env.Dispose();
                }
                File.WriteAllText(Path.Combine(output,"measurements.json"),JsonSerializer.Serialize(records,new JsonSerializerOptions{WriteIndented=true}));
            }
            RenderingServer.ViewportSetMeasureRenderTime(GetViewport().GetViewportRid(),true);
            C(world.Experiment.IsB&&GetTree().CurrentScene.SceneFilePath=="res://scenes/Main.tscn","real B Main scene and Session");
            C(!VisualQuality.Puppet||view.PuppetFur?.Renderer?.Count==8,"accepted eight samples active on authoritative shared head");
            Camera();await Shot("normal");await Shot("normal-close",1.25f);
            if(args.ContainsKey("b-art-pilot")){File.WriteAllText(Path.Combine(output,"checks.json"),JsonSerializer.Serialize(new{pass=true,pilot=true,facts}));QuitGracefully();return;}
            Camera(1.25f);float mass=h.Mass;var cutWatch=Stopwatch.StartNew();
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true});Input.FlushBufferedEvents();
            simulation.Inputs[1]=(default,p.Yaw,p.Pitch,Input.IsMouseButtonPressed(MouseButton.Left)?Buttons.Primary:0);simulation.Tick(.02f);
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false});Input.FlushBufferedEvents();simulation.Inputs.Clear();simulation.EndStroke(1);
            salon.Sync(world,1,.016f);Camera(1.25f);await Frame();double firstDraw=cutWatch.Elapsed.TotalMilliseconds;
            C(h.Mass<mass,"native LMB -> Session tool input -> shared density removed");
            var motionRows=new List<object>();var motionImages=new List<(int frame,Image image)>();double begin=Time.GetTicksMsec()/1000d;
            // PNG compression must not pause the spring on every sampled frame.
            // Buffer real readbacks, retaining wall-clock timestamps, and encode later.
            for(int i=0;i<360;i++){
                await Frame();double stamp=Time.GetTicksMsec()/1000d-begin;
                if(i%5==0)motionImages.Add((i,GetViewport().GetTexture().GetImage()));
                motionRows.Add(new{time=stamp,offset=view.PuppetFur?.OffsetMeters??view.VisualOffsetMeters});
            }
            foreach(var sample in motionImages){using var image=sample.image;if(image.SavePng(Path.Combine(output,"cut-motion-"+sample.frame.ToString("D3")+".png"))!=Error.Ok)throw new IOException("cut motion PNG");}
            File.WriteAllText(Path.Combine(output,"motion.json"),JsonSerializer.Serialize(new{fixedFirstPerson=true,actualToolCut=true,pngOutsideMotionWindow=true,firstDrawMs=firstDraw,rows=motionRows}));
            C(!VisualQuality.Puppet||view.PuppetFur!.PeakMeters>.015f,"committed haircut has visible bounded bounce");
            C(!VisualQuality.Puppet||view.PuppetFur!.PeakMeters<=.0721f,"bounce stays inside 7.2 cm bound");await Shot("native-cut",1.25f);
            var repeated=new List<object>();
            foreach(float x in new[]{-.24f,.24f,0f}){
                var direction=System.Numerics.Vector3.Normalize(h.ToWorld(new(x,.57f,.30f))-Session.Eye(p));p.Yaw=MathF.Atan2(-direction.X,-direction.Z);p.Pitch=MathF.Asin(direction.Y);p.Cooldown=0;
                world.Time+=.2f;var timer=Stopwatch.StartNew();float before=h.Mass;simulation.UseTool(p,false);simulation.EndStroke(1);salon.Sync(world,1,.016f);await Frame();repeated.Add(new{removed=before-h.Mass,firstDrawMs=timer.Elapsed.TotalMilliseconds,updateMs=view.PuppetFur?.LastUpdateMs});
            }
            File.WriteAllText(Path.Combine(output,"warm-cuts.json"),JsonSerializer.Serialize(repeated));
            h.Volume=initial.Clone();h.Volume.Brush(new(EffectKind.CutPlane,1,System.Numerics.Vector3.UnitY),new(0,.60f,0),3);await Shot("trimmed",1.25f);
            h.Volume=initial.Clone();h.Volume.Brush(new(EffectKind.RemoveHair,.22f,System.Numerics.Vector3.UnitZ),new(.05f,.5f,.37f),.3f);await Shot("carved",1.25f);
            Camera(1.25f);mass=h.Mass;p.Held=1;world.Tools.First(t=>t.Id==1).Holder=1;p.Cooldown=0;simulation.UseTool(p,false);salon.Sync(world,1,.016f);
            C(h.Mass>mass,"production growth tool adds visible authoritative material");await Shot("grown",1.25f);
            foreach(var (name,effect) in new[]{("wet",new HairEffect(EffectKind.Wet,1)),("frozen",new HairEffect(EffectKind.ChangeTemperature,-90)),("charred",new HairEffect(EffectKind.Ignite,1))}){
                foreach(var patch in h.Patches){patch.Wet=patch.Char=patch.Glue=0;patch.Temperature=20;patch.Burning=false;if(patch.Root.X>0){HairSystem.Apply(h,patch,effect,false);if(name=="charred"){patch.Char=.9f;patch.Burning=false;}}}
                await Shot(name,1.25f);
            }
            h.Volume.Fill(_=>-1);salon.Sync(world,1,.016f);await Wait(8);C(view.PuppetFur?.Renderer==null&&!view.GetChildren().OfType<MeshInstance3D>().Any(n=>n.Visible&&n.Mesh?.GetSurfaceCount()>0),"shaving to empty leaves no ghost hair");
            h.Volume=initial.Clone();salon.Sync(world,1,.016f);await Wait(8);C(!VisualQuality.Puppet||view.PuppetFur!.Renderer!=null,"empty head can rebuild without empty arrays");
            foreach(var patch in h.Patches){patch.Wet=patch.Char=patch.Glue=0;patch.Temperature=20;patch.Burning=false;}
            world.Experiment.LeaveStarted=world.Time-2;world.Experiment.Leave=LeaveStage.ToMirror;CustomerMotion.Apply(world);await Shot("customer-standing");
            C(salon.GetNode<Node3D>("SharedCustomer").GetNodeOrNull<Node3D>("Leg_1")!=null,"production customer body controls retained");
            File.WriteAllText(Path.Combine(output,"checks.json"),JsonSerializer.Serialize(new{pass=true,facts,firstDrawMs=firstDraw,peak=view.PuppetFur?.PeakMeters,renderer=RenderingServer.GetCurrentRenderingMethod(),scene=GetTree().CurrentScene.SceneFilePath},new JsonSerializerOptions{WriteIndented=true}));
            GD.Print("B_ART_MAINLINE_OK "+output);QuitGracefully();
        }catch(Exception ex){File.WriteAllText(Path.Combine(output,"failure.txt"),ex.ToString());GD.PushError("B_ART_MAINLINE_FAIL "+ex);QuitGracefully(1);}
    }
}
