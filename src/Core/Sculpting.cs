using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Hairball.Core;

public readonly record struct BrushContext(int HeadId,Vector3 Point,Vector3 Normal,float Radius,bool Seed=false);
public sealed class SculptStroke
{
    public int Tool,Head,Size,Revision;public bool Fine;
    public Vector3 Plane,Normal,Previous;
    public float LastTime;
    public float[]? Field;
}
public static class ToolQuery
{
    public static float EffectRadius(int definition,bool secondary)=>definition==3&&secondary?.32f:definition==7?.16f:Tools.Get(definition).Radius;
    public static List<(Head head,Patch patch,float distance,Vector3 point)> EffectHits(WorldState world,PlayerState p,int definition,bool secondary,Vector3 dir,float range)
    {
        var def=Tools.Get(definition) with{Radius=EffectRadius(definition,secondary)};var origin=Session.ToolEye(world,p);
        var hits=new List<(Head head,Patch patch,float distance,Vector3 point)>();
        foreach(var h in world.Heads)
        {
            if(TwistCards.BlockHair(world,p,h)||HumanCustomer.Self(world,p,h)||h.Locked || (h.Barber && h.Owner==p.Id) || (h.AttachedTo>=0 && world.Heads.First(x=>x.Id==h.AttachedTo).Locked)) continue;
            var localOrigin=h.ToLocal(origin);
            bool hit=h.Raycast(origin,dir,range,out var point,out float distance);
            // Broad sprays may catch the silhouette. Precision tools still hit the actual shell.
            if(!hit && def.Id is "growth" or "blower" or "flame" or "nitrogen" or "vacuum")
            {
                var side=Vector3.Cross(dir,Vector3.UnitY);if(side.LengthSquared()<.01f)side=Vector3.UnitX;else side=Vector3.Normalize(side);
                var up=Vector3.Cross(side,dir);float nearest=float.MaxValue;
                for(int j=0;j<8;j++)
                {
                    var offset=(side*MathF.Cos(j*MathF.PI/4)+up*MathF.Sin(j*MathF.PI/4))*def.Radius*.65f;
                    if(h.Raycast(origin+offset,dir,range,out var q,out var d)&&d<nearest){hit=true;nearest=d;distance=d;point=q;}
                }
            }
            // Bald scalp remains a valid seed for regrowth or transferred material.
            if(!hit && (def.Id=="growth" || def.Id=="vacuum"&&secondary))
            {
                float d=Vector3.Dot(h.Position-origin,dir);var q=origin+dir*d-h.Position;
                if(d>.15f&&d<range&&q.Length()<(h.Facial?.2f:.48f)){hit=true;distance=d-(h.Facial?.05f:.28f);point=h.ToLocal(origin+dir*distance);}
            }
            if(hit)hits.Add((h,HairSystem.MaterialAt(h,point),distance,point));
        }
        if(def.Id is "clipper" or "glue" or "nitrogen" or "nail" or "vacuum")
        {var first=hits.OrderBy(x=>x.distance).FirstOrDefault();if(first.head!=null)hits.RemoveAll(x=>x.head.Id!=first.head.Id);}
        return hits;
    }
    public static float Radius(int definition,int size)=>definition==0?new[]{.14f,.21f,.28f}[Math.Clamp(size,0,2)]:new[]{.21f,.28f,.42f}[Math.Clamp(size,0,2)];
    public static Vector3 Direction(WorldState world,PlayerState player,int definition)
    {
        var dir=Session.Aim(player);
        if(definition==1)foreach(var other in world.Players.Where(q=>q.Active&&q.Id!=player.Id&&q.UsingBlower&&Vector3.Distance(q.Position,player.Position)<5))dir=Vector3.Normalize(dir+Session.Aim(other)*.8f);
        return dir;
    }
    public static List<BrushContext> Find(WorldState world,PlayerState player,int definition,bool fine,Vector3 dir,float range)
    {
        var found=new List<(float Distance,BrushContext Context)>();var origin=Session.ToolEye(world,player);float radius=Radius(definition,player.BrushSize);
        foreach(var h in world.Heads)
        {
            if(TwistCards.BlockHair(world,player,h)||HumanCustomer.Self(world,player,h)||h.Locked||(h.Barber&&h.Owner==player.Id)||(h.AttachedTo>=0&&world.Heads.FirstOrDefault(x=>x.Id==h.AttachedTo)?.Locked==true))continue;
            bool seed=false;bool hit=h.Raycast(origin,dir,range,out var point,out float distance);
            if(!hit&&definition==1&&!fine)
            {
                var side=Vector3.Cross(dir,Vector3.UnitY);side=side.LengthSquared()<.01f?Vector3.UnitX:Vector3.Normalize(side);var up=Vector3.Cross(side,dir);
                for(int i=0;i<8;i++){var offset=(side*MathF.Cos(i*MathF.PI/4)+up*MathF.Sin(i*MathF.PI/4))*radius*h.GeometryScale*.65f;if(h.Raycast(origin+offset,dir,range,out var q,out var d)&&(!hit||d<distance)){point=q;distance=d;hit=true;}}
            }
            if(!hit&&definition==1&&(!h.Loose||h.Miniature))
            {
                // Seed on the actual head-facing surface, even when its hair was shaved bald.
                var o=h.ToLocal(origin);var localDir=h.LocalDirection(dir);var radii=h.Facial?(h.Region==HairRegion.Beard?new Vector3(.63f,.38f,.32f):new Vector3(.44f,.14f,.19f)):new Vector3(.405f,.37f,.36f);
                var a=o/radii;var b=localDir/radii;float aa=Vector3.Dot(b,b),bb=Vector3.Dot(a,b),disc=bb*bb-aa*(Vector3.Dot(a,a)-1);
                if(disc>=0){float t=(-bb-MathF.Sqrt(disc))/aa;if(t>0&&t*h.GeometryScale<range){point=o+localDir*t;distance=t*h.GeometryScale;hit=seed=true;}}
            }
            if(hit)found.Add((distance,new(h.Id,point,seed?Vector3.Normalize(point):h.Volume.Normal(point),radius*h.GeometryScale,seed)));
        }
        var result=found.OrderBy(x=>x.Distance).Select(x=>x.Context).ToList();
        return definition==0||fine?result.Take(1).ToList():result;
    }
}

public sealed partial class Session
{
    readonly Dictionary<int,SculptStroke> strokes=new();
    readonly Dictionary<int,int> topologyRevision=new();
    readonly Dictionary<int,HairVolume> topologyVolumes=new();
    readonly HashSet<int> endedGestures=new();
    public SculptStroke? Stroke(int player)=>strokes.GetValueOrDefault(player);
    public void EndStroke(int player){strokes.Remove(player);endedGestures.Remove(player);Ledger.Close(player);}
    public void SetBrushSize(int id,int size){if(State.Player(id) is {} p)p.BrushSize=Math.Clamp(size,0,2);}

    void UseSculpt(PlayerState p,ToolState tool,bool fine)
    {
        bool growthFeedback=State.Experiment.IsB&&tool.Definition==1;
        if(endedGestures.Contains(p.Id)&&!growthFeedback)return;
        int definition=tool.Definition;var dir=ToolQuery.Direction(State,p,definition);float range=ObstructionDistance?.Invoke(ToolEye(State,p),dir,Tools.Get(definition).Range)??Tools.Get(definition).Range;
        var hits=ToolQuery.Find(State,p,definition,fine,dir,range);
        // Contact is still real when a gesture or its affordable growth has ended.
        // Reporting it never unlocks the gesture or applies material to a fresh target.
        if(growthFeedback&&hits.Count>0)
        {
            var contact=hits[0];var target=State.Heads.First(h=>h.Id==contact.HeadId);
            tool.ContactHead=target.Id;tool.HitHair=true;tool.Contact=target.ToWorld(contact.Point);
            tool.ContactState=HairMaterials.State(HairSystem.MaterialAt(target,contact.Point));
        }
        if(endedGestures.Contains(p.Id))return;
        if(State.Experiment.IsB&&definition==1&&hits.Count>0&&!hits.Any(hit=>CanAffordGrowth(State.Heads.First(h=>h.Id==hit.HeadId),tool)))
        {ResetGrowthHold(p);return;}
        strokes.TryGetValue(p.Id,out var stroke);
        if(stroke!=null&&(stroke.Tool!=tool.Id||stroke.Fine!=fine||stroke.Size!=p.BrushSize)){EndStroke(p.Id);stroke=null;}
        var h=stroke==null?(hits.Count>0?State.Heads.First(x=>x.Id==hits[0].HeadId):null):State.Heads.FirstOrDefault(x=>x.Id==stroke.Head);
        if(h==null||h.Locked||TwistCards.BlockHair(State,p,h)||HumanCustomer.Self(State,p,h)){if(definition==1)ResetGrowthHold(p);if(stroke!=null){EndStroke(p.Id);endedGestures.Add(p.Id);}return;}
        // Losing the original object ends the gesture. Do not acquire a different head until release.
        var context=hits.FirstOrDefault(x=>x.HeadId==h.Id);
        if(stroke==null)
        {
            stroke=new(){Tool=tool.Id,Head=h.Id,Size=p.BrushSize,Fine=fine,Plane=context.Point,Previous=context.Point,Normal=context.Normal,Revision=h.Volume.Revision,LastTime=State.Time-.05f};strokes[p.Id]=stroke;
        }
        float dt=Math.Clamp(State.Time-stroke.LastTime,0,.1f);stroke.LastTime=State.Time;p.Cooldown=.05f;
        float radius=ToolQuery.Radius(definition,p.BrushSize);Vector3 center=context.Point;
        if(definition==0)
        {
            var o=h.ToLocal(ToolEye(State,p));var d=h.LocalDirection(dir);float denominator=Vector3.Dot(d,stroke.Normal);
            if(Math.Abs(denominator)<.12f)return;float t=Vector3.Dot(stroke.Plane-o,stroke.Normal)/denominator;
            if(t<=0||t*h.GeometryScale>range)return;center=o+d*t;
            if(hits.Count>0&&hits[0].HeadId!=h.Id)return;
        }
        else if(!hits.Any(x=>x.HeadId==h.Id)){ResetGrowthHold(p);endedGestures.Add(p.Id);return;}
        tool.ContactState=HairMaterials.State(HairSystem.MaterialAt(h,center));tool.EffectMass=0;
        tool.LastUse=State.Time;tool.ContactHead=h.Id;tool.Contact=h.ToWorld(center);tool.HitHair=true;
        if(stroke.Revision!=h.Volume.Revision)stroke.Field=null;
        float before=h.Mass;var result=new VolumeEditResult();
        var growthBefore=State.Experiment.IsB&&definition==1?h.Volume.Clone():null;
        bool growthClipped=false;
        float growthDistance=definition==1?TakeGrowthDistance(p,fine,dt):0;
        var mold=definition==1&&State.Experiment.IsB?GrowthMold(h,center):null;
        if(mold!=null){MoldSystem.Grow(h,mold,growthDistance);stroke.Field=null;}
        int steps=Math.Clamp((int)MathF.Ceiling(Vector3.Distance(stroke.Previous,center)/(radius*.4f)),1,32);
        for(int i=1;mold==null&&i<=steps;i++)
        {
            var point=Vector3.Lerp(stroke.Previous,center,(float)i/steps);
            if(definition==0)h.Volume.Sculpt(h.Volume,point,stroke.Normal,radius,fine?.015f:.05f,false);
            else
            {
                if(context.Seed&&h.Volume.Sample(point)<0)
                    h.Volume.Brush(new(EffectKind.AddHair,.012f,stroke.Normal),point,.14f);
                // Bound each advection step; a frame spike must not turn high pressure into a jump.
                int growthSteps=State.Experiment.IsB?Math.Max(1,(int)MathF.Ceiling(growthDistance/steps/.04f)):1;
                for(int j=0;j<growthSteps;j++)h.Volume.GrowAlong(ref stroke.Field,point,stroke.Normal,radius,growthDistance/steps/growthSteps,State.Experiment.IsB&&!fine?.65f:0);
            }
        }
        if(growthBefore!=null&&!SpendGrowth(h,growthBefore,tool)){stroke.Field=null;growthClipped=true;}
        if(definition==1&&h.Mass>before)foreach(var patch in h.Patches.Where(q=>HairSystem.DistanceToSegment(center,q.Root,q.Root+q.Direction*q.Length)<radius+.25f))patch.Young=1;
        stroke.Previous=center;stroke.Revision=h.Volume.Revision;
        float delta=h.Mass-before;tool.EffectMass=Math.Abs(delta);result.Added=Math.Max(0,delta);result.Removed=Math.Max(0,-delta);
        if(result.VisibleRemoved>0)DebrisSystem.Emit(State.Debris,h.ToWorld(center),Vector3.Transform(stroke.Normal,h.Orientation)*.35f,result.VisibleRemoved,DebrisMaterial.From(h,center));
        // The broad spray still has collateral uses; each secondary target receives one time-scaled dab.
        if(definition==1&&!fine&&mold==null)foreach(var extra in hits.Where(x=>x.HeadId!=h.Id))
        {
            if(State.Experiment.IsB&&tool.GrowthRemaining<=0)break;
            var other=State.Heads.First(x=>x.Id==extra.HeadId);var saved=State.Experiment.IsB?other.Volume.Clone():null;float otherBefore=other.Mass;float[]? field=null;
            int growthSteps=State.Experiment.IsB?Math.Max(1,(int)MathF.Ceiling(growthDistance/.04f)):1;
            for(int i=0;i<growthSteps;i++)other.Volume.GrowAlong(ref field,extra.Point,extra.Normal,radius,growthDistance/growthSteps,State.Experiment.IsB?.65f:0);
            if(saved!=null&&!SpendGrowth(other,saved,tool))growthClipped=true;
            float added=Math.Max(0,other.Mass-otherBefore);tool.EffectMass+=added;p.Added+=added;
            if(added>0)foreach(var patch in other.Patches.Where(q=>HairSystem.DistanceToSegment(extra.Point,q.Root,q.Root+q.Direction*q.Length)<radius+.25f))patch.Young=1;
        }
        if(growthClipped&&tool.EffectMass<=.000001f){ResetGrowthHold(p);endedGestures.Add(p.Id);}
        if(State.Experiment.IsB&&tool.GrowthRemaining<=.000001f){RecordAction(PartyAction.BottleEmpty,p.Id,tool.Id);ResetGrowthHold(p);}
        if(Math.Abs(delta)>.000001f){h.RestTime=0;Event(p.Id,hits.Select(x=>{var head=State.Heads.First(h=>h.Id==x.HeadId);return head.Facial?head.ParentHead:head.Id;}).Distinct().Count(),h.ToWorld(center),$"{p.Name}: {Tools.Get(definition).Name} → {Math.Max(1,hits.Count)} head(s)",3);}
        p.Added+=Math.Max(0,h.Mass-before);p.Removed+=Math.Max(0,before-h.Mass);
        if(h.Barber&&h.Owner!=p.Id&&before-h.Mass>.000001f&&definition==0)RecordAction(PartyAction.ClipFriend,p.Id,h.Owner);
        FinalizeHair(h,p.Id,result);
    }

    bool HasRoot(Head h,Vector3 point)
    {
        foreach(var p in h.Patches)if(p.Anchored&&Vector3.DistanceSquared(p.AnchorLocal,point)<HairVolume.Step*HairVolume.Step*2.5f)return true;
        if(h.Loose&&!h.Miniature)return false;
        if(h.Facial)return point.Z<=.08f&&point.Z>=-.4f&&Math.Abs(point.X)<.72f&&Math.Abs(point.Y)<.44f;
        return Math.Abs(HairVolume.Ellipsoid(point,Vector3.Zero,new(.405f,.37f,.36f)))<HairVolume.Step;
    }
    void FinalizeHair(Head head,int source,VolumeEditResult? edit=null)
    {
        if(head.Locked||(topologyRevision.GetValueOrDefault(head.Id,-1)==head.Volume.Revision&&topologyVolumes.GetValueOrDefault(head.Id)==head.Volume))return;
        var pieces=head.Volume.DetachUnsupported(p=>HasRoot(head,p),head.Loose&&!head.Miniature);
        edit?.Detached.AddRange(pieces);
        topologyRevision[head.Id]=head.Volume.Revision;
        topologyVolumes[head.Id]=head.Volume;
        foreach(var piece in pieces)
        {
            var bounds=piece.Bounds(0);var low=HairVolume.Position(bounds.x0,bounds.y0,bounds.z0);var high=HairVolume.Position(bounds.x1,bounds.y1,bounds.z1);var center=(low+high)*.5f;
            float mass=piece.Mass*MathF.Pow(head.GeometryScale,3);
            if((high-low).Length()*head.GeometryScale<.12f||!piece.Data.Any(b=>b>=128)||State.Heads.Count(h=>h.Fragment&&h.AttachedTo<0)>=12)
                DebrisSystem.Emit(State.Debris,head.ToWorld(center),Vector3.UnitY*.1f,mass,DebrisMaterial.From(head,center));
            else
            {
                var fragment=head.Clone();fragment.Miniature=false;fragment.Holder=0;fragment.Id=State.NextHead++;fragment.Owner=source;fragment.ParentHead=-1;fragment.Barber=false;fragment.Loose=true;fragment.Fragment=true;fragment.AttachedTo=-1;fragment.Volume=piece;fragment.RestTime=0;fragment.Velocity=Vector3.Zero;
                fragment.FragmentColor=head.Fragment?head.FragmentColor:head.Barber?new(.35f,.24f,.41f):head.Patches.Any(p=>p.Wig)?new(.68f,.44f,.73f):new(.47f,.32f,.23f);
                if(head.Material==HeadMaterialKind.Wool&&!head.Fragment)fragment.FragmentColor=new(.875f,.804f,.694f);
                // Shift by whole lattice cells: preserve the exact shape and material,
                // while placing the object's origin near its actual detached geometry.
                int sx=Math.Clamp((int)MathF.Round(center.X/HairVolume.Step),bounds.x1-(HairVolume.NX-2),bounds.x0-1);
                int sy=Math.Clamp((int)MathF.Round(center.Y/HairVolume.Step),bounds.y1-(HairVolume.NY-2),bounds.y0-1);
                int sz=Math.Clamp((int)MathF.Round(center.Z/HairVolume.Step),bounds.z1-(HairVolume.NZ-2),bounds.z0-1);
                var shift=new Vector3(sx,sy,sz)*HairVolume.Step;var shifted=new byte[HairVolume.Count];
                for(int z=1;z<HairVolume.NZ-1;z++)for(int y=1;y<HairVolume.NY-1;y++)for(int x=1;x<HairVolume.NX-1;x++)
                {int xx=x+sx,yy=y+sy,zz=z+sz;if(xx>=0&&xx<HairVolume.NX&&yy>=0&&yy<HairVolume.NY&&zz>=0&&zz<HairVolume.NZ)shifted[HairVolume.Index(x,y,z)]=piece.Data[HairVolume.Index(xx,yy,zz)];}
                piece.Data=shifted;piece.Revision++;fragment.Position=head.ToWorld(shift);
                foreach(var patch in fragment.Patches){patch.Root-=shift;patch.AnchorLocal-=shift;}
                foreach(var p in fragment.Patches){p.Anchored=false;p.Wig=false;}
                State.Heads.Add(fragment);topologyRevision[fragment.Id]=fragment.Volume.Revision;topologyVolumes[fragment.Id]=fragment.Volume;
            }
        }
    }
    void DepositFragment(Head h)
    {
        var b=h.Volume.Bounds(0);var center=HairVolume.Position((b.x0+b.x1)/2,(b.y0+b.y1)/2,(b.z0+b.z1)/2);var position=h.ToWorld(center);
        var floor=DebrisCast?.Invoke(position,position-Vector3.UnitY*20)??new Vector3(position.X,.015f,position.Z);
        DebrisSystem.Deposit(State.Debris,floor,h.Mass,DebrisMaterial.From(h,center));
    }
}
