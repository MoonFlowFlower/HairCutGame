using Godot;
using Hairball.Core;
using System;
using System.Linq;
using NVector=System.Numerics.Vector3;

namespace Hairball;
public static class AccessChecks
{
    static void Require(bool value,string name){if(!value)throw new Exception(name);GD.Print("ACCESS_PASS "+name);}
    public static async void Run(Main root,SalonView view)
    {
        try
        {
            foreach(var existing in view.Bodies.Values)existing.CollisionLayer=0;
            var body=new Body{Position=new(0,0,-4.5f)};root.AddChild(body);body.Setup(900,0);
            async System.Threading.Tasks.Task Step(Vector2 movement,bool jump=false){await root.ToSignal(root.GetTree(),SceneTree.SignalName.PhysicsFrame);body.Move(movement,0,1d/60,false,jump);}
            for(int i=0;i<20;i++)await Step(Vector2.Zero);
            Require(body.IsOnFloor(),"real capsule stands on shop floor");
            async System.Threading.Tasks.Task ReplayMotor(string name,Vector3 start,Vector2 move,bool jumping,int count,int checkpoint)
            {
                body.RestoreMotion(start,Vector3.Zero,false,false);
                for(int i=0;i<20;i++)await Step(Vector2.Zero);
                Vector3 savedPosition=default,savedVelocity=default;bool savedGround=false,savedJump=false;
                for(int i=0;i<count;i++){
                    await Step(move,jumping&&i<35);
                    if(i==checkpoint){savedPosition=body.Position;savedVelocity=body.Velocity;savedGround=body.IsOnFloor();savedJump=body.JumpHeld;}
                }
                var expected=body.Position;var expectedVelocity=body.Velocity;
                body.RestoreMotion(savedPosition,savedVelocity,savedGround,savedJump);
                // Reconciliation replays several fixed commands in one physics frame.
                for(int i=checkpoint+1;i<count;i++)body.Move(move,0,1d/60,false,jumping&&i<35);
                Require(body.Position.DistanceTo(expected)<.025f&&body.Velocity.DistanceTo(expectedVelocity)<.1f,$"prediction replay {name} actual={body.Position} expected={expected} velocity={body.Velocity}/{expectedVelocity}");
            }
            await ReplayMotor("jump and landing",new(0,0,-4.5f),Vector2.Zero,true,90,15);
            await ReplayMotor("wall collision",new(4,0,-4.5f),Vector2.Right,false,90,10);
            var probe=new Session();probe.AddPlayer(900,"Probe");probe.StartMatch();view.Bodies[900]=body;
            var oldFrame=new MotionFrame{Round=probe.State.Round,Time=1,Head=new(0,probe.State.SharedHead.Position,default),Players=[new(900,1,new(0,0,-4.5f),default,default,0,0,true,false)]};
            var newFrame=new MotionFrame{Round=probe.State.Round,Time=2,Head=oldFrame.Head,Players=[new(900,2,new(2,0,-4.5f),default,default,0,0,true,false)]};
            view.SyncMotionColliders(newFrame,1);view.RenderMotion(probe.State,1,oldFrame,newFrame,.5f);
            Require(body.Position==new Vector3(2,0,-4.5f)&&body.Model.GlobalPosition.DistanceTo(new(1,0,-4.5f))<.001f,"remote visual interpolation cannot rewind prediction collision body");
            view.Bodies.Remove(900);body.Model.Position=Vector3.Zero;
            body.RestoreMotion(new(0,0,-4.5f),Vector3.Zero,false,false);for(int i=0;i<20;i++)await Step(Vector2.Zero);
            float peak=0;for(int i=0;i<120;i++){await Step(Vector2.Zero,true);peak=Math.Max(peak,body.Position.Y);}
            Require(peak>1.15f&&body.Position.Y<.08f,"Space impulse jumps and holding does not auto-bounce");
            var ladder=new LadderView();root.AddChild(ladder);ladder.Sync(new(){Id=77,Position=new(-2.5f,0,-.7f)},true);
            await ReplayMotor("physical stair stepping",new(-2.5f,0,.8f),new(0,-1),false,38,12);
            body.Position=new(-2.5f,0,.8f);body.Velocity=Vector3.Zero;
            for(int i=0;i<12;i++)await Step(Vector2.Zero);
            for(int i=0;i<120&&body.Position.Z> -1.39f;i++)await Step(new(0,-1));
            for(int i=0;i<15;i++)await Step(Vector2.Zero);
            Require(body.Position.Y>1.20f&&body.IsOnFloor(),$"walk up physical treads and stand on platform ({body.Position})");
            var s=new Session();var p=s.AddPlayer(1,"Access")!;s.StartMatch();s.State.Phase=Phase.Build;
            var head=s.State.SharedHead;head.Volume.Fill(v=>Math.Min(.5f-Math.Abs(v.X),Math.Min(.4f-Math.Abs(v.Z),Math.Min(v.Y-.2f,3.3f-v.Y))));
            p.Position=Art.N(body.Position);var d=head.Position+new NVector(0,2.65f,.35f)-Session.Eye(p);p.Yaw=MathF.Atan2(-d.X,-d.Z);p.Pitch=MathF.Asin(d.Y/d.Length());p.Held=0;s.State.Tools[0].Holder=1;
            float before=head.Mass;s.UseTool(p,false);Require(head.Mass<before,"standing on ladder can shave high hair using authoritative aim");
            var panel=new GoalSection();root.AddChild(panel);var guideCamera=new Camera3D();root.AddChild(guideCamera);
            // Godot's headless dummy renderer retains the initial GPU texture image.
            // Check the uploaded source there; -Rendered verifies actual GPU readback too.
            Image ReadGuide()=>DisplayServer.GetName()=="headless"?(Image)panel.ProjectionPixels!.Duplicate():((StandardMaterial3D)panel.MaterialOverride).AlbedoTexture.GetImage();
            void Guide(Vector3 eye,Vector3 look,Vector3 up){guideCamera.Position=eye;guideCamera.LookAt(look,up);panel.UpdateView(guideCamera,Transform3D.Identity);}
            float Alpha(Vector3 point)
            {
                var cameraPoint=guideCamera.ToLocal(point);var planePoint=panel.ToLocal(guideCamera.ToGlobal(cameraPoint/-cameraPoint.Z));var size=((QuadMesh)panel.Mesh).Size;
                using var pixels=ReadGuide();
                int x=(int)((planePoint.X/size.X+.5f)*pixels.GetWidth()),y=(int)((.5f-planePoint.Y/size.Y)*pixels.GetHeight());
                return x>=0&&y>=0&&x<pixels.GetWidth()&&y<pixels.GetHeight()?pixels.GetPixel(x,y).A:0;
            }
            panel.SetGoal(6);Guide(new(0,.8f,4),new(0,.8f,0),Vector3.Up);
            Require(panel.Mesh is QuadMesh&&panel.GlobalBasis.Z.Dot(guideCamera.GlobalBasis.Z)>.999f,"target is one camera-facing plane");
            Require(Alpha(new(0,.7f,0))==0&&Alpha(new(.46f,.7f,0))>0&&Alpha(new(0,1.3f,0))>0,"front laundry outline has two posts, crossbar and opening");
            float frontWidth=((QuadMesh)panel.Mesh).Size.X;
            Guide(new(4,.8f,0),new(0,.8f,0),Vector3.Up);
            Require(((QuadMesh)panel.Mesh).Size.X<frontWidth*.55f&&Alpha(new(0,.7f,0))>0,$"side laundry outline is a narrow solid post, not the front arch (width={((QuadMesh)panel.Mesh).Size.X}, front={frontWidth}, alpha={Alpha(new(0,.7f,0))})");
            Guide(new(0,4,0),Vector3.Zero,Vector3.Forward);
            Require(Alpha(new(0,1.3f,0))>0&&((QuadMesh)panel.Mesh).Size.Y<((QuadMesh)panel.Mesh).Size.X*.5f,"top laundry outline shows crossbar depth");
            panel.SetGoal(7);panel.UpdateView(guideCamera,Transform3D.Identity);
            Require(Alpha(new(0,.55f,0))==0&&Alpha(new(.4f,.55f,0))>0,"top bowl projection exposes bore and rim");
            Guide(new(0,.55f,4),new(0,.55f,0),Vector3.Up);
            Require(Alpha(new(0,.55f,0))>0,"front bowl projection includes far wall instead of a false cutaway");
            using var beforeMove=ReadGuide();
            guideCamera.Position+=Vector3.Up*.8f;panel.UpdateView(guideCamera,new(Basis.Identity,Vector3.Up*.8f));
            using var afterMove=ReadGuide();
            Require(beforeMove.GetData().SequenceEqual(afterMove.GetData()),"guide stays aligned when chair and eye translate together");
            guideCamera.Position=new(0,.7f,-4);guideCamera.Rotation=Vector3.Zero;panel.UpdateView(guideCamera,Transform3D.Identity);
            Require(panel.Mesh==null,"target behind camera cannot draw a reflected guide");
            panel.SetGoal(6);Guide(new(0,.8f,.1f),new(0,.8f,0),Vector3.Up);
            Require(panel.Mesh is QuadMesh&&((QuadMesh)panel.Mesh).Size.IsFinite(),"near-plane crossing produces finite clipped projection");
            var watch=System.Diagnostics.Stopwatch.StartNew();
            for(int i=0;i<60;i++){float angle=i*Mathf.Tau/60;Guide(new(Mathf.Sin(angle)*2,.8f,Mathf.Cos(angle)*2),new(0,.8f,0),Vector3.Up);}
            GD.Print($"GUIDE_ORBIT_TIMING updates=60 mean_ms={watch.Elapsed.TotalMilliseconds/60:0.00} renderer={DisplayServer.GetName()}");
            panel.QueueFree();guideCamera.QueueFree();
            Require(view.CanPlaceLadder(new(4,0,0),0,1)&&!view.CanPlaceLadder(new(-2.5f,0,-.7f),0,1),"placement accepts clear floor and rejects occupied geometry");
            s.State.Customers[0].ChairHeight=-.3f;view.Sync(s.State,1,.1f);
            var historical=new ReplayFrame{Heads=s.State.Heads.Select(h=>h.Clone()).ToList(),Players=[p.Position],Customers=[new(){Slot=0,ChairHeight=1}],Ladders=s.State.Ladders.Select(l=>l.Clone()).ToList()};
            historical.Ladders[0].Position+=NVector.UnitX*3;view.Sync(s.State,1,.1f,historical);
            Require(Math.Abs(view.GetNode<Node3D>("ChairCollider_0").Position.Y-.55f)<.001f,"replay chair visuals cannot move live chair collision");
            Require(view.Ladders[0].Position==Art.V(s.State.Ladders[0].Position)&&view.Ladders[0].Colliders.All(c=>c.CollisionLayer==16),"replay ladder visuals preserve live platform collision");
            GD.Print("ACCESS_CHECKS_OK");root.QuitGracefully();
        }
        catch(Exception e){GD.PushError("ACCESS_CHECKS_FAIL "+e);root.QuitGracefully(1);}
    }
}
