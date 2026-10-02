using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
namespace Hairball.Core;
public static class PartyBalance
{
    public const float Threshold=.16f,MaxAngle=MathF.PI/15,Rate=MathF.PI/45;
    public static Vector3 Centroid(Head h)
    {
        Vector3 sum=default;float total=0;
        for(int i=0;i<h.Volume.Data.Length;i++){float m=Math.Clamp((HairVolume.Decode(h.Volume.Data[i])+HairVolume.Step*.5f)/HairVolume.Step,0,1);if(m<=0)continue;sum+=HairVolume.Position(i%HairVolume.NX,i/HairVolume.NX%HairVolume.NY,i/(HairVolume.NX*HairVolume.NY))*m;total+=m;}
        return total>0?sum/total:default;
    }
    public static float Offset(WorldState w)
    {
        var head=w.SharedHead;float sum=Centroid(head).X*head.Mass,total=head.Mass+5; // Neck/body contributes a centered 5-unit base.
        foreach(var wig in w.Heads.Where(h=>h.AttachedTo==0)){sum+=(wig.AttachedOffset+Centroid(wig)*wig.GeometryScale).X*wig.Mass;total+=wig.Mass;}
        foreach(var prop in w.Props.Where(p=>p.Attached&&p.Holder==0)){float mass=prop.Goal==-90?ShopCat.Weight(w):prop.Mold==MoldKind.None?3:.5f;sum+=prop.Local.X*mass;total+=mass;}
        return total>0?sum/total:0;
    }
    public static float Advance(float tilt,float offset,float dt)
    {float target=-MathF.Sign(offset)*Math.Clamp((Math.Abs(offset)-Threshold)*.65f,0,MaxAngle);return tilt+Math.Clamp(target-tilt,-Rate*dt,Rate*dt);}
}
public enum Combo { FrozenImpact,HeatGlue,WindSpray,WetFire }
public static class ComboText
{
    public static string Text(Combo c)=>L.T(c switch{Combo.FrozenImpact=>"Frozen hair shatters under a heavy hit",Combo.HeatGlue=>"Heat loosens glue",Combo.WindSpray=>"Wind carries mist and flame",_=>"Wet hair resists ignition"});
}
public sealed partial class Session
{
    public readonly HashSet<Combo> KnownCombos=new();
    float balanceAt;
    float balanceOffset;
    void TickBalance(float dt)
    {
        if(!State.Experiment.IsB||State.Experiment.Resolved||State.Phase!=Phase.Build)return;
        if(State.Time>=balanceAt){balanceOffset=PartyBalance.Offset(State);balanceAt=State.Time+.25f;}
        var e=State.Experiment;e.Tilt=PartyBalance.Advance(e.Tilt,balanceOffset,dt);
        if(Math.Abs(e.Tilt)>.03f)CustomerRemarks.Say(e,Remark.Neck,State.Time);
    }
    void Discover(Combo combo)
    {
        var e=State.Experiment;if(!e.IsB||KnownCombos.Contains(combo))return;KnownCombos.Add(combo);e.Discoveries.Add(combo);e.Combo=combo;e.ComboUntil=State.Time+4;
        ExperimentEvent("combo",0,State.SharedHead.Position,combo.ToString());
    }
}
