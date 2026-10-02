using Hairball.Core;
using System.Numerics;
internal static class PartyBodyTests
{
    public static void Run(Action<string,Action> test,Action<bool,string> check)
    {
        void C(bool b,string m="p9 assertion")=>check(b,m);
        Session World(int count=4){var s=ExperimentTests.World();for(int i=2;i<=count;i++)s.AddPlayer(i,"P"+i);s.StartMatch();s.State.Phase=Phase.Build;s.State.Remaining=150;return s;}
        void Aim(Session s,PlayerState p,Vector3 point){var d=Vector3.Normalize(point-Session.Eye(p));p.Yaw=MathF.Atan2(-d.X,-d.Z);p.Pitch=MathF.Asin(d.Y);}
        void Hold(Session s,PlayerState p,CoopKind kind){var x=s.State.Props.Single(x=>x.Coop==kind);x.Holder=p.Id;p.CarriedProp=x.Id;}
        test("p9 down is opt-in, drops hand, look stays free, slap and immunity",()=>{
            var s=World(2);var p=s.State.Player(1)!;var a=s.State.Player(2)!;C(!s.Down(p,2));p.AllowDown=true;p.Held=1;s.State.Tools[1].Holder=p.Id;var pose=p.Position;C(s.Down(p,2)&&p.Held<0&&p.DownUntil==s.State.Time+4);C(PartyAccidents.Movement(p,Vector2.One,s.State.Time)==Vector2.Zero);s.Inputs[p.Id]=(default,1,.4f,0);s.Tick(.1f);C(p.Yaw==1&&p.Pitch==.4f&&p.Position==pose);
            a.Position=p.Position+new Vector3(0,0,1.7f);Aim(s,a,p.Position+Vector3.UnitY*.8f);C(s.SlapRevive(a)&&p.DownUntil==0&&!s.Down(p,2));s.State.Time+=3.01f;C(s.Down(p,2));s.Tick(4.01f);C(p.DownUntil==0&&s.ActionLog.Any(x=>x.Kind==PartyAction.Revive));
        });
        test("p9 held G throws on release and swept auto-catch records both actors",()=>{
            var s=World(2);var p=s.State.Player(1)!;var q=s.State.Player(2)!;p.Position=new(3,0,2);p.Yaw=0;p.Pitch=0;q.Position=new(3,0,0);q.Yaw=MathF.PI;s.State.SharedHead.Position=new(10,10,10);p.Held=1;s.State.Tools[1].Holder=p.Id;
            for(int i=0;i<20;i++){s.Inputs[p.Id]=(default,0,0,Buttons.Drop);s.Tick(1f/60);}C(s.State.Tools[1].Holder==p.Id,"charge must not release early");s.Inputs[p.Id]=(default,0,0,0);s.Tick(1f/60);C(p.Held<0&&s.State.Tools[1].Velocity.Length()>4);for(int i=0;i<24&&q.Held<0;i++)s.Tick(1f/60);C(q.Held==1,"fast flight crossing hand must catch");C(s.ActionLog.Any(a=>a.Kind==PartyAction.Throw&&a.Actor==1)&&s.ActionLog.Any(a=>a.Kind==PartyAction.Catch&&a.Actor==2));
        });
        test("p9 coop shelf absent at 1/2P and appears at 3/4P",()=>{for(int n=1;n<=4;n++){var s=World(n);s.ForceAI=false;s.StartMatch();C(s.State.Props.Count(p=>p.Coop!=CoopKind.None)==(n<3?0:5));}});
        test("p9 scissors require two presses and 350 ms, no repeat while held",()=>{
            var s=World();var a=s.State.Player(1)!;var b=s.State.Player(2)!;a.Position=new(0,1,2.2f);b.Position=new(1,1,2.2f);Aim(s,a,s.State.SharedHead.ToWorld(new(0,.45f,.2f)));Hold(s,a,CoopKind.ScissorA);Hold(s,b,CoopKind.ScissorB);s.InputTimes[a.Id]=s.State.Time;s.Inputs[a.Id]=(default,a.Yaw,a.Pitch,Buttons.Primary);s.Tick(.016f);C(!s.ActionLog.Any(x=>x.Kind==PartyAction.CoopCut));s.State.Time+=.5f;s.InputTimes[b.Id]=s.State.Time;s.Inputs[b.Id]=(default,b.Yaw,b.Pitch,Buttons.Primary);s.Tick(.016f);C(!s.ActionLog.Any(x=>x.Kind==PartyAction.CoopCut));
            s.Inputs[a.Id]=(default,a.Yaw,a.Pitch,0);s.Inputs[b.Id]=(default,b.Yaw,b.Pitch,0);s.Tick(.016f);s.InputTimes[a.Id]=s.State.Time;s.InputTimes[b.Id]=s.State.Time-.2f;s.Inputs[a.Id]=(default,a.Yaw,a.Pitch,Buttons.Primary);s.Inputs[b.Id]=(default,b.Yaw,b.Pitch,Buttons.Primary);float before=s.State.SharedHead.Mass;s.Tick(.016f);C(s.ActionLog.Any(x=>x.Kind==PartyAction.CoopCut)&&s.State.SharedHead.Mass<before);var count=s.ActionLog.Count(x=>x.Kind==PartyAction.CoopCut);s.Tick(.5f);C(s.ActionLog.Count(x=>x.Kind==PartyAction.CoopCut)==count);
        });
        test("p9 hose effect uses pump at 2.5 flow; too-long hose drops without forces",()=>{
            var s=World();var a=s.State.Player(1)!;var b=s.State.Player(2)!;a.Position=new(0,0,2);b.Position=new(1,0,2);Aim(s,a,s.State.SharedHead.ToWorld(new(0,.4f,.3f)));Hold(s,a,CoopKind.Nozzle);Hold(s,b,CoopKind.Tank);s.Inputs[b.Id]=(default,b.Yaw,b.Pitch,Buttons.Primary);s.Tick(.016f);C(s.State.SharedHead.Patches.Any(p=>p.Glue>.8f));C(s.ActionLog.Any(x=>x.Kind==PartyAction.CoopSpray&&x.Actor==b.Id));b.Position=new(4,0,2);var pose=a.Position;s.Tick(.016f);C(a.CarriedProp<0&&b.CarriedProp<0&&a.Impulse==Vector3.Zero&&b.Impulse==Vector3.Zero&&a.Position==pose);
        });
        test("p9 chair is bounded at 45 degrees/sec and probes share rotated head",()=>{
            var s=World(2);var p=s.State.Player(1)!;p.Position=new(-.8f,0,2);Aim(s,p,Session.ChairControl(0));C(s.RotateChair(p,.1f));C(Math.Abs(s.State.Customers[0].ChairYaw-MathF.PI/40)<.0001f);CustomerMotion.Apply(s.State);C(s.State.SharedHead.Rotation.Y==s.State.Customers[0].ChairYaw);var v=new Vector3(.2f,.6f,.3f);C(Vector3.Distance(s.State.SharedHead.ToLocal(s.State.SharedHead.ToWorld(v)),v)<.001f);
        });
        test("p9 ladder stabilizer removes only tool wobble",()=>{
            var s=World(2);var p=s.State.Player(1)!;var l=s.State.Ladders[0];p.Position=l.Position+new Vector3(0,1,0);s.State.Time=.15f;var eye=Session.Eye(p);C(Session.ToolEye(s.State,p)!=eye);l.Supporter=2;C(Session.ToolEye(s.State,p)==eye&&p.Position==l.Position+new Vector3(0,1,0));
        });
        test("p9 hose disconnect cannot auto-catch its own dropped part",()=>{
            var s=World();var a=s.State.Player(1)!;var b=s.State.Player(2)!;a.Position=new(0,1,2.2f);b.Position=new(4,1,2.2f);Aim(s,a,s.State.SharedHead.Position);Aim(s,b,s.State.SharedHead.Position);Hold(s,a,CoopKind.Nozzle);Hold(s,b,CoopKind.Tank);s.Inputs[a.Id]=(default,a.Yaw,a.Pitch,0);s.Inputs[b.Id]=(default,b.Yaw,b.Pitch,0);
            for(int i=0;i<50;i++)s.Tick(.02f);C(a.CarriedProp<0&&b.CarriedProp<0&&s.State.Props.Where(p=>p.Coop is CoopKind.Nozzle or CoopKind.Tank).All(p=>p.Holder==0),"parts must remain dropped after physics, not re-catch immediately");
        });
        test("p9 extended input time/rotation and down motion roundtrip",()=>{
            var input=new InputTick(1,Vector2.One,.1f,.2f,Buttons.ChairLeft,12);C(MotionWire.ReadInputs(MotionWire.Inputs([input])).Single()==input);C(MotionWire.ReadInputs(MotionWire.Inputs([input with{Time=float.NaN}])).Length==0);
            var f=new MotionFrame{Players=[new(1,1,default,default,default,0,0,true,false,2,3,4)]};C(MotionWire.Decode(MotionWire.Encode(f))!.Players[0].DownUntil==4);
        });
    }
}
