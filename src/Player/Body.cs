using Godot;
namespace Hairball;
public partial class Body : CharacterBody3D
{
    public int Peer;
    public Node3D Model=null!;
    bool jumpHeld;
    bool? restoredGround;
    public bool JumpHeld=>jumpHeld;
    public void RestoreMotion(Vector3 position,Vector3 velocity,bool grounded,bool held)
    {
        // MoveAndSlide also remembers floor normals/platform contacts internally.
        // Refresh those at the restored location before replay, without retaining
        // the refresh displacement or consuming a gameplay input.
        Position=position;Velocity=grounded?Vector3.Down*.01f:Vector3.Zero;MoveAndSlide();
        Position=position;Velocity=velocity;restoredGround=grounded;jumpHeld=held;
    }
    public Body()
    {
        CollisionLayer=4;CollisionMask=23;FloorSnapLength=.32f;
        AddChild(new CollisionShape3D{Position=new(0,.86f,0),Shape=new CapsuleShape3D{Radius=.28f,Height=1.72f}});
    }
    public void Setup(int peer,int slot)
    {Peer=peer;Model=Art.Person(this,Art.Team[slot]);Model.RotationDegrees=new(0,180,0);Art.Layers(Model,1u<<(slot+1));}
    public void Move(Vector2 input,float yaw,double dt,bool anchored=false,bool jump=false,Vector3 impulse=default,bool frozen=false)
    {
        if(frozen){Velocity=Vector3.Zero;jumpHeld=jump;Model.Rotation=new(0,yaw+Mathf.Pi,0);return;}
        var direction=new Vector3(input.X,0,input.Y).Rotated(Vector3.Up,yaw);
        if(direction.Length()>1)direction=direction.Normalized();
        bool grounded=restoredGround??IsOnFloor();restoredGround=null;float speed=anchored?1.3f:3.4f;
        float vertical=grounded?-.2f:Velocity.Y-(float)dt*12;
        if(jump&&!jumpHeld&&grounded)vertical=5.8f;jumpHeld=jump;
        var motion=direction*speed*(float)dt;
        if(grounded&&vertical<=0&&motion.LengthSquared()>.000001f&&TestMove(GlobalTransform,motion))
        {
            var raised=GlobalTransform;raised.Origin+=Vector3.Up*.28f;
            if(!TestMove(GlobalTransform,Vector3.Up*.28f)&&!TestMove(raised,motion))
            {
                var future=raised.Origin+motion;
                var floor=GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(future+Vector3.Up*.02f,future-Vector3.Up*.31f,19));
                if(floor.Count>0)Position+=Vector3.Up*.28f;
            }
        }
        Velocity=new Vector3(direction.X*speed,vertical,direction.Z*speed)+impulse;
        MoveAndSlide();
        Model.Rotation=new(0,yaw+Mathf.Pi,0);
    }
}
