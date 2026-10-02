using System;
using System.Linq;
using System.Collections.Generic;
namespace Hairball.Core;
public sealed partial class Session
{
    public readonly ImpactLedger Ledger=new();
    readonly GoalQuality quality=new();
    readonly Dictionary<int,float> initialBarberMass=new();
    int impactDepth;
    public void EnsureLedger()
    {
        if(!Ledger.Started)Ledger.Begin(State.Round,quality.Read(State).Value);
    }
    public GoalHealth LiveHealth()=>quality.Read(State);
    ImpactContext Context(int tool,bool special=false)
    {
        var c=State.Customers[0];return (tool is 7 or 8 or 9?ImpactContext.Dangerous:0)
            |(c.Action!=CustomerReaction.None?ImpactContext.Reacting:0)
            |(c.ValidatorWarned?ImpactContext.Validator:0)|(special?ImpactContext.Ordered:0);
    }
    void RecordEnvironment(GoalHealth before)
    {
        var after=quality.Read(State);if(Math.Abs(after.Value-before.Value)<.00001f&&before.Viable==after.Viable)return;
        var sources=State.SharedHead.Patches.Where(p=>p.Burning).Select(p=>p.BurnSource).Distinct().ToArray();
        int actor=after.Value<before.Value&&sources.Length==1?sources[0]:0;
        Ledger.Observe(actor,-1,"environment",before.Value,after.Value,State.Time,State.Remaining,context:Context(-1),position:State.SharedHead.Position,criticalLost:State.Remaining<=15&&before.Viable&&!after.Viable);
    }
    static int MaterialStamp(Head h)
    {
        var hash=new HashCode();hash.Add(h.Volume.Revision);hash.Add(h.Position);hash.Add(h.AttachedTo);
        foreach(var p in h.Patches){hash.Add(p.Young);hash.Add(p.Wet);hash.Add(p.Char);hash.Add(p.Glue);hash.Add(p.Temperature);hash.Add(p.Stiffness);hash.Add(p.Anchored);hash.Add(p.Burning);}
        return hash.ToHashCode();
    }
    sealed class ImpactScope : IDisposable
    {
        readonly Session s;readonly PlayerState player;readonly ToolState tool;readonly GoalHealth before;
        readonly int penalty,eventId;readonly Dictionary<int,System.Numerics.Vector3> propPositions;readonly Dictionary<int,int> headStamps;
        readonly Dictionary<int,System.Numerics.Vector3> impulses;
        public ImpactScope(Session owner,PlayerState p,ToolState t)
        {
            s=owner;player=p;tool=t;s.EnsureLedger();before=s.LiveHealth();penalty=s.State.Job.Penalty;eventId=s.State.NextEvent;
            propPositions=s.State.Props.ToDictionary(x=>x.Id,x=>x.Position);
            headStamps=s.State.Heads.ToDictionary(h=>h.Id,h=>MaterialStamp(h));s.impactDepth++;
            impulses=s.State.Players.ToDictionary(p=>p.Id,p=>p.Impulse);
        }
        public void Dispose()
        {
            s.impactDepth--;var after=s.LiveHealth();
            var changed=s.State.Heads.Where(h=>!headStamps.TryGetValue(h.Id,out int stamp)||stamp!=MaterialStamp(h)).Select(h=>h.Facial?h.ParentHead:h.Id).Distinct().Order().ToArray();
            int propChanges=s.State.Props.Count(x=>propPositions.GetValueOrDefault(x.Id)!=x.Position);
            int targets=Math.Max(propChanges+changed.Length,s.State.Events.Where(e=>e.Id>=eventId&&e.Source==player.Id).Select(e=>e.Targets).DefaultIfEmpty(0).Max());
            int forces=s.State.Players.Count(p=>impulses.TryGetValue(p.Id,out var before)&&p.Impulse!=before);targets=Math.Max(targets,changed.Length+forces);
            string target=changed.Length>0?string.Join(',',changed):"world";
            s.Ledger.Observe(player.Id,tool.Definition,target,before.Value,after.Value,s.State.Time,s.State.Remaining,s.State.Job.Penalty-penalty,targets,
                discrete:tool.Definition is 6 or 7||tool.Special!=0,context:s.Context(tool.Definition,tool.Special!=0)|(changed.Length+forces+propChanges>0?ImpactContext.Changed:0),position:tool.HitHair?tool.Contact:s.State.SharedHead.Position,
                criticalLost:s.State.Remaining<=15&&before.Viable&&!after.Viable);
        }
    }
    void RecordIncident(int actor,float severity,System.Numerics.Vector3 position,int targets=1)
    {
        if(impactDepth>0||State.Phase!=Phase.Build)return;EnsureLedger();var q=LiveHealth();
        Ledger.Observe(actor,-1,"incident",q.Value,q.Value,State.Time,State.Remaining,severity,targets,true,Context(-1),position);
    }
    void FinishImpacts()
    {
        Ledger.Advance(State.Time,true);
        foreach(var p in State.Players.Where(p=>p.Active))Ledger.For(p.Id).OwnHairLoss=Math.Max(0,initialBarberMass.GetValueOrDefault(p.Id)-State.Barber(p.Slot).Mass);
        State.Experiment.ResultHair=State.Players.Where(p=>p.Active).Select(p=>new HairTrace(p.Id,p.Name,initialBarberMass.GetValueOrDefault(p.Id),State.Barber(p.Slot).Mass)).ToList();
        State.Job.Awards=Ledger.Awards(State.Players,State.Time);
        Replay.Consider(State,Ledger);
    }
}
