using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Hairball.Core;
public sealed class ReplayFrame
{
    public float Time;
    public CatState Cat=new();
    public TwistState Twist=new();
    public ExperimentState Experiment=new();
    public List<int> BracingPlayers=new();
    public List<Head> Heads = new();
    public List<Vector3> Players = new();
    public List<float> Recovery = new();
    public List<ToolState> Tools = new();
    public List<CustomerState> Customers = new();
    public List<LadderState> Ladders=new();
    public DebrisState Debris=new();
    public List<PropState> Props=new();
    public List<int> ReferencePlayers=new();
}
public sealed class ReplayRecorder
{
    public List<ReplayFrame> Rolling = new(), Best = new();
    float clock, bestValue=-1;
    int lastEvent=-1;
    public string Caption="A hard day's questionable work";
    public Vector3 Focus;
    float captureUntil;
    public void Clear() { Rolling.Clear(); Best.Clear(); clock=0; bestValue=-1; lastEvent=-1; captureUntil=0;Caption="A hard day's questionable work";Focus=default; }
    public static float Rank(GameEvent e) => e.Value + e.Targets*4;
    public void Record(WorldState w,float dt,ImpactLedger? ledger=null)
    {
        clock+=dt; if(clock<.1f) return; clock=0;
        var frame=new ReplayFrame {Time=w.Time,Heads=w.Heads.Select(h=>h.Clone()).ToList(),Players=w.Players.Where(p=>p.Active).Select(p=>p.Position).ToList(),Recovery=w.Customers.Select(c=>c.Recovery).ToList(),Tools=w.Tools.Select(t=>new ToolState{Id=t.Id,Definition=t.Definition,Special=t.Special,Holder=t.Holder,Charge=t.Charge,GrowthRemaining=t.GrowthRemaining,LastUse=t.LastUse,Contact=t.Contact,HitHair=t.HitHair,ContactState=t.ContactState,EffectMass=t.EffectMass,Position=t.Holder==0?t.Position:w.Player(t.Holder)!.Position+new Vector3(.35f,1.1f,-.4f)}).ToList()};
        frame.Cat=w.Cat.Clone();frame.Twist=w.Twist.Clone();frame.Experiment=w.Experiment.Clone();frame.BracingPlayers=w.Players.Where(p=>p.Bracing).Select(p=>p.Id).ToList();
        frame.Customers=w.Customers.Select(c=>c.Clone()).ToList();
        frame.Ladders=w.Ladders.Select(l=>l.Clone()).ToList();
        frame.ReferencePlayers=w.Players.Where(p=>p.ReferenceUp).Select(p=>p.Id).ToList();
        frame.Debris=w.Debris.Clone();frame.Props=w.Props.Select(p=>p.Clone()).ToList();
        Rolling.Add(frame); Rolling.RemoveAll(f=>f.Time<w.Time-10);
        if(ledger!=null)Consider(w,ledger);
        else foreach(var e in w.Events.Where(e=>e.Id>lastEvent))
        {
            float value=Rank(e)+w.Events.Where(x=>x.Time>e.Time-2).Sum(x=>x.Value)*.15f;
            if(value>bestValue) { bestValue=value; Best=Rolling.Where(f=>f.Time>w.Time-3).ToList(); captureUntil=w.Time+3; Caption=e.Text; Focus=e.Position; }
            lastEvent=Math.Max(lastEvent,e.Id);
        }
        if(w.Time<captureUntil && (Best.Count==0 || Best[^1]!=frame)) Best.Add(frame);
    }
    public void Consider(WorldState w,ImpactLedger ledger)
    {
        foreach(var e in ledger.Events.Where(e=>e.Closed).OrderBy(e=>e.Id))
        {
            float value=ImpactLedger.Rank(e);if(value<=bestValue||value<=0)continue;
            bestValue=value;Best=Rolling.Where(f=>f.Time>=e.Time-2.5f&&f.Time<=e.Time+3).ToList();captureUntil=e.Time+3;Focus=e.Position;
            string actor=w.Players.FirstOrDefault(p=>p.Id==e.Actor)?.Name??"Customer / environment";
            Caption=$"{actor}: "+(e.Rescue>ImpactLedger.Epsilon?"Measured rescue":e.Delta< -ImpactLedger.Epsilon||e.Severity>0?"A costly accident":e.Money<0?"Shared-wallet purchase":"One action, many consequences");
        }
    }
    public void Select(WorldState w)
    {
        if(Best.Count<5) Best=Rolling.ToList();
        w.HighlightTitle=Caption; w.HighlightStart=Best.Count>0?Best[0].Time:0;w.HighlightFocus=Focus;
    }
}

