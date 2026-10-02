using System;
using System.Linq;
namespace Hairball.Core;
public readonly record struct GoalHealth(float Value,bool Viable);
public sealed class GoalQuality
{
    long stamp=long.MinValue, shapeStamp=long.MinValue;
    float shapeValue;
    GoalHealth cached;
    public GoalHealth Read(WorldState world)
    {
        long next=world.Job.Goal+7;
        foreach(var h in world.Heads.Where(h=>h.Id==0||h.AttachedTo==0))
        {
            next=unchecked(next*31+HashCode.Combine(h.Position,h.Rotation));
            next=unchecked(next*31+h.Volume.GetHashCode());next=unchecked(next*31+h.Volume.Revision);
            foreach(var p in h.Patches)next=unchecked(next*31+HashCode.Combine((int)(p.Glue*32),(int)(p.Stiffness*32),p.Frozen,p.Burning,p.Anchored,(int)(p.Young*32),(int)(p.Wet*32),(int)(p.Char*32)));
        }
        foreach(var prop in world.Props)next=unchecked(next*31+HashCode.Combine(prop.Id,prop.Holder,prop.Attached,(int)(prop.Position.X*20),(int)(prop.Position.Y*20),(int)(prop.Position.Z*20),prop.Failed));
        if(next==stamp)return cached;stamp=next;
        long geometry=world.Job.Goal;foreach(var h in world.Heads.Where(h=>h.Id==0||h.AttachedTo==0))geometry=unchecked(geometry*31+HashCode.Combine(h.Volume.GetHashCode(),h.Volume.Revision));
        if(geometry!=shapeStamp){shapeStamp=geometry;shapeValue=Validation.Shape(world);}
        if(world.Job.Goal==0)
        {
            var head=world.SharedHead;var pad=Helipad.Measure(head);
            float stability=Math.Clamp(head.Patches.Average(p=>p.Resistance)/.4f,0,1);
            float heat=head.Patches.Count(p=>p.Burning)>=4?.35f:1;
            cached=new(Math.Clamp(pad.Quality*(.65f+.35f*stability)*heat,0,100),pad.Stable);
        }
        else {float shape=shapeValue;var weights=GoalMaterials.Weights(world.Job.Goal);float state=GoalMaterials.Evaluate(world.SharedHead,world.Job.Goal);float quality=shape*weights.Shape+state*weights.State+PhysicalProps.Readiness(world)*weights.Function;cached=new(quality,shape>=65&&(weights.State<=0||state>=40));}
        return cached;
    }
}
