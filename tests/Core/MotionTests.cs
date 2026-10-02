using Hairball.Core;
using System.Numerics;
internal static class MotionTests
{
    public static void Run(Action<string,Action> test,Action<bool,string> check)
    {
        void C(bool b,string m="motion assertion")=>check(b,m);
        test("motion compact packets preserve motor state below datagram budget",()=>{
            var f=new MotionFrame{Sequence=42,Round=1,Time=5,Head=new(0,new(1,2,3),new(.1f)),Players=Enumerable.Range(1,4).Select(i=>new PlayerMotion(i,30,new(i,1,0),new(2,0,1),new(.2f),.3f,.4f,true,true)).ToArray(),Props=Enumerable.Range(0,16).Select(i=>new MovingPose(i,new(i),new(.2f))).ToArray()};
            f.Attention=AttentionStage.Commit;f.AttentionSource=new(1,2,3);f.AttentionStarted=4;f.MirrorBlocked=f.Bracing=true;f.Flight=FlightStage.Contact;f.StableSeconds=1.2f;f.LandingIssues=LandingIssue.Soft;
            var bytes=MotionWire.Encode(f);C(bytes.Length<1200);var copy=MotionWire.Decode(bytes)!;C(copy.Players.SequenceEqual(f.Players)&&copy.Props.SequenceEqual(f.Props)&&copy.Head==f.Head);
            C(copy.Attention==f.Attention&&copy.AttentionSource==f.AttentionSource&&copy.AttentionStarted==4&&copy.MirrorBlocked&&copy.Bracing&&copy.Flight==f.Flight&&copy.StableSeconds==1.2f&&copy.LandingIssues==LandingIssue.Soft);
            C(MotionWire.Decode(bytes[..^1])==null);C(MotionWire.Decode(new byte[1300])==null);
        });
        test("motion input bundles reject invalid and bound repeated input histories",()=>{
            var ticks=Enumerable.Range(1,30).Select(i=>new InputTick(i,new(1,0),.2f,.1f,Buttons.Primary));
            var copy=MotionWire.ReadInputs(MotionWire.Inputs(ticks));C(copy.Length==12&&copy[0].Sequence==19&&copy[^1].Sequence==30);
            C(MotionWire.ReadInputs(MotionWire.Inputs([new(1,new(float.NaN,0),0,0,0)])).Length==0);
            C(MotionWire.ReadInputs([1,2,3]).Length==0);
        });
        test("motion redundant inputs never move authority twice and resume after outage",()=>{
            var q=new InputTimeline();var ticks=Enumerable.Range(1,12).Select(i=>new InputTick(i,Vector2.UnitX,0,0,0)).ToArray();q.Add(ticks);q.Add(ticks);C(q.Count==12);
            var consumed=new List<int>();while(q.Count>0){var chunk=q.Consume();C(chunk.Length<=3);consumed.AddRange(chunk.Select(t=>t.Sequence));}C(consumed.SequenceEqual(Enumerable.Range(1,12)));
            q.Add(ticks);C(q.Count==0);q.Add([new(500,Vector2.Zero,0,0,0)]);C(q.Consume().Single().Sequence==500);C(q.Ack==500);
        });
        test("motion jitter buffer discards old frames and never rewinds playback",()=>{
            var b=new MotionBuffer();float last=-1;int seq=0;
            for(int tick=0;tick<600;tick++){
                float now=tick/60f;if(tick%3==0&&tick%21!=0){seq++;C(b.Add(new(){Sequence=seq,Time=now,Round=1},now));C(!b.Add(new(){Sequence=seq-1,Time=now-1,Round=1},now));}
                if(b.Advance(1/60f) is {} s){float at=s.A.Time+(s.B.Time-s.A.Time)*s.Blend;C(at>=last-.0001f,"playback rewound");C(at<=now+.0001f);last=at;}
            }C(b.Delay>=.1f&&b.Delay<=.28f);b.Clear();C(b.Advance(.1f)==null);
        });
        test("motion authority catchup cannot exceed elapsed movement budget",()=>{
            var q=new InputTimeline();q.Add(Enumerable.Range(1,120).Select(i=>new InputTick(i,Vector2.UnitX,0,0,0)));int total=0;
            for(int i=0;i<60;i++){total+=q.ForTick(MotionWire.Step).Length;C(total<=i+1,"client injected extra simulation time");}
            C(total==60);
        });
        test("motion catchup preserves short interact press and release",()=>{
            var q=new InputTimeline();q.Add(Enumerable.Range(1,12).Select(i=>new InputTick(i,Vector2.Zero,0,0,i==2?Buttons.Interact:Buttons.None)));
            C(q.Consume().Single().Sequence==1);C(q.Consume().Single().Buttons==Buttons.Interact);C(q.Consume().All(t=>t.Buttons==Buttons.None));
        });
        test("large world wire reads both formats and reduces repeated hair payload",()=>{
            bool original=Wire.LegacyCompression;
            try{
                var session=new Session();for(int i=1;i<=4;i++)session.AddPlayer(i,"Peer"+i);session.StartMatch();
                Wire.LegacyCompression=true;var old=Wire.Encode(session.State);Wire.LegacyCompression=false;var compact=Wire.Encode(session.State);
                C(compact.Length<old.Length*.65f,"repeated hair should fit the larger compression window");
                foreach(var bytes in new[]{old,compact}){var copy=Wire.Decode<WorldState>(bytes);C(copy.Heads.Count==session.State.Heads.Count);foreach(var h in copy.Heads)C(h.Volume.Data.SequenceEqual(session.State.Heads.Single(x=>x.Id==h.Id).Volume.Data));}
            }finally{Wire.LegacyCompression=original;}
        });
    }
}
