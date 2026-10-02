using Godot;
using Hairball.Core;
using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
namespace Hairball;
public partial class Main
{
    async void TargetCardChecks()
    {
        try{
            void C(bool good,string m){if(!good)throw new InvalidOperationException(m);GD.Print("TARGET_CARD_ENGINE_PASS "+m);}
            simulation.TargetPicker=_=>16;simulation.StartMatch();world.Phase=Phase.Build;world.Remaining=150;simulation.Tick(.016f);var player=world.Player(1)!;
            salon!.Sync(world,1,.016f);salon.Camera.Position=new(3.0f,2.1f,.5f);salon.Camera.LookAt(new Godot.Vector3(3.0f,1,-3.3f));
            C(world.Props.Count(p=>p.Gesture>=0)==6&&world.Experiment.ResultTarget==null,"six gesture props / no broadcast target before reveal");
            async Task Capture(string name){if(DisplayServer.GetName()=="headless")return;await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var picture=GetViewport().GetTexture().GetImage();string folder=args.TryGetValue("qa-output",out var output)?output:ProjectSettings.GlobalizePath(OS.HasFeature("editor")?"res://artifacts":"user://playtests/phase10");Directory.CreateDirectory(folder);C(picture.SavePng(Path.Combine(folder,"expansion-10-"+name+".png"))==Error.Ok,"screenshot "+name);}
            var wire=Wire.Encode(world);foreach(var locale in new[]{"zh","en"}){L.SetLocale(locale);salon.Sync(world,1,.016f);hud.Update(world,1,true,false,salon.Mirror,0);await Capture("props-"+locale);C(wire.SequenceEqual(Wire.Encode(world)),"localization is presentation only");}
            var prop=world.Props.First(p=>p.Gesture>=0);prop.Holder=1;player.CarriedProp=prop.Id;prop.Position=world.SharedHead.Position;simulation.Tick(.016f);C(world.Experiment.Remark is Remark.StyleHot or Remark.StyleCold,"AI responds through authoritative remarks and species audio path");
            simulation.Drop(player);world.Remaining=0;for(int i=0;i<200&&!world.Experiment.Resolved;i++)simulation.Tick(.1f);C(world.Experiment.ResultTarget?.Kind==TargetKind.Emotion&&world.Experiment.ResultStyle.Count==3,"real departure reveals emotion attributes and item checks");
            foreach(var locale in new[]{"zh","en"}){L.SetLocale(locale);salon.Sync(world,1,.016f);hud.Update(world,1,true,false,salon.Mirror,0);await Capture("reveal-"+locale);}
            GD.Print("TARGET_CARD_ENGINE_OK props / private target / AI remark / real reveal / bilingual authority invariance");QuitGracefully();
        }catch(Exception e){GD.PushError("TARGET_CARD_ENGINE_FAIL "+e);QuitGracefully(1);}
    }
}
