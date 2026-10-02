using Godot;
using Hairball.Core;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Hairball;
public partial class Main
{
    async void TwistChecks()
    {
        try {
            void C(bool b,string m){if(!b)throw new Exception(m);}
            for(int id=2;id<=4;id++)simulation.AddPlayer(id,"P"+id);
            foreach(var p in world.Players)p.AllowViewChanges=true;simulation.DebugCustomerActor=1;simulation.MatchRounds=3;
            PresentationSettings.AllowViewChanges=true;
            string folder=args.GetValueOrDefault("qa-output",ProjectSettings.GlobalizePath("res://artifacts"));Directory.CreateDirectory(folder);
            foreach(var card in TwistCards.All){simulation.ForceAI=card.AI;simulation.TwistPicker=_=>card.Kind;simulation.StartMatch();simulation.NextRound();world.Phase=Phase.Build;world.Remaining=150;C(world.Twist.Kind==card.Kind,"eligible "+card.Kind);
                localId=card.Kind is TwistKind.BlindBuild or TwistKind.OneViewer or TwistKind.Family?3:1;var p=world.Player(localId)!;salon!.Camera.Position=card.Kind==TwistKind.MirrorWorld?new(0,1.7f,0):new(3,3,5);salon.Camera.LookAt(card.Kind==TwistKind.MirrorWorld?new Vector3(0,1.8f,3.15f):new Vector3(0,1.8f,0));hud.ShowMenu(false);hud.PrivateTarget=simulation.OwnTarget(localId);hud.PrivateTargetRound=world.Round;hud.GuessOptions=simulation.OwnOptions(localId);hud.TargetHeld=true;var recipe=simulation.OwnRecipe(localId);hud.RecipeChinese=recipe.Chinese;hud.RecipeEnglish=recipe.English;salon.PrivateTarget=hud.PrivateTarget;
                if(card.Kind==TwistKind.FogMirror){world.Twist.FogUntil=world.Time+20;world.Twist.Lines.Add(new(new(-.2f,-.1f),new(.2f,.2f)));}
                if(card.Kind==TwistKind.SleepingAI){world.Time+=8.1f;simulation.Tick(.01f);C(world.Twist.HintChinese.Length>0,"sleep clue");}
                if(card.Kind==TwistKind.Contrarian){world.Player(1)!.Expression=1;world.Player(1)!.ExpressionAt=world.Time-.15f;}
                byte[] facts=Wire.Encode(world);var cameraRotation=salon.Camera.Rotation;
                foreach(var language in new[]{"zh","en"}){L.SetLocale(language);salon.Sync(world,localId,.016f);hud.Update(world,localId,true,false,salon.Mirror,0);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();C(image.SavePng(Path.Combine(folder,"expansion-12-"+card.Kind+"-"+language+".png"))==Error.Ok,"PNG");}
                C(facts.SequenceEqual(Wire.Encode(world)),"render cannot change authority "+card.Kind);C(salon.Camera.Rotation==cameraRotation,"camera frame remains unchanged "+card.Kind);
            }
            GD.Print("EXPANSION_12_ENGINE_OK 16 cards; bilingual render; authority/camera invariant");QuitGracefully(0);
        }catch(Exception e){GD.PushError("EXPANSION_12_ENGINE_FAIL "+e);QuitGracefully(1);}
    }
}
