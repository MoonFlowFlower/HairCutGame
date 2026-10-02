using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
namespace Hairball.Core;
public enum MoldKind { None,Pad,Bowl,Ring }
public static class MoldSystem
{
    public const float HoleRadius=.3f,GraceSeconds=1;
    public const int MaxHoles=8;
    public static Vector3 Local(PropState p,Vector3 world)=>Vector3.Transform(world-p.Position,Quaternion.Inverse(Quaternion.CreateFromYawPitchRoll(p.Rotation.Y,p.Rotation.X,p.Rotation.Z)))/p.MaterialScale;
    public static Vector3 World(PropState p,Vector3 local)=>p.Position+Vector3.Transform(local*p.MaterialScale,Quaternion.CreateFromYawPitchRoll(p.Rotation.Y,p.Rotation.X,p.Rotation.Z));
    public static float Interior(MoldKind kind,Vector3 p)=>kind switch{
        MoldKind.Pad=>Math.Min(.33f-Math.Abs(p.X),Math.Min(.12f-Math.Abs(p.Y),.29f-Math.Abs(p.Z))),
        MoldKind.Bowl=>Math.Min(.36f-new Vector2(p.X,p.Z).Length(),Math.Min(.15f+p.Y,.14f-p.Y)),
        MoldKind.Ring=>Math.Min(.39f-new Vector2(p.X,p.Z).Length(),Math.Min(new Vector2(p.X,p.Z).Length()-.15f,.12f-Math.Abs(p.Y))),_=>-1};
    public static Vector3 HoleNormal(Vector3 p)=>Vector3.Normalize(new Vector3(p.X,p.Y*2,p.Z)+new Vector3(0,.0001f,0));
    public static bool Puncture(PropState mold,Vector3 world)
    {
        var p=Local(mold,world);if(p.Length()>.7f||mold.Holes.Count>=MaxHoles||mold.Holes.Any(q=>Vector3.Distance(q,p)<HoleRadius*.65f))return false;
        mold.Holes.Add(p);return true;
    }
    public static bool Full(Head head,PropState mold)
    {
        int count=0,filled=0;
        for(int z=1;z<HairVolume.NZ-1;z++)for(int y=1;y<HairVolume.NY-1;y++)for(int x=1;x<HairVolume.NX-1;x++){
            var local=HairVolume.Position(x,y,z);if(Interior(mold.Mold,Local(mold,head.ToWorld(local)))<.025f)continue;count++;if(HairVolume.Decode(head.Volume.Data[HairVolume.Index(x,y,z)])>=Interior(mold.Mold,Local(mold,head.ToWorld(local)))*.8f)filled++;
        }
        return count>0&&filled>=count*.95f;
    }
    public static float Grow(Head head,PropState mold,float distance)
    {
        if(distance<=0||mold.Mold==MoldKind.None)return 0;float before=head.Mass;bool full=Full(head,mold),changed=false;var baseline=(byte[])head.Volume.Data.Clone();
        if(full)foreach(var hole in mold.Holes){var normal=HoleNormal(hole);var worldNormal=Vector3.Transform(normal,Quaternion.CreateFromYawPitchRoll(mold.Rotation.Y,mold.Rotation.X,mold.Rotation.Z));
            if(head.Raycast(World(mold,hole+normal*1.5f),-worldNormal,1.7f,out var front,out _)){float[]? field=null;head.Volume.GrowAlong(ref field,front,head.LocalDirection(worldNormal),HoleRadius/head.GeometryScale,distance,.65f);}}
        for(int z=1;z<HairVolume.NZ-1;z++)for(int y=1;y<HairVolume.NY-1;y++)for(int x=1;x<HairVolume.NX-1;x++){
            int i=HairVolume.Index(x,y,z);var pos=Local(mold,head.ToWorld(HairVolume.Position(x,y,z)));float interior=Interior(mold.Mold,pos),limit=interior;
            if(full)foreach(var hole in mold.Holes){var normal=HoleNormal(hole);float t=Vector3.Dot(pos-hole,normal);if(t>=0&&t<1.5f)limit=Math.Max(limit,HoleRadius-(pos-hole-normal*t).Length());}
            float old=HairVolume.Decode(baseline[i]);float requested=interior>=-HairVolume.Step*.5f?Math.Max(HairVolume.Decode(head.Volume.Data[i]),Math.Min(interior,old+distance)):HairVolume.Decode(head.Volume.Data[i]);
            float value=Math.Max(old,Math.Min(limit,requested));byte b=HairVolume.Encode(value);if(b!=head.Volume.Data[i]){head.Volume.Data[i]=b;changed=true;}
        }
        if(changed)head.Volume.Revision++;return Math.Max(0,head.Mass-before);
    }
    public static bool Touching(WorldState w,PropState mold)=>w.Heads.Where(h=>h.Id==0||h.AttachedTo==0).Any(h=>h.Volume.Sample(h.ToLocal(mold.Position-Vector3.UnitY*.12f))>-.22f);
}
public sealed partial class Session
{
    public bool MoldsEnabled=true;
    void StageMolds()
    {
        if(!State.Experiment.IsB||!MoldsEnabled||State.Props.Any(p=>p.Mold!=MoldKind.None))return;
        for(int i=0;i<3;i++)State.Props.Add(new(){Id=State.Round*100+90+i,Goal=-2-i,Mold=(MoldKind)(i+1),Position=new(-.6f+i*.65f,.965f,4.1f)});
    }
    PropState? GrowthMold(Head head,Vector3 point)=>State.Props.Where(m=>m.Mold!=MoldKind.None&&(m.Holder!=0||m.Pinned||m.Attached&&State.Time-m.PlacedAt<MoldSystem.GraceSeconds)&&MoldSystem.Touching(State,m)&&Vector3.Distance(m.Position,head.ToWorld(point))<.8f).MinBy(m=>Vector3.Distance(m.Position,head.ToWorld(point)));
    bool AffectMolds(PlayerState p,ToolState tool)
    {
        if(!State.Experiment.IsB||tool.Definition is not (0 or 6 or 8 or 9))return false;
        var origin=ToolEye(State,p);var dir=Aim(p);float range=ObstructionDistance?.Invoke(origin,dir,Tools.Get(tool.Definition).Range)??Tools.Get(tool.Definition).Range;
        var mold=State.Props.Where(m=>m.Mold!=MoldKind.None&&!(p.Customer&&(m.Attached||m.Pinned))&&m.Holder!=p.Id&&HairSystem.DistanceToSegment(m.Position,origin,origin+dir*range)<.48f).OrderBy(m=>Vector3.DistanceSquared(origin,m.Position)).FirstOrDefault();if(mold==null)return false;
        float t=Math.Max(0,Vector3.Dot(mold.Position-origin,dir)-.15f);var point=origin+dir*t;
        if(MoldSystem.Puncture(mold,point))RecordAction(PartyAction.MoldHole,p.Id,mold.Id);
        if(tool.Definition==6&&MoldSystem.Touching(State,mold)){if(State.Player(mold.Holder) is {} carrier)carrier.CarriedProp=-1;mold.Holder=0;mold.Pinned=true;mold.Attached=true;mold.Local=State.SharedHead.ToLocal(mold.Position);mold.AttachedRotation=mold.Rotation-State.SharedHead.Rotation;}
        tool.LastUse=State.Time;tool.Contact=point;tool.HitHair=false;if(tool.Charge>0)tool.Charge--;p.Cooldown=Tools.Get(tool.Definition).Cooldown;
        return true;
    }
}

