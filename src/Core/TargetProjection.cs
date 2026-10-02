using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Hairball.Core;

// The guide queries the exact scoring primitives, not a separately authored picture.
public static class TargetProjection
{
    public static (Vector3 Min,Vector3 Max) Bounds(ITargetVolume target)
    {
        if(target is BoxVolume box)return(box.Center-box.Half,box.Center+box.Half);
        if(target is CylinderVolume cylinder){var half=new Vector3(cylinder.Radius,cylinder.HalfHeight,cylinder.Radius);return(cylinder.Center-half,cylinder.Center+half);}
        if(target is UnionVolume union)
        {
            var min=new Vector3(float.PositiveInfinity);var max=new Vector3(float.NegativeInfinity);
            foreach(var part in union.Parts){var b=Bounds(part);min=Vector3.Min(min,b.Min);max=Vector3.Max(max,b.Max);}return(min,max);
        }
        throw new NotSupportedException($"Target guide has no bounds for {target.GetType().Name}");
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static bool Hit(ITargetVolume target,Vector3 origin,Vector3 direction,float near=0)
    {
        if(target is UnionVolume union){foreach(var part in union.Parts)if(Hit(part,origin,direction,near))return true;return false;}
        float lo=near,hi=float.PositiveInfinity;
        if(target is BoxVolume box)
        {
            var p=origin-box.Center;
            return Slab(p.X,direction.X,box.Half.X,ref lo,ref hi)&&Slab(p.Y,direction.Y,box.Half.Y,ref lo,ref hi)&&Slab(p.Z,direction.Z,box.Half.Z,ref lo,ref hi);
        }
        if(target is CylinderVolume cylinder)
        {
            var p=origin-cylinder.Center;
            if(!Slab(p.Y,direction.Y,cylinder.HalfHeight,ref lo,ref hi))return false;
            float a=direction.X*direction.X+direction.Z*direction.Z;
            float radial=p.X*p.X+p.Z*p.Z;
            if(a<1e-12f)return radial<=cylinder.Radius*cylinder.Radius&&radial>=cylinder.Hole*cylinder.Hole;
            float b=p.X*direction.X+p.Z*direction.Z;
            float discriminant=b*b-a*(radial-cylinder.Radius*cylinder.Radius);
            if(discriminant<0)return false;
            float root=MathF.Sqrt(discriminant);lo=Math.Max(lo,(-b-root)/a);hi=Math.Min(hi,(-b+root)/a);
            if(lo>hi)return false;
            // The inner bore is convex: only a segment wholly inside it is empty.
            var start=p+direction*lo;var end=p+direction*hi;
            return Math.Max(start.X*start.X+start.Z*start.Z,end.X*end.X+end.Z*end.Z)>=cylinder.Hole*cylinder.Hole;
        }
        throw new NotSupportedException($"Target guide has no ray query for {target.GetType().Name}");
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static bool Slab(float p,float d,float half,ref float lo,ref float hi)
    {
        if(Math.Abs(d)<1e-8f)return Math.Abs(p)<=half;
        float a=(-half-p)/d,b=(half-p)/d;
        lo=Math.Max(lo,Math.Min(a,b));hi=Math.Min(hi,Math.Max(a,b));return lo<=hi;
    }
}
