using System;
using System.Linq;
using System.Numerics;
namespace Hairball.Core;

// Read-only presentation rules. Never write visual deformation back into density/probes.
public static class MaterialFeel
{
    public static float Mobility(Patch p)=>p.Anchored||p.Frozen?0:.11f*MathF.Pow(1-p.Resistance,2);
    public static float Sink(WorldState w,PropState prop)
    {
        if(!prop.Attached||prop.Goal!=0)return 0;
        var contacts=Session.LandingProbes(w,prop).Where(p=>p.HasContact).ToArray();
        if(contacts.Length==0)return 0;
        float softness=contacts.Average(p=>Math.Clamp((.4f-p.Resistance)/.4f,0,1));
        return .16f*softness*(1-MathF.Exp(-Math.Max(0,w.Time-prop.PlacedAt)*3));
    }
    public static float Facing(Vector3 eye,Vector3 head,Vector3 fault)
    {
        var side=fault-head;side.Y=0;var viewer=eye-head;viewer.Y=0;
        if(side.LengthSquared()<.005f||viewer.LengthSquared()<.005f)return 1;
        return .2f+.8f*Math.Clamp((Vector3.Dot(Vector3.Normalize(side),Vector3.Normalize(viewer))+.15f)/1.15f,0,1);
    }
    public static float HairLossPercent(float initial,float current)=>initial>0&&float.IsFinite(initial)&&float.IsFinite(current)?Math.Clamp((initial-current)/initial*100,0,100):0;
}
public sealed record HairTrace(int Actor,string Name,float InitialMass,float FinalMass)
{
    public float LossPercent=>MaterialFeel.HairLossPercent(InitialMass,FinalMass);
}
