using Godot;
using Hairball.Core;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using V=System.Numerics.Vector3;
namespace Hairball;
public partial class Main
{
    async void ExpansionBodyChecks()
    {
        try{
            void C(bool b,string message){if(!b)throw new InvalidOperationException(message);GD.Print("PARTY_BODY_ENGINE_PASS "+message);}
            for(int id=2;id<=4;id++)simulation.AddPlayer(id,"P"+id);simulation.StartMatch();world.Phase=Phase.Build;world.Remaining=150;salon!.Sync(world,localId,.016f);
            var a=world.Player(1)!;var b=world.Player(2)!;a.Position=new(3,0,2);b.Position=new(3,0,0);a.Yaw=0;a.Pitch=0;b.Yaw=MathF.PI;
            var tool=world.Tools.First(t=>t.Id==1);tool.Holder=a.Id;a.Held=tool.Id;
            void KeyInput(Key key,bool down){Input.ParseInputEvent(new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=down});Input.FlushBufferedEvents();}
            void Tick(){simulation.Tick(1f/60);salon.Sync(world,1,1f/60);}
            async Task Physics(int count,Action action){for(int i=0;i<count;i++){await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);action();}}
            KeyInput(Key.G,true);C(Input.IsPhysicalKeyPressed(Key.G),"native G press enters Godot input");
            await Physics(20,()=>{simulation.Inputs[a.Id]=(default,a.Yaw,a.Pitch,Input.IsPhysicalKeyPressed(Key.G)?Buttons.Drop:Buttons.None);Tick();});C(a.Held==tool.Id,"G holds the object until release");
            KeyInput(Key.G,false);simulation.Inputs[a.Id]=(default,a.Yaw,a.Pitch,Buttons.None);Tick();C(a.Held<0&&tool.Velocity.Length()>4,"native G release launches a charged tool");
            await Physics(25,Tick);C(b.Held==tool.Id,"swept native simulation catches tool at empty hands");
            var camera=salon.Camera.Transform;var body=salon.Bodies[b.Id];body.Position=Art.V(b.Position);b.AllowDown=true;C(simulation.Down(b,a.Id),"downed state enabled for this test");
            var before=body.Position;for(int i=0;i<12;i++)PartyBodyMove(body,b,new Godot.Vector2(1,1),.7f,1f/60,true,Godot.Vector3.Zero,world.Time);C(body.Position==before&&salon.Camera.Transform==camera,"downed Body rejects movement/jump and leaves FPS camera unchanged");
            a.Position=b.Position+new V(0,0,1.7f);var d=V.Normalize(b.Position+V.UnitY*.8f-Session.Eye(a));a.Yaw=MathF.Atan2(-d.X,-d.Z);a.Pitch=MathF.Asin(d.Y);
            KeyInput(Key.E,true);simulation.Inputs[a.Id]=(default,a.Yaw,a.Pitch,Input.IsPhysicalKeyPressed(Key.E)?Buttons.Interact:0);Tick();KeyInput(Key.E,false);C(b.DownUntil==0&&simulation.ActionLog.Any(x=>x.Kind==PartyAction.Revive),"native E slap revives immediately");
            simulation.Inputs.Clear();simulation.Drop(a);simulation.Drop(b);a.Position=new(0,1,2.2f);b.Position=new(1,1,2.2f);d=V.Normalize(world.SharedHead.ToWorld(new(0,.45f,.2f))-Session.Eye(a));a.Yaw=MathF.Atan2(-d.X,-d.Z);a.Pitch=MathF.Asin(d.Y);
            foreach(var pair in new[]{(a,CoopKind.ScissorA),(b,CoopKind.ScissorB)}){var prop=world.Props.Single(p=>p.Coop==pair.Item2);prop.Holder=pair.Item1.Id;pair.Item1.CarriedProp=prop.Id;}
            simulation.Inputs[a.Id]=(default,a.Yaw,a.Pitch,Buttons.None);simulation.Inputs[b.Id]=(default,b.Yaw,b.Pitch,Buttons.None);Tick();
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true});Input.FlushBufferedEvents();C(Input.IsMouseButtonPressed(MouseButton.Left),"native mouse action enters Godot input");
            simulation.Inputs[a.Id]=(default,a.Yaw,a.Pitch,Input.IsMouseButtonPressed(MouseButton.Left)?Buttons.Primary:0);simulation.InputTimes[a.Id]=world.Time;simulation.Inputs[b.Id]=(default,b.Yaw,b.Pitch,Buttons.Primary);simulation.InputTimes[b.Id]=world.Time-.15f;float mass=world.SharedHead.Mass;Tick();Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false});C(world.SharedHead.Mass<mass&&simulation.ActionLog.Any(x=>x.Kind==PartyAction.CoopCut),"native scissors input cuts with two timestamped handles");
            C(salon.Camera.Transform==camera,"throw catch revive and scissors never shift FPS camera");
            if(DisplayServer.GetName()!="headless"){
                b.AllowDown=true;b.DownImmune=0;simulation.Down(b,a.Id);simulation.Inputs.Clear();Tick();foreach(var p in world.Players)if(salon.Bodies.TryGetValue(p.Id,out var actorBody))actorBody.Position=Art.V(p.Position);salon.Sync(world,1,.016f);salon.Camera.Position=new(3,3,5);salon.Camera.LookAt(new Godot.Vector3(-1,1,-.5f));
                foreach(var locale in new[]{"zh","en"}){L.SetLocale(locale);hud.Update(world,1,true,false,salon.Mirror,0);salon.Sync(world,1,.016f);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var picture=GetViewport().GetTexture().GetImage();var path=ProjectSettings.GlobalizePath("res://artifacts/expansion-9-body-"+locale+".png");C(picture.SavePng(path)==Error.Ok,"body/co-op bilingual screenshot "+locale);}
            }
            GD.Print("PARTY_BODY_ENGINE_OK native G / swept catch / Body down / native E slap / native scissors / camera invariance");QuitGracefully();
        }catch(Exception e){GD.PushError("PARTY_BODY_ENGINE_FAIL "+e);QuitGracefully(1);}finally{foreach(var key in new[]{Key.G,Key.E})Input.ParseInputEvent(new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=false});Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false});}
    }
}

