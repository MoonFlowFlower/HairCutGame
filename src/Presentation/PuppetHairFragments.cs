using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Hairball;

// Cosmetic consumers of already-detached volumes; never reinsert into the head.
public sealed class PuppetHairFragments
{
    sealed class Piece { public PuppetLabHair Hair=null!;public Transform3D Initial;public float FloorDrop,Age;public double Born; }
    readonly List<Piece> pieces=new();readonly Node3D parent;readonly PuppetHairSettings settings;
    readonly System.Diagnostics.Stopwatch clock=System.Diagnostics.Stopwatch.StartNew();
    public const int Maximum=12;public float BeatSeconds=>settings.Prefall?.32f:0;
    public int Count=>pieces.Count;public float? FreezeAge;public bool Paused;
    public IEnumerable<PuppetLabHair> Hairs=>pieces.Select(p=>p.Hair);
    public int Triangles=>pieces.Sum(p=>p.Hair.Core.Visible?p.Hair.Triangles:p.Hair.ShellTriangles+(p.Hair.VolumeFur?.FinTriangles??0));
    public float OldestAge=>pieces.Count==0?0:pieces.Max(p=>p.Age);
    public float GreatestDrop=>pieces.Count==0?0:pieces.Max(p=>p.Initial.Origin.Y-p.Hair.Root.GlobalPosition.Y);
    public void ResumeAges(){foreach(var p in pieces)p.Born=clock.Elapsed.TotalSeconds-(FreezeAge??p.Age);FreezeAge=null;}
    public PuppetHairFragments(Node3D parent,PuppetHairSettings settings){this.parent=parent;this.settings=settings;}
    public void Clear(){foreach(var p in pieces)if(GodotObject.IsInstanceValid(p.Hair.Root))p.Hair.Root.Free();pieces.Clear();FreezeAge=null;}
    public void Emit(IReadOnlyList<HairVolume> detached,Transform3D initial)
    {
        foreach(var volume in detached)
        {
            if(volume.Mass<.015f)continue;
            if(pieces.Count==Maximum){pieces[0].Hair.Root.Free();pieces.RemoveAt(0);}
            // A detached volume keeps the original head-local coordinates.
            // Reuse its fixed tuft field, clipped by this piece's occupancy.
            // The bounded debris renderer samples that field without copying
            // the full-head fin mesh or creating roots on the new cut face.
            var h=new PuppetLabHair(parent,0,volume,settings with {Lod=3,Shells=settings.SurfaceSamples?Math.Min(8,settings.Shells):0,DebrisVolume=settings.SurfaceSamples,Warning=false});h.SetLook(6);h.Root.GlobalTransform=initial;
            var b=volume.Bounds(0);float bottom=initial.Origin.Y+HairVolume.Position(0,b.y0,0).Y/PuppetLabHair.Scale*initial.Basis.Scale.Y;
            pieces.Add(new Piece{Hair=h,Initial=initial,FloorDrop=Math.Max(0,bottom-.04f),Born=clock.Elapsed.TotalSeconds});
        }
    }
    public void Tick(float dt)
    {
        if(Paused)return;
        foreach(var p in pieces)
        {
            p.Age=FreezeAge??(float)(clock.Elapsed.TotalSeconds-p.Born);float t=Math.Max(0,p.Age-BeatSeconds);
            float sag=.009f*Math.Min(1,p.Age/.20f),drop=Math.Min(p.FloorDrop,sag+4.9f*t*t);
            var pose=p.Initial;pose.Origin+=new Vector3(.005f*MathF.Sin(p.Age*22)*MathF.Exp(-p.Age*5),-drop,Math.Min(.24f,t*.20f));p.Hair.Root.GlobalTransform=pose;
        }
    }
}
