using Godot;
using Hairball.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
namespace Hairball;
public partial class Main
{
    async void ExpansionFeelChecks()
    {
        try{
            if(DisplayServer.GetName()=="headless")throw new Exception("phase 8 capture requires rendering");
            string output=args.GetValueOrDefault("qa-output",ProjectSettings.GlobalizePath(OS.HasFeature("editor")?"res://artifacts":"user://playtests/phase8"));Directory.CreateDirectory(output);
            async Task Frames(int n=3){for(int i=0;i<n;i++)await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);}
            async Task Shot(string name){await Frames();using var image=GetViewport().GetTexture().GetImage();if(image.SavePng(Path.Combine(output,"expansion-8-"+name+".png"))!=Error.Ok)throw new IOException("phase 8 screenshot write failed");}
            for(int id=2;id<=4;id++)simulation.AddPlayer(id,"Player "+id);
            world.Phase=Phase.Arrival;salon!.Sync(world,localId,.016f);await Frames(8);
            world.Phase=Phase.Build;world.Remaining=120;var h=world.SharedHead;h.Volume.Fill(v=>Math.Max(Math.Min(.68f-Math.Abs(v.X),Math.Min(.18f-Math.Abs(v.Y-.7f),.5f-Math.Abs(v.Z))),Math.Min(.45f-new System.Numerics.Vector2(v.X,v.Z).Length(),.3f-Math.Abs(v.Y-.35f))));
            var prop=world.Props.Single(p=>p.Goal==0);prop.Attached=true;prop.Local=new(0,1.14f,0);prop.Position=h.ToWorld(prop.Local);prop.PlacedAt=0;
            var player=world.Player(localId)!;var tool=world.Tools.First(t=>t.Id==0);tool.Holder=localId;player.Held=tool.Id;player.Position=new(0,0,2.7f);salon.Bodies[localId].Position=Art.V(player.Position);
            salon.Camera.Position=new(2.7f,3,3.4f);salon.Camera.LookAt(Art.V(h.Position)+Vector3.Up*.7f);
            foreach(var state in new[]{"soft","hard","glue","frozen"}){
                foreach(var p in h.Patches){p.Glue=state=="glue"?1:0;p.Stiffness=state=="hard"?1:0;p.Temperature=state=="frozen"?-65:20;p.Anchored=false;}
                world.Time+=.4f;tool.LastUse=world.Time-.08f;tool.Contact=h.ToWorld(new(0,.88f,.3f));tool.HitHair=true;tool.ContactState=state=="glue"?HairState.Glued:state=="frozen"?HairState.Frozen:HairState.Normal;
                world.Experiment.LandingIssues=state=="soft"?LandingIssue.Soft:LandingIssue.None;
                var wire=Wire.Encode(world);var camera=salon.Camera.Transform;
                foreach(var lang in new[]{"zh","en"}){L.SetLocale(lang);salon.Sync(world,localId,.016f);hud.Update(world,localId,true,false,salon.Mirror,0);await Shot(state+"-"+lang);}
                if(!wire.SequenceEqual(Wire.Encode(world))||salon.Camera.Transform!=camera)throw new Exception("visual material/locale changed authority or FPS camera");
            }
            world.Phase=Phase.Build;world.Experiment.Leave=LeaveStage.Mirror;salon.SyncGallery(world);await Frames(5);
            foreach(var lang in new[]{"zh","en"}){L.SetLocale(lang);salon.SyncGallery(world);await Shot("countdown-"+lang);}
            world.Experiment.Resolved=true;world.Experiment.ResultStarted=world.Time-8;world.Experiment.CustomerActor=0;world.Experiment.Success=false;world.Phase=Phase.Results;
            world.Experiment.ResultHair=world.Players.Select(p=>new HairTrace(p.Id,p.Name,10,6+p.Slot)).ToList();world.Experiment.ResultActions=[new(world.Time,PartyAction.Bell,localId,player.Name,0)];
            var watch=Stopwatch.StartNew();while(salon.LastPhotoPath==""&&watch.Elapsed.TotalSeconds<8){salon.Sync(world,localId,.016f);await Frames();}
            var entry=GalleryStore.Load(ProjectSettings.GlobalizePath("user://gallery")).FirstOrDefault(e=>e.Image==salon.LastPhotoPath);
            if(entry?.Metadata.Assets?.Length!=6||entry.Metadata.Hair?.Length!=4)throw new Exception("paired photo, group or four portraits missing");
            foreach(var key in new[]{"before","group","avatar-0"})File.Copy(GalleryStore.AssetPath(entry.Image,key),Path.Combine(output,"expansion-8-photo-"+key+".png"),true);
            foreach(var lang in new[]{"zh","en"}){L.SetLocale(lang);hud.Update(world,localId,true,false,salon.Mirror,0);await Shot("results-"+lang);}
            GD.Print("EXPANSION_FEEL_ENGINE_OK bilingual four materials / soft sink / countdown / before-after + group + 4 portraits / unchanged authority and FPS camera");QuitGracefully();
        }catch(Exception e){GD.PushError("EXPANSION_FEEL_ENGINE_FAIL "+e);QuitGracefully(1);}
    }
}
