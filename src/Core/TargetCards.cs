using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
namespace Hairball.Core;
public enum TargetKind {Normal,Emotion,Degree}
public sealed record TargetCard(int Id,TargetKind Kind,string Chinese,string English,StyleRule[] Rules)
{
    public string Title=>L.Locale=="zh"?Chinese:English;
    public string Face(bool reveal=false)=>Title+(Kind==TargetKind.Emotion&&!reveal?"":"\n"+string.Join("\n",Rules.Select(StyleAttributes.Text)));
}
public sealed record PropReading(StyleAttribute Attribute,float Value);
public sealed record GestureProp(int Id,string Chinese,string English,PropReading[] Readings)
{
    public string Name=>L.Locale=="zh"?Chinese:English;
}
public static class TargetCards
{
    // Independent English idioms; these are deliberately not literal translations of the Chinese puns.
    const string PropTable="""
飞机头|Beehive|Height:2.4;Front:1;Wet:0
锅盖头|Bowl cut|Height:.65;Side:0;Volume:1
鸡窝头|Bedhead|Volume:2.2;Horns:3;Wet:1
马尾|Mullet|Front:-1;Side:1;Wig:1
丸子头|Space bun|Horns:1;Side:0;Glue:1
蘑菇头|Mushroom cap|Volume:2.2;Height:.9;Wet:1
板寸|Flat top|Height:.45;Side:0;Char:0
冲天辫|Spire|Height:2.4;Horns:1;Young:1
蜂窝|Honeycomb|Height:1.15;Glue:1;Volume:2.2
鸭尾|Duck tail|Front:1;Side:-1;Wet:1
冰淇淋|Soft serve|Ice:1;Horns:1;Volume:1
皇冠|Crown|Horns:3;Glue:1;Object:0
镜子|Looking glass|Side:0;Wet:0;Volume:.25
羽毛|Feather duster|Side:-1;Young:1;Wig:0
煤球|Coal puff|Char:1;Wet:0;Front:-1
雨伞|Umbrella|Ice:0;Object:0;Wet:1
台灯|Lamp shade|Side:1;Height:1.15;Char:0
弹簧|Double coil|Young:1;Horns:2;Glue:0
书本|Book stack|Object:3;Front:-1;Volume:.25
蝴蝶结|Bow tie|Wig:1;Side:-1;Height:.65
""";
    const string CardTable="""
Normal|双角冲天|Twin horns to the sky|Height:1.35:4;Side:0:0;Horns:2:2
Normal|雨天偏左|Rain on the left|Side:-1:-1;Wet:.65:1
Normal|后梳胶发|Slicked back|Front:-1:-1;Glue:.65:1
Normal|低低的假发|Low wig day|Height:0:.8;Wig:1:1
Normal|冰冻尖顶|Frozen peak|Ice:.65:1;Height:1.35:4
Normal|新生大蓬头|Fresh big hair|Young:.65:1;Volume:1.8:20
Normal|干燥三冠|Dry triple crown|Wet:0:.35;Horns:3:3
Normal|右侧凝胶|Gel to the right|Side:1:1;Glue:.65:1
Normal|左侧委托|Left landing|Object:3:3;Side:0:0
Normal|焦黑对称|Symmetric charcoal|Char:.65:1;Side:0:0
Normal|湿润一角|One wet peak|Wet:.65:1;Horns:1:1
Normal|后偏少发|Sparse at the back|Front:-1:-1;Volume:0:.55
Normal|冻住两角|Two icy peaks|Ice:.65:1;Horns:2:2
Normal|偏右不戴假发|Bare and right|Side:1:1;Wig:0:0
Normal|前偏蓬松|Big in front|Front:1:1;Volume:1.8:20
Normal|干爽低发|Low and dry|Height:0:.8;Wet:0:.35
Emotion|我要出席一场很冷的婚礼|A wedding in a freezer|Ice:.65:1;Side:0:0;Wig:1:1
Emotion|今天我要轰轰烈烈地登场|Make a grand entrance|Height:1.35:4;Volume:1.8:20
Emotion|像刚从暴雨里走出来|Just escaped a storm|Wet:.65:1;Front:-1:-1
Emotion|我想低调但不能认出我|Discreet disguise|Height:0:.8;Wig:1:1
Emotion|我想像一位怪物国王|Monster royalty|Horns:3:3;Glue:.65:1
Emotion|我重新开始啦|A fresh start|Young:.65:1;Char:0:.35
Emotion|像左边吹来了风|A wind from the left|Side:1:1;Front:-1:-1
Emotion|今天有点燃尽了|Feeling burnt out|Char:.65:1;Volume:0:.55
Degree|不高不低刚刚好|A modest lift|Height:.9:1.4
Degree|只有两只角|Exactly two peaks|Horns:2:2
Degree|只有一点湿|Just a little wet|Wet:0:.25
Degree|冻住大半|Mostly frozen|Ice:.65:1
Degree|只要正常发量|Ordinary volume|Volume:.55:1.8
Degree|黏糊糊的|Sticky enough|Glue:.65:1
""";
    static float F(string value)=>float.Parse(value,CultureInfo.InvariantCulture);
    public static readonly GestureProp[] Props=PropTable.Split('\n',StringSplitOptions.RemoveEmptyEntries).Select((row,id)=>{var cells=row.Trim().Split('|');return new GestureProp(id,cells[0],cells[1],cells[2].Split(';').Select(t=>{var r=t.Split(':');return new PropReading(Enum.Parse<StyleAttribute>(r[0]),F(r[1]));}).ToArray());}).ToArray();
    public static readonly TargetCard[] Cards=CardTable.Split('\n',StringSplitOptions.RemoveEmptyEntries).Select((row,id)=>{var cells=row.Trim().Split('|');return new TargetCard(id+1,Enum.Parse<TargetKind>(cells[0]),cells[1],cells[2],cells[3].Split(';').Select(t=>{var r=t.Split(':');return new StyleRule(Enum.Parse<StyleAttribute>(r[0]),F(r[1]),F(r[2]));}).ToArray());}).ToArray();
    public static bool Covers(GestureProp prop,StyleRule rule)=>prop.Readings.Any(r=>r.Attribute==rule.Attribute&&rule.Match(r.Value));
    public static bool Distractor(GestureProp prop,TargetCard card)=>prop.Readings.Any(r=>!card.Rules.Any(rule=>rule.Attribute==r.Attribute&&rule.Match(r.Value)));
    public static int[] Deal(TargetCard card,int seed)
    {
        var rng=new Random(seed);var pool=Props.Where(p=>Distractor(p,card)&&(card.Kind==TargetKind.Degree||!card.Rules.All(r=>Covers(p,r)))).OrderBy(_=>rng.Next()).ToArray();
        var dealt=new List<GestureProp>();foreach(var rule in card.Rules)if(!dealt.Any(p=>Covers(p,rule))){var p=pool.FirstOrDefault(p=>Covers(p,rule))??throw new InvalidOperationException("Uncoverable target "+card.Id);dealt.Add(p);}
        dealt.AddRange(pool.Where(p=>!dealt.Contains(p)).Take(6-dealt.Count));if(dealt.Count!=6)throw new InvalidOperationException("Insufficient distractors");return dealt.Select(p=>p.Id).ToArray();
    }
    public static List<StyleCheck> Judge(WorldState world,TargetCard card){var metrics=StyleAttributes.Evaluate(world);return card.Rules.Select(r=>new StyleCheck(r,metrics[r.Attribute],r.Match(metrics[r.Attribute]))).ToList();}
}
public sealed partial class Session
{
    public Func<int,int>? TargetPicker;
    TargetCard? targetCard;
    int[] gestureDeal=[];
    float styleAt,openingAt=-1,questionAt=-100;
    public TargetCard? CurrentTarget=>targetCard; // Host-only Session field, never part of WorldState.
    void ResetTargetCards()
    {
        int lastTarget=targetCard?.Id??-1;targetCard=null;gestureDeal=[];openingAt=-1;styleAt=0;questionAt=-100;
        if(!State.Experiment.IsB)return;int seed=RandomNumberGenerator.GetInt32(int.MaxValue);targetCard=TargetCards.Cards[(TargetPicker?.Invoke(State.Round)??RandomNumberGenerator.GetInt32(TargetCards.Cards.Length))%TargetCards.Cards.Length];if(TargetPicker==null&&targetCard.Id==lastTarget)targetCard=TargetCards.Cards[targetCard.Id%TargetCards.Cards.Length];gestureDeal=TargetCards.Deal(targetCard,seed);StageGestureProps();
    }
    void StageGestureProps(){if(targetCard==null||State.Props.Any(p=>p.Gesture>=0))return;for(int i=0;i<gestureDeal.Length;i++)State.Props.Add(new(){Id=State.Round*1000+900+i,Goal=-40,Gesture=gestureDeal[i],Position=new(4.5f-i*.55f,.92f,-3.3f)});}
    void TickTargetCards(float dt)
    {
        if(targetCard==null||State.Phase!=Phase.Build)return;StageGestureProps();if(State.Experiment.CustomerActor!=0||State.Twist.Kind==TwistKind.SleepingAI)return;if(openingAt<0){openingAt=State.Time;styleAt=State.Time+10;}
        if(State.Time-openingAt<15&&State.Time-questionAt>=2&&State.Props.FirstOrDefault(p=>p.Gesture>=0&&p.Holder!=0&&Vector3.Distance(p.Position,State.SharedHead.Position)<1.5f) is {} prop&&PropQuestion(prop)){questionAt=State.Time;var reading=TargetCards.Props[prop.Gesture];bool warm=targetCard.Rules.Any(r=>TargetCards.Covers(reading,r));CustomerRemarks.Say(State.Experiment,warm?Remark.StyleHot:Remark.StyleCold,State.Time);}
        if(State.Time>=styleAt){styleAt=State.Time+10;bool warm=TargetCards.Judge(State,targetCard).Count(c=>c.Match)*2>=targetCard.Rules.Length;CustomerRemarks.Say(State.Experiment,warm?Remark.StyleHot:Remark.StyleCold,State.Time);}
    }
    void RevealTarget(){if(targetCard==null)return;State.Experiment.ResultTarget=targetCard;State.Experiment.ResultStyle=TargetCards.Judge(State,targetCard);}
}
