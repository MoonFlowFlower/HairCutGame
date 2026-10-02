using System;
using System.Numerics;
using System.Linq;
namespace Hairball.Core;

public readonly record struct PadMeasurement(int Contacts,float Height,float Variance,float Quality,bool Stable);
public static class Helipad
{
    public static PadMeasurement Measure(Head h)
    {
        int count=0;float sum=0,min=99,max=-99;
        for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
            if(h.Volume.Raycast(new(x*.38f,2.6f,z*.29f),-Vector3.UnitY,2.55f,out var p,out _)&&p.Y>.25f)
            {count++;sum+=p.Y;min=Math.Min(min,p.Y);max=Math.Max(max,p.Y);}
        float variance=count>0?max-min:3;float quality=count/9f*70+Math.Max(0,1-variance/.35f)*(count/9f)*30;
        return new(count,count>0?sum/count:0,variance,quality,count>=7&&variance<.18f&&h.Patches.Average(p=>p.Resistance)>.4f&&h.Patches.Count(p=>p.Burning)<4);
    }
    public static void Tick(PropState prop,Head head,float dt,float elapsed)
    {
        if(elapsed>1&&!prop.Failed)
        {
            float resistance=head.Patches.Average(p=>p.Resistance);
            head.Volume.Brush(new(EffectKind.ApplyForce,dt*.018f*(1-resistance),new(.2f,-1,.05f)),new(0,.8f,0),1.6f);
        }
        var pad=Measure(head);
        if(elapsed<3)prop.Position=Vector3.Lerp(prop.StartPosition,head.Position+new Vector3(0,pad.Height+.26f,0),Math.Clamp(elapsed/3,0,1));
        else if(!prop.Failed)
        {
            if(!pad.Stable){prop.Failed=true;prop.Velocity=new(.8f,.4f,.35f);}
            else{prop.Position=head.Position+new Vector3(0,pad.Height+.26f,0);prop.HeldTime+=dt;}
        }
        if(prop.Failed){prop.Velocity+=new Vector3(.3f,-4,0)*dt;prop.Position+=prop.Velocity*dt;if(prop.Position.Y<.25f){prop.Position=new(prop.Position.X,.25f,prop.Position.Z);prop.Velocity=default;}}
    }
}
