using Godot;
using Hairball.Core;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Hairball;
public partial class Main
{
    async void HumanCustomerChecks()
    {
        try{
            void C(bool b,string text){if(!b)throw new Exception(text);GD.Print("HUMAN_ENGINE_PASS "+text);}
            simulation.ForceAI=false;for(int id=2;id<=4;id++)simulation.AddPlayer(id,"P"+id);simulation.StartMatch();world.Phase=Phase.Build;world.Remaining=150;
            var p=world.Player(1)!;salon!.Sync(world,1,.016f);await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            var body=salon.Bodies[1];var view=new Vector3(.2f,p.Yaw,0);salon.Camera.Rotation=view;
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true});Input.FlushBufferedEvents();C(Input.IsMouseButtonPressed(MouseButton.Left),"actual mouse input");
            simulation.Inputs[1]=(default,p.Yaw,.2f,Buttons.Primary);simulation.Tick(.016f);simulation.Tick(.14f);C(world.SharedHead.Rotation.X!=0&&salon.Camera.Rotation==view,"nod moves authoritative head; FPS camera unchanged");
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false});Input.FlushBufferedEvents();
            hud.PrivateTarget=simulation.OwnTarget(1);hud.PrivateTargetRound=world.Round;hud.TargetHeld=true;
            async Task Shot(string label){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();string folder=args.GetValueOrDefault("qa-output",ProjectSettings.GlobalizePath("res://artifacts"));Directory.CreateDirectory(folder);C(image.SavePng(Path.Combine(folder,"expansion-11-"+label+".png"))==Error.Ok,"screenshot "+label);}
            foreach(var language in new[]{"zh","en"}){L.SetLocale(language);salon.Sync(world,1,.016f);salon.Camera.Position=new(0,1.7f,0);salon.Camera.Rotation=new(0,Mathf.Pi,0);hud.Update(world,1,true,false,salon.Mirror,0);await Shot("private-"+language);}
            C(salon.Camera.CullMask!=(uint.MaxValue&~(1u<<1)),"own shared head layer excluded without hiding shop");
            for(int i=0;i<5;i++){Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.W,Pressed=true});Input.FlushBufferedEvents();C(Input.IsPhysicalKeyPressed(Key.W),"actual W pressed");simulation.Inputs[1]=(default,p.Yaw,p.Pitch,Buttons.Stand);simulation.Tick(.04f);Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.W,Pressed=false});Input.FlushBufferedEvents();simulation.Inputs[1]=(default,p.Yaw,p.Pitch,0);simulation.Tick(.04f);}
            C(p.Standing,"host QTE counted rising edges");var start=body.Position;
            for(int i=0;i<60;i++){PartyBodyMove(body,p,new(0,-1),p.Yaw,1f/60,false,Vector3.Zero,world.Time);p.Position=Art.N(body.Position);simulation.Inputs[1]=(new(0,-1),p.Yaw,p.Pitch,0);simulation.Tick(1f/60);await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);}
            C(body.Position.DistanceTo(start)>1&&Art.V(world.SharedHead.Position).DistanceTo(body.Position+Vector3.Up*1.7f)<.4f,"actual CharacterBody walk carries shared head");
            // Bounded poses put the real E input in sitting/slapping range; movement above was physical.
            body.Position=new(0,0,1);p.Position=Art.N(body.Position);simulation.Inputs[1]=(default,p.Yaw,p.Pitch,0);simulation.Tick(.02f);
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.E,Pressed=true});Input.FlushBufferedEvents();C(Input.IsPhysicalKeyPressed(Key.E),"actual E pressed");simulation.Inputs[1]=(default,p.Yaw,p.Pitch,Buttons.Interact);simulation.Tick(.02f);C(!p.Standing,"E at chair sits down");
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.E,Pressed=false});Input.FlushBufferedEvents();simulation.Inputs[1]=(default,p.Yaw,p.Pitch,0);simulation.Tick(.02f);
            simulation.Inputs[1]=(default,p.Yaw,p.Pitch,Buttons.Secondary);simulation.Tick(.02f);simulation.Tick(.2f);C(Math.Abs(HumanCustomer.Expression(world).Y)>.1f,"shake changes authoritative yaw");
            simulation.Inputs[1]=(default,p.Yaw+.4f,.2f,Buttons.ChairUp);simulation.Tick(.02f);C(p.HeadLook&&HumanCustomer.Expression(world).Length()>.2f,"R turns head toward eyes");
            var barber=world.Player(2)!;barber.Position=new(0,0,1.2f);var hand=PartyBodies.Hand(barber)-Session.Eye(p);float aimYaw=MathF.Atan2(-hand.X,-hand.Z),aimPitch=MathF.Asin(hand.Y/hand.Length());
            simulation.Inputs[1]=(default,aimYaw,aimPitch,0);simulation.Tick(.02f);simulation.Inputs[1]=(default,aimYaw,aimPitch,Buttons.Interact);simulation.Tick(.02f);C(barber.DisabledUntil>world.Time&&barber.Impulse==default,"E slap disables tool .6 seconds without body impulse");
            world.Remaining=0;simulation.Tick(.016f);for(int i=0;i<205&&!world.Experiment.Resolved;i++)simulation.Tick(.1f);C(world.Experiment.Resolved&&world.Experiment.ResultStyle.Count>0,"20 second local reveal publishes card checks");
            world.Experiment.ReturnCut=false;world.Experiment.ResultStarted=world.Time-8;salon.Sync(world,1,.016f);
            for(int i=0;i<120&&salon.RevealPhoto==null;i++){salon.SyncGallery(world);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);}
            C(salon.RevealPhoto!=null,"actual finished-photo texture");hud.ResultPhoto=salon.RevealPhoto;
            foreach(var language in new[]{"zh","en"}){L.SetLocale(language);hud.Update(world,1,true,false,salon.Mirror,0);await Shot("result-"+language);}
            C(VoiceFiltered(1)&&!VoiceFiltered(2),"customer role binds feature-only voice");GD.Print("HUMAN_CUSTOMER_ENGINE_OK real mouse/W/body / stable camera / role voice / private and reveal bilingual");QuitGracefully();
        }catch(Exception e){GD.PushError("HUMAN_CUSTOMER_ENGINE_FAIL "+e);QuitGracefully(1);}
    }
}
