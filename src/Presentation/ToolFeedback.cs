using Godot;
using Hairball.Core;
using System;
using System.Linq;
using System.Collections.Generic;
namespace Hairball;

// Fixed pool per station; no emitter or tween is allocated per tool tick.
public partial class ToolFeedback : Node3D
{
    readonly MeshInstance3D[,] particles=new MeshInstance3D[4,20];
    readonly MeshInstance3D[,] strands=new MeshInstance3D[4,3];
    readonly StandardMaterial3D mist=Art.Material(new(.48f,1,.20f,.55f));
    readonly StandardMaterial3D glue=Art.Material(new("ffe39c"));
    readonly StandardMaterial3D air=Art.Material(new(.7f,.94f,1,.55f));
    readonly StandardMaterial3D ice=Art.Material(new("b4eaff"),true);
    public int VisibleParticles {get;private set;}
    public int VisibleGrowthParticles {get;private set;}
    public float GrowthStreamSpeed {get;private set;}
    public override void _Ready()
    {
        for(int slot=0;slot<4;slot++)
        {
            for(int i=0;i<20;i++){particles[slot,i]=Art.Ball(this,Vector3.Zero,Vector3.One,Colors.White);particles[slot,i].Visible=false;particles[slot,i].CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;}
            for(int i=0;i<3;i++){strands[slot,i]=Art.Cylinder(this,Vector3.Zero,.014f,1,new("ffe39c"));strands[slot,i].Visible=false;strands[slot,i].CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;}
        }
    }
    static void Segment(MeshInstance3D mesh,Vector3 from,Vector3 to)
    {var delta=to-from;mesh.Position=(from+to)*.5f;mesh.Quaternion=delta.LengthSquared()>.00001f?new Quaternion(Vector3.Up,delta.Normalized()):Quaternion.Identity;mesh.Scale=new(1,Math.Max(.001f,delta.Length()),1);}
    public void Sync(WorldState world,int localId,Vector3 muzzle,float time,IEnumerable<ToolState>? history=null)
    {
        VisibleParticles=VisibleGrowthParticles=0;GrowthStreamSpeed=0;
        for(int s=0;s<4;s++){for(int i=0;i<20;i++)particles[s,i].Visible=false;for(int i=0;i<3;i++)strands[s,i].Visible=false;}
        foreach(var tool in history??world.Tools.Concat(CoopFeedback(world)))
        {
            var player=world.Player(tool.Holder);if(player==null||time-tool.LastUse>.22f||time<tool.LastUse)continue;
            bool chips=tool.HitHair&&tool.ContactState==HairState.Frozen;
            bool sticky=tool.HitHair&&tool.ContactState==HairState.Glued;
            if(!chips&&!sticky&&tool.Definition is not (1 or 3 or 4 or 10))continue;
            // A growth plume represents material actually added. Empty supply and missed rays
            // stay distinct in the HUD instead of presenting a successful high-pressure spray.
            bool growth=tool.Definition==1&&world.Experiment.IsB;
            if(growth&&(!tool.HitHair||tool.EffectMass<=.00001f))continue;
            float power=growth?GrowthSpray.Power(player.GrowthHeldSeconds):1;
            bool fine=growth&&player.GrowthFine;
            int slot=player.Slot;var from=history!=null?Art.V(tool.Position):player.Id==localId?muzzle:Art.V(Session.Eye(player)+Session.Aim(player)*.45f);
            var to=Art.V(tool.Contact);var direction=(to-from).Normalized();var side=direction.Cross(Vector3.Up).Normalized();var up=side.Cross(direction);
            if(tool.Definition==4||sticky)
            {
                var a=from.Lerp(to,.33f)+Vector3.Down*.035f;var b=from.Lerp(to,.70f)+Vector3.Down*.045f;
                for(int i=0;i<3;i++)strands[slot,i].Visible=true;
                Segment(strands[slot,0],from,a);Segment(strands[slot,1],a,b);Segment(strands[slot,2],b,to);
            }
            if(chips){for(int i=0;i<8;i++){var chip=particles[slot,i];chip.Visible=true;VisibleParticles++;float age=Math.Max(0,time-tool.LastUse);chip.MaterialOverride=ice;chip.Position=to+new Vector3(MathF.Sin(i*7)*age*1.8f,age*(1+i%3)*.5f-age*age*8,MathF.Cos(i*7)*age*1.8f);chip.Scale=new(.025f,.035f,.05f);chip.Rotation=new(i,time*8,i*.7f);}continue;}
            int count=growth?(fine?5:12+(int)MathF.Round((power-1)*4)):tool.Definition==4?6:20;
            float speed=growth?(fine?.75f:3.4f*power):tool.Definition==3?2.8f:1.9f;
            if(growth){VisibleGrowthParticles+=count;GrowthStreamSpeed=Math.Max(GrowthStreamSpeed,speed);}
            for(int i=0;i<count;i++)
            {
                var particle=particles[slot,i];particle.Visible=true;VisibleParticles++;
                float t=(time*speed+i*.618f)%1;
                float angle=i*2.4f+time*1.3f,width=.025f+t*(tool.Definition==3?.25f:.14f);
                var offset=(side*MathF.Sin(angle)+up*MathF.Cos(angle))*width;
                particle.Position=from.Lerp(to,t)+offset;
                float size=growth?(.040f+t*.075f)*(fine?.7f:1+(power-1)*.14f):.035f+t*.10f;
                if(tool.Definition==4){particle.Position=to+new Vector3(MathF.Sin(i*7)*.045f,-(time*1.5f+i*.17f)%1*.12f,MathF.Cos(i*7)*.035f);size=.045f;}
                particle.MaterialOverride=tool.Definition==1?mist:tool.Definition==4?glue:air;
                particle.Scale=tool.Definition==3?new(size*.23f,size*.23f,size*2):Vector3.One*size;
                particle.Quaternion=tool.Definition==3?new Quaternion(Vector3.Back,direction):Quaternion.Identity;
            }
        }
    }
    public static IEnumerable<ToolState> CoopFeedback(WorldState w){var tank=w.Props.FirstOrDefault(p=>p.Coop==CoopKind.Tank);return w.Props.Where(p=>p.Coop==CoopKind.Nozzle&&p.Holder!=0).Select(p=>new ToolState{Holder=p.Holder,Definition=tank?.CoopMode==1?5:tank?.CoopMode==2?10:4,LastUse=p.LastUse,Contact=p.Contact,HitHair=p.HitHair,EffectMass=.8f,ContactState=tank?.CoopMode==1?HairState.Frozen:tank?.CoopMode==2?HairState.Wet:HairState.Glued});}
}
