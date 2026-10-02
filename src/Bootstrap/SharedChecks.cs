using Godot;
using Hairball.Core;
using System;
using System.Linq;
using V=System.Numerics.Vector3;
namespace Hairball;
public static class SharedChecks
{
    static void Require(bool value,string name){if(!value)throw new Exception(name);GD.Print("SHARED_PASS "+name);}
    public static async void Run(Main root,SalonView view)
    {
        try
        {
            foreach(var existing in view.Bodies.Values)existing.CollisionLayer=0;
            var s=new Session();var a=s.AddPlayer(1,"A")!;var b=s.AddPlayer(2,"B")!;s.StartMatch();s.State.Phase=Phase.Build;s.State.Remaining=75;
            // Keep this shared physics fixture clear of B's real call-button stand.
            a.Position=new(-1.7f,0,2.8f);b.Position=new(-1.7f,0,1.4f);a.Yaw=0;
            var body=new Body{Position=Art.V(b.Position)};root.AddChild(body);body.Setup(902,1);
            async System.Threading.Tasks.Task Step(V impulse=default){await root.ToSignal(root.GetTree(),SceneTree.SignalName.PhysicsFrame);body.Move(Vector2.Zero,0,1d/60,impulse:Art.V(impulse));}
            for(int i=0;i<15;i++)await Step();
            var before=body.Position;s.State.Tools[3].Holder=1;a.Held=3;s.UseTool(a,false);
            Require(b.Impulse.Z<0,"actual blower action creates authoritative teammate impulse");
            for(int i=0;i<20;i++){await Step(b.Impulse);b.Impulse*=.935f;}
            Require(body.Position.Z<before.Z-.12f&&body.IsOnFloor(),$"production CharacterBody moves from blower impulse on real floor before={before} after={body.Position} grounded={body.IsOnFloor()}");
            var blocker=new Body{Position=body.Position+Vector3.Forward*.65f};root.AddChild(blocker);blocker.Setup(903,2);
            for(int i=0;i<10;i++)await Step();
            Require(body.TestMove(body.GlobalTransform,Vector3.Forward*.8f),"player capsules genuinely obstruct one another");
            s.Stimulate(.6f,1,CustomerStimulus.Noise);s.Tick(.01f);var rest=s.State.SharedHead.Position;s.Tick(.95f);view.Sync(s.State,1,0);
            var collider=view.GetNode<Node3D>("SharedHeadCollider");
            Require(collider.Position==Art.V(s.State.SharedHead.Position)&&collider.Position.Y<rest.Y-.1f,"head physics collider follows authoritative duck");
            var hit=root.GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(collider.Position+Vector3.Back*2,collider.Position+Vector3.Forward*2,2));
            Require(hit.Count>0,"moving head collider remains physically ray-hittable");
            var mesh=new HeadView();root.AddChild(mesh);var h=Head.Create(800,0);mesh.Update(h,1);Require(mesh.SurfaceTriangles>0,"continuous hair shell draws");
            var empty=Head.Create(800,0);empty.Volume.Fill(_=>-1);empty.Volume.Revision=h.Volume.Revision;mesh.Update(empty,2);
            Require(mesh.SurfaceTriangles==0,"same-revision replacement cannot leave a stale shell after round reset");
            s.State.Customers[0].Action=CustomerReaction.None;a.Held=1;s.State.Tools[1].Holder=1;s.Drop(a);float release=s.State.Tools[1].Position.Y;
            for(int i=0;i<100;i++)s.Tick(.02f);
            Require(s.State.Tools[1].Position.Y<release-.5f&&s.State.Tools[1].Position.Y>=.22f,"dropped shared tool falls and remains retrievable above floor");
            GD.Print("SHARED_CHECKS_OK");root.QuitGracefully();
        }
        catch(Exception e){GD.PushError("SHARED_CHECKS_FAIL "+e);root.QuitGracefully(1);}
    }
}

