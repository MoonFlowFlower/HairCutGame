using System;
using System.Linq;
using System.Numerics;
namespace Hairball.Core;

// One reusable practice doll, carried as a whole. Hair remains the normal editable volume.
public static class MiniatureModel
{
    public const float Scale=.52f, Foot=.8528f, Radius=.28f;
    public static Vector3 Center(Head h)=>h.Position-Vector3.UnitY*.38f;
    public static bool InReach(PlayerState p,Head h)=>Session.LookingAt(p,Center(h),.5f,2.5f)
        ||h.Raycast(Session.Eye(p),Session.Aim(p),2.5f,out _,out _);
    public static void Stage(WorldState w)
    {
        w.Heads.RemoveAll(h=>h.Miniature);
        foreach(var p in w.Players)p.CarriedModel=-1;
        var h=GoalMaterials.Reference(w.Job.Goal);
        h.Id=w.NextHead++;h.Miniature=true;h.Loose=true;h.GeometryScale=Scale;
        h.Position=new(-3,1.3f+Foot,-3.6f);
        if(w.Job.Goal==4)foreach(var patch in h.Patches)
            if(GoalMaterials.InZone(4,patch.Root+patch.Direction*patch.Length))patch.Char=1;
        w.Heads.Add(h);
    }
}

public sealed partial class Session
{
    // World geometry only: excludes the doll's own player-blocking proxy.
    public Func<Vector3,Vector3,(Vector3 Point,Vector3 Normal)?>? ModelCast;
    public bool HandleModel(PlayerState p)
    {
        if(Sticky(p))return false;
        if(State.Phase is not (Phase.Arrival or Phase.Choice or Phase.Preview or Phase.Build))return false;
        if(p.CarriedModel>=0){ReleaseModel(p);return true;}
        var h=State.Heads.FirstOrDefault(h=>h.Miniature&&h.Holder==0&&!h.Locked&&MiniatureModel.InReach(p,h));
        if(h==null)return false;
        var delta=MiniatureModel.Center(h)-Eye(p);float distance=delta.Length();
        if(distance>.01f&&(ObstructionDistance?.Invoke(Eye(p),delta/distance,distance)??distance)<distance-.3f)return false;
        if(p.CarryLadder>=0&&!DropLadder(p))return false;
        Drop(p);h.Holder=p.Id;h.Velocity=default;p.CarriedModel=h.Id;
        return true;
    }
    public void ReleaseModel(PlayerState p)
    {
        var h=State.Heads.FirstOrDefault(h=>h.Id==p.CarriedModel&&h.Holder==p.Id);
        p.CarriedModel=-1;
        if(h!=null){h.Holder=0;h.Velocity=Aim(p)*.45f;}
    }
    void AffectModels(PlayerState actor,ToolState tool,float range)
    {
        if(tool.Definition is not (2 or 3 or 7 or 9))return;
        var origin=ToolEye(State,actor);var dir=Aim(actor);
        foreach(var h in State.Heads.Where(h=>h.Miniature&&h.Holder==0&&!h.Locked))
        {
            var center=MiniatureModel.Center(h);float along=Vector3.Dot(center-origin,dir);
            if(along<=0||along>range||HairSystem.DistanceToSegment(center,origin,origin+dir*range)>.65f)continue;
            h.Velocity+=dir*(tool.Definition==2?-1.5f:tool.Definition==7?3:2)+Vector3.UnitY*.4f;
        }
    }
    void MoveModel(Head h,Vector3 motion)
    {
        if(motion.LengthSquared()<.0000001f)return;
        float fraction=1;Vector3 normal=default;
        // Swept body corners stop both held and falling dolls at walls, benches and the floor.
        foreach(float y in new[]{-MiniatureModel.Foot+.025f,-.35f,.18f})
        foreach(var xz in new[]{Vector2.Zero,new Vector2(-.27f,-.22f),new Vector2(.27f,-.22f),new Vector2(-.27f,.22f),new Vector2(.27f,.22f)})
        {
            var from=h.Position+new Vector3(xz.X,y,xz.Y);
            var contact=ModelCast?.Invoke(from,from+motion);
            if(contact is {} hit){float f=Math.Clamp(Vector3.Distance(from,hit.Point)/motion.Length()-.01f,0,1);if(f<fraction){fraction=f;normal=hit.Normal;}}
        }
        h.Position+=motion*fraction;
        if(fraction<1){float into=Vector3.Dot(h.Velocity,normal);if(into<0)h.Velocity-=normal*into;h.Velocity*=.65f;}
        if(h.Position.Y<MiniatureModel.Foot){h.Position.Y=MiniatureModel.Foot;h.Velocity.Y=0;h.Velocity*=.85f;}
        h.Position=new(Math.Clamp(h.Position.X,-5.55f,5.55f),h.Position.Y,Math.Clamp(h.Position.Z,-5.55f,5.55f));
    }
    public void TickModels(float dt)
    {
        foreach(var h in State.Heads.Where(h=>h.Miniature))
        {
            if(State.Twist.Kind==TwistKind.MiniDemo){h.Velocity=default;continue;}
            if(h.Holder==0&&FlightContact(h.Position,h.Position+h.Velocity*dt,h.Thrower,h.ThrownAt,false,p=>{h.Holder=p.Id;h.Velocity=default;p.CarriedModel=h.Id;},()=>h.Velocity=default))continue;
            if(h.Holder!=0&&State.Player(h.Holder) is {} carrier)
            {
                var target=Eye(carrier)+Aim(carrier)*1.05f+Vector3.UnitY*.12f;
                MoveModel(h,target-h.Position);h.Velocity=default;h.Rotation=new(0,carrier.Yaw,0);continue;
            }
            h.Holder=0;
            foreach(var p in State.Players.Where(p=>p.Active))
            {
                var delta=MiniatureModel.Center(h)-(p.Position+Vector3.UnitY*.85f);
                var horizontal=new Vector3(delta.X,0,delta.Z);float distance=horizontal.Length();
                if(Math.Abs(delta.Y)<1.1f&&distance<.65f&&distance>.001f)h.Velocity+=horizontal/distance*Math.Min(1,dt*8);
            }
            // Bound collision steps even after a slow frame.
            int steps=Math.Max(1,(int)MathF.Ceiling(Math.Min(dt,.25f)/.02f));float step=Math.Min(dt,.25f)/steps;
            for(int i=0;i<steps;i++){h.Velocity-=Vector3.UnitY*step*5*TwistCards.Gravity(State);MoveModel(h,h.Velocity*step);}
        }
    }
}

