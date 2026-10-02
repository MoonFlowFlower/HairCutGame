using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
namespace Hairball.Core;
public sealed record RoundMemory(int Round,TargetCard Target,TargetCard? Family,bool Success);
public static class ReturningCustomer
{
    public const float Growth=.025f;
    public static void Grow(Head h)
    {
        for(int i=0;i<h.Volume.Data.Length;i++)if(h.Volume.Data[i]>0)h.Volume.Data[i]=HairVolume.Encode(HairVolume.Decode(h.Volume.Data[i])+Growth/h.GeometryScale);
        h.Volume.Revision++;h.Locked=false;h.Holder=0;h.Velocity=default;
        foreach(var p in h.Patches){p.Length=Math.Min(3,p.Length+Growth);if(p.Anchored)p.AnchorPoint=h.ToWorld(p.AnchorLocal);}
    }
}
public sealed partial class Session
{
    public bool ReturningEnabled;
    List<Head> RetainedHeads()=>State.ReturningHeads&&State.Round>0?State.Heads.Where(h=>h.Id==0||h.ParentHead==0||h.AttachedTo==0).Select(h=>h.Clone()).ToList():[];
    void RestoreReturning(List<Head> saved,string profile)
    {
        if(saved.Count==0)return;State.Heads.RemoveAll(h=>h.Id==0||h.ParentHead==0||h.AttachedTo==0);State.Heads.AddRange(saved);State.Customers[0].Profile=profile;
        State.SharedHead.Position=WorkCenter+Vector3.UnitY*1.5f;State.SharedHead.Rotation=default;
        foreach(var h in saved){if(h.AttachedTo==0){h.Position=State.SharedHead.ToWorld(h.AttachedOffset);h.Rotation=State.SharedHead.Rotation+h.AttachedRotation;}ReturningCustomer.Grow(h);}SyncFaces();
    }
    void RememberRound(){if(State.Experiment.ResultTarget is {} card&&!State.RoundHistory.Any(r=>r.Round==State.Round)){State.RoundHistory.Add(new(State.Round,card,State.Twist.ResultFamily,State.Experiment.Success));if(State.RoundHistory.Count>4)State.RoundHistory.RemoveAt(0);}}
}
