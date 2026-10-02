using Godot;
using Hairball.Core;
using System;
using System.Linq;
namespace Hairball;
public partial class HeadView
{
    Vector3 visualOffset,visualVelocity,observedPosition,observedRotation,observedVelocity;
    float observedTime=-1,observedContact=-100,wetMass=1,visualStiffness=42,visualDamping=9,compliance=1,worldScale=1;
    public float VisualOffsetMeters=>visualOffset.Length()*worldScale;
    public Vector3 VisualDisplacementMeters=>visualOffset*worldScale;
    public float VisualPeakMeters {get;private set;}
    public void VisualImpulse(Vector3 worldVelocity)
    {
        if(fur!=null){fur.Impulse(worldVelocity*3);return;}
        if(!look||VisualQuality.Phase<6||!VisualQuality.Dynamics)return;
        visualVelocity=(visualVelocity+GlobalBasis.Orthonormalized().Inverse()*worldVelocity*compliance/wetMass).LimitLength(.55f/worldScale);
    }
    void ObserveMotion(Head h,float time)
    {
        if(fur==null&&(!look||VisualQuality.Phase<6)||!VisualQuality.Dynamics){observedTime=-1;return;}
        worldScale=Math.Max(.05f,h.GeometryScale);
        float wet=stateAverage.X,frost=stateAverage.Y,glue=stateAverage.W;
        wetMass=1+wet*1.4f;visualStiffness=42+frost*140+glue*90;visualDamping=9+wet*4+frost*12+glue*10;compliance=1/(1+frost*5+glue*3);
        var position=Art.V(h.Position);var rotation=Art.V(h.Rotation);float dt=time-observedTime;
        if(observedTime>=0&&dt>0&&dt<.25f)
        {
            var velocity=(position-observedPosition)/dt;
            if(position.DistanceTo(observedPosition)<.4f)
            {
                VisualImpulse((observedVelocity-velocity).LimitLength(3)*.07f);
                var turn=new Vector3(Mathf.Wrap(rotation.X-observedRotation.X,-Mathf.Pi,Mathf.Pi),Mathf.Wrap(rotation.Y-observedRotation.Y,-Mathf.Pi,Mathf.Pi),Mathf.Wrap(rotation.Z-observedRotation.Z,-Mathf.Pi,Mathf.Pi));
                VisualImpulse(new Vector3(turn.Z,-Math.Abs(turn.X)*.15f,-turn.X).LimitLength(.3f)*.5f);
            }
            else {visualOffset=visualVelocity=Vector3.Zero;}
            observedVelocity=velocity;
        }
        if(dt>0||observedTime<0){observedPosition=position;observedRotation=rotation;observedTime=time;}
    }
    void ObserveContact(System.Numerics.Vector3 world,float time,float weight)
    {
        if(time<=observedContact+.001f)return;observedContact=time;
        if(weight<.5f)return; // Resting props do not excite the spring every frame.
        var direction=(GlobalPosition-Art.V(world)).Normalized();if(direction.IsZeroApprox())direction=Vector3.Up;
        VisualImpulse((direction+Vector3.Up*.25f)*(.20f*weight));
    }
    public override void _Process(double delta)
    {
        if(HeadlessSimulationOnly)return;
        if(fur!=null){fur.Step((float)delta);return;}
        if(!look||VisualQuality.Phase<6||!VisualQuality.Dynamics){visualOffset=visualVelocity=Vector3.Zero;return;}
        if(visualOffset==Vector3.Zero&&visualVelocity==Vector3.Zero)return;
        float remaining=Math.Min((float)delta,.1f);
        while(remaining>0){float dt=Math.Min(remaining,1f/120);visualVelocity+=(-visualOffset*visualStiffness-visualVelocity*visualDamping)/wetMass*dt;visualOffset+=visualVelocity*dt;remaining-=dt;}
        float limit=.025f/worldScale;if(visualOffset.Length()>limit){visualOffset=visualOffset.LimitLength(limit);if(visualVelocity.Dot(visualOffset)>0)visualVelocity-=visualOffset.Normalized()*visualVelocity.Dot(visualOffset.Normalized());}
        if(visualOffset.LengthSquared()<1e-10f&&visualVelocity.LengthSquared()<1e-9f){visualOffset=visualVelocity=Vector3.Zero;}
        VisualPeakMeters=Math.Max(VisualPeakMeters,VisualOffsetMeters);shell.SetInstanceShaderParameter("visual_offset",visualOffset);
    }
}
