using Godot;
using Hairball.Core;
namespace Hairball;

public partial class PracticeModelView : Node3D
{
    readonly AnimatableBody3D body=new(){CollisionLayer=2,CollisionMask=0,SyncToPhysics=false};
    public PracticeModelView()
    {
        Name="PracticeModelBody";
        var person=Art.Person(this,new("577c8d"));
        person.Scale=Vector3.One*MiniatureModel.Scale;
        person.Position=Vector3.Down*MiniatureModel.Foot;
        AddChild(body);
        body.AddChild(new CollisionShape3D{Position=new(0,-.33f,0),Shape=new BoxShape3D{Size=new(.56f,1.04f,.46f)}});
    }
    public void Sync(Head h,bool live)
    {
        Position=Art.V(h.Position);Rotation=Art.V(h.Rotation);
        body.CollisionLayer=live&&h.Holder==0?2u:0u;
    }
}
