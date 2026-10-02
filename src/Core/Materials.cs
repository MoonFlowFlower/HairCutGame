using System;
using System.Linq;
using System.Numerics;
namespace Hairball.Core;

public enum HairState { Normal, Young, Wet, Frozen, Glued, Charred }
public static class HairMaterials
{
    public static HairState State(Patch p)=>p.Frozen?HairState.Frozen:p.Char>.45f?HairState.Charred:p.Glue>.45f?HairState.Glued:p.Wet>.3f?HairState.Wet:p.Young>.25f?HairState.Young:HairState.Normal;
    public static float Motion(Patch p)=>p.Anchored||p.Frozen?0:p.Glue>.45f?.002f:p.Char>.45f?.004f:p.Wet>.3f?.009f:p.Young>.25f?.035f:.017f;
    public static Vector3 Color(Patch p,Vector3 basis)
    {
        var c=Vector3.Lerp(basis,new(.68f,.46f,.26f),p.Young*.55f);
        c*=1-p.Wet*.35f;c=Vector3.Lerp(c,new(.13f,.12f,.11f),p.Char);
        c=Vector3.Lerp(c,new(.78f,.57f,.22f),p.Glue*.4f);
        return p.Frozen?new(.67f,.83f,.91f):c;
    }
    public static void Set(Patch p,HairState state)
    {
        p.Young=p.Wet=p.Char=p.Glue=0;p.Temperature=20;p.Burning=false;
        switch(state){case HairState.Young:p.Young=1;break;case HairState.Wet:p.Wet=1;break;case HairState.Frozen:p.Temperature=-65;break;case HairState.Glued:p.Glue=1;break;case HairState.Charred:p.Char=1;break;}
    }
}
public static class GoalMaterials
{
    public static (float Shape,float State,float Function) Weights(int goal)=>goal switch{0=>(.6f,.15f,.25f),1=>(.45f,.15f,.4f),4=>(.55f,.25f,.2f),2=>(.6f,0,.4f),_=>(.6f,.1f,.3f)};
    public static bool InZone(int goal,Vector3 p)=>goal==4?p.Y<.62f&&new Vector2(p.X,p.Z).Length()>.35f:goal==0?p.Y>.55f:true;
    public static float Suitability(int goal,Patch p)=>goal switch{1=>Math.Clamp(1-p.Resistance,0,1),4=>p.Char,0 or 3 or 5 or 6=>Math.Clamp(p.Resistance/.65f,0,1),_=>1};
    public static float Evaluate(Head head,int goal)
    {
        var points=Goals.All[goal].TargetSamples.Where(p=>InZone(goal,p)).ToArray();
        return points.Length==0?100:100*points.Average(p=>head.Volume.Sample(p)>-HairVolume.Step?Suitability(goal,HairSystem.MaterialAt(head,p)):0);
    }
    public static float Distance(ITargetVolume target,Vector3 p)=>target switch {
        BoxVolume b=>Math.Min(b.Half.X-Math.Abs(p.X-b.Center.X),Math.Min(b.Half.Y-Math.Abs(p.Y-b.Center.Y),b.Half.Z-Math.Abs(p.Z-b.Center.Z))),
        CylinderVolume c=>Math.Min(c.HalfHeight-Math.Abs(p.Y-c.Center.Y),Math.Min(c.Radius-new Vector2(p.X-c.Center.X,p.Z-c.Center.Z).Length(),c.Hole>0?new Vector2(p.X-c.Center.X,p.Z-c.Center.Z).Length()-c.Hole:999)),
        UnionVolume u=>u.Parts.Max(t=>Distance(t,p)),_=>-1};
    public static Head Reference(int goal)
    {
        var head=Head.Create(-100,0);var target=Goals.All[goal].Target;
        // Signed distance sampled from the same target predicate used by scoring.
        head.Volume.Fill(p=>Distance(target,p));
        foreach(var patch in head.Patches){patch.Length=1.2f;if(goal is 0 or 3 or 5 or 6)patch.Glue=.85f;else if(goal==1)patch.Young=.8f;}
        return head;
    }
}
