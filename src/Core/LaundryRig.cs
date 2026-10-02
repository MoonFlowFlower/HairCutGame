using System;
using System.Linq;
using System.Numerics;
namespace Hairball.Core;

// Read-only load presentation: never grants support or changes scoring.
public readonly record struct LaundryContact(Vector3 Point,float Strength,bool OnHair);
public static class LaundryRig
{
    static readonly Vector3[] loadSamples={new(-.46f,.65f,0),new(.46f,.65f,0),new(0,1.3f,0)};
    public static LaundryContact Contact(Head head,int index)
    {
        float x=(index-1)*.43f;
        if(head.Volume.Raycast(new(x,2.8f,.08f),-Vector3.UnitY,3.2f,out var point,out _))
            return new(point,Scoring.Support(head,point-Vector3.UnitY*.07f,.18f),true);
        return new(new((index==0?-1:1)*.38f,-.08f,.12f),0,false);
    }
    public static float Wind(WorldState world,Head head)
    {
        float wind=.12f;
        foreach(var p in world.Players.Where(p=>p.Active&&p.UsingBlower))
        {
            var origin=Session.Eye(p);var target=head.Position+Vector3.UnitY*.8f;
            if(Vector3.Dot(target-origin,Session.Aim(p))>0&&HairSystem.DistanceToSegment(target,origin,origin+Session.Aim(p)*6)<1.5f)wind=1;
        }
        return wind;
    }
    public static float Tension(Head head,float wind)
    {
        // Cheap local support samples drive acting, never the validation result.
        float missing=0;
        foreach(var point in loadSamples)
            if(head.Volume.Sample(point)<0)missing+=.12f;
        return Math.Clamp(.12f+(1-head.Patches.Average(p=>p.Resistance))*.35f+missing+wind*.25f,0,1);
    }
    public static Vector3 LoadRotation(Head head,float time,float wind)
    {float t=Tension(head,wind);return new(MathF.Sin(time*1.7f)*.006f*t,0,MathF.Sin(time*2.1f)*(.009f+.013f*wind)*t);}
}

public sealed partial class Session
{
    // Explicit playable preset; ordinary commissions still start with ordinary hair.
    public void StageLaundry(int slot=0)
    {
        if(!Lab)return;
        var p=State.Players.First(p=>p.Slot==slot);State.Job.Goal=6;PhysicalProps.Stage(State);var h=State.SharedHead;
        float Box(Vector3 v,Vector3 center,Vector3 half){var q=Vector3.Abs(v-center)-half;return -Vector3.Max(q,Vector3.Zero).Length()-Math.Min(Math.Max(q.X,Math.Max(q.Y,q.Z)),0);}
        h.Volume.Fill(v=>{
            float cap=HairVolume.Ellipsoid(v,new(0,.02f,-.07f),new(.43f,.37f,.36f));
            if(v.Z>.13f&&v.Y<.24f)cap=Math.Min(cap,.13f-v.Z);
            float left=Box(v,new(-.46f,.65f,0),new(.19f,.7f,.22f));
            float neck=1-Math.Clamp(Math.Abs(v.Y-.88f)/.15f,0,1);
            float right=Box(v,new(.46f,.70f,0),new(.17f-neck*.075f,.75f,.2f));
            float beamHeight=1.34f+.09f*v.X-.11f*Math.Max(0,1-v.X*v.X/.4f);
            float beam=Box(v,new(0,beamHeight,0),new(.65f,.12f,.22f));
            return Math.Max(cap,Math.Max(left,Math.Max(right,beam)));
        });
        foreach(var patch in h.Patches){patch.Glue=patch.Root.X>.1f?.55f:.08f;patch.Stiffness=.15f;}
        p.Position=WorkCenter+new Vector3(.52f,0,2.9f);
        var delta=h.Position+new Vector3(.46f,.65f,.18f)-Eye(p);p.Yaw=MathF.Atan2(-delta.X,-delta.Z);p.Pitch=MathF.Asin(delta.Y/delta.Length());
        Drop(p);p.Held=4;State.Tools.First(t=>t.Id==4).Holder=p.Id;
        foreach(int id in new[]{1,3,9})State.Tools.First(t=>t.Id==id).Position=WorkCenter+new Vector3(1.25f,.25f,.5f+(id==1?0:id==3?.4f:.8f));
        var wig=SpawnWig(WorkCenter+new Vector3(-1.25f,.45f,-.35f),1);wig.GeometryScale=.65f;
        DebrisSystem.Deposit(State.Debris,WorkCenter+new Vector3(.85f,.015f,-.25f),1.1f,new(){Color=new(.47f,.32f,.23f)});
        State.Notice="";
    }
}

