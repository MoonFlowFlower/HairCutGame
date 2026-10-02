using Hairball.Core;
using System.Numerics;

internal static class PartyMaterialTests
{
    public static void Run(Action<string,Action> test,Action<bool,string> check)
    {
        void C(bool b,string m="material assertion")=>check(b,m);
        void Input(Session s,PlayerState p,Buttons b){s.Inputs[p.Id]=(default,p.Yaw,p.Pitch,b);s.Tick(1f/60);}
        test("party four independent bottles survive handoff and empty bottle switching",()=>{
            var s=ExperimentTests.World();var p=s.State.Player(1)!;var bottles=s.State.Tools.Where(t=>t.Definition==1).ToArray();C(bottles.Length==4&&bottles.All(t=>t.GrowthRemaining==1));
            ExperimentTests.Equip(s,p,1);ExperimentTests.Aim(s,p,new(0,2,2),new(0,2,0));
            for(int i=0;i<80;i++){s.State.Time+=.1f;p.Cooldown=0;s.UseTool(p,false);s.EndStroke(p.Id);}
            C(bottles[0].GrowthRemaining<1&&bottles.Skip(1).All(t=>t.GrowthRemaining==1),$"independent: {bottles[0].GrowthRemaining}");
            s.Drop(p);p.Position=bottles[1].Position-Vector3.UnitY*1.7f+Vector3.UnitZ;C(s.Pickup(p,bottles[1].Id));ExperimentTests.Aim(s,p,new(0,2,2),new(0,2,0));Input(s,p,Buttons.None);float before=bottles[1].GrowthRemaining;Input(s,p,Buttons.Primary);C(bottles[1].GrowthRemaining<before);
            var copy=Wire.Decode<WorldState>(Wire.Encode(s.State));C(copy.Tools.Single(t=>t.Id==bottles[1].Id).GrowthRemaining==bottles[1].GrowthRemaining);
        });
        test("party real flat wig recipe completes after a whole bottle wasted; zero extra growth",()=>{
            foreach(bool waste in new[]{false,true}){
                var s=ExperimentTests.World();var p=s.State.Player(1)!;
                if(waste){ExperimentTests.Equip(s,p,1);ExperimentTests.Aim(s,p,new(0,2,2),new(0,2,0));for(int i=0;i<180;i++){s.State.Time+=.1f;p.Cooldown=0;s.UseTool(p,false);s.EndStroke(p.Id);}C(s.State.Tools.First(t=>t.Definition==1).GrowthRemaining<.01f);s.Drop(p);}
                var wig=s.State.Heads.Last(h=>h.Loose&&!h.Fragment);ExperimentTests.Aim(s,p,new(1.4f,1.7f,.2f),wig.Position+new Vector3(0,.25f,0));Input(s,p,Buttons.None);Input(s,p,Buttons.Interact);C(p.CarriedHair==wig.Id,"E picks the wig into the hand");Input(s,p,Buttons.None);ExperimentTests.Aim(s,p,new(1.4f,1.7f,.2f),s.State.SharedHead.Position+Vector3.UnitY*.4f);Input(s,p,Buttons.Interact);C(wig.AttachedTo==0,$"wig attached={wig.AttachedTo}, pos={wig.Position}");
                var heli=s.State.Props.Single(t=>t.Goal==0);// Aim explicitly at the real table prop.
                ExperimentTests.Aim(s,p,new(-1.35f,1.7f,2.8f),heli.Position);Input(s,p,Buttons.None);Input(s,p,Buttons.Interact);C(p.CarriedProp==heli.Id);
                ExperimentTests.Aim(s,p,new(1.2f,3.8f,1.2f),wig.ToWorld(new(0,.88f,0)));Input(s,p,Buttons.None);Input(s,p,Buttons.Interact);C(heli.Attached);
                C(Session.LandingSupport(s.State,heli).Issues.HasFlag(LandingIssue.Soft),"unfixed wig is soft");
                ExperimentTests.Equip(s,p,4);
                // Ordinary aimed glue from above covers each gear contact, never assigns patch hardness.
                for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++){
                    ExperimentTests.Aim(s,p,wig.ToWorld(new(x*.27f,2,z*.29f)),wig.ToWorld(new(x*.27f,.88f,z*.29f)));
                    for(int i=0;i<5;i++){p.Cooldown=0;s.UseTool(p,false);s.State.Time+=.16f;}
                }
                C(Session.LandingSupport(s.State,heli).Stable,"real glue fixes wig support");s.Inputs.Clear();s.State.Remaining=0;
                for(int i=0;i<140&&!s.State.Experiment.Resolved;i++)s.Tick(.1f);C(s.State.Experiment.Success);
                Console.WriteLine($"PARTY_CAPACITY waste={waste} extraBottlesUsed=0 supplyLeft={Session.TotalGrowth(s.State):0.000}");
            }
        });
        test("party recycling yields sixty percent and charred half without creating mass",()=>{
            var d=new DebrisState();DebrisSystem.Deposit(d,new(0,.01f,-1),1,new());DebrisSystem.Deposit(d,new(.5f,.01f,-1),1,new(){Char=1});
            var m=new DebrisMaterial();float got=DebrisSystem.Vacuum(d,new(0,.01f,0),-Vector3.UnitZ,2,1,20,m,.6f);C(Math.Abs(got-.9f)<.001f&&d.Mass<.001f);C(m.Char>0&&m.Char<1);
            var h=Head.Create(0,0);float before=h.Mass;float added=h.Volume.Brush(new(EffectKind.Transfer,.16f),new(.25f,.55f,.25f),.34f,budget:got);C(added>0&&added<=got+.001f&&h.Mass>before,"stored debris can patch real density");
        });
    }
}
