using Hairball.Core;
using System.Numerics;

static class SculptTests
{
    public static void Run(Action<string,Action> test,Action<bool,string> check)
    {
        void Check(bool b,string message="sculpt/debris assertion")=>check(b,message);
        void Near(float a,float b,float tolerance=.003f)=>Check(Math.Abs(a-b)<tolerance,$"{a} != {b}");
        Session Scene(int tool)
        {
            var s=new Session{Lab=true};var p=s.AddPlayer(1,"Sculpt")!;s.StartMatch();var h=s.State.SharedHead;
            h.Volume.Fill(v=>HairVolume.Ellipsoid(v,new(0,.4f,0),new(.5f,.55f,.45f)));
            s.State.Heads.RemoveAll(h=>h.Facial);p.Held=tool;s.State.Tools[tool].Holder=1;
            p.Position=h.Position+new Vector3(0,.4f-1.7f,2);p.Yaw=0;p.Pitch=0;return s;
        }
        void Hold(Session s,float seconds,Buttons buttons,float step=1f/60)
        {for(float t=0;t<seconds-.00001f;t+=step){s.Inputs[1]=(Vector2.Zero,0,0,buttons);s.Tick(Math.Min(step,seconds-t));}}
        test("locked shave stops deepening; release starts a new layer",()=>{
            var s=Scene(0);float original=s.State.SharedHead.Mass;Hold(s,.2f,Buttons.Primary);float once=s.State.SharedHead.Mass;
            Hold(s,2,Buttons.Primary);Near(once,s.State.SharedHead.Mass);Check(once<original);
            Hold(s,.1f,Buttons.None);Hold(s,.2f,Buttons.Primary);Check(s.State.SharedHead.Mass<once);
            Check(s.State.Debris.Mass>0,"removed hair must become debris");
        });
        test("fine shave removes less than normal; wheel radius changes affected area",()=>{
            float Removed(bool fine,int size){var s=Scene(0);s.State.Player(1)!.BrushSize=size;float mass=s.State.SharedHead.Mass;Hold(s,.2f,fine?Buttons.Secondary:Buttons.Primary);return mass-s.State.SharedHead.Mass;}
            Check(Removed(true,1)>0&&Removed(true,1)<Removed(false,1));Check(Removed(false,0)<Removed(false,2));
        });
        test("fast dragging fills a continuous locked cutting plane",()=>{
            var s=Scene(0);var head=s.State.SharedHead;var p=s.State.Player(1)!;
            head.Volume.Fill(v=>Math.Min(.7f-Math.Abs(v.X),Math.Min(.25f-Math.Abs(v.Y-.4f),.4f-Math.Abs(v.Z))));
            p.Position.X=head.Position.X-.45f;Hold(s,.1f,Buttons.Primary);
            p.Position.X=head.Position.X+.45f;Hold(s,.1f,Buttons.Primary);
            for(float x=-.4f;x<=.401f;x+=.1f){Check(head.Volume.Raycast(new(x,.4f,2),-Vector3.UnitZ,4,out var hit,out _));Near(hit.Z,.35f,.009f);}
            float mass=head.Mass;Hold(s,.5f,Buttons.Primary);Near(mass,head.Mass);
        });
        test("growth cannot silently switch targets during a held gesture",()=>{
            var s=Scene(1);var first=s.State.SharedHead;var p=s.State.Player(1)!;Hold(s,.2f,Buttons.Primary);
            var other=Head.Create(200,2);other.Position=first.Position+new Vector3(1.3f,0,0);s.State.Heads.Add(other);s.Tick(.016f);float before=other.Mass;
            p.Position.X+=1.3f;Hold(s,.4f,Buttons.Primary);Near(before,other.Mass);
            Hold(s,.1f,Buttons.None);Hold(s,.4f,Buttons.Primary);Check(other.Mass>before,"release and repress should intentionally acquire new target");
        });
        test("time-driven growth remains close across simulation frame rates",()=>{
            float Grow(float dt,bool fine){var s=Scene(1);float mass=s.State.SharedHead.Mass;Hold(s,1.2f,fine?Buttons.Secondary:Buttons.Primary,dt);return s.State.SharedHead.Mass-mass;}
            float a=Grow(1f/60,false),b=Grow(1f/30,false),fine=Grow(1f/60,true);Check(a>0&&fine>0&&fine<a,$"normal {a}, fine {fine}");Near(a,b,Math.Max(.03f,a*.12f));
        });
        test("rendered thin tetrahedral surface is hittable",()=>{
            var v=new HairVolume();v.Data[HairVolume.Index(17,9,13)]=129;v.Revision++;
            var shell=HairShell.Build(v);Check(shell.Indices.Count>0);
            var a=shell.Vertices[shell.Indices[0]];var b=shell.Vertices[shell.Indices[1]];var c=shell.Vertices[shell.Indices[2]];var center=(a+b+c)/3;var normal=Vector3.Normalize(Vector3.Cross(b-a,c-a));
            if(Vector3.Dot(normal,v.Normal(center))<0)normal=-normal;
            Check(v.Raycast(center+normal*.03f,-normal,.06f,out var hit,out _));Near(Vector3.Distance(hit,center),0,.0001f);
        });
        test("disconnected neck falls while all supported posts and tetra diagonals survive",()=>{
            var v=new HairVolume();v.Fill(p=>Math.Max(.27f-Vector3.Distance(p,new(0,.25f,0)),.3f-Vector3.Distance(p,new(0,1.3f,0))));float before=v.Mass;
            var detached=v.DetachUnsupported(p=>p.Y<.5f,false);Check(detached.Count==1);Near(before,v.Mass+detached.Sum(p=>p.Mass));Check(!v.Raycast(new(0,1.3f,2),-Vector3.UnitZ,4,out _,out _));
            var two=new HairVolume();two.Fill(p=>Math.Max(.18f-Vector3.Distance(p,new(-.45f,.25f,0)),.18f-Vector3.Distance(p,new(.45f,.25f,0))));Check(two.DetachUnsupported(p=>p.Y<.3f,false).Count==0,"both roots must survive");
            var pin=new HairVolume();pin.Fill(p=>.2f-Vector3.Distance(p,new(0,1.4f,0)));Check(pin.DetachUnsupported(p=>Vector3.Distance(p,new(0,1.4f,0))<.15f,false).Count==0,"real support preserves suspended material");
        });
        test("cut and fragment bookkeeping conserve original material",()=>{
            var s=Scene(0);float before=s.State.Heads.Sum(h=>h.Mass)+s.State.Debris.Mass;Hold(s,.2f,Buttons.Primary);
            Near(before,s.State.Heads.Sum(h=>h.Mass)+s.State.Debris.Mass,.01f);
        });
        test("authoritative vacuum accounts absorption once and respects remaining capacity",()=>{
            foreach(float stored in new[]{0f,19.95f})
            {
                var s=Scene(2);var p=s.State.Player(1)!;p.Reservoir=stored;
                float before=s.State.Heads.Sum(h=>h.Mass)+s.State.Debris.Mass+p.Reservoir;
                Hold(s,.4f,Buttons.Primary);
                Check(p.Reservoir>stored&&p.Reservoir<=20,"vacuum must recover material without overflowing capacity");
                Near(before,s.State.Heads.Sum(h=>h.Mass)+s.State.Debris.Mass+p.Reservoir,.015f);
            }
        });
        test("large severed volume keeps its shape, falls, then merges without material loss",()=>{
            var s=Scene(0);s.Inputs.Clear();var h=s.State.SharedHead;
            h.Volume.Fill(v=>Math.Max(.25f-Vector3.Distance(v,new(0,.25f,0)),.3f-Vector3.Distance(v,new(0,1.3f,0))));
            float before=s.State.Heads.Sum(x=>x.Mass);s.Tick(.016f);
            var fragment=s.State.Heads.Single(x=>x.Fragment);Check(fragment.Volume.Raycast(new(0,0,2),-Vector3.UnitZ,4,out _,out _),"shifted fragment still has its shell");
            Near(before,s.State.Heads.Sum(x=>x.Mass)+s.State.Debris.Mass,.01f);
            fragment.Rotation=new(.4f,.3f,.2f);
            for(int i=0;i<240;i++)s.Tick(1f/60);
            float bottom=HairShell.Build(fragment.Volume).Vertices.Min(v=>fragment.ToWorld(v).Y);
            Check(bottom>=0&&bottom<.04f,$"rotated actual shell must touch floor, bottom={bottom}");
            for(int i=0;i<860;i++)s.Tick(1f/60);
            Check(!s.State.Heads.Any(x=>x.Fragment),"resting fragment must convert into a persistent pile");Check(s.State.Debris.Piles.Count>0);
            Near(before,s.State.Heads.Sum(x=>x.Mass)+s.State.Debris.Mass,.02f);
        });
        test("moving a support releases its resting clippings instead of leaving them floating",()=>{
            var d=new DebrisState();DebrisSystem.Deposit(d,new(1,1,1),2,new());DebrisSystem.Deposit(d,new(1,0,1),1,new());
            DebrisSystem.ReleaseSupport(d,new(1,0,1),.8f);Check(d.Piles.Count==1&&d.Flights.Count==1);Near(d.Mass,3);
            for(int i=0;i<180;i++)DebrisSystem.Tick(d,1f/60,null);Check(d.Piles.All(p=>p.Position.Y<.1f));Near(d.Mass,3);
            var s=Scene(0);s.State.Customers[0].ChairHeight=.5f;DebrisSystem.Deposit(s.State.Debris,Session.WorkCenter+Vector3.UnitY*1.23f,2,new());
            s.NextRound();Check(s.State.Debris.Flights.Count>0&&s.State.Debris.Piles.Count==0,"round-reset chair must release elevated clippings");Near(s.State.Debris.Mass,2);
        });
        test("falling clippings settle on supports and vacuum/blower do not duplicate mass",()=>{
            var state=new DebrisState();var material=new DebrisMaterial{Color=new(.4f,.2f,.1f)};
            DebrisSystem.Emit(state,new(0,2,0),Vector3.Zero,3,material);
            Vector3? Table(Vector3 from,Vector3 to)=>from.Y>=1&&to.Y<1?new Vector3(to.X,1,to.Z):null;
            for(int i=0;i<120;i++)DebrisSystem.Tick(state,1f/60,Table);
            Near(state.Mass,3);Check(state.Flights.Count==0&&state.Piles.Count==1&&state.Piles[0].Position.Y==1);
            DebrisSystem.Blow(state,new(0,1,2),-Vector3.UnitZ,4,.6f);Near(state.Mass,3);Check(state.Piles.Count==0&&state.Flights.Count>0);
            for(int i=0;i<240;i++)DebrisSystem.Tick(state,1f/60,null);
            var pile=state.Piles.Single();var output=new DebrisMaterial();float taken=DebrisSystem.Vacuum(state,pile.Position+Vector3.UnitZ*2,-Vector3.UnitZ,4,.6f,1,output);
            Near(taken,1);Near(state.Mass,2);Near(output.Color.X,.4f);
        });
        test("debris survives rounds and replay copies; new match clears it",()=>{
            var s=Scene(0);DebrisSystem.Deposit(s.State.Debris,new(0,.015f,0),2,new(){Color=Vector3.One});s.NextRound();Near(s.State.Debris.Mass,2);
            s.Replay.Record(s.State,.2f);var copy=Wire.Decode<WorldState>(Wire.Encode(s.State));Near(copy.Debris.Mass,2);s.State.Debris.Piles[0].Mass=3;Near(s.Replay.Rolling[0].Debris.Mass,2);
            s.StartMatch();Near(s.State.Debris.Mass,0);
        });
        test("airborne and resting budgets preserve accumulated material",()=>{
            var state=new DebrisState();var mat=new DebrisMaterial{Color=Vector3.One};
            for(int i=0;i<1000;i++)DebrisSystem.Emit(state,new(i%30*.3f-4.5f,2,i/30*.3f-4.5f),Vector3.Zero,.1f,mat);
            Check(state.Flights.Count<=64);Near(state.Mass,100,.02f);
            for(int i=0;i<2500;i++)DebrisSystem.Deposit(state,new(i%50*.4f,0,i/50*.4f),.1f,mat);
            Check(state.Piles.Count<=2048);Near(state.Mass,350,.1f);
        });
    }
}
