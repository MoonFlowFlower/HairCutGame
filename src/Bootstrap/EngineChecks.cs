using Godot;
using Hairball.Core;
using System;
using System.Linq;
using NVector=System.Numerics.Vector3;

namespace Hairball;
public static class EngineChecks
{
    static void Require(bool condition,string detail){if(!condition)throw new InvalidOperationException(detail);GD.Print("ENGINE_PASS "+detail);}
    public static void Run(Main root,SalonView view,Func<NVector,NVector,float,float>? obstacle)
    {
        var s=new Session();var p=s.AddPlayer(1,"Integration")!;s.StartMatch();s.State.Phase=Phase.Build;s.State.Remaining=75;s.ObstructionDistance=obstacle;
        for(int id=2;id<=4;id++)s.AddPlayer(id,"Helper "+id);
        Require(s.State.Customers.Count==1&&s.State.Heads.Count(h=>!h.Barber&&!h.Loose&&!h.Facial)==1,"four players share exactly one customer");
        Require(s.State.SharedHead.Patches.Count==32,"spawn 32-patch customer and shared barber representation");
        for(int index=0;index<Tools.All.Length;index++)
        {
            var h=s.State.SharedHead;h.Volume=HairVolume.Create();foreach(var patch in h.Patches){patch.Length=.8f;patch.Glue=0;patch.Temperature=20;patch.Burning=false;patch.Anchored=false;patch.Direction=NVector.UnitY;}
            s.State.Heads.RemoveAll(x=>x.ParentHead==0);foreach(var region in new[]{HairRegion.LeftBrow,HairRegion.RightBrow,HairRegion.Beard})s.State.Heads.Add(Head.CreateFace(h,region));
            p.Position=Session.WorkCenter+new NVector(0,0,2);p.Yaw=0;p.Pitch=.12f;p.Cooldown=0;
            var t=s.State.Tools.First(x=>x.Definition==index);t.Holder=1;p.Held=t.Id;var before=Wire.Encode(s.State.Heads.Where(x=>x.Id==0||x.ParentHead==0).ToArray());s.UseTool(p,false);
            Require(!before.SequenceEqual(Wire.Encode(s.State.Heads.Where(x=>x.Id==0||x.ParentHead==0).ToArray())),"real authoritative aim/query/effect: "+Tools.Get(index).Id);
            t.Holder=0;
        }
        // Directly align an unsecured wig with suction; the generic query must release its attachment.
        var wig=s.SpawnWig(s.State.SharedHead.Position+new NVector(0,.4f,0),0);wig.AttachedTo=0;
        p.Held=2;s.State.Tools[2].Holder=1;p.Cooldown=0;p.Pitch=.27f;p.Reservoir=0;s.UseTool(p,false);
        Require(wig.AttachedTo<0,"vacuum detaches physical wig");
        s.Incident(1,0,25);Require(s.State.Customers[0].Recovery>0,"catastrophic KO entered");
        s.Tick(.01f);var seated=s.State.SharedHead.Position;s.Tick(.7f);view.PhysicsCustomers(s.State);Require(s.State.SharedHead.Position!=seated,"telegraphed reaction moves actual queried head");
        s.Tick(4.2f);view.PhysicsCustomers(s.State);Require(s.State.Customers[0].Recovery==0,"automatic reseat after KO");
        for(int goal=0;goal<8;goal++)
        {
            s.State.Job.Goal=goal;PhysicalProps.Stage(s.State);s.State.Phase=Phase.Build;view.Sync(s.State,1,.1f);
            var ids=s.State.Props.Select(p=>p.Id).ToArray();
            Validation.Begin(s.State);Require(ids.SequenceEqual(s.State.Props.Select(p=>p.Id)),"same construction prop identities survive validation "+goal);s.State.Phase=Phase.Validation;
            for(int step=0;step<90;step++)Validation.Tick(s.State,.1f,step*.1f);
            view.Sync(s.State,1,.1f);Validation.Finish(s.State);
            Require(s.State.Props.Count>0,"goal ghost + rendered validation "+Goals.All[goal].Name);
        }
        s.State.Phase=Phase.Build;s.State.Remaining=.01f;s.Tick(.02f);float mass=s.State.SharedHead.Mass;p.Cooldown=0;s.UseTool(p,false);
        Require(s.State.SharedHead.Mass==mass&&s.State.Barber(0).Locked,"shared timeout disables deliberate edits");
        var hit=root.GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(new(0,1,2),new(0,1,0),2));
        Require(hit.Count>0,"chair/customer collider blocks physical movement path");
        s.State.Phase=Phase.Build;p.ReferenceUp=true;var facts=Wire.Encode(s.State);view.Sync(s.State,1,.016f);
        Require(view.Camera.GetNode<Node3D>("OffhandReference").Visible,"offhand board visibly raises in the real scene");
        Require(facts.SequenceEqual(Wire.Encode(s.State)),"reference renders cannot alter authoritative hair, tools or props");
        Require(view.GetChildren().OfType<TargetReference>().Single().GetChildren().OfType<SubViewport>().Count()==3,"front side and top use three actual render viewports");
        p.ReferenceUp=false;view.Sync(s.State,1,.016f);Require(!view.Camera.GetNode<Node3D>("OffhandReference").Visible,"reference board lowers without changing primary slot");
        var mini=s.State.Heads.Single(h=>h.Miniature);
        Require(view.GetChildren().OfType<PracticeModelView>().Count()==1,"one physical practice doll rendered beside the immutable board");
        var doll=view.GetChildren().OfType<PracticeModelView>().Single();
        Require(doll.GetChildren().OfType<AnimatableBody3D>().Single().CollisionLayer==2,"practice doll blocks player movement when placed");
        mini.Holder=1;p.CarriedModel=mini.Id;view.Sync(s.State,1,.016f);
        Require(doll.GetChildren().OfType<AnimatableBody3D>().Single().CollisionLayer==0,"carried practice doll does not trap its carrier");
        mini.Holder=0;p.CarriedModel=-1;mini.Position=new(-3,3.5f,-3.6f);
        s.ModelCast=(from,to)=>{
            var contact=root.GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(Art.V(from),Art.V(to),49));
            return contact.Count>0?(Art.N(contact["position"].AsVector3()),Art.N(contact["normal"].AsVector3())):null;
        };
        for(int i=0;i<180;i++)s.TickModels(1f/60);
        Require(Math.Abs(mini.Position.Y-(1.3f+MiniatureModel.Foot-.025f))<.045f,"practice doll lands on actual shop display pedestal collision");
        GD.Print("ENGINE_INTEGRATION_OK");
    }
}
