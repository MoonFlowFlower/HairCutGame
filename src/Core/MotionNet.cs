using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
namespace Hairball.Core;

public readonly record struct InputTick(int Sequence,Vector2 Move,float Yaw,float Pitch,Buttons Buttons,float Time=0);
public readonly record struct PlayerMotion(int Id,int Ack,Vector3 Position,Vector3 Velocity,Vector3 Impulse,float Yaw,float Pitch,bool Grounded,bool JumpHeld,float FreezeUntil=0,float GlueUntil=0,float DownUntil=0,bool Customer=false,bool Standing=false,bool GlueHead=false,Vector3 GlueOffset=default);
public readonly record struct MovingPose(int Id,Vector3 Position,Vector3 Rotation);
public sealed class MotionFrame
{
    public int Sequence,Round,MatchNumber,ExpressionKind;public float Time,ExpressionAt;
    public PlayerMotion[] Players=[];
    public MovingPose Head;
    public MovingPose[] Props=[];
    public AttentionStage Attention;
    public Vector3 AttentionSource;
    public float AttentionStarted,StableSeconds;
    public bool MirrorBlocked,Bracing;
    public FlightStage Flight;
    public LeaveStage Leave;
    public LandingIssue LandingIssues;
}
public static class MotionWire
{
    public const float Step=1f/60;
    public const int MaxProps=24;
    static void V(BinaryWriter w,Vector3 v){w.Write(v.X);w.Write(v.Y);w.Write(v.Z);}
    static Vector3 V(BinaryReader r)=>new(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
    static bool Finite(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z);
    public static byte[] Inputs(IEnumerable<InputTick> input)
    {
        var ticks=input.TakeLast(12).ToArray();using var stream=new MemoryStream();using var w=new BinaryWriter(stream);
        w.Write(ticks.Length);foreach(var t in ticks){w.Write(t.Sequence);w.Write(t.Move.X);w.Write(t.Move.Y);w.Write(t.Yaw);w.Write(t.Pitch);w.Write((int)t.Buttons);w.Write(t.Time);}return stream.ToArray();
    }
    public static InputTick[] ReadInputs(byte[] data)
    {
        if(data.Length<4||data.Length>340)return [];using var r=new BinaryReader(new MemoryStream(data));int count=r.ReadInt32();
        if(count<1||count>12||data.Length!=4+count*28)return [];
        var result=new InputTick[count];
        for(int i=0;i<count;i++){
            int seq=r.ReadInt32();float x=r.ReadSingle(),z=r.ReadSingle(),yaw=r.ReadSingle(),pitch=r.ReadSingle();int buttons=r.ReadInt32();float time=r.ReadSingle();
            if(seq<=0||!float.IsFinite(x)||!float.IsFinite(z)||!float.IsFinite(yaw)||!float.IsFinite(pitch)||!float.IsFinite(time)||time<0)return [];
            result[i]=new(seq,new(Math.Clamp(x,-1,1),Math.Clamp(z,-1,1)),yaw,Math.Clamp(pitch,-1.35f,1.35f),(Buttons)(buttons&2047),time);
        }return result;
    }
    public static byte[] Encode(MotionFrame frame)
    {
        using var stream=new MemoryStream();using var w=new BinaryWriter(stream);
        w.Write(frame.Sequence);w.Write(frame.Round);w.Write(frame.Time);w.Write(frame.Players.Length);
        foreach(var p in frame.Players){w.Write(p.Id);w.Write(p.Ack);V(w,p.Position);V(w,p.Velocity);V(w,p.Impulse);w.Write(p.Yaw);w.Write(p.Pitch);w.Write(p.Grounded);w.Write(p.JumpHeld);w.Write(p.FreezeUntil);w.Write(p.GlueUntil);w.Write(p.DownUntil);w.Write(p.Customer);w.Write(p.Standing);w.Write(p.GlueHead);V(w,p.GlueOffset);}
        V(w,frame.Head.Position);V(w,frame.Head.Rotation);w.Write(frame.Props.Length);
        foreach(var p in frame.Props){w.Write(p.Id);V(w,p.Position);V(w,p.Rotation);}
        w.Write((byte)frame.Attention);V(w,frame.AttentionSource);w.Write(frame.AttentionStarted);w.Write(frame.MirrorBlocked);w.Write(frame.Bracing);w.Write((byte)frame.Flight);w.Write(frame.StableSeconds);w.Write((int)frame.LandingIssues);w.Write((byte)frame.Leave);w.Write(frame.MatchNumber);w.Write(frame.ExpressionKind);w.Write(frame.ExpressionAt);return stream.ToArray();
    }
    public static MotionFrame? Decode(byte[] data)
    {
        if(data.Length<44||data.Length>1200)return null;
        try{
            using var r=new BinaryReader(new MemoryStream(data));var f=new MotionFrame{Sequence=r.ReadInt32(),Round=r.ReadInt32(),Time=r.ReadSingle()};int count=r.ReadInt32();
            if(count<0||count>4||!float.IsFinite(f.Time))return null;f.Players=new PlayerMotion[count];
            for(int i=0;i<count;i++){var p=new PlayerMotion(r.ReadInt32(),r.ReadInt32(),V(r),V(r),V(r),r.ReadSingle(),r.ReadSingle(),r.ReadBoolean(),r.ReadBoolean(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadBoolean(),r.ReadBoolean(),r.ReadBoolean(),V(r));if(!Finite(p.GlueOffset)||!Finite(p.Position)||!Finite(p.Velocity)||!Finite(p.Impulse)||!float.IsFinite(p.Yaw)||!float.IsFinite(p.Pitch)||!float.IsFinite(p.FreezeUntil)||!float.IsFinite(p.GlueUntil)||!float.IsFinite(p.DownUntil))return null;f.Players[i]=p;}
            f.Head=new(0,V(r),V(r));if(!Finite(f.Head.Position)||!Finite(f.Head.Rotation))return null;
            count=r.ReadInt32();if(count<0||count>MaxProps)return null;f.Props=new MovingPose[count];
            for(int i=0;i<count;i++){f.Props[i]=new(r.ReadInt32(),V(r),V(r));if(!Finite(f.Props[i].Position)||!Finite(f.Props[i].Rotation))return null;}
            f.Attention=(AttentionStage)r.ReadByte();f.AttentionSource=V(r);f.AttentionStarted=r.ReadSingle();f.MirrorBlocked=r.ReadBoolean();f.Bracing=r.ReadBoolean();f.Flight=(FlightStage)r.ReadByte();f.StableSeconds=r.ReadSingle();f.LandingIssues=(LandingIssue)r.ReadInt32();f.Leave=(LeaveStage)r.ReadByte();f.MatchNumber=r.ReadInt32();f.ExpressionKind=r.ReadInt32();f.ExpressionAt=r.ReadSingle();
            if(!float.IsFinite(f.ExpressionAt)||f.ExpressionKind<0||f.ExpressionKind>2||!Finite(f.AttentionSource)||!float.IsFinite(f.AttentionStarted)||!float.IsFinite(f.StableSeconds)||!Enum.IsDefined(f.Attention)||!Enum.IsDefined(f.Flight)||!Enum.IsDefined(f.Leave))return null;
            return r.BaseStream.Position==data.Length?f:null;
        }catch(EndOfStreamException){return null;}
    }
}

// Redundant input bundles are deduplicated. Catch-up is bounded; a stalled peer
// cannot queue seconds of movement or make authority simulate arbitrary dt.
public sealed class InputTimeline
{
    readonly SortedDictionary<int,InputTick> queued=new();
    float credit;
    public int Ack {get;private set;}
    public int Count=>queued.Count;
    public void Add(IEnumerable<InputTick> ticks)
    {
        var incoming=ticks.OrderBy(t=>t.Sequence).ToArray();
        if(incoming.Length>0&&(long)incoming[0].Sequence>Ack+240L){queued.Clear();Ack=incoming[0].Sequence-1;}
        foreach(var t in incoming)if(t.Sequence>Ack&&(long)t.Sequence<=Ack+240L&&queued.Count<120)queued.TryAdd(t.Sequence,t);
    }
    public InputTick[] Consume(int budget=3)
    {
        if(queued.Count==0||budget<1)return [];
        int count=Math.Min(budget,queued.Count>6?Math.Min(3,queued.Count-3):1);
        // Session evaluates tool/interact edges once per host tick. Never swallow
        // a press/release transition inside a movement catch-up batch.
        var first=queued.Values.First();var result=queued.Values.Take(count).TakeWhile(t=>t.Buttons==first.Buttons).ToArray();
        foreach(var tick in result){queued.Remove(tick.Sequence);Ack=tick.Sequence;}return result;
    }
    public InputTick[] ForTick(float dt)
    {credit=Math.Min(12,credit+dt/MotionWire.Step);var ticks=Consume((int)credit);credit-=ticks.Length;return ticks;}
}

// Monotonic server-time playout; a late packet cannot reverse presentation time.
public sealed class MotionBuffer
{
    readonly List<MotionFrame> frames=new();
    float time,arrival,interval=.05f,jitter;
    public float Delay=>Math.Clamp(.10f+jitter*3,.10f,.28f);
    public int LatestSequence=>frames.Count==0?0:frames[^1].Sequence;
    public int Underruns {get;private set;}
    public void Clear(){frames.Clear();time=arrival=jitter=0;interval=.05f;Underruns=0;}
    public bool Add(MotionFrame frame,float now)
    {
        if(frame.Sequence<=LatestSequence)return false;
        if(frames.Count>0){float gap=now-arrival;jitter+=(Math.Abs(gap-interval)-jitter)*.1f;interval+=(gap-interval)*.02f;}
        if(frames.Count==0||frame.Round!=frames[^1].Round){frames.Clear();time=frame.Time-Delay;}
        frames.Add(frame);if(frames.Count>32)frames.RemoveAt(0);arrival=now;return true;
    }
    public (MotionFrame A,MotionFrame B,float Blend)? Advance(float dt)
    {
        if(frames.Count==0)return null;float latest=frames[^1].Time,target=latest-Delay;
        float rate=Math.Clamp(1+(target-time)*2,.85f,1.15f);time=Math.Min(latest,time+Math.Max(0,dt)*rate);
        if(time>=latest)Underruns++;
        while(frames.Count>2&&frames[1].Time<=time)frames.RemoveAt(0);
        var a=frames[0];var b=frames.Count>1?frames[1]:a;
        float blend=b.Time>a.Time?Math.Clamp((time-a.Time)/(b.Time-a.Time),0,1):0;return(a,b,blend);
    }
}
