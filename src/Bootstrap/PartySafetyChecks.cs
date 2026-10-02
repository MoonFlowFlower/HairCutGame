using Godot;
using Hairball.Core;
using System;
using System.Linq;
using System.Threading.Tasks;
using V=System.Numerics.Vector3;
namespace Hairball;
public partial class Main
{
    void PartySafetyEngine()
    {
        var body=salon!.Bodies[localId];body.RestoreMotion(new(3,0,2),Vector3.Zero,true,false);var p=world.Player(localId)!;
        p.FreezeUntil=world.Time+2;var start=body.Position;var camera=salon.Camera.Transform;
        for(int i=0;i<120;i++)PartyBodyMove(body,p,new(1,1),i*.01f,1f/60,true,Vector3.Zero,world.Time+i/60f);
        if(body.Position.DistanceTo(start)>.001f||body.Velocity!=Vector3.Zero||salon.Camera.Transform!=camera)throw new InvalidOperationException("frozen Body moved camera or position");
        p.FreezeUntil=0;PartyBodyMove(body,p,new(1,0),1.2f,1f/60,false,Vector3.Zero,world.Time+2.01f);
        if(body.Position.DistanceTo(start)<.01f)throw new InvalidOperationException("Body did not automatically recover movement");
        hud.VerifySecretControls(world,p);
        GD.Print("PARTY_SAFETY_ENGINE_OK native Body freeze / look independent / automatic recovery");
    }
    async void PartySafetyReview()
    {
        try{
            world.Phase=Phase.Build;world.Remaining=120;simulation.TaskPicker=_=>SecretKind.Rescue;simulation.Tick(.01f);salon!.Sync(world,localId,.016f);PartySafetyEngine();
            async Task Photo(string name){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://artifacts/party-p6-"+name+".png"));}
            var p=world.Player(localId)!;p.Position=new(0,0,2.6f);salon.Bodies[localId].Position=Art.V(p.Position);salon.Camera.Position=new(0,1.7f,2.6f);salon.Camera.Rotation=Vector3.Zero;hud.OwnSecret=simulation.OwnTask(localId);p.FreezeUntil=world.Time+2;
            foreach(var lang in new[]{"zh","en"}){L.SetLocale(lang);hud.Update(world,localId,true,false,salon.Mirror,0);await Photo("freeze-"+lang);}
            p.FreezeUntil=0;for(int id=2;id<=4;id++)simulation.AddPlayer(id,"Player "+id);
            foreach(var action in new[]{PartyAction.FreezeFriend,PartyAction.ThawFriend,PartyAction.GlueFriend,PartyAction.ReleaseFriend,PartyAction.FireFriend,PartyAction.WaterFriend}){world.Time+=.1f;simulation.RecordAction(action,2,1);}
            world.Experiment.ResultActions=simulation.ActionLog.TakeLast(6).ToList();world.Experiment.ResultTasks=world.Players.Where(q=>q.Active).Select(q=>new TaskReveal(q.Id,q.Name,(SecretKind)(q.Slot+3),q.Slot%2==0)).ToList();world.Phase=Phase.Results;world.Experiment.Success=true;world.Experiment.BasePay=40;world.Experiment.Tip=20;world.Experiment.Curtain=CurtainReaction.Funny;
            foreach(var lang in new[]{"zh","en"}){L.SetLocale(lang);hud.Update(world,localId,true,false,salon.Mirror,0);await Photo("results-"+lang);}
            hud.ShowMenu(true);foreach(var lang in new[]{"zh","en"}){L.SetLocale(lang);await Photo("menu-"+lang);}
            GD.Print("PARTY_SAFETY_REVIEW_OK injected HUD fixtures / 4 reveals / 6 explicit facts / bilingual menu and status");QuitGracefully();
        }catch(Exception e){GD.PushError("PARTY_SAFETY_FAIL "+e);QuitGracefully(1);}
    }
}
