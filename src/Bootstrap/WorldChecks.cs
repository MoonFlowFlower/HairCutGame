using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace Hairball;
public partial class Main
{
    async void WorldChecks()
    {
        try {
            void C(bool b,string message){if(!b)throw new Exception(message);}
            string folder=args.GetValueOrDefault("qa-output",ProjectSettings.GlobalizePath("res://artifacts"));Directory.CreateDirectory(folder);
            simulation.ForceAI=true;simulation.CatPicker=_=>true;simulation.CatEnabled=true;simulation.ReturningEnabled=true;simulation.MatchRounds=3;simulation.TwistsEnabled=false;simulation.StartMatch();world.Phase=Phase.Build;world.Remaining=150;
            var p=world.Player(1)!;p.AllowDrag=true;p.Position=new(0,0,1.2f);salon!.Sync(world,1,.016f);await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            async Task Shot(string name){foreach(var lang in new[]{"zh","en"}){L.SetLocale(lang);salon.Sync(world,1,.016f);hud.ResultPhoto=salon.RevealPhoto;hud.ReviewPhotoRound=salon.ReturnPhotoRound;hud.Update(world,1,true,false,salon.Mirror,0);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();C(image.SavePng(Path.Combine(folder,"expansion-13-"+name+"-"+lang+".png"))==Error.Ok,"PNG "+name);}}
            salon.Camera.Position=new(3,3,5);salon.Camera.LookAt(new(0,1.7f,0));simulation.BeginCatAction(CatAction.Sleep);await Shot("cat-warning");simulation.Tick(1.51f);C(world.Cat.Mode==CatMode.Sleeping&&ShopCat.Weight(world)>0,"cat carries actual weight");await Shot("cat-sleep");
            var cat=ShopCat.Prop(world)!;p.Position=new(cat.Position.X,0,cat.Position.Z+1);var dir=System.Numerics.Vector3.Normalize(cat.Position+System.Numerics.Vector3.UnitY*.1f-Session.Eye(p));p.Yaw=MathF.Atan2(-dir.X,-dir.Z);p.Pitch=MathF.Asin(dir.Y);
            Input.ParseInputEvent(new InputEventKey{Keycode=Key.E,PhysicalKeycode=Key.E,Pressed=true});Input.FlushBufferedEvents();C(Input.IsPhysicalKeyPressed(Key.E),"actual E input");simulation.Inputs[1]=(default,p.Yaw,p.Pitch,Buttons.Interact);simulation.Tick(.02f);Input.ParseInputEvent(new InputEventKey{Keycode=Key.E,PhysicalKeycode=Key.E,Pressed=false});Input.FlushBufferedEvents();simulation.Inputs.Clear();C(cat.Holder==1&&world.Cat.Mode==CatMode.Held,"E lifts cat off shared head");await Shot("cat-carry");simulation.HandleCat(p);
            var body=salon.Bodies[1];p.Position=new(0,0,1.3f);p.GlueHead=true;p.GlueUntil=world.Time+3;p.GlueOffset=world.SharedHead.ToLocal(Session.Eye(p));world.Experiment.LeaveStarted=world.Time;world.SharedHead.Position+=System.Numerics.Vector3.UnitX*2;body.RestoreMotion(Art.V(p.Position),Vector3.Zero,true,false);var before=body.Position;var rotation=salon.Camera.Rotation;
            for(int i=0;i<30;i++){await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);PartyBodyMove(body,p,Vector2.Zero,.7f,1f/60,false,Vector3.Zero,world.Time+i/60f);}float distance=new Vector2(body.Position.X-before.X,body.Position.Z-before.Z).Length();C(distance>.1f&&distance<.45f,"real CharacterBody drag is bounded .8m/s: "+distance);C(salon.Camera.Rotation==rotation,"drag never rotates camera");p.AllowDrag=false;body.RestoreMotion(before,Vector3.Zero,true,false);for(int i=0;i<10;i++)PartyBodyMove(body,p,Vector2.Zero,.7f,1f/60,false,Vector3.Zero,world.Time);C(new Vector2(body.Position.X-before.X,body.Position.Z-before.Z).Length()<.01f,"opt-out blocks dragging");
            simulation.StartMatch();world.Phase=Phase.Build;world.Remaining=150;for(int i=0;i<240;i++)simulation.Tick(.1f);C(world.Experiment.Drowsiness>.2f,"AI quiet sleep");
            p=world.Player(1)!;p.Position=new(0,0,1.3f);dir=System.Numerics.Vector3.Normalize(Session.CustomerSlapPoint(world)-Session.Eye(p));p.Yaw=MathF.Atan2(-dir.X,-dir.Z);p.Pitch=MathF.Asin(dir.Y);
            await Shot("dozing");C(hud.ExperimentPromptText.Contains("SLAP CUSTOMER"),"English actual slap prompt");float sleepyPitch=world.SharedHead.Rotation.X;
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true});Input.FlushBufferedEvents();C(Input.IsMouseButtonPressed(MouseButton.Left),"actual LMB input");simulation.Inputs[1]=(default,p.Yaw,p.Pitch,Input.IsMouseButtonPressed(MouseButton.Left)?Buttons.Primary:0);simulation.Tick(.02f);
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false});Input.FlushBufferedEvents();simulation.Inputs.Clear();C(world.Experiment.Drowsiness==0&&world.Experiment.Attention.Cause=="wake:slap"&&world.SharedHead.Rotation.X<sleepyPitch,"native LMB immediately lifts AI head");C(world.Events.Any(e=>e.Source==1&&e.Text=="Customer slapped awake"),"explicit slap fact");await Shot("slap-awake");
            for(int i=0;i<215;i++)simulation.Tick(.1f);simulation.Hear(new(3,2,0),1,"qa_wake");C(world.Experiment.Drowsiness==0,"novel wake");
            simulation.StartMatch();simulation.CatEnabled=false;
            for(int round=1;round<=3;round++){
                if(round>1)simulation.NextRound();world.Phase=Phase.Build;world.Remaining=0;simulation.Inputs.Clear();for(int i=0;i<125&&!world.Experiment.Resolved;i++)simulation.Tick(.1f);C(world.Experiment.Resolved,"round revealed "+round);world.Phase=Phase.Results;world.Remaining=9999;salon.Sync(world,1,.016f);await salon.ReturnPhotoForQA(world,folder);C(world.RoundHistory.Count==round,"only revealed history");
            }
            world.Phase=Phase.Complete;salon.Sync(world,1,.016f);hud.ResultPhoto=salon.RevealPhoto;await Shot("returning-history");C(salon.ReturnPhotoRound==1,"ordered replay starts with round one");
            GD.Print("EXPANSION_13_ENGINE_OK cat weight/E rescue; CharacterBody .8m/s drag and opt-out; native LMB AI slap/head lift/prompt; AI sound wake; 3-round photos/history; bilingual");QuitGracefully(0);
        }catch(Exception e){GD.PushError("EXPANSION_13_ENGINE_FAIL "+e);QuitGracefully(1);}
    }
}
