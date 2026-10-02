using Hairball.Core;
using System.Numerics;
public static class SharedTests
{
    public static void Run(Action<string,Action> test,Action<bool,string> check)
    {
        void Check(bool b,string why="shared assertion")=>check(b,why);
        Session New(int count=1){var s=new Session();for(int i=1;i<=count;i++)s.AddPlayer(i,"Barber "+i);s.StartMatch();return s;}
        void Build(Session s){s.State.Phase=Phase.Build;s.State.Remaining=75;}
        void Equip(Session s,int player,int tool){var p=s.State.Player(player)!;s.Drop(p);p.Held=tool;s.State.Tools.Single(t=>t.Id==tool).Holder=player;p.Cooldown=0;}
        test("shared lifecycle creates one customer for four peers and preserves edits on late join/disconnect",()=>{
            var s=New(3);Build(s);s.State.SharedHead.Patches[0].Glue=.83f;var before=s.State.SharedHead.Volume.Data.ToArray();s.AddPlayer(4,"Late");s.RemovePlayer(2);
            Check(s.State.Customers.Count==1&&s.State.SharedHead.Owner==0&&s.State.Heads.Count(h=>!h.Barber&&!h.Loose&&!h.Facial)==1);
            Check(s.State.SharedHead.Patches[0].Glue==.83f&&before.SequenceEqual(s.State.SharedHead.Volume.Data)&&!s.State.SharedHead.Locked);
        });
        test("shared vote validates choices, counts active votes, resolves ties deterministically",()=>{
            var s=New(4);s.State.Phase=Phase.Choice;s.Choose(1,1);s.Choose(2,1);s.Choose(3,5);s.Choose(4,999);
            Check(s.State.Player(4)!.Vote==-1);s.State.Job.Resolve(s.State.Players,4);Check(s.State.Job.Goal==1);
            s.RemovePlayer(2);s.State.Job.Resolve(s.State.Players,99);int tie=s.State.Job.Goal;s.State.Job.Resolve(s.State.Players,99);Check(s.State.Job.Goal==tie&&new[]{1,5}.Contains(tie));
        });
        test("entrance vote preview lead to 75 seconds and E cannot individually cash out",()=>{
            var s=New();Check(s.State.Phase==Phase.Arrival);s.Tick(5.01f);Check(s.State.Phase==Phase.Choice);s.Choose(1,0);s.Tick(6.01f);Check(s.State.Phase==Phase.Preview);s.Tick(3.01f);
            Check(s.State.Phase==Phase.Build&&s.State.Remaining==75);s.Inputs[1]=(Vector2.Zero,0,0,Buttons.Interact);s.Tick(1);Check(s.State.Remaining==74&&!s.State.SharedHead.Locked);
            s.Tick(73.9f);Check(s.State.Phase==Phase.Build);s.Tick(.11f);Check(s.State.Phase==Phase.Validation&&s.State.SharedHead.Locked);
        });
        test("shared wallet settles once, persists across jobs, and no per-player score participates",()=>{
            var job=new SharedJob{Penalty=30,Result=new(){Shape=80,Function=100}};job.Settle();Check(job.Delta==120&&job.Wallet==320);job.Settle();Check(job.Wallet==320);job.Begin(1,2);Check(job.Wallet==320&&job.Penalty==0&&!job.Settled);
        });
        test("three shared jobs preserve barber material and complete with one wallet",()=>{
            var s=New(2);s.State.Barber(1).Patches[0].Glue=.78f;
            for(int r=1;r<=3;r++){s.Tick(5.1f);s.Tick(6.1f);s.Tick(3.1f);Check(s.State.Phase==Phase.Build);s.Tick(75.1f);s.Tick(9.1f);Check(s.State.Phase==Phase.Results&&s.State.Job.Settled);s.Tick(7.1f);s.Tick(6.1f);}
            Check(s.State.Phase==Phase.Complete&&s.State.Round==3&&s.State.Job.Wallet>200&&s.State.Barber(1).Patches[0].Glue==.78f);
        });
        test("panic has anticipation, real displacement, deterministic pose, recovery and cooldown",()=>{
            var s=New();Build(s);var c=s.State.Customers[0];s.Stimulate(.6f,1,CustomerStimulus.Noise);s.Tick(.01f);var before=s.State.SharedHead.Position;
            Check(c.Action==CustomerReaction.Duck&&CustomerMotion.Anticipating(c,s.State.Time));s.Tick(.45f);Check(s.State.SharedHead.Position==before);s.Tick(.4f);Check(s.State.SharedHead.Position.Y<before.Y);
            var copy=Wire.Decode<WorldState>(Wire.Encode(s.State));CustomerMotion.Apply(copy);Check(copy.SharedHead.Position==s.State.SharedHead.Position&&copy.SharedHead.Rotation==s.State.SharedHead.Rotation);
            s.Tick(1.2f);Check(c.Action==CustomerReaction.None&&c.ReactionCount==1&&c.ReactionCooldown>0);s.Tick(.5f);Check(c.ReactionCount==1&&c.Panic<.6f);
            s.Tick(4.2f);Check(c.Action==CustomerReaction.Turn);
        });
        test("noise heat and wind tool actions actually stimulate customer panic",()=>{
            foreach(int id in new[]{7,8,3}){var s=New();Build(s);Equip(s,1,id);float before=s.State.Customers[0].Panic;s.UseTool(s.State.Player(1)!,false);Check(s.State.Customers[0].Panic>before,"stimulus tool "+id);}
        });
        test("blower and suction move another player's body and preserve host ownership rules",()=>{
            var s=New(2);Build(s);var a=s.State.Player(1)!;var b=s.State.Player(2)!;b.Position=new(0,0,1.2f);s.Tick(.01f);Equip(s,1,3);s.UseTool(a,false);Check(b.Impulse.Z<0);
            b.Impulse=default;b.ImpactCooldown=0;Equip(s,1,2);s.UseTool(a,false);Check(b.Impulse.Z>0);
            b.Held=3;b.Cooldown=0;float charge=s.State.Tools[3].Charge;s.UseTool(b,false);Check(s.State.Tools[3].Holder!=b.Id&&s.State.Tools[3].Charge==charge);
        });
        test("held trimmer spatial extent shoves and drops a nearby teammate's tool",()=>{
            var s=New(2);Build(s);var p=s.State.Player(2)!;p.Position=new(0,0,1.8f);Equip(s,1,9);Equip(s,2,1);s.Tick(.02f);
            Check(p.Held==-1&&p.Impulse.Length()>0&&s.State.Job.Penalty==8&&s.State.Events.Any(e=>e.Source==1&&e.Text.Contains("Tool impact")));
        });
        test("two players reshape the same authoritative head without protection ownership",()=>{
            var s=New(2);Build(s);var a=s.State.Player(1)!;var b=s.State.Player(2)!;b.Position=a.Position;b.Yaw=0;b.Pitch=.1f;a.Pitch=.1f;Equip(s,1,1);Equip(s,2,11);
            var h=s.State.SharedHead;float mass=h.Mass;s.UseTool(a,false);float first=h.Mass;s.UseTool(b,false);Check(first>mass&&h.Mass>first&&a.Added>0&&b.Added>0);
        });
        test("helipad detects area and flatness and landing requires a real stable dwell",()=>{
            var h=Head.Create(0,0);GoalFixtures.Build(h,0);var pad=Helipad.Measure(h);Check(pad.Stable&&pad.Quality>90);
            var prop=new PropState{Goal=0};for(int i=0;i<90;i++)Helipad.Tick(prop,h,.1f,i*.1f);Check(!prop.Failed&&prop.HeldTime>2);
            h.Volume.Fill(_=>-1);Check(!Helipad.Measure(h).Stable);prop=new(){Goal=0};Helipad.Tick(prop,h,.1f,4);Check(prop.Failed);
        });
        test("rotor approach disturbs real flexible material before timer expires",()=>{
            var s=New();Build(s);s.State.Remaining=14;var h=s.State.SharedHead;var before=h.Volume.Data.ToArray();s.Tick(.3f);Check(s.State.Customers[0].ValidatorWarned&&!h.Volume.Data.SequenceEqual(before)&&s.State.Phase==Phase.Build);
        });
        test("customer reaction snapshot and replay copy do not alias live parameters",()=>{
            var s=New();Build(s);s.Stimulate(.6f,1,CustomerStimulus.Impact);s.Tick(.12f);var recorded=s.Replay.Rolling.Last().Customers[0];s.State.Customers[0].Panic=0;Check(recorded.Panic>.5f&&recorded.Action!=CustomerReaction.None);
        });
        test("llama reuses solid material pipeline and telegraphs bounded directional spit",()=>{
            var s=New(2);s.NextRound();Build(s);var c=s.State.Customers[0];Check(c.Profile=="llama"&&s.State.SharedHead.Material==HeadMaterialKind.Wool&&s.State.SharedHead.Clone().Material==HeadMaterialKind.Wool);
            s.Stimulate(.65f,1,CustomerStimulus.Noise);s.Tick(.01f);Check(c.Action==CustomerReaction.Spit&&s.State.Player(1)!.Obscured==0);
            s.Tick(.66f);Check(s.State.Player(1)!.Obscured>0&&s.State.Player(2)!.Obscured==0&&c.SignatureUsed);
            var copied=Wire.Decode<WorldState>(Wire.Encode(s.State));Check(copied.Customers[0].SignatureDirection==c.SignatureDirection&&copied.SharedHead.Material==HeadMaterialKind.Wool);
            s.Tick(1.5f);Check(s.State.Player(1)!.Obscured==0&&c.ReactionCount==1);
            Equip(s,1,0);float mass=s.State.SharedHead.Mass;s.State.Player(1)!.Pitch=.1f;s.UseTool(s.State.Player(1)!,false);Check(s.State.SharedHead.Mass<mass);
        });
    }
}
