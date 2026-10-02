using System;
using System.Collections.Generic;
using System.Linq;
using System.Buffers.Binary;
using Concentus;
using Concentus.Enums;

namespace Hairball.Core;

public enum VoiceKind : byte { Opus, Features }
public enum VoiceSpecies : byte { Human, Bird }
public readonly record struct VoiceFeature(float Loudness,float Pitch,bool Voiced,bool Onset)
{
    public byte[] Pack()=>[(byte)Math.Clamp((int)(Loudness*255),0,255),(byte)Math.Clamp((int)((Pitch-60)/740*255),0,255),(byte)((Voiced?1:0)|(Onset?2:0)),0];
    public static VoiceFeature Unpack(ReadOnlySpan<byte> b)=>new(b[0]/255f,60+b[1]/255f*740,(b[2]&1)!=0,(b[2]&2)!=0);
    public static VoiceFeature Extract(ReadOnlySpan<float> samples,bool wasSpeaking)
    {
        double power=0;foreach(float x in samples)power+=x*x;float rms=(float)Math.Sqrt(power/Math.Max(1,samples.Length));
        // Normalized autocorrelation; only pitch/envelope leave this machine.
        int bestLag=0;double best=.35;
        for(int lag=60;lag<=600;lag+=2)
        {
            double c=0,a=0,b=0;for(int i=0;i<samples.Length-lag;i+=4){c+=samples[i]*samples[i+lag];a+=samples[i]*samples[i];b+=samples[i+lag]*samples[i+lag];}
            double score=c/Math.Sqrt(Math.Max(1e-15,a*b));if(score>best){best=score;bestLag=lag;}
        }
        return new(Math.Clamp(rms*4,0,1),bestLag>0?48000f/bestLag:180,bestLag>0&&rms>.005f,!wasSpeaking&&rms>.008f);
    }
}
public sealed record VoicePacket(VoiceKind Kind,VoiceSpecies Species,uint Sequence,uint Timestamp,byte[] Payload)
{
    public const int Header=12,MaxPayload=160;
    public byte[] Encode()
    {
        var b=new byte[Header+Payload.Length];b[0]=1;b[1]=(byte)Kind;b[2]=(byte)Species;
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(4),Sequence);BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(8),Timestamp);Payload.CopyTo(b,Header);return b;
    }
    public static VoicePacket? Parse(byte[] b)
    {
        if(b.Length<=Header||b.Length>Header+MaxPayload||b[0]!=1||b[1]>1||b[2]>1||b[3]!=0||b[1]==1&&(b.Length!=Header+4||b[14]>3||b[15]!=0))return null;
        return new((VoiceKind)b[1],(VoiceSpecies)b[2],BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(4)),BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(8)),b[Header..]);
    }
}
public sealed class VoiceEncoder
{
    public const int Rate=48000,Samples=960;
    readonly IOpusEncoder encoder;
    uint sequence;bool speaking;float tail;int featureHalf;
    public long OpusPackets,FeaturePackets;
    public int Bitrate { get=>encoder.Bitrate;set=>encoder.Bitrate=value<=16000?16000:24000; }
    public VoiceEncoder()
    {
        OpusCodecFactory.AttemptToUseNativeLibrary=false;
        encoder=OpusCodecFactory.CreateEncoder(Rate,1,OpusApplication.OPUS_APPLICATION_VOIP);
        encoder.Bitrate=24000;encoder.UseInbandFEC=true;encoder.PacketLossPercent=2;encoder.UseVBR=false;encoder.Complexity=5;
    }
    public VoicePacket? Process(float[] mono,uint stamp,bool filtered,VoiceSpecies species,bool enabled,bool ptt,bool pushHeld,float gain=1,float gate=.008f)
    {
        if(mono.Length!=Samples)throw new ArgumentException("Voice frames must be 20 ms");
        if(!enabled||ptt&&!pushHeld){speaking=false;tail=0;return null;}
        var pcm=new float[Samples];double power=0;for(int i=0;i<Samples;i++){pcm[i]=Math.Clamp(float.IsFinite(mono[i])?mono[i]*Math.Clamp(gain,0,4):0,-1,1);power+=pcm[i]*pcm[i];}
        bool loud=Math.Sqrt(power/Samples)>=Math.Clamp(gate,.001f,.2f);tail=loud?.16f:Math.Max(0,tail-.02f);
        if(!loud&&tail<=0){speaking=false;return null;}
        // This branch precedes all codec calls. Filtered audio is never encoded.
        if(filtered)
        {
            if(featureHalf++%2!=0)return null;
            var feature=VoiceFeature.Extract(pcm,speaking);speaking=loud;FeaturePackets++;
            return new(VoiceKind.Features,species,++sequence,stamp,feature.Pack());
        }
        speaking=loud;var samples=new short[Samples];for(int i=0;i<Samples;i++)samples[i]=(short)(pcm[i]*32767);
        var output=new byte[VoicePacket.MaxPayload];int n=encoder.Encode(samples.AsSpan(),Samples,output.AsSpan(),output.Length);
        OpusPackets++;return new(VoiceKind.Opus,species,++sequence,stamp,output[..n]);
    }
}
public sealed class VoiceDecoder
{
    readonly IOpusDecoder decoder;
    public VoiceDecoder(){OpusCodecFactory.AttemptToUseNativeLibrary=false;decoder=OpusCodecFactory.CreateDecoder(48000,1);}
    public float[] Decode(byte[]? opus,bool fec=false)
    {
        var pcm=new short[960];decoder.Decode(opus==null?ReadOnlySpan<byte>.Empty:opus.AsSpan(),pcm.AsSpan(),960,fec);
        return pcm.Select(x=>x/32768f).ToArray();
    }
}
public static class SpeciesVoice
{
    public static float[] Synthesize(VoiceFeature feature,VoiceSpecies species,uint seed)
    {
        var samples=new float[1920];float pitch=Math.Clamp(feature.Pitch,80,500)*(species==VoiceSpecies.Bird?2.7f:1);
        double vowel=500+seed%4*230;
        for(int i=0;i<samples.Length;i++)
        {
            double t=i/48000.0,env=Math.Sin(Math.PI*(i+.5)/samples.Length)*(feature.Onset?1.15*Math.Exp(-t*7):.85);
            double wave=species==VoiceSpecies.Bird?Math.Sin(2*Math.PI*(pitch*t+4*t*t*200)):
                .65*Math.Sin(2*Math.PI*pitch*t)+.22*Math.Sin(2*Math.PI*vowel*t)+.13*Math.Sin(2*Math.PI*(vowel*2.1)*t);
            if(!feature.Voiced)wave=.45*Math.Sin(2*Math.PI*(1371+seed%7*31)*t)+.3*Math.Sin(2*Math.PI*2279*t);
            samples[i]=(float)(wave*env*feature.Loudness*.32);
        }
        return samples;
    }
}
public sealed class VoiceJitter
{
    readonly SortedDictionary<uint,VoicePacket> packets=new();
    readonly Queue<double> transits=new();
    uint next;double due,lastArrival,previousTransit,playoutBuffer;bool started;int empty;
    VoiceKind kind;
    public VoiceKind Kind=>kind;
    public double BufferSeconds { get;private set; }=.06;
    public double MaxBufferSeconds=.09;
    public double JitterSeconds { get;private set; }
    public long Received,Late,Lost,Played,CatchUp;
    public bool Add(VoicePacket p,double now)
    {
        if(started&&now-lastArrival>.35){packets.Clear();started=false;}
        if(started&&p.Kind!=kind){packets.Clear();started=false;}
        if(started&&p.Sequence<next){Late++;return false;}
        if(packets.ContainsKey(p.Sequence))return false;
        double transit=now-p.Timestamp/1000.0;
        if(p.Timestamp>0){transits.Enqueue(transit);while(transits.Count>100)transits.Dequeue();}
        if(Received>0)JitterSeconds+=(Math.Min(.25,Math.Abs(transit-previousTransit))-JitterSeconds)/16;
        previousTransit=transit;BufferSeconds=Math.Clamp(.06+JitterSeconds*1.25,.06,Math.Clamp(MaxBufferSeconds,.06,.12));lastArrival=now;Received++;
        if(!started){next=p.Sequence;kind=p.Kind;due=now+BufferSeconds;playoutBuffer=BufferSeconds;started=true;empty=0;}
        else if(BufferSeconds>playoutBuffer){due+=BufferSeconds-playoutBuffer;playoutBuffer=BufferSeconds;}
        if(p.Sequence>next+32)return false;
        packets[p.Sequence]=p;return true;
    }
    public bool Pop(double now,out VoicePacket? p,out VoicePacket? following)
    {
        p=null;following=null;if(!started)return false;
        // A delayed startup packet must not set a permanent speech backlog.
        // Estimate the route's lower transit bound from a sliding window, then
        // discard frames which have outlived that transit plus our jitter budget.
        // This also recovers after a stalled render frame without replaying old speech.
        if(transits.Count>=10&&packets.Count>0)
        {
            var sorted=transits.Order().ToArray();double budget=sorted[sorted.Length/10]+BufferSeconds+.02;
            uint resume=next;
            foreach(var entry in packets)
            {
                if(entry.Key<next||entry.Value.Timestamp==0)continue;
                if(now-entry.Value.Timestamp/1000d<=budget)break;
                resume=entry.Key+1;
            }
            if(resume>next){CatchUp+=resume-next;foreach(uint n in packets.Keys.TakeWhile(n=>n<resume).ToArray())packets.Remove(n);next=resume;due=now;empty=0;}
        }
        if(now<due)return false;
        packets.Remove(next,out p);packets.TryGetValue(next+1,out following);next++;due+=kind==VoiceKind.Features?.04:.02;
        if(p==null){Lost++;if(++empty>=6){started=false;packets.Clear();}}else{empty=0;Played++;}
        return true;
    }
}
public static class VoiceRoute
{
    public static int[] Recipients(IEnumerable<int> actors,int speaker)=>actors.Where(a=>a!=speaker).Distinct().ToArray();
    public static bool Allowed(VoicePacket p,bool filtered)=>!filtered||p.Kind==VoiceKind.Features;
}

// Injectable-source QA only. Goertzel avoids per-sample trigonometry and verifies
// that the expected speaker tone dominates the other injected identities.
public static class VoiceProbe
{
    public static double Power(float[] pcm,double frequency)
    {
        double coefficient=2*Math.Cos(2*Math.PI*frequency/48000),a=0,b=0;
        foreach(float sample in pcm){double next=sample+coefficient*a-b;b=a;a=next;}
        return Math.Max(0,a*a+b*b-coefficient*a*b);
    }
    public static bool Matches(float[] pcm,int actor,IEnumerable<int> actors)
    {double expected=Power(pcm,180+actor*70);return expected>25&&actors.Where(a=>a!=actor).All(a=>Power(pcm,180+a*70)<expected*.9);}
}
