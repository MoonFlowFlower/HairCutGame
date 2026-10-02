using Hairball.Core;
using System.Numerics;
public static class ImpactTests
{
    public static void Run(Action<string,Action> test,Action<bool,string> check)
    {
        void Check(bool value,string message="impact assertion")=>check(value,message);
        void Near(float a,float b)=>Check(Math.Abs(a-b)<.02f,$"{a} != {b}");
        ImpactLedger Ledger(float start=50){var l=new ImpactLedger();l.Begin(1,start);return l;}
        Session Session(){var s=new Session();s.AddPlayer(1,"A");s.AddPlayer(2,"B");s.StartMatch();s.State.Phase=Phase.Build;s.State.Remaining=75;return s;}
        void Aim(PlayerState p,Vector3 target){var d=Vector3.Normalize(target-Hairball.Core.Session.Eye(p));p.Yaw=MathF.Atan2(-d.X,-d.Z);p.Pitch=MathF.Asin(d.Y);}
        void AtButton(Session s,int actor,int option){var p=s.State.Player(actor)!;p.Position=new(3,0,-3.5f);Aim(p,SpecialOrders.Button(option));}
        test("ledger coalesces continuous samples and records net delta, not action count",()=>{
            var l=Ledger();l.Observe(1,1,"0",50,53,0,50);l.Observe(1,1,"0",53,57,.15f,50);l.Observe(1,1,"0",57,55,.3f,50);l.Advance(3);
            Check(l.Events.Count==1&&l.Events[0].Samples==3);Near(l.For(1).Positive,5);Near(l.For(1).Negative,0);Near(l.For(1).Surviving,5);
        });
        test("ledger interleaving never credits a teammate's quality change",()=>{
            var l=Ledger();l.Observe(1,1,"0",50,55,0,50);l.Observe(2,1,"0",55,59,.1f,50);l.Observe(1,1,"0",59,61,.2f,50);l.Advance(3);
            Near(l.For(1).Positive,7);Near(l.For(2).Positive,4);Near(l.For(1).Surviving+l.For(2).Surviving,11);
        });
        test("ledger epsilon ignores tiny windows and target/tool changes close windows",()=>{
            var l=Ledger();l.Observe(1,0,"0",50,50.05f,0,50);l.Close(1);Check(l.Events.Count==0);
            l.Observe(1,0,"0",50,53,1,50);l.Observe(1,0,"barber",53,54,1.1f,50);l.Observe(1,1,"barber",54,56,1.2f,50);l.Advance(4);Check(l.Events.Count==3);
        });
        test("crisis accumulates a short drop and permits unattributed environmental cause",()=>{
            var l=Ledger(80);l.Observe(0,-1,"wind",80,74,0,50);Check(l.Crises.Count==0);l.Observe(0,-1,"wind",74,67,.2f,50);
            Check(l.Crises.Count==1&&l.Crises[0].PrimaryCause==0);Near(l.Crises[0].Baseline,80);l.Advance(10);Check(l.Crises[0].Expired);
        });
        test("recovery splits measured gains between helpers and caps at old baseline",()=>{
            var l=Ledger(80);l.Observe(1,9,"0",80,60,0,50,discrete:true);l.Observe(2,4,"0",60,70,1,50,discrete:true);l.Observe(3,4,"0",70,90,2,50,discrete:true);l.Advance(5);
            Near(l.For(2).Rescue,10);Near(l.For(3).Rescue,10);Near(l.For(3).Surviving,10);Check(l.Crises[0].Resolved);
        });
        test("self-created crisis and repeated destroy/restore never farm rescue or contributor",()=>{
            var l=Ledger(80);l.Observe(1,9,"0",80,60,0,50,discrete:true);l.Observe(1,1,"0",60,80,1,50,discrete:true);l.Advance(4);
            Near(l.For(1).Rescue,0);Near(l.For(1).Surviving,0);
            l.Observe(1,9,"0",80,60,5,50,discrete:true);l.Observe(2,4,"0",60,80,6,50,discrete:true);l.Observe(1,9,"0",80,60,9,50,discrete:true);l.Observe(2,4,"0",60,80,10,50,discrete:true);l.Advance(13);Near(l.For(2).Rescue,20);
        });
        test("mixed-cause rescue excludes actor's own caused portion",()=>{
            var l=Ledger(80);l.Observe(1,9,"0",80,70,0,50,discrete:true);l.Observe(2,9,"0",70,50,.2f,50,discrete:true);l.Observe(1,1,"0",50,80,1,50,discrete:true);l.Advance(4);Near(l.For(1).Rescue,20);
        });
        test("positive impact is provisional and destructive replacement reduces surviving credit",()=>{
            var l=Ledger(30);l.Observe(1,1,"0",30,50,0,50,discrete:true);l.Advance(1);Near(l.For(1).Surviving,0);l.Advance(2.1f);Near(l.For(1).Surviving,20);
            l.Observe(2,0,"0",50,35,2.2f,50,discrete:true);l.Advance(5);Near(l.For(1).Surviving,5);Near(l.For(2).Negative,15);
        });
        test("late urgency changes ledger highlight rank only",()=>{
            var e=new ImpactEvent{Rescue=10};float normal=ImpactLedger.Rank(e);e.Context=ImpactContext.Late;Check(ImpactLedger.Rank(e)>normal);var j=new SharedJob{Result=new(){Shape=70,Function=100}};j.Settle();Near(j.Wallet,340);
        });
        test("three spotlights and deterministic ties never change team payout",()=>{
            var l=Ledger(30);l.Observe(1,1,"0",30,40,0,50,discrete:true);l.Observe(2,1,"0",40,50,1,50,discrete:true);l.Observe(3,7,"body",50,50,2,50,severity:12,discrete:true);l.Observe(4,1,"order",50,50,3,50,money:-35);l.Advance(6);
            var players=Enumerable.Range(1,4).Select(i=>new PlayerState{Id=i,Slot=i-1,Name="P"+i}).ToArray();var awards=l.Awards(players,6);
            Check(awards.Count==3&&awards[0].Actor==1&&awards.Any(a=>a.Title=="Big Spender")&&awards.Any(a=>a.Title=="Biggest Accident"));
            Check(Wire.Decode<List<SpotlightAward>>(Wire.Encode(awards)).Select(a=>a.Title+a.Actor).SequenceEqual(awards.Select(a=>a.Title+a.Actor)));
        });
        test("ledger ranks and selects the expected historical replay window",()=>{
            var s=Session();var l=Ledger(80);var replay=new ReplayRecorder();
            for(int i=0;i<50;i++){s.State.Time=i*.1f;if(i==20)l.Observe(1,9,"0",80,50,2,50,severity:12,discrete:true);if(i==30)l.Observe(2,4,"0",50,80,3,10,discrete:true);l.Advance(s.State.Time);replay.Record(s.State,.1f,l);}
            replay.Select(s.State);Check(s.State.HighlightTitle.Contains("B")&&s.State.HighlightTitle.Contains("rescue")&&replay.Best.Any(f=>Math.Abs(f.Time-3)<.01f));
        });
        test("live helipad health agrees with actual viable and empty landing checks",()=>{
            var s=Session();GoalFixtures.Build(s.State.SharedHead,0);var q=new GoalQuality();Check(q.Read(s.State).Viable);Near(q.Read(s.State).Value,100);
            s.State.SharedHead.Volume.Fill(_=>-1);Check(!q.Read(s.State).Viable);Near(q.Read(s.State).Value,0);
        });
        test("live other-goal shape reuses final evaluator without mutating world",()=>{
            var s=Session();for(int goal=1;goal<8;goal++){s.State.Job.Goal=goal;GoalFixtures.Build(s.State.SharedHead,goal);var before=Wire.Encode(s.State);float value=new GoalQuality().Read(s.State).Value;var weights=GoalMaterials.Weights(goal);Near(value,Validation.Shape(s.State)*weights.Shape+GoalMaterials.Evaluate(s.State.SharedHead,goal)*weights.State+PhysicalProps.Readiness(s.State)*weights.Function);Check(before.SequenceEqual(Wire.Encode(s.State)));}
        });
        test("host order pending, teammate cancel and atomic commit deliver one physical tool",()=>{
            var s=Session();AtButton(s,1,0);AtButton(s,2,1);Check(s.RequestOrder(1,0));Check(!s.RequestOrder(2,1));Near(s.State.Job.Wallet,200);AtButton(s,2,2);Check(s.CancelOrder(2));s.Tick(3);Near(s.State.Job.Wallet,200);
            AtButton(s,1,1);Check(s.RequestOrder(1,1));s.Tick(2.6f);Near(s.State.Job.Wallet,175);Check(s.State.Tools.Count(t=>t.Special>0)==1&&s.State.Order.Delivered==1);s.Tick(1);Near(s.State.Job.Wallet,175);Near(s.Ledger.For(1).Spend,25);
        });
        test("order rejects remote proximity spoof, insufficient money, timeout and disconnect",()=>{
            var s=Session();Check(!s.RequestOrder(1,0)&&!s.RequestOrder(999,0));AtButton(s,1,0);s.State.Job.Wallet=34;Check(!s.RequestOrder(1,0));s.State.Job.Wallet=200;Check(s.RequestOrder(1,0));s.State.Remaining=.1f;s.Tick(3);Near(s.State.Job.Wallet,200);Check(!s.State.Order.Pending);
            s.State.Phase=Phase.Build;s.State.Remaining=75;s.Tick(1);Check(s.RequestOrder(1,0));s.RemovePlayer(1);s.Tick(3);Near(s.State.Job.Wallet,200);
        });
        test("core copies stay free and specials produce one real generic-effect burst",()=>{
            var s=Session();for(int id=0;id<5;id++)Check(s.State.Tools.Count(t=>t.Definition==id&&t.Charge==-1)==4);
            foreach(int special in new[]{1,2}){var p=s.State.Player(1)!;p.Position=Hairball.Core.Session.Spawn(0);Aim(p,s.State.SharedHead.Position+new Vector3(0,.45f,0));var t=new ToolState{Id=900+special,Special=special,Definition=special==1?1:5,Charge=1,Holder=1};s.State.Tools.Add(t);p.Held=t.Id;p.Cooldown=0;float mass=s.State.SharedHead.Mass;s.UseTool(p,false);Check(t.Charge==0);if(special==1)Check(s.State.SharedHead.Mass>mass);else Check(s.State.SharedHead.Patches.Any(p=>p.Frozen));}
        });
        test("spatial live-score index matches brute-force coverage for all goals",()=>{
            var random=new Random(492);foreach(var goal in Goals.All){var points=goal.TargetSamples.Where((_,i)=>i%3==0).Select(v=>v+new Vector3((float)random.NextDouble()-.5f,(float)random.NextDouble()-.5f,(float)random.NextDouble()-.5f)*.7f).Concat(new[]{new Vector3(-.24f,0,.24f),new Vector3(4,4,4)}).ToArray();
                float precision=(float)points.Count(p=>goal.Target.Contains(p,.15f))/points.Length;float coverage=(float)goal.TargetSamples.Count(t=>points.Any(p=>Vector3.DistanceSquared(p,t)<.24f*.24f))/goal.TargetSamples.Length;
                Near(Scoring.Shape(points,goal),precision+coverage<.0001f?0:200*precision*coverage/(precision+coverage));}
        });
        test("late loss of viability can open a small-drop crisis and ledger history stays bounded",()=>{
            var l=Ledger(80);l.Observe(0,-1,"support",80,79,0,10,criticalLost:true,discrete:true);Check(l.Crises.Count==1&&l.Crises[0].PrimaryCause==0);
            l.Observe(2,4,"support",79,80,1,9,discrete:true);l.Advance(4);Near(l.For(2).Rescue,1);
            for(int i=0;i<2400;i++)l.Observe(1,3,"body",80,80,i+5,50,targets:2,discrete:true);
            Check(l.Events.Count<=2048&&l.Crises.Count<=128&&l.Events.All(e=>e.Closed));
        });
        test("order rechecks funds on commit without debt or phantom delivery",()=>{
            var s=Session();AtButton(s,1,0);Check(s.RequestOrder(1,0));s.State.Job.Wallet=20;s.Tick(3);
            Check(!s.State.Order.Pending&&s.State.Order.Delivered==0&&s.State.Order.Spent==0&&s.State.Job.Wallet==20);
        });
        test("severe body incident retains its known cause even without hair-quality loss",()=>{
            var l=Ledger(80);var e=l.Observe(3,7,"body",80,80,1,50,severity:25,discrete:true)!;
            Check(l.Crises.Count==1&&l.Crises[0].PrimaryCause==3&&l.Crises[0].CauseEvents.Contains(e.Id));
            l.Observe(3,1,"head",80,90,2,50,discrete:true);l.Advance(5);Near(l.For(3).Rescue,0);Near(l.For(3).Surviving,10);
        });
    }
}
