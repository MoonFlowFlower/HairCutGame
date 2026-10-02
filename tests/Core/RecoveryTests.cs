using Hairball.Core;
using System.Security.Cryptography;
internal static class RecoveryTests
{
    public static void Run(Action<string,Action> test,Action<bool,string> check)
    {
        void C(bool b,string m="recovery assertion")=>check(b,m);
        test("recovery pause deadline cannot be extended by a second disconnect",()=>{var w=new RecoveryWindow();C(w.Begin(50));C(w.Paused);C(!w.Begin(60));C(w.Deadline==80);w.Continue();C(!w.Paused&&w.Open);C(w.Expired(80));w.Finish(80);w.Begin(85);C(!w.Paused);w.Finish(86);w.Begin(100);C(w.Paused);});
        test("recovery chunk flow bounded and loss of chunk ack repaired by cumulative receipt",()=>{
            byte[] bytes=RandomNumberGenerator.GetBytes(21544);var s=new ChunkSend(1,0,20,bytes);var r=new ChunkReceive(1,0,20,bytes.Length,s.Hash);
            foreach(var c in s.Pump()){C(c.Data.Length<=900);r.Add(c.Index,c.Data);}C(s.Outstanding<=32);C(r.Complete);C(r.Finish().SequenceEqual(bytes));
            foreach(int n in r.AckWindow())if(n<0)s.AckPrefix(-n-1);else s.Ack(n);C(s.Received);
        });
        test("recovery large transfer window stays at 32 under reordering",()=>{
            byte[] data=RandomNumberGenerator.GetBytes(800000);var s=new ChunkSend(2,1,12,data);var r=new ChunkReceive(2,1,12,data.Length,s.Hash);int loops=0;
            while(!s.Received&&loops++<200){var parts=s.Pump().Reverse().ToArray();C(s.Outstanding<=32);foreach(var c in parts){r.Add(c.Index,c.Data);C(!r.Add(c.Index,c.Data));}foreach(int n in r.AckWindow())if(n<0)s.AckPrefix(-n-1);else s.Ack(n);}
            C(r.Complete&&s.Received);C(r.Finish().SequenceEqual(data));
        });
        test("recovery invalid transfer metadata and corrupt body never apply",()=>{
            int rejected=0;try{new ChunkReceive(1,0,0,int.MaxValue,new byte[32]);}catch(ArgumentException){rejected++;}
            var data=new byte[901];var s=new ChunkSend(1,0,1,data);var r=new ChunkReceive(1,0,1,data.Length,s.Hash);
            try{r.Add(8,new byte[1]);}catch(ArgumentException){rejected++;}
            try{r.Finish();}catch(ArgumentException){rejected++;}
            r.Add(0,new byte[900]);r.Add(1,new byte[]{1});try{r.Finish();}catch(ArgumentException){rejected++;}C(rejected==4);
        });
        test("resume attempt cannot replace a newer binding",()=>{var a=new RecoveryIdentity{Attempt=7};C(a.ResumeError("r","r",a.Token,1,30,false,6)=="stale_attempt");C(a.ResumeError("r","r",a.Token,1,30,false,7)=="stale_attempt");C(a.ResumeError("r","r",a.Token,1,30,false,8)=="");C(a.ResumeError("r","r",a.Token,1,30,true,8)=="duplicate");});
        test("recovery identity requires exact random credential",()=>{var a=new RecoveryIdentity{Actor=2};var b=new RecoveryIdentity{Actor=3};C(a.Matches(a.Token));C(!a.Matches(b.Token));C(!a.Matches(""));C(a.Token.Length==64);});
        test("recovery rejects occupied identity wrong room and expired reservation",()=>{
            var a=new RecoveryIdentity{Actor=2};
            C(a.ResumeError("room","room",a.Token,20,40,false)=="");
            C(a.ResumeError("room","room",a.Token,20,40,true)=="duplicate");
            C(a.ResumeError("room","old",a.Token,20,40,false)=="expired");
            C(a.ResumeError("room","room",a.Token,40,40,false)=="expired");
        });
        test("recovery missing chunks and lost acks recover independently without reliable ordering",()=>{
            var data=RandomNumberGenerator.GetBytes(100000);var s=new ChunkSend(1,0,1,data);var r=new ChunkReceive(1,0,1,data.Length,s.Hash);var rng=new Random(729);
            for(int tick=0;tick<600&&!s.Received;tick++){
                foreach(var c in s.Pump(8,tick*.05,.35).Reverse()){
                    if(tick<30&&c.Index==2||rng.NextDouble()<.15)continue;
                    r.Add(c.Index,c.Data);if(rng.NextDouble()>.15)s.Ack(c.Index);
                }
                foreach(int n in r.AckWindow())if(n<0)s.AckPrefix(-n-1);else s.Ack(n);
                C(s.Outstanding<=32);
            }
            C(s.Received&&r.Complete);C(r.Finish().SequenceEqual(data));
        });
        test("network away presence preserves serialized stable identity",()=>{var s=new Session();var p=s.AddPlayer(2,"Friend")!;p.NetworkAway=true;var clone=Wire.Decode<WorldState>(Wire.Encode(s.State));C(clone.Player(2)!.NetworkAway&&clone.Player(2)!.Slot==p.Slot);});
    }
}
