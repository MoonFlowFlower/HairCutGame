using Hairball.Core;
using System.Numerics;
internal static class MiniatureTests
{
    public static void Run(Action<string,Action> test,Action<bool,string> check)
    {
        void C(bool b,string m="miniature assertion")=>check(b,m);
        Session World(){var s=new Session{Lab=true};s.AddPlayer(1,"A");s.StartMatch();return s;}
        Head Model(Session s)=>s.State.Heads.Single(h=>h.Miniature);
        void Aim(PlayerState p,Vector3 eye,Vector3 point){p.Position=eye-Vector3.UnitY*1.7f;var d=Vector3.Normalize(point-eye);p.Yaw=MathF.Atan2(-d.X,-d.Z);p.Pitch=MathF.Asin(d.Y);p.Cooldown=0;}
        test("miniature E pickup/drop is exclusive, drops tool, cannot use held tool",()=>{
            var s=World();var p=s.State.Player(1)!;var h=Model(s);C(!s.HandleModel(p));
            Aim(p,h.Position+Vector3.UnitZ*1.5f,MiniatureModel.Center(h));p.Held=1;s.State.Tools[1].Holder=1;
            s.Inputs[1]=(Vector2.Zero,p.Yaw,p.Pitch,Buttons.Interact);s.Tick(.01f);
            C(p.CarriedModel==h.Id&&h.Holder==1&&p.Held==-1&&s.State.Tools[1].Holder==0);
            var other=s.AddPlayer(2,"B")!;Aim(other,Session.Eye(p),MiniatureModel.Center(h));C(!s.HandleModel(other));
            var before=Wire.Encode(h);p.Held=1;s.State.Tools[1].Holder=1;s.UseTool(p,false);C(before.SequenceEqual(Wire.Encode(h)));p.Held=-1;s.State.Tools[1].Holder=0;
            s.RemovePlayer(1);C(h.Holder==0&&p.CarriedModel==-1);
        });
        test("miniature actual tools cut grow freeze burn and push without scoring the customer",()=>{
            foreach(int id in new[]{0,1,3,5,8}){
                var s=World();var p=s.State.Player(1)!;var h=Model(s);h.Position=new(-3,1.7f,0);h.Volume=HairVolume.Create();foreach(var patch in h.Patches)patch.Glue=0;
                Aim(p,h.Position+new Vector3(0,.2f,1.5f),h.Position+new Vector3(0,.2f,0));
                p.Held=id;s.State.Tools[id].Holder=1;var before=Wire.Encode(h);var main=Wire.Encode(s.State.SharedHead);float score=s.LiveHealth().Value;
                s.UseTool(p,false);C(!before.SequenceEqual(Wire.Encode(h)),"real miniature tool "+id);C(main.SequenceEqual(Wire.Encode(s.State.SharedHead)));C(s.LiveHealth().Value==score);
            }
        });
        test("miniature swept collision settles on supports and held doll cannot pass walls",()=>{
            var s=World();var h=Model(s);h.Position=new(2,3,2);
            s.ModelCast=(a,b)=>a.Y>=1&&b.Y<1?(Vector3.Lerp(a,b,(a.Y-1)/(a.Y-b.Y)),Vector3.UnitY):null;
            for(int i=0;i<240;i++)s.TickModels(1f/60);
            C(Math.Abs(h.Position.Y-(1+MiniatureModel.Foot-.025f))<.04f,$"settled {h.Position.Y}");
            s.ModelCast=null;for(int i=0;i<240;i++)s.TickModels(1f/60);C(Math.Abs(h.Position.Y-MiniatureModel.Foot)<.001f);
            var p=s.State.Player(1)!;h.Holder=1;p.CarriedModel=h.Id;h.Position=new(0,2,2);Aim(p,new(2,2,2),new(3,2,2));
            s.ModelCast=(a,b)=>a.X<1&&b.X>=1?(Vector3.Lerp(a,b,(1-a.X)/(b.X-a.X)),-Vector3.UnitX):null;
            s.TickModels(.02f);C(h.Position.X<.75f,"held swept wall");
        });
        test("miniature is snapshotted and replayed independently from immutable board targets",()=>{
            var s=World();var h=Model(s);h.Holder=1;s.State.Player(1)!.CarriedModel=h.Id;h.Position=new(1,2,3);h.Patches[0].Wet=.8f;
            var original=Wire.Encode(GoalMaterials.Reference(s.State.Job.Goal));
            var copy=Wire.Decode<WorldState>(Wire.Encode(s.State));C(copy.Heads.Single(h=>h.Miniature).Holder==1&&copy.Player(1)!.CarriedModel==h.Id);
            s.Replay.Record(s.State,.11f);h.Volume.Fill(_=>-1);h.Position=default;
            var old=s.Replay.Rolling.Last().Heads.Single(h=>h.Miniature);C(old.Position==new Vector3(1,2,3)&&old.Mass>0&&old.Patches[0].Wet==.8f);
            C(original.SequenceEqual(Wire.Encode(GoalMaterials.Reference(s.State.Job.Goal))));
            s.NextRound();C(s.State.Heads.Count(h=>h.Miniature)==1&&s.State.Player(1)!.CarriedModel==-1);
        });
        test("miniature detached pieces are ordinary debris, never duplicate dolls",()=>{
            var s=World();var h=Model(s);h.Volume.Fill(v=>Math.Max(HairVolume.Ellipsoid(v,Vector3.Zero,new(.45f)),HairVolume.Ellipsoid(v,new(1,1,0),new(.25f))));
            s.Tick(.01f);C(s.State.Heads.Count(h=>h.Miniature)==1);C(s.State.Heads.Any(h=>h.Fragment&&!h.Miniature));
        });
    }
}
