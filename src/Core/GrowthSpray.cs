using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Hairball.Core;

public static class GrowthSpray
{
    public const float BottleCapacity=1;
    public const float InitialRate=.24f, MaximumRate=.48f, RampSeconds=1.8f, FineRate=.045f;
    public static float Power(float heldSeconds)
    {
        float t=Math.Clamp(float.IsFinite(heldSeconds)?heldSeconds:0,0,RampSeconds)/RampSeconds;
        return 1+(MaximumRate/InitialRate-1)*t*t*(3-2*t);
    }
    public static float Rate(float heldSeconds,bool fine=false)=>fine?FineRate:InitialRate*Power(heldSeconds);
    public static bool CanAfford(Head head,float material)
    {
        float Cell(byte value)=>Math.Clamp((HairVolume.Decode(value)+HairVolume.Step*.5f)/HairVolume.Step,0,1)*
            HairVolume.Step*HairVolume.Step*HairVolume.Step/HairVolume.UnitVolume*MathF.Pow(head.GeometryScale,3);
        return material>=Math.Min(Cell(96)-Cell(95),Cell(160)-Cell(159))-.000001f;
    }
    // Integrate the ramp, so a held spray spends the same time/distance at every tick rate.
    public static float Distance(float fromSeconds,float seconds,bool fine=false)
    {
        if(!float.IsFinite(seconds)||seconds<=0)return 0;
        if(fine)return FineRate*seconds;
        float start=Math.Max(0,float.IsFinite(fromSeconds)?fromSeconds:0);
        float Integral(float time)
        {
            float ramp=Math.Min(time,RampSeconds),t=ramp/RampSeconds;
            return InitialRate*(ramp+(MaximumRate/InitialRate-1)*RampSeconds*(t*t*t-.5f*t*t*t*t))+MaximumRate*Math.Max(0,time-RampSeconds);
        }
        return Integral(start+seconds)-Integral(start);
    }
}

public sealed partial class Session
{
    sealed class GrowthHold
    {
        public int Tool,Head;
        public bool Fine;
        public float Distance;
    }
    readonly Dictionary<int,GrowthHold> growthHolds=new();

    void ResetGrowthHold(PlayerState p)
    {
        p.GrowthHeldSeconds=0;p.GrowthFine=false;growthHolds.Remove(p.Id);
    }

    void UpdateGrowthHold(PlayerState p,Buttons buttons,float dt)
    {
        bool primary=buttons.HasFlag(Buttons.Primary),fine=!primary&&buttons.HasFlag(Buttons.Secondary);
        var tool=State.Tools.FirstOrDefault(t=>t.Id==p.Held&&t.Holder==p.Id&&t.Definition==1&&t.Special==0&&t.Charge!=0);
        if(!State.Experiment.IsB||State.Phase!=Phase.Build||primary&&!PartyAccidents.Primary(p,State.Time)||!p.Active||p.NetworkAway||p.Bracing||p.ReferenceUp||
            p.CarriedProp>=0||p.CarriedModel>=0||p.CarryLadder>=0||(!primary&&!fine)||tool==null||
            tool.GrowthRemaining<=0||!float.IsFinite(dt)||dt<=0||endedGestures.Contains(p.Id))
        {ResetGrowthHold(p);return;}

        if(State.Players.Any(q=>q.Active&&q.Id!=p.Id&&q.UsingBlower&&Vector3.Distance(q.Position,p.Position)<5))Discover(Combo.WindSpray);
        var direction=ToolQuery.Direction(State,p,1);
        float range=ObstructionDistance?.Invoke(Eye(p),direction,Tools.Get(1).Range)??Tools.Get(1).Range;
        var hits=ToolQuery.Find(State,p,1,fine,direction,range);
        if(hits.Count>0&&!hits.Any(hit=>CanAffordGrowth(State.Heads.First(h=>h.Id==hit.HeadId),tool)))
        {ResetGrowthHold(p);return;}
        strokes.TryGetValue(p.Id,out var stroke);
        if(p.Cooldown>0&&(stroke==null||stroke.Tool!=tool.Id))
        {ResetGrowthHold(p);return;}
        // A gesture stays on its original head; missing it must never charge a new target.
        int head=stroke!=null&&stroke.Tool==tool.Id&&stroke.Fine==fine?stroke.Head:hits.FirstOrDefault().HeadId;
        if(!hits.Any(h=>h.HeadId==head))
        {
            if(stroke!=null&&stroke.Tool==tool.Id)endedGestures.Add(p.Id);
            ResetGrowthHold(p);return;
        }
        if(!growthHolds.TryGetValue(p.Id,out var hold)||hold.Tool!=tool.Id||hold.Head!=head||hold.Fine!=fine)
        {
            p.GrowthHeldSeconds=0;growthHolds[p.Id]=hold=new(){Tool=tool.Id,Head=head,Fine=fine};
        }
        p.GrowthFine=fine;
        hold.Distance+=GrowthSpray.Distance(p.GrowthHeldSeconds,dt,fine);
        p.GrowthHeldSeconds=fine?0:Math.Min(GrowthSpray.RampSeconds,p.GrowthHeldSeconds+dt);
    }

    bool CanAffordGrowth(Head head,ToolState tool)=>GrowthSpray.CanAfford(head,tool.GrowthRemaining);
    public static float TotalGrowth(WorldState w)=>w.Tools.Where(t=>t.Definition==1).Sum(t=>t.GrowthRemaining);

    float TakeGrowthDistance(PlayerState p,bool fine,float legacyDt)
    {
        if(!State.Experiment.IsB)return .18f*(fine?.25f:1)*legacyDt;
        if(!growthHolds.TryGetValue(p.Id,out var hold))return GrowthSpray.Rate(0,fine)*legacyDt;
        float distance=hold.Distance;hold.Distance=0;return distance;
    }
}
