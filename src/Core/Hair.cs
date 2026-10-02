using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Hairball.Core;

public enum EffectKind { AddHair, RemoveHair, ApplyForce, ChangeTemperature, ChangeStiffness, Glue, Anchor, Detach, Transfer, CutPlane, Puncture, Ignite, Wet }
public readonly record struct HairEffect(EffectKind Kind, float Amount, Vector3 Direction = default, int Source = 0, Vector3 Point = default);

public sealed class Patch
{
    public Vector3 Root;
    public Vector3 Direction = Vector3.UnitY;
    public float Length = .55f, Temperature = 20, Glue, Char, Stiffness, Young, Wet;
    public bool Burning, Anchored, Wig;
    public Vector3 AnchorPoint;
    public Vector3 AnchorLocal;
    public int Source, BurnSource;
    public bool Frozen => Temperature < -10;
    public float Resistance => Anchored ? 1 : Math.Clamp(Math.Max(Stiffness, Math.Max(Glue, Frozen ? .94f : Math.Max(.025f,.18f-Young*.15f-Wet*.08f))), 0, 1);
    public Patch Clone() => (Patch)MemberwiseClone();
}

public sealed class Head
{
    public int Id, Owner;
    public HeadMaterialKind Material;
    public bool Barber, Locked, Loose;
    public bool Fragment, Miniature;
    public int Holder;
    public int Thrower;
    public float ThrownAt;
    public float RestTime;
    public Vector3 FragmentColor;
    public Vector3 Position, Velocity;
    public int AttachedTo = -1;
    public Vector3 AttachedOffset=new(0,.4f,0), AttachedRotation;
    public bool Bonded;
    public int ParentHead=-1;
    public HairRegion Region;
    public float GeometryScale=1;
    HairVolume? supportVolume;
    int supportRevision=-1;
    Quaternion supportRotation;
    float supportScale;
    Vector3 supportOffset;
    public Vector3 LowestSurfaceOffset()
    {
        if(supportVolume!=Volume||supportRevision!=Volume.Revision||supportRotation!=Orientation||supportScale!=GeometryScale)
        {
            supportVolume=Volume;supportRevision=Volume.Revision;supportRotation=Orientation;supportScale=GeometryScale;
            var shell=HairShell.Build(Volume);supportOffset=Vector3.Zero;float lowest=float.PositiveInfinity;
            foreach(var point in shell.Vertices){var world=Vector3.Transform(point*GeometryScale,Orientation);if(world.Y<lowest){lowest=world.Y;supportOffset=world;}}
        }
        return supportOffset;
    }
    public Vector3 Rotation;
    public bool Facial=>ParentHead>=0;
    public Quaternion Orientation=>Quaternion.CreateFromYawPitchRoll(Rotation.Y,Rotation.X,Rotation.Z);
    public Vector3 ToLocal(Vector3 world)=>Vector3.Transform(world-Position,Quaternion.Inverse(Orientation))/GeometryScale;
    public Vector3 ToWorld(Vector3 local)=>Position+Vector3.Transform(local*GeometryScale,Orientation);
    public Vector3 LocalDirection(Vector3 direction)=>Vector3.Transform(direction,Quaternion.Inverse(Orientation));
    public bool Raycast(Vector3 origin,Vector3 direction,float range,out Vector3 point,out float distance)
    {bool hit=Volume.Raycast(ToLocal(origin),LocalDirection(direction),range/GeometryScale,out point,out distance);distance*=GeometryScale;return hit;}
    public List<Patch> Patches = new();
    public HairVolume Volume = new();
    public float BurnClock;
    public float Mass => Volume.Mass*GeometryScale*GeometryScale*GeometryScale;
    public static Vector3 FaceOffset(HairRegion region)=>region switch{HairRegion.LeftBrow=>new(-.18f,.25f,.35f),HairRegion.RightBrow=>new(.18f,.25f,.35f),_=>new(0,-.25f,.31f)};
    public static Head CreateFace(Head parent,HairRegion region)
    {
        var h=Create(10000+parent.Id*3+(int)region,parent.Owner,parent.Barber);h.ParentHead=parent.Id;h.Region=region;h.GeometryScale=region==HairRegion.Beard?.5f:.35f;
        h.Material=parent.Material;
        h.Volume.Fill(v=>HairVolume.Ellipsoid(v,Vector3.Zero,region==HairRegion.Beard?new(.63f,.38f,.32f):new(.44f,.14f,.19f)));
        foreach(var p in h.Patches){p.Root*=.35f;p.Direction=Vector3.UnitZ;p.Length=.25f;}
        h.Position=parent.ToWorld(FaceOffset(region));h.Rotation=parent.Rotation;return h;
    }
    public static Head Create(int id, int owner, bool barber = false)
    {
        var h = new Head { Id = id, Owner = owner, Barber = barber, Volume=HairVolume.Create(barber) };
        for (int z = 0; z < 4; z++) for (int x = 0; x < 8; x++)
        {
            float px = (x - 3.5f) * .145f, pz = (z - 1.5f) * .235f;
            float y = .27f * MathF.Sqrt(Math.Max(0, 1 - px * px / .4f - pz * pz / .3f));
            h.Patches.Add(new Patch { Root = new(px, y, pz), Direction = Vector3.Normalize(new(px * .45f, 1, pz * .5f)), Length = barber ? .28f : .52f + .1f * MathF.Sin(x * 3 + z) });
        }
        return h;
    }
    public Head Clone() => new() { Id = Id, Owner = Owner, Material=Material, Barber = Barber, Locked = Locked, Loose = Loose, Miniature=Miniature,Holder=Holder,Thrower=Thrower,ThrownAt=ThrownAt,Fragment=Fragment,RestTime=RestTime,FragmentColor=FragmentColor, Position = Position, Velocity = Velocity, AttachedTo = AttachedTo, AttachedOffset=AttachedOffset,AttachedRotation=AttachedRotation,Bonded=Bonded, Patches = Patches.Select(p => p.Clone()).ToList(),Volume=Volume.Clone(),BurnClock=BurnClock,ParentHead=ParentHead,Region=Region,GeometryScale=GeometryScale,Rotation=Rotation };
}
public enum HairRegion { Scalp, LeftBrow, RightBrow, Beard }

public static class HairSystem
{
    public const float MaxLength = 3;
    public static float Apply(Head head, Patch p, HairEffect e, bool updateVolume=true)
    {
        if (head.Locked || !float.IsFinite(e.Amount) || !Finite(e.Direction) || !Finite(e.Point)) return 0;
        float before = p.Length;
        if(updateVolume && e.Kind is EffectKind.AddHair or EffectKind.Transfer or EffectKind.RemoveHair or EffectKind.Detach or EffectKind.CutPlane or EffectKind.Puncture or EffectKind.ApplyForce)
        {
            var center=e.Point==default?p.Root+p.Direction*p.Length:head.ToLocal(e.Point);
            var adjusted=e;
            if(e.Kind==EffectKind.ApplyForce)adjusted=e with{Amount=e.Amount*(1-p.Resistance)};
            head.Volume.Brush(adjusted,center,.32f,center-e.Direction*4,8);
        }
        switch (e.Kind)
        {
            case EffectKind.AddHair: case EffectKind.Transfer: p.Length += Math.Max(0, e.Amount); if(e.Kind==EffectKind.AddHair)p.Young=1; break;
            case EffectKind.RemoveHair: case EffectKind.Puncture: p.Length -= Math.Max(0, e.Amount); break;
            case EffectKind.CutPlane:
                float denominator = Vector3.Dot(p.Direction, e.Direction);
                if (Math.Abs(denominator) > .01f) p.Length = Math.Min(p.Length, Math.Max(0, Vector3.Dot(e.Point - head.Position - p.Root, e.Direction) / denominator));
                break;
            case EffectKind.ApplyForce:
                if (p.Frozen && e.Amount > 1.5f && !p.Anchored) p.Length *= .3f;
                else if (e.Direction.LengthSquared() > .001f)
                    p.Direction = Vector3.Normalize(p.Direction + Vector3.Normalize(e.Direction) * e.Amount * (1 - p.Resistance));
                break;
            case EffectKind.ChangeTemperature:
                if(e.Amount>0&&p.Wet>0){float dry=Math.Min(p.Wet,e.Amount/130);p.Wet-=dry;p.Temperature+=Math.Max(0,e.Amount-dry*130);}else p.Temperature += e.Amount;
                if (p.Temperature > 90 && p.Length > .03f && !p.Burning) {p.Burning = true;p.BurnSource=e.Source;}
                if (p.Temperature < 0) p.Burning = false;
                break;
            case EffectKind.ChangeStiffness: p.Stiffness = Math.Clamp(p.Stiffness + e.Amount, 0, 1); break;
            case EffectKind.Glue: p.Glue = Math.Clamp(p.Glue + e.Amount*(1-p.Wet*.8f), 0, 1); break;
            case EffectKind.Anchor: p.Anchored = true; p.AnchorPoint = e.Point; p.AnchorLocal=head.ToLocal(e.Point); break;
            case EffectKind.Detach: p.Anchored = false; p.Length = 0; break;
            case EffectKind.Wet: p.Wet=Math.Clamp(p.Wet+e.Amount,0,1);p.Burning=false;p.Temperature=Math.Min(35,p.Temperature);break;
            case EffectKind.Ignite: if(p.Wet>.15f)break;if(!p.Burning)p.BurnSource=e.Source; p.Burning = p.Length > .02f; p.Temperature = Math.Max(p.Temperature, 110); break;
        }
        p.Length = Math.Clamp(p.Length, 0, MaxLength);
        p.Temperature = Math.Clamp(p.Temperature, -100, 250);
        p.Source = e.Source;
        return p.Length - before;
    }
    public static bool Finite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
    public static IEnumerable<Vector3> Samples(Head h)=>h.Volume.Samples();
    public static Patch MaterialAt(Head h,Vector3 local)
    {
        var nearest=h.Patches[0];float best=float.MaxValue;
        foreach(var p in h.Patches){float d=DistanceToSegment(local,p.Root,p.Root+p.Direction*p.Length);if(d<best){best=d;nearest=p;}}
        return nearest;
    }
    public static void Tick(Head h, float dt)
    {
        if (h.Locked) return;
        h.BurnClock+=dt;
        if(h.BurnClock>=.12f)
        {
            if(h.Patches.Any(p=>p.Burning))h.Volume.Erode(Math.Min(h.BurnClock,.25f)*.24f,pos=>MaterialAt(h,pos).Burning?.7f+.3f*MathF.Sin(pos.X*23+pos.Y*17+pos.Z*31):0);
            h.BurnClock=0;
        }
        foreach (var p in h.Patches)
        {
            p.Young=Math.Max(0,p.Young-dt/35);p.Wet=Math.Max(0,p.Wet-dt/50);
            if(p.Temperature>65)p.Glue=Math.Max(0,p.Glue-dt*.2f);
            if (p.Anchored && !h.Loose)
            {
                var d = p.AnchorPoint - h.ToWorld(p.Root);
                if (d.Length() > 3.5f) p.Anchored = false;
                else if (d.LengthSquared() > .01f)
                {
                    var target=h.ToLocal(p.AnchorPoint);var shift=target-p.AnchorLocal;
                    if(shift.LengthSquared()>.0001f)h.Volume.Brush(new(EffectKind.ApplyForce,Math.Min(.25f,shift.Length()),Vector3.Normalize(shift)),p.AnchorLocal,.5f);
                    p.AnchorLocal=target;p.Direction = Vector3.Normalize(d); p.Length = Math.Clamp(d.Length(), 0, 3);
                }
            }
            if (p.Burning)
            {
                p.Length = Math.Max(0, p.Length - dt * .24f);
                p.Char = Math.Min(1, p.Char + dt * .45f);
                p.Glue = Math.Max(0, p.Glue - dt * .15f);
                if (p.Length < .02f) p.Burning = false;
                foreach (var q in h.Patches)
                    if (!q.Burning && q.Length > .03f && Vector3.Distance(p.Root, q.Root) < .24f)
                    { q.Temperature += dt * 40; if (q.Temperature > 90) { q.Burning = true; q.Source = p.Source; q.BurnSource=p.BurnSource; } }
            }
            else p.Temperature += (20 - p.Temperature) * dt * .012f;
        }
    }
    public static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        var d = b - a;
        float t = d.LengthSquared() < .00001f ? 0 : Math.Clamp(Vector3.Dot(point - a, d) / d.LengthSquared(), 0, 1);
        return Vector3.Distance(point, a + d * t);
    }
}

