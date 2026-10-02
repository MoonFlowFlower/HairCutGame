using Godot;
using Hairball.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using V=System.Numerics.Vector3;
namespace Hairball;
public partial class Main
{
    void StageExperimentReview()
    {
        if(!args.TryGetValue("v06-review",out var scenario))return;
        world.Phase=Phase.Build;world.Remaining=120;var h=world.SharedHead;var p=world.Player(localId)!;
        p.Position=new(1.05f,0,2.05f);p.Held=-1;
        if(scenario is not ("table" or "growth-spray"))
        {
            h.Volume.Fill(v=>Math.Max(Math.Min(.68f-Math.Abs(v.X),Math.Min(.18f-Math.Abs(v.Y-.7f),.5f-Math.Abs(v.Z))),Math.Min(.45f-new System.Numerics.Vector2(v.X,v.Z).Length(),.3f-Math.Abs(v.Y-.35f))));
            foreach(var patch in h.Patches)patch.Glue=scenario=="soft"?0:1;
            var prop=world.Props.Single(x=>x.Goal==0);prop.Attached=true;prop.Local=new(0,1.14f,0);prop.Position=h.ToWorld(prop.Local);
        }
        if(scenario=="tolerance"){world.Experiment.Tolerance=78;world.Experiment.Remark=Remark.Warning;world.Experiment.RemarkUntil=world.Time+100;}
        if(scenario=="walk"){world.Experiment.LeaveStarted=world.Time-4;world.Experiment.Leave=LeaveStage.ToMirror;CustomerMotion.Apply(world);}
        if(scenario=="results"){world.Experiment.LeaveStarted=world.Time-12;world.Experiment.Leave=LeaveStage.Done;world.Remaining=-12;}
        var point=h.Position+new V(0,.55f,0);
        if(scenario=="growth-spray"){p.Position=new(0,0,2.45f);p.Held=1;world.Tools.First(t=>t.Id==1).Holder=p.Id;point=h.ToWorld(new(0,.42f,.4f));}
        var target=point-Session.Eye(p);yaw=p.Yaw=MathF.Atan2(-target.X,-target.Z);pitch=p.Pitch=MathF.Asin(target.Y/target.Length());
    }
    void ExperimentReviewInput(ref Vector2 movement,ref Buttons buttons)
    {if(args.GetValueOrDefault("v06-review")=="growth-spray"){movement=Vector2.Zero;buttons=Buttons.Primary;}}
    async void PartyGalleryChecks()
    {
        try{
            if(DisplayServer.GetName()=="headless")throw new InvalidOperationException("gallery check needs rendering");
            world.Phase=Phase.Build;world.Experiment.LeaveStarted=world.Time-6;world.Experiment.Leave=LeaveStage.Mirror;CustomerMotion.Apply(world);
            salon!.Sync(world,localId,.016f);for(int i=0;i<4;i++)await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            simulation.RecordAction(PartyAction.Bell,localId);world.Experiment.ResultActions=simulation.ActionLog.TakeLast(6).ToList();world.Experiment.Resolved=true;world.Experiment.Success=false;world.Experiment.Curtain=CurtainReaction.Complaint;world.Phase=Phase.Results;
            salon.SyncGallery(world);for(int i=0;i<3;i++)await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);salon.SyncGallery(world);
            var found=GalleryStore.Load(ProjectSettings.GlobalizePath("user://gallery"));if(salon.LastPhotoPath==""||!found.Any(x=>x.Image==salon.LastPhotoPath&&!x.Metadata.Success&&x.Metadata.Round==world.Round&&x.Metadata.Actions.Any(a=>a.Actor==localId)))throw new InvalidOperationException("actual PNG or authoritative metadata missing");
            foreach(var lang in new[]{"zh","en"}){L.SetLocale(lang);hud.Update(world,localId,true,false,salon.Mirror,0);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://artifacts/party-p4-results-"+lang+".png"));}
            GD.Print("PARTY_GALLERY_ENGINE_OK actual mirror PNG / final metadata / bilingual results / local wall");QuitGracefully();
        }catch(Exception e){GD.PushError("PARTY_GALLERY_ENGINE_FAIL "+e);QuitGracefully(1);}
    }
    async void ExperimentChecks()
    {
        try
        {
            void Check(bool b,string text){if(!b)throw new InvalidOperationException(text);GD.Print("V06_ENGINE_PASS "+text);}
            var s=simulation;var w=world;var p=w.Player(localId)!;
            void Aim(V point){var d=V.Normalize(point-Session.Eye(p));yaw=p.Yaw=MathF.Atan2(-d.X,-d.Z);pitch=p.Pitch=MathF.Asin(d.Y);}
            void Tick(Buttons b=Buttons.None){s.Inputs[p.Id]=(default,p.Yaw,p.Pitch,b);s.Tick(1f/60);salon!.PhysicsCustomers(w);salon.Sync(w,localId,1f/60);}
            async Task Frames(int n=2){for(int i=0;i<n;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);}
            void Platform(bool hard){w.SharedHead.Volume.Fill(v=>Math.Max(Math.Min(.68f-Math.Abs(v.X),Math.Min(.18f-Math.Abs(v.Y-.7f),.5f-Math.Abs(v.Z))),Math.Min(.45f-new System.Numerics.Vector2(v.X,v.Z).Length(),.3f-Math.Abs(v.Y-.35f))));foreach(var q in w.SharedHead.Patches){q.Glue=hard?1:0;q.Temperature=20;q.Burning=false;q.Stiffness=0;q.Anchored=false;}}
            void Reset(){s.Inputs.Clear();s.StartMatch();w.Phase=Phase.Build;w.Remaining=150;salon!.Sync(w,localId,.016f);}
            Reset();Check(s.BuildSeconds==150&&w.Customers.Count==1&&!w.Tools.Any(t=>t.Definition==7),"150s shared B with no sniper shelf");
            var prop=w.Props.Single(t=>t.Goal==0);p.Position=new(-1.35f,0,2.8f);Aim(prop.Position);await Frames();Tick(Buttons.Interact);
            Check(p.CarriedProp==prop.Id&&prop.Holder==p.Id&&p.Held<0,"real E picks table commission and frees main hand");
            Tick();Platform(false);p.Position=new(1.2f,1.2f,1.2f);Aim(w.SharedHead.ToWorld(new V(0,.88f,0)));await Frames();Tick(Buttons.Interact);
            Check(prop.Attached&&prop.Holder==0&&w.Experiment.PlacementCount==1,"real E places at aimed hair surface");
            for(int i=0;i<90;i++)Tick();Check(!prop.Attached&&w.Experiment.SlipCount==1&&!w.Experiment.Resolved,"soft fixture slides after one second without ending round");
            Platform(true);p.Position=prop.Position+new V(0,0,1.6f);p.Position.Y=0;Aim(prop.Position);Tick();await Frames();Tick(Buttons.Interact);
            Check(prop.Holder==p.Id,"slipped commission can be picked up with E");
            Tick();p.Position=new(1.2f,1.2f,1.2f);Aim(w.SharedHead.ToWorld(new V(0,.88f,0)));await Frames();Tick(Buttons.Interact);
            for(int i=0;i<190;i++)Tick();Check(prop.Attached&&w.Experiment.StableSeconds==3&&!w.Experiment.Resolved,"stable trials remain live and never auto-submit");
            p.Position=new(1.6f,0,2.65f);Aim(PartyLoop.Bell);Tick();await Frames();Tick(Buttons.Interact);
            Check(w.Experiment.Leave==LeaveStage.Rising&&w.Experiment.BellActor==p.Id,"ordinary E rings early completion bell");
            s.Inputs.Clear();w.Experiment.LeaveStarted=w.Time-4;w.Experiment.Leave=LeaveStage.ToMirror;CustomerMotion.Apply(w);salon!.PhysicsCustomers(w);salon.Sync(w,localId,.016f);
            var collider=salon.GetNode<Node3D>("SharedHeadCollider");Check(collider.Position.DistanceTo(Art.V(w.SharedHead.Position))<.001f,"moving customer physics and query transforms agree");Check(((AnimatableBody3D)collider).CollisionLayer==0,"departing customer cannot forcibly shove FPS players; material query stays active");
            var unbraced=PartyLoop.Pose(w).Rotation.Length();p.Position=w.SharedHead.Position+new V(1.3f,-1.5f,0);Aim(w.SharedHead.Position);Tick(Buttons.Interact);
            Check(p.Bracing&&PartyLoop.Pose(w).Rotation.Length()<unbraced*.4f,"close E brace dampens walking sway");
            Tick();s.Drop(p);var glue=w.Tools.First(t=>t.Definition==4);glue.Holder=p.Id;p.Held=glue.Id;
            Aim(w.SharedHead.ToWorld(new V(.4f,.7f,0)));p.Cooldown=0;Tick(Buttons.Primary);Check(glue.HitHair,"real tool input hits moving head");
            for(int i=0;i<650&&!w.Experiment.Resolved;i++)Tick();Check(w.Experiment.Success&&w.Experiment.Leave==LeaveStage.Done,"supported fixture survives complete mirror and door route");
            var copy=Wire.Decode<WorldState>(Wire.Encode(w));Check(copy.Experiment.Leave==LeaveStage.Done&&copy.Experiment.PlacementCount==2&&copy.Experiment.BellActor==p.Id,"new route placement and bell facts round-trip");
            Reset();p.Position=new(0,0,2.1f);Aim(w.SharedHead.ToWorld(new V(0,.5f,.4f)));var fire=w.Tools.First(t=>t.Definition==8);fire.Holder=p.Id;p.Held=fire.Id;
            for(int i=0;i<20;i++)Tick(Buttons.Primary);float danger=w.Experiment.Tolerance;Check(danger>0,"real flame raises tolerance");
            s.Drop(p);var water=w.Tools.First(t=>t.Definition==10);water.Holder=p.Id;p.Held=water.Id;
            for(int i=0;i<300;i++){if(i%10==0&&w.SharedHead.Patches.FirstOrDefault(x=>x.Burning) is {} burn){var tip=w.SharedHead.ToWorld(burn.Root+burn.Direction*Math.Min(.25f,burn.Length));var radial=V.Normalize(new V(tip.X-w.SharedHead.Position.X,0,tip.Z-w.SharedHead.Position.Z)+new V(.01f,0,.01f));p.Position=w.SharedHead.Position+radial*2.1f-V.UnitY*1.5f;Aim(tip);}Tick(Buttons.Primary);}
            Check(w.Experiment.Tolerance<danger&&!w.Experiment.Resolved,"ordinary water rescues fire");
            foreach(string lang in new[]{"zh","en"}){L.SetLocale(lang);hud.Update(w,localId,true,false,salon.Mirror,0);Check(hud.ExperimentGoalText.Contains(L.T("Customer tolerance")),"bilingual visible tolerance "+lang);}
            Reset();p.Position=new(0,0,2);Aim(w.SharedHead.ToWorld(new V(0,.5f,.4f)));var bottle=w.Tools.First(t=>t.Definition==1);bottle.Holder=p.Id;p.Held=bottle.Id;for(int i=0;i<40;i++)Tick(Buttons.Primary);
            Check(bottle.GrowthRemaining<1&&w.Tools.Where(t=>t.Definition==1&&t.Id!=bottle.Id).All(t=>t.GrowthRemaining==1),"actual growth spends only held bottle");
            var state=Wire.Decode<WorldState>(Wire.Encode(w));Check(state.Tools.Single(t=>t.Id==bottle.Id).GrowthRemaining==bottle.GrowthRemaining,"bottle volume synchronizes");
            var model=Art.Tool(salon,1);Check(model.GetNode<Node3D>("BottleFluid")!=null,"growth bottle has physical liquid window");model.QueueFree();
            PartySafetyEngine();GD.Print("V06_ENGINE_OK");QuitGracefully();
        }
        catch(Exception e){GD.PushError("V06_ENGINE_FAIL "+e);QuitGracefully(1);}
    }
}

