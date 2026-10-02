using Hairball.Core;
using System.Numerics;
internal static class TargetCardTests
{
    public static void Run(Action<string,Action> test,Action<bool,string> check)
    {
        void C(bool b,string message="target card assertion")=>check(b,message);
        WorldState World(){var s=ExperimentTests.World();return s.State;}
        test("p10 fixed geometry distinguishes two horns from a broad triangle",()=>{
            var w=World();var h=w.SharedHead;C(StyleAttributes.Evaluate(w)[StyleAttribute.Horns]==0,"ordinary cap has no protruding horn");
            h.Volume.Fill(p=>Math.Max(HairVolume.Ellipsoid(p,new(-.5f,.9f,0),new(.23f,.95f,.3f)),HairVolume.Ellipsoid(p,new(.5f,.9f,0),new(.23f,.95f,.3f))));C(StyleAttributes.Evaluate(w)[StyleAttribute.Horns]==2,"two separated peaks");
            h.Volume.Fill(p=>Math.Min(1.65f-Math.Abs(p.X)*1.25f-Math.Abs(p.Z)*.2f-p.Y,Math.Min(.9f-Math.Abs(p.X),.45f-Math.Abs(p.Z))));C(StyleAttributes.Evaluate(w)[StyleAttribute.Horns]==1,"one broad triangle / ridge");
        });
        test("p10 height left-right front-back and volume use actual density",()=>{
            var w=World();var h=w.SharedHead;var original=h.Volume.Clone();
            foreach(var point in new[]{new Vector3(-.6f,1.4f,-.6f),new Vector3(.6f,1.4f,.6f)}){h.Volume.Fill(p=>HairVolume.Ellipsoid(p,point,new(.3f,.5f,.3f)));var m=StyleAttributes.Evaluate(w);C(m[StyleAttribute.Height]>1.35f&&m[StyleAttribute.Side]==Math.Sign(point.X)&&m[StyleAttribute.Front]==Math.Sign(point.Z));}
            h.Volume=original;C(Math.Abs(StyleAttributes.Evaluate(w)[StyleAttribute.Volume]-1)<.001f);h.Volume.Fill(p=>.20f-p.Length());C(StyleAttributes.Evaluate(w)[StyleAttribute.Volume]<.55f);h.Volume.Fill(p=>HairVolume.Ellipsoid(p,new(0,.7f,0),new(1,1.2f,1)));C(StyleAttributes.Evaluate(w)[StyleAttribute.Volume]>1.8f);
        });
        test("p10 material shares, wig and commission location are measured",()=>{
            var w=World();foreach(var p in w.SharedHead.Patches){p.Wet=p.Glue=p.Char=p.Young=1;p.Temperature=-20;}
            var m=StyleAttributes.Evaluate(w);foreach(var a in new[]{StyleAttribute.Wet,StyleAttribute.Glue,StyleAttribute.Char,StyleAttribute.Young,StyleAttribute.Ice})C(m[a]==1,a.ToString());C(m[StyleAttribute.Wig]==0);
            var wig=Head.Create(501,0);wig.Loose=true;wig.AttachedTo=0;wig.Position=w.SharedHead.Position;foreach(var p in wig.Patches)p.Wig=true;w.Heads.Add(wig);C(StyleAttributes.Evaluate(w)[StyleAttribute.Wig]==1);
            var prop=w.Props.Single(p=>p.Id==w.Experiment.HelicopterId);prop.Attached=true;prop.Position=w.SharedHead.ToWorld(new(-.8f,1,0));C(StyleAttributes.Evaluate(w)[StyleAttribute.Object]==3);
        });
        test("p10 equal left-right mass is not sufficient evidence of symmetry",()=>{
            var w=World();w.SharedHead.Volume.Fill(p=>Math.Max(.27f-Vector3.Distance(p,new(-.55f,1.3f,0)),.27f-Vector3.Distance(p,new(.55f,.3f,0))));C(StyleAttributes.Evaluate(w)[StyleAttribute.Side]!=0);w.SharedHead.Volume.Fill(p=>Math.Max(.27f-Vector3.Distance(p,new(-.55f,1.3f,0)),.27f-Vector3.Distance(p,new(.55f,1.3f,0))));C(StyleAttributes.Evaluate(w)[StyleAttribute.Side]==0);
            var frame=new MotionFrame{Sequence=1,Players=Enumerable.Range(1,4).Select(id=>new PlayerMotion(id,0,default,default,default,0,0,true,false)).ToArray(),Props=Enumerable.Range(1,24).Select(id=>new MovingPose(id,new(id,0,0),default)).ToArray()};var bytes=MotionWire.Encode(frame);C(bytes.Length<1200&&MotionWire.Decode(bytes)!.Props.Length==24,"all expanded props fit one bounded motion datagram");
        });
        test("p10 every 30-card deal has coverage and distractors for 50 seeds",()=>{
            C(TargetCards.Cards.Length==30&&TargetCards.Cards.Count(c=>c.Kind==TargetKind.Emotion)==8&&TargetCards.Cards.Count(c=>c.Kind==TargetKind.Degree)==6);C(TargetCards.Props.Length==20);
            foreach(var card in TargetCards.Cards)for(int seed=0;seed<50;seed++){var ids=TargetCards.Deal(card,seed);C(ids.Length==6&&ids.Distinct().Count()==6&&ids.SequenceEqual(TargetCards.Deal(card,seed)));var props=ids.Select(id=>TargetCards.Props[id]).ToArray();C(card.Rules.All(r=>props.Any(p=>TargetCards.Covers(p,r))),"coverage "+card.Id);C(props.All(p=>TargetCards.Distractor(p,card)));if(card.Kind!=TargetKind.Degree)C(props.All(p=>!card.Rules.All(r=>TargetCards.Covers(p,r))));else C(card.Rules.Length==1);}
            foreach(var pun in new[]{"飞机头","锅盖头","鸡窝头","马尾","丸子头","蘑菇头","板寸","冲天辫"})C(TargetCards.Props.Any(p=>p.Chinese==pun));C(TargetCards.Props.Any(p=>p.English=="Beehive")&&TargetCards.Props.Any(p=>p.English=="Mullet"));
        });
        test("p10 evaluator is read only and target never enters build snapshot",()=>{
            var s=ExperimentTests.World();s.TargetPicker=_=>16;s.StartMatch();s.State.Phase=Phase.Build;s.Tick(.01f);C(s.CurrentTarget?.Kind==TargetKind.Emotion);var before=Wire.Encode(s.State);StyleAttributes.Evaluate(s.State);TargetCards.Judge(s.State,s.CurrentTarget!);C(before.SequenceEqual(Wire.Encode(s.State)));var received=Wire.Decode<WorldState>(before);C(received.Experiment.ResultTarget==null&&received.Experiment.ResultStyle.Count==0&&received.Props.Count(p=>p.Gesture>=0)==6);C(!System.Text.Encoding.UTF8.GetString(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(s.State,new System.Text.Json.JsonSerializerOptions{IncludeFields=true})).Contains(s.CurrentTarget!.Chinese));
            s.State.Remaining=0;for(int i=0;i<200&&!s.State.Experiment.Resolved;i++)s.Tick(.1f);C(s.State.Experiment.ResultTarget?.Id==s.CurrentTarget.Id&&s.State.Experiment.ResultStyle.Count==s.CurrentTarget.Rules.Length);
        });
        test("p10 tips use matches, never make physical failure a success",()=>{
            var w=World();var card=new TargetCard(99,TargetKind.Degree,"测试","Test",[new(StyleAttribute.Wig,0,0)]);var result=PartyResults.Evaluate(w,false,card);C(result.Base==0&&result.Tip==10&&result.Reaction==CurtainReaction.Amazed);w.Job.Penalty=999;C(PartyResults.Evaluate(w,true,card).Tip==0);C(!w.Experiment.Success);C(TargetCards.Cards.Where(c=>c.Kind==TargetKind.Emotion).All(c=>c.Rules.Length is 2 or 3&&c.Face(true)!=c.Face()));
        });
    }
}

