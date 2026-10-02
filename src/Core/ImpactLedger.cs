using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
namespace Hairball.Core;

[Flags] public enum ImpactContext { None=0,Late=1,Dangerous=2,Reacting=4,Validator=8,Ordered=16,Changed=32 }
public sealed class ImpactEvent
{
    public int Id,Job,Actor,Tool,Targets,Samples,CrisisId=-1,Money;
    public string Target="",Action="tool";
    public float Start,Time,QualityBefore,QualityAfter,Delta,Severity,Rescue,Surviving;
    public bool Closed;
    public ImpactContext Context;
    public Vector3 Position;
}
public sealed class ImpactTotals
{
    public float Positive,Negative,Surviving,Rescue,LateRescue,Accident,Chaos,Risky,OwnHairLoss;
    public int Spend;
}
public sealed class Crisis
{
    public int Id,PrimaryCause;
    public float Start,BottomTime,Baseline,Bottom,RecoveredTo;
    public bool Resolved,Expired;
    public List<int> CauseEvents=new();
    public Dictionary<int,float> SelfDebt=new();
}
public sealed class SpotlightAward { public string Title="",Name="";public int Actor,EventId; }

// Host-only instrumentation. Nothing here changes hair, tool effects, validation or payout.
public sealed class ImpactLedger
{
    public const float Epsilon=.2f,WindowSeconds=.6f,ConfirmSeconds=2,RescueSeconds=8;
    public readonly List<ImpactEvent> Events=new();
    public readonly List<Crisis> Crises=new();
    public readonly Dictionary<int,ImpactTotals> Totals=new();
    readonly Dictionary<int,ImpactEvent> windows=new();
    readonly List<(float Time,float Quality,int Actor,float Loss,int Event)> recent=new();
    readonly List<(ImpactEvent Event,float Amount,float Time)> credit=new();
    readonly float[] rescuedBands=new float[1000];
    int sequence,job,crisisSequence;
    float highWater;
    public bool Started {get;private set;}
    public float LastQuality {get;private set;}
    public ImpactTotals For(int actor){if(!Totals.TryGetValue(actor,out var value))Totals[actor]=value=new();return value;}
    public void Begin(int round,float quality)
    {
        Events.Clear();Crises.Clear();Totals.Clear();windows.Clear();recent.Clear();credit.Clear();Array.Clear(rescuedBands);
        sequence=crisisSequence=0;job=round;highWater=LastQuality=quality;Started=true;
    }
    public static float Rank(ImpactEvent e)
    {
        float urgency=e.Context.HasFlag(ImpactContext.Late)?1.25f:1;
        return Math.Max(0,-e.Delta)*1.3f+e.Severity+e.Rescue*2*urgency+Math.Max(0,e.Targets-1)*3
            +(e.Context.HasFlag(ImpactContext.Dangerous)&&e.Severity<8?e.Surviving*.6f:0)
            +(e.Context.HasFlag(ImpactContext.Reacting)&&e.Severity>0?5:0)
            +(e.Context.HasFlag(ImpactContext.Ordered)&&Math.Abs(e.Delta)>Epsilon?4:0);
    }
    public ImpactEvent? Observe(int actor,int tool,string target,float before,float after,float time,float remaining,
        float severity=0,int targets=1,bool discrete=false,ImpactContext context=0,Vector3 position=default,int money=0,bool criticalLost=false)
    {
        if(!Started||!float.IsFinite(before)||!float.IsFinite(after))return null;
        before=Math.Clamp(before,0,100);after=Math.Clamp(after,0,100);float delta=after-before;LastQuality=after;
        if(Math.Abs(delta)<.00001f&&severity==0&&targets<2&&money==0&&!criticalLost&&!context.HasFlag(ImpactContext.Changed))return null;
        if(remaining<=15)context|=ImpactContext.Late;
        if(windows.TryGetValue(actor,out var old)&&(old.Tool!=tool||old.Target!=target||time-old.Start>=WindowSeconds||discrete))Close(actor);
        if(!windows.TryGetValue(actor,out var e))
        {
            e=new(){Id=++sequence,Job=job,Actor=actor,Tool=tool,Target=target,Start=time,Time=time,QualityBefore=before,QualityAfter=after,Position=position};
            windows[actor]=e;Events.Add(e);if(Events.Count>2048)Events.RemoveAt(0);
        }
        e.Time=time;e.QualityAfter=after;e.Delta+=delta;e.Severity+=Math.Max(0,severity);e.Targets=Math.Max(e.Targets,targets);e.Context|=context;e.Samples++;e.Money+=money;
        if(money!=0)e.Action="order";
        recent.RemoveAll(x=>time-x.Time>2);
        recent.Add((time,before,actor,Math.Max(0,-delta),e.Id));
        foreach(var expired in Crises.Where(c=>!c.Resolved&&!c.Expired&&time-c.BottomTime>RescueSeconds))expired.Expired=true;
        var crisis=Crises.LastOrDefault(c=>!c.Resolved&&!c.Expired);
        float peak=recent.Max(x=>x.Quality);
        if(crisis==null&&(peak-after>=12||severity>=12||criticalLost))
        {
            crisis=new(){Id=++crisisSequence,Start=time,BottomTime=time,Baseline=peak,Bottom=after,RecoveredTo=after};
            foreach(var cause in recent.Where(x=>x.Loss>0))
            {if(!crisis.CauseEvents.Contains(cause.Event))crisis.CauseEvents.Add(cause.Event);crisis.SelfDebt[cause.Actor]=crisis.SelfDebt.GetValueOrDefault(cause.Actor)+cause.Loss;}
            crisis.PrimaryCause=crisis.SelfDebt.OrderByDescending(x=>x.Value).ThenBy(x=>x.Key).Select(x=>x.Key).FirstOrDefault();
            if(severity>=12)
            {if(!crisis.CauseEvents.Contains(e.Id))crisis.CauseEvents.Add(e.Id);if(crisis.SelfDebt.Count==0)crisis.PrimaryCause=actor;}
            Crises.Add(crisis);if(Crises.Count>128)Crises.RemoveAt(0);
        }
        else if(crisis!=null&&delta<0)
        {
            crisis.SelfDebt[actor]=crisis.SelfDebt.GetValueOrDefault(actor)-delta;
            if(!crisis.CauseEvents.Contains(e.Id)){crisis.CauseEvents.Add(e.Id);if(crisis.CauseEvents.Count>2048)crisis.CauseEvents.RemoveAt(0);}
            if(after<crisis.Bottom){crisis.Bottom=after;crisis.BottomTime=time;}
        }
        if(crisis!=null)
        {
            e.CrisisId=crisis.Id;
            if(delta>0)
            {
                float lo=Math.Max(before,crisis.RecoveredTo),hi=Math.Min(after,crisis.Baseline);
                float recovery=Math.Max(0,hi-lo),self=Math.Min(recovery,crisis.SelfDebt.GetValueOrDefault(actor));
                crisis.SelfDebt[actor]=Math.Max(0,crisis.SelfDebt.GetValueOrDefault(actor)-self);
                if(actor!=0&&recovery>self)
                {
                    // Each tenth of quality can generate rescue credit only once per job.
                    float earned=0;
                    for(int i=0;i<1000;i++)
                    {float part=Math.Max(0,Math.Min(hi,(i+1)*.1f)-Math.Max(lo+self,i*.1f));float allowed=Math.Min(part,.1f-rescuedBands[i]);rescuedBands[i]+=allowed;earned+=allowed;}
                    e.Rescue+=earned;
                }
                crisis.RecoveredTo=Math.Max(crisis.RecoveredTo,Math.Min(after,crisis.Baseline));
                if(after>=crisis.Baseline-Epsilon)crisis.Resolved=true;
            }
        }
        if(delta<0)
        {
            float loss=-delta;
            for(int i=credit.Count-1;i>=0&&loss>0;i--)
            {var item=credit[i];float take=Math.Min(loss,item.Amount);credit[i]=(item.Event,item.Amount-take,item.Time);loss-=take;}
        }
        // Unique progress beyond the job's old high-water mark prevents destroy/restore contribution farming.
        float useful=Math.Max(0,after-Math.Max(before,highWater));highWater=Math.Max(highWater,after);
        if(actor!=0&&useful>0)
        {
            if(credit.Count>0&&credit[^1].Event==e){var item=credit[^1];credit[^1]=(e,item.Amount+useful,time);}
            else credit.Add((e,useful,time));
        }
        credit.RemoveAll(x=>x.Amount<.00001f);
        if(discrete||money!=0)Close(actor);
        Advance(time);return e;
    }
    public void Close(int actor)
    {
        if(!windows.Remove(actor,out var e))return;e.Closed=true;
        if(Math.Abs(e.Delta)<Epsilon)e.Delta=0;
        if(e.Delta==0&&e.Severity==0&&e.Targets<2&&e.Money==0&&e.Rescue==0&&!e.Context.HasFlag(ImpactContext.Changed)){Events.Remove(e);credit.RemoveAll(x=>x.Event==e);return;}
        var total=For(actor);total.Positive+=Math.Max(0,e.Delta);total.Negative+=Math.Max(0,-e.Delta);
        total.Rescue+=e.Rescue;if(e.Context.HasFlag(ImpactContext.Late))total.LateRescue+=e.Rescue;
        total.Accident=Math.Max(total.Accident,Math.Max(0,-e.Delta)+e.Severity);
        total.Chaos+=Math.Max(0,e.Targets-1);total.Spend+=Math.Max(0,-e.Money);
    }
    public void Advance(float time,bool flush=false)
    {
        foreach(var c in Crises.Where(c=>!c.Resolved&&!c.Expired&&time-c.BottomTime>RescueSeconds))c.Expired=true;
        foreach(var pair in windows.ToArray())if(flush||time-pair.Value.Time>.2f||time-pair.Value.Start>=WindowSeconds)Close(pair.Key);
        foreach(var e in Events)e.Surviving=0;
        foreach(var t in Totals.Values){t.Surviving=0;t.Risky=0;}
        foreach(var c in credit)
        {
            if(!flush&&time-c.Time<ConfirmSeconds)continue;
            if(c.Event.Delta<Epsilon||!c.Event.Closed)continue;
            c.Event.Surviving+=c.Amount;var total=For(c.Event.Actor);total.Surviving+=c.Amount;
            if(c.Event.Context.HasFlag(ImpactContext.Dangerous)&&c.Event.Severity<8)total.Risky+=c.Amount;
        }
    }
    public List<SpotlightAward> Awards(IEnumerable<PlayerState> players,float time)
    {
        Advance(time,true);var result=new List<SpotlightAward>();var known=players.OrderBy(p=>p.Slot).ThenBy(p=>p.Id).ToArray();
        void Pick(string title,Func<ImpactTotals,float> metric,Func<ImpactEvent,float>? evidence=null)
        {
            var p=known.OrderByDescending(p=>metric(For(p.Id))).ThenBy(p=>p.Slot).ThenBy(p=>p.Id).FirstOrDefault();
            if(p==null||metric(For(p.Id))<Epsilon)return;
            result.Add(new(){Title=title,Actor=p.Id,Name=p.Name,EventId=Events.Where(e=>e.Actor==p.Id).OrderByDescending(evidence??Rank).ThenBy(e=>e.Id).Select(e=>e.Id).FirstOrDefault()});
        }
        Pick("Rescue Barber",x=>x.Rescue,e=>e.Rescue);Pick("Biggest Contributor",x=>x.Surviving,e=>e.Surviving);Pick("Biggest Accident",x=>x.Accident,e=>Math.Max(0,-e.Delta)+e.Severity);Pick("Big Spender",x=>x.Spend,e=>Math.Max(0,-e.Money));
        Pick("Last-Second Save",x=>x.LateRescue,e=>e.Context.HasFlag(ImpactContext.Late)?e.Rescue:0);Pick("Risky Genius",x=>x.Risky,e=>e.Context.HasFlag(ImpactContext.Dangerous)&&e.Severity<8?e.Surviving:0);Pick("Chaos Magnet",x=>x.Chaos);Pick("Self-Hair Victim",x=>x.OwnHairLoss);
        return result.Take(3).ToList();
    }
}
