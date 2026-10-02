using System;
using System.Linq;
using System.Numerics;

namespace Hairball.Core;

public static class Validation
{
    public static float Shape(WorldState w)=>Scoring.Shape(HairSystem.Samples(w.SharedHead).Concat(w.Heads.Where(h=>h.AttachedTo==0).SelectMany(h=>HairSystem.Samples(h).Select(v=>w.SharedHead.ToLocal(h.ToWorld(v))))),Goals.All[w.Job.Goal]);
    public static void Begin(WorldState w)
    {
        foreach(var prop in w.Props.Where(p=>p.Goal==w.Job.Goal)){prop.StartPosition=prop.Position;prop.StartLocal=w.SharedHead.ToLocal(prop.Position);prop.Holder=0;
            if(prop.Goal==2&&(new Vector2(prop.StartLocal.X,prop.StartLocal.Z).Length()>.2f||prop.StartLocal.Y<0||prop.StartLocal.Y>1.3f))prop.Failed=true;
            if(prop.Goal==5&&(prop.StartLocal.X>-.45f||prop.StartLocal.X< -1.2f))prop.Failed=true;
        }
        // Seat recovered customers for the ceremony; hairstyle facts stay locked and unchanged.
        foreach(var c in w.Customers){c.Recovery=0;c.Reaction=0;c.Action=CustomerReaction.None;w.SharedHead.Position=Session.WorkCenter+new Vector3(0,1.5f+c.ChairHeight,0);}
        foreach(var head in w.Heads.Where(h=>!h.Barber&&!h.Loose&&!h.Facial))head.Rotation=default;
        foreach(var face in w.Heads.Where(h=>h.Facial)){var parent=w.Heads.First(h=>h.Id==face.ParentHead);face.Position=parent.ToWorld(Head.FaceOffset(face.Region));face.Rotation=parent.Rotation;}
        foreach(var wig in w.Heads.Where(h=>h.AttachedTo>=0))wig.Position=w.Heads.First(h=>h.Id==wig.AttachedTo).ToWorld(wig.AttachedOffset);
        w.ValidationHeads=w.Heads.Where(h=>!h.Barber).Select(h=>h.Clone()).ToList();
        foreach(var h in w.ValidationHeads)h.Locked=false;
        int goal=w.Job.Goal;
        w.Job.Result=new(){Goal=goal,State=GoalMaterials.Evaluate(w.SharedHead,goal),Shape=Shape(w),Penalty=w.Job.Penalty};
        if(goal==0)w.Job.Result.Shape=w.Job.Result.Shape*.35f+Helipad.Measure(w.SharedHead).Quality*.65f;
    }

    public static void Tick(WorldState w,float dt,float elapsed)
    {
        foreach(var prop in w.Props.Where(p=>p.Goal==w.Job.Goal))
        {
            var head=w.ValidationHeads.FirstOrDefault(h=>h.Id==prop.Slot*2)??w.SharedHead;
            if(prop.Goal==0){Helipad.Tick(prop,head,dt,elapsed);continue;}
            // The validation may deform its own working copy, never submitted gameplay facts.
            var attached=w.ValidationHeads.Where(h=>h.AttachedTo==head.Id).ToArray();
            var material=head;
            float Support(Vector3 local,float radius)=>Math.Max(Scoring.Support(material,local,radius),attached.Select(h=>Scoring.Support(h,h.ToLocal(head.ToWorld(local)),radius)).DefaultIfEmpty(0).Max());
            bool Occupied(Vector3 local)=>material.Volume.Sample(local)>0||attached.Any(h=>h.Volume.Sample(h.ToLocal(head.ToWorld(local)))>0);
            float t=Math.Max(0,elapsed-1); float angle=prop.Index*2.399f;
            if(prop.Index==0 && elapsed>2)
            {
                foreach(var patch in head.Patches)
                {
                    if(prop.Goal is 0 or 6)HairSystem.Apply(head,patch,new(EffectKind.ApplyForce,dt*.15f,new Vector3(.8f,-.1f,.25f)),false);
                    if(prop.Goal is 3 or 5)HairSystem.Apply(head,patch,new(EffectKind.ApplyForce,dt*.08f,new Vector3(.1f,-1,0)),false);
                }
                if(prop.Goal is 0 or 6 or 3 or 5)
                {
                    float resistance=head.Patches.Average(p=>p.Resistance);
                    var force=prop.Goal is 0 or 6?new Vector3(.8f,-.1f,.25f):new Vector3(.1f,-1,0);
                    head.Volume.Brush(new(EffectKind.ApplyForce,dt*.12f*(1-resistance),force),new(0,.8f,0),3);
                }
                HairSystem.Tick(head,dt);
            }
            Vector3 local=prop.StartLocal;
            if(prop.Goal==2)local+=Vector3.Transform(Vector3.UnitY,Quaternion.CreateFromYawPitchRoll(prop.Rotation.Y,prop.Rotation.X,prop.Rotation.Z))*(t*.43f);
            if(prop.Goal==5)local+=Vector3.UnitX*Math.Min(t*.28f,1.7f);
            if(prop.Goal==3)local=Vector3.Lerp(prop.StartLocal,new(0,1.8f,0),Math.Clamp(t/3,0,1));
            if(prop.Goal==1)local=Session.Rotate(prop.StartLocal,t*.3f);
            if(prop.Goal==7&&t>4)local-=Vector3.UnitY*((t-4)*.6f);
            if(elapsed>1.5f && !prop.Failed)
            {
                bool okay;
                if(prop.Goal==2)
                {
                    // Clearance of actual material along the moving rocket's path, plus a nonempty silo.
                    okay=material.Mass>5 && !Occupied(local) && !Occupied(local+new Vector3(.1f,0,0)) && !Occupied(local-new Vector3(.1f,0,0));
                }
                else if(prop.Goal is 1 or 7)
                {
                    int rim=0;
                    for(int j=0;j<8;j++) { float a=j*MathF.PI/4; if(Support(new(MathF.Cos(a)*.4f,.5f,MathF.Sin(a)*.4f),.3f)>.15f) rim++; }
                    bool cavity=!Occupied(new(0,.55f,0))&&!Occupied(new(0,.75f,0));
                    okay=rim>=6 && cavity && Math.Abs(local.X)<.35f && Math.Abs(local.Z)<.35f && (prop.Goal==7||Math.Abs(local.Y-.55f)<.5f);
                }
                else
                {
                    var contact=local-new Vector3(0,.1f,0);
                    float strength=Support(contact,prop.Goal==3?.4f:.33f);
                    okay=strength>(prop.Goal is 0 or 5 or 6 ? .45f:.24f) && material.Patches.Count(p=>p.Burning)<4;
                    // The cat climbs an approximate trunk; demand final top only after arrival.
                    if(prop.Goal==3 && t<2.4f) okay=head.Mass>8;
                }
                if(!okay && (prop.Goal!=0 || t>2.4f)) prop.Failed=true;
                else prop.HeldTime+=dt;
            }
            if(prop.Failed)
            {
                prop.Velocity+=new Vector3(.3f,-4,0)*dt;
                prop.Position+=prop.Velocity*dt;
                if(prop.Position.Y<.18f) prop.Position=new(prop.Position.X,.18f,prop.Position.Z);
            }
            else prop.Position=head.Position+local;
        }
    }
    public static void Finish(WorldState w)
    {
        var props=w.Props.Where(p=>p.Goal==w.Job.Goal).ToArray();
        float fraction=(float)props.Count(x=>!x.Failed&&x.HeldTime>2)/Math.Max(1,PhysicalProps.Count(w.Job.Goal));
        w.Job.Result.Function=fraction*100;w.Job.Result.Prop=fraction*100;
    }
}

