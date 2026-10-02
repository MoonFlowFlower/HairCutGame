using System;
using System.Linq;
using System.Numerics;
namespace Hairball.Core;
public static class PhysicalProps
{
    public static float Readiness(WorldState w)
    {
        var head=w.SharedHead;int goal=w.Job.Goal;
        bool Ready(PropState prop){if(prop.Holder!=0||prop.Failed)return false;var v=head.ToLocal(prop.Position);return goal switch{
            0=>Helipad.Measure(head).Stable,
            1 or 7=>new Vector2(v.X,v.Z).Length()<.35f&&v.Y>.1f&&v.Y<.95f&&!(head.Volume.Sample(new(0,.55f,0))>0),
            2=>new Vector2(v.X,v.Z).Length()<.18f&&v.Y>-.1f&&v.Y<1.6f&&Math.Abs(prop.Rotation.X)<.2f&&!(head.Volume.Sample(v)>0),
            3=>Scoring.Support(head,new(0,1.65f,0),.4f)>.3f,
            _=>Scoring.Support(head,v-Vector3.UnitY*.1f,.3f)>.3f};}
        return 100f*w.Props.Count(p=>p.Goal==goal&&Ready(p))/Count(goal);
    }
    // Ideal placements are reference artwork only; gameplay never snaps to these points.
    public static Vector3 ReferencePosition(int goal,int index)
    {float angle=index*2.399f;return goal switch{0=>new(0,1.14f,0),1=>new(MathF.Sin(angle)*.13f,.5f,MathF.Cos(angle)*.13f),2=>new(0,.8f,0),3=>new(0,1.92f,0),4=>new(MathF.Cos(angle)*.17f,1.44f,MathF.Sin(angle)*.17f),5=>new(-.75f,1.32f,0),6=>new((index-1)*.4f,1.42f,.05f),_=>new(MathF.Sin(angle)*.13f,.4f,MathF.Cos(angle)*.13f)};}
    public static float Clearance(int goal)=>goal switch{0=>.26f,1=>.14f,2=>.55f,3=>.12f,4=>.16f,5=>.18f,6=>.45f,_=>.06f};
    public static int Count(int goal)=>goal switch{1 or 6=>3,4=>5,7=>12,_=>1};
    public static void Stage(WorldState w)
    {
        if(w.Twist.Kind!=TwistKind.MiniDemo)MiniatureModel.Stage(w);
        w.Props.Clear();
        for(int i=0;i<Count(w.Job.Goal);i++)w.Props.Add(new(){Id=w.Round*100+w.Job.Goal*12+i,Goal=w.Job.Goal,Index=i,Position=new(-1.2f+(i%5)*.35f,.825f+Clearance(w.Job.Goal),3.8f+(i/5)*.3f)});
        foreach(var player in w.Players)player.CarriedProp=-1;
        Session.StageExperimentProps(w);
    }
    public static void Tick(WorldState w,float dt,Func<Vector3,Vector3,Vector3?>? cast,Func<PropState,Vector3,Vector3,bool>? flight=null)
    {
        var head=w.SharedHead;
        foreach(var prop in w.Props)
        {
            if(prop.Goal==-90)continue;
            float clearance=prop.Mold!=MoldKind.None?.14f:prop.Goal==-1?.47f:Clearance(prop.Goal);
            if(prop.Mold!=MoldKind.None&&prop.Attached&&!prop.Pinned&&w.Time-prop.PlacedAt>=MoldSystem.GraceSeconds){prop.Attached=false;prop.Released=true;prop.Velocity=new(.65f,0,.25f);}
            if(prop.Mold!=MoldKind.None&&prop.Pinned&&!MoldSystem.Touching(w,prop)){prop.Pinned=prop.Attached=false;prop.Released=true;}
            if(prop.Holder!=0&&w.Player(prop.Holder) is {} carrier){if(prop.Coop==CoopKind.LargePad)continue;prop.Position=carrier.Customer&&!carrier.Standing&&prop.Mold!=MoldKind.None?head.Position+Vector3.UnitY*.65f:Session.Eye(carrier)+Session.Aim(carrier)*.9f;prop.Velocity=default;if(prop.Mold!=MoldKind.None)prop.Rotation=new(carrier.Pitch,carrier.Yaw,0);if(prop.Goal==-1)prop.Rotation=new(0,carrier.Yaw,0);continue;}
            if(prop.Attached){
                if(w.Experiment.IsB&&(prop.Goal==0||prop.Mold!=MoldKind.None)){prop.Position=head.ToWorld(prop.Local);prop.Rotation=head.Rotation+prop.AttachedRotation;continue;}
                if(prop.OnScalp||head.Volume.Sample(prop.Local-Vector3.UnitY*clearance)>-HairVolume.Step||head.Volume.Sample(prop.Local)>-HairVolume.Step){prop.Position=head.ToWorld(prop.Local);continue;}
                prop.Attached=false;prop.Released=true;
            }
            if(!prop.Released)continue;
            prop.Velocity-=Vector3.UnitY*dt*5*TwistCards.Gravity(w);
            var end=prop.Position+prop.Velocity*dt;
            if(flight?.Invoke(prop,prop.Position,end)==true)continue;
            if(!(w.Experiment.IsB&&(prop.Goal==0||prop.Mold!=MoldKind.None))&&head.Raycast(prop.Position-Vector3.UnitY*(clearance-.04f),-Vector3.UnitY,Math.Max(.08f,prop.Position.Y-end.Y+.08f),out var hit,out _))
            {prop.Local=hit+Vector3.UnitY*clearance;prop.Position=head.ToWorld(prop.Local);prop.Velocity=default;prop.Attached=true;continue;}
            var local=head.ToLocal(end);float radial=local.X*local.X/(.405f*.405f)+local.Z*local.Z/(.36f*.36f);
            if(!(w.Experiment.IsB&&(prop.Goal==0||prop.Mold!=MoldKind.None))&&radial<1){float skin=.37f*MathF.Sqrt(1-radial);if(local.Y-clearance<skin&&head.ToLocal(prop.Position).Y-clearance>=skin-.1f){prop.Local=new(local.X,skin+clearance,local.Z);prop.Position=head.ToWorld(prop.Local);prop.Attached=prop.OnScalp=true;prop.Velocity=default;continue;}}
            var contact=cast?.Invoke(prop.Position-Vector3.UnitY*(clearance-.03f),end-Vector3.UnitY*clearance);
            if(contact.HasValue){end=contact.Value+Vector3.UnitY*clearance;prop.Velocity=default;prop.Released=false;}
            if(end.Y<clearance){end.Y=clearance;prop.Velocity=default;prop.Released=false;}
            prop.Position=end;
        }
    }
}
public sealed partial class Session
{
    public bool HandleProp(PlayerState p)
    {
        if(Sticky(p))return false;
        if(State.Phase!=Phase.Build)return false;
        if(p.CarriedProp>=0){using var action=new ImpactScope(this,p,new(){Definition=-1});ReleaseProp(p);return true;}
        var prop=State.Props.Where(t=>t.Holder==0&&LookingAt(p,t.Position,t.Goal==-1?.45f:.25f,2.5f)).OrderBy(t=>Vector3.DistanceSquared(t.Position,Eye(p))).FirstOrDefault();
        if(prop==null)return false;
        if(p.CarryLadder>=0&&!DropLadder(p))return false;
        using var pickup=new ImpactScope(this,p,new(){Definition=-1});
        Drop(p);prop.Pinned=false;prop.Holder=p.Id;prop.Attached=false;p.CarriedProp=prop.Id;
        if(State.Experiment.IsB&&prop.Goal==0){State.Experiment.PickupCount++;ExperimentEvent("commission_pickup",p.Id,prop.Position,"picked up commission");}
        return true;
    }
    public void ReleaseProp(PlayerState p)
    {
        var prop=State.Props.FirstOrDefault(t=>t.Id==p.CarriedProp);p.CarriedProp=-1;if(prop==null)return;
        if(prop.Pinned){prop.Holder=0;prop.Attached=true;return;}
        prop.Thrower=p.Id;prop.ThrownAt=State.Time;
        prop.Rotation=new(0,p.Yaw,0);prop.AttachedRotation=prop.Rotation-State.SharedHead.Rotation;
        prop.PlacedAt=State.Time;prop.Pinned=false;prop.Holder=0;prop.OnScalp=false;prop.Released=true;prop.Velocity=Aim(p)*.3f;prop.Position=Eye(p)+Aim(p)*.8f;
        if(p.Customer||prop.Goal==-1||prop.Goal==-90||prop.Gesture>=0){prop.Rotation=new(0,p.Yaw,0);return;} // Gesture props are expressions, not head attachments.
        // Place exactly at the aimed surface; no goal-specific snap or success position.
        var head=State.SharedHead;
        float range=ObstructionDistance?.Invoke(Eye(p),Aim(p),2.5f)??2.5f;Head? surface=null;Vector3 hit=default;float nearest=range;
        foreach(var h in State.Heads.Where(h=>h.Id==0||State.Experiment.IsB&&h.AttachedTo==0))
            if(h.Raycast(Eye(p),Aim(p),nearest,out var point,out var distance)){nearest=distance;surface=h;hit=point;}
        if(surface!=null)
        {var normal=Vector3.Transform(surface.Volume.Normal(hit),surface.Orientation);var world=surface.ToWorld(hit)+(normal.Y>.4f?Vector3.UnitY*(prop.Mold!=MoldKind.None?.12f:PhysicalProps.Clearance(prop.Goal)):normal*.12f);prop.Local=head.ToLocal(world);prop.Position=world;prop.Attached=normal.Y>.4f||prop.Goal==6;
            if(State.Experiment.IsB&&prop.Goal==0&&prop.Attached){State.Experiment.PlacementCount++;State.Experiment.BadSupportSeconds=State.Experiment.StableSeconds=0;CustomerRemarks.Say(State.Experiment,Remark.Object,State.Time);ExperimentEvent("commission_place",p.Id,prop.Position,"placed commission");}}

    }
}
