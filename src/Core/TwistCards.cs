using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
namespace Hairball.Core;

public enum TwistKind { None,FogMirror,MirrorWorld,MiniDemo,PropsOnly,Contrarian,LimitedQuestions,Reverse,Family,OneViewer,BlindBuild,SplitInfo,CustomerHelper,SleepingAI,PowerOut,Fan,LowGravity }
public sealed record TwistCard(TwistKind Kind,char Category,string Chinese,string English,string ChineseRule,string EnglishRule,bool Human=false,bool AI=false,int Players=1,int Barbers=1,bool Comfort=false)
{
    public string Title=>L.Locale=="zh"?Chinese:English;
    public string Rule=>L.Locale=="zh"?ChineseRule:EnglishRule;
    public string Condition=>L.Locale=="zh"?$"{(Human?"真人顾客 · ":AI?"AI 顾客 · ":"")}至少 {Players} 人 / {Barbers} 名理发师":$"{(Human?"Human customer · ":AI?"AI customer · ":"")}Min {Players} players / {Barbers} barbers";
}
public sealed record FogLine(Vector2 From,Vector2 To);
// Only public modifiers and visible line geometry. Private options/cards live in Session.
public sealed class TwistState
{
    public TwistKind Kind;
    public int FamilyActor,ViewerActor,HelperTool=3,DemoTool,Questions;
    public float Started,FogUntil,HintUntil,QuestionAt=-100;
    public string HintChinese="",HintEnglish="",Cancelled="";
    public List<FogLine> Lines=new();
    public TargetCard? ResultFamily;
    public List<StyleCheck> FamilyChecks=new();
    public bool GuessCorrect;
    public TwistState Clone()=>new(){Kind=Kind,FamilyActor=FamilyActor,ViewerActor=ViewerActor,HelperTool=HelperTool,DemoTool=DemoTool,Questions=Questions,Started=Started,FogUntil=FogUntil,HintUntil=HintUntil,QuestionAt=QuestionAt,HintChinese=HintChinese,HintEnglish=HintEnglish,Cancelled=Cancelled,Lines=new(Lines),ResultFamily=ResultFamily,FamilyChecks=new(FamilyChecks),GuessCorrect=GuessCorrect};
}
public static class TwistCards
{
    public static readonly TwistCard[] All=[
        new(TwistKind.FogMirror,'A',"雾镜作画","Fog canvas","吹风/喷水起雾；顾客按住左键作画，20 秒消散","Blow or spray the mirror; customer holds LMB to draw; fades in 20s",Human:true,Players:2),
        new(TwistKind.MirrorWorld,'A',"镜像世界","Mirror world","顾客的世界画面左右翻转","Customer sees the world flipped horizontally",Human:true,Players:2,Comfort:true),
        new(TwistKind.MiniDemo,'A',"迷你示范","Mini demonstration","顾客用指定工具示范 10 秒，模型留在桌上","Customer demonstrates with the assigned tool for 10s; model stays on table",Human:true,Players:2),
        new(TwistKind.PropsOnly,'A',"只能用道具","Props only","顾客只能举道具和走动，禁用点头摇头","Customer can hold props and walk; nod and shake disabled",Human:true,Players:2),
        new(TwistKind.Contrarian,'A',"反话局","Contrarian","别人看到的点头和摇头互换","Other players see nod and shake exchanged",Human:true,Players:2),
        new(TwistKind.LimitedQuestions,'A',"限次提问","Six questions","全队最多举道具提问 6 次","The team has at most six prop questions"),
        new(TwistKind.Reverse,'B',"反转","Reverse","理发师拿目标卡；顾客看不到头发，按 1–4 猜目标","Barbers receive the target; customer cannot see hair, presses 1–4 to guess",Human:true,Players:2,Comfort:true),
        new(TwistKind.Family,'B',"挑剔的家属","Fussy relative","家属拿另一张卡，可以说话但不能做头发","Relative receives another card, can speak but cannot edit hair",Human:true,Players:4,Barbers:3),
        new(TwistKind.OneViewer,'B',"只有一个人看得见","One observer","只有指定理发师看得到顾客的表达动作","Only the selected barber sees the customer's expressive gestures",Human:true,Players:3,Barbers:2),
        new(TwistKind.BlindBuild,'B',"蒙眼施工","Covered construction","理发师只看到布罩轮廓；顾客看到真实状态","Barbers see a cloth outline; customer sees actual hair",Human:true,Players:2,Comfort:true),
        new(TwistKind.SplitInfo,'B',"各知一半","Split information","目标在后脑勺上；顾客只拿制作提示","Target appears on back of head; customer receives only a recipe",Human:true,Players:2),
        new(TwistKind.CustomerHelper,'C',"顾客帮手","Customer helper","顾客只能用吹风机操作道具和模具","Customer may use only a blower, affecting props and molds",Human:true,Players:2),
        new(TwistKind.SleepingAI,'C',"顾客睡着了","Sleep talk","AI 用梦话碎词提示目标","AI offers fragmented target clues in sleep talk",AI:true),
        new(TwistKind.PowerOut,'D',"停电","Power cut","只剩玩家头顶手电光","Only players' head flashlights remain",Comfort:true),
        new(TwistKind.Fan,'D',"风扇","Shop fan","周期性弱风吹动轻物和松软头发","Periodic weak wind moves light props and soft hair"),
        new(TwistKind.LowGravity,'D',"低重力","Low gravity","物件和碎发重力降到 35%，玩家不变","Props and clippings have 35% gravity; player gravity unchanged")];
    public static TwistCard? Card(TwistKind kind)=>All.FirstOrDefault(c=>c.Kind==kind);
    public static bool Eligible(TwistCard card,WorldState w)
    {
        bool human=w.Experiment.CustomerActor!=0;var actors=w.Players.Where(p=>p.Active&&!p.NetworkAway).ToArray();
        if(card.Human&&!human||card.AI&&human||actors.Length<card.Players||Session.BarberCount(w)<card.Barbers)return false;
        if(!card.Comfort)return true;
        return card.Kind is TwistKind.MirrorWorld or TwistKind.Reverse?actors.Any(p=>p.Customer&&p.AllowViewChanges):card.Kind==TwistKind.BlindBuild?actors.Where(p=>!p.Customer).All(p=>p.AllowViewChanges):actors.All(p=>p.AllowViewChanges);
    }
    public static TwistKind Draw(WorldState w,int seed,TwistKind last)
    {
        var pool=All.Where(c=>Eligible(c,w)).ToArray();char previous=Card(last)?.Category??' ';
        var different=pool.Where(c=>c.Category!=previous).ToArray();if(different.Length>0)pool=different;
        return pool.Length==0?TwistKind.None:pool[new Random(seed).Next(pool.Length)].Kind;
    }
    public static bool BlockHair(WorldState w,PlayerState p,Head h)=>w.Twist.Kind==TwistKind.MiniDemo&&h.Miniature&&!p.Customer&&w.Time-w.Twist.Started<10||p.Id==w.Twist.FamilyActor||p.Customer&&w.Twist.Kind==TwistKind.CustomerHelper||p.Customer&&w.Twist.Kind==TwistKind.MiniDemo&&(!h.Miniature||w.Time-w.Twist.Started>=10);
    public static bool AllowExpression(WorldState w,PlayerState p)=>w.Twist.Kind!=TwistKind.PropsOnly&&!(w.Twist.Kind==TwistKind.LimitedQuestions&&w.Twist.Questions>=6&&w.Time-w.Twist.QuestionAt>1)&&!(w.Twist.Kind==TwistKind.CustomerHelper&&p.Held>=0);
    public static float Gravity(WorldState w)=>w.Phase==Phase.Build&&w.Twist.Kind==TwistKind.LowGravity?.35f:1;
    public static Vector3 Gesture(WorldState w,int kind)
    {
        var p=w.Players.FirstOrDefault(p=>p.Active&&p.Customer);if(p==null)return default;
        return Gesture(w.Time,p.Expression,p.ExpressionAt,kind,w.Players.Any(p=>p.Active&&p.Bracing));
    }
    public static Vector3 Gesture(float time,int expression,float at,int kind,bool brace)
    {
        float age=time-at,duration=expression==1?.6f:.8f;
        float wave=age>=0&&age<duration?MathF.Sin(age/duration*MathF.Tau):0;
        return(kind==1?new Vector3(MathF.PI/15*wave,0,0):new Vector3(0,MathF.PI/12*wave,0))*(brace?.22f:1);
    }
    public static Vector3 DisplayDelta(WorldState w,int viewer,float time,int expression,float at,bool brace)
    {
        if(viewer==w.Experiment.CustomerActor||w.Experiment.Resolved)return default;var actual=Gesture(time,expression,at,expression,brace);return w.Twist.Kind==TwistKind.Contrarian?Gesture(time,expression,at,expression==1?2:1,brace)-actual:w.Twist.Kind==TwistKind.OneViewer&&viewer!=w.Twist.ViewerActor?-actual:default;
    }
    public static Vector3 DisplayDelta(WorldState w,int viewer)
    {
        var p=w.Players.FirstOrDefault(p=>p.Active&&p.Customer);if(p==null||viewer==p.Id||w.Experiment.Resolved)return default;
        var actual=Gesture(w,p.Expression);
        return w.Twist.Kind==TwistKind.Contrarian?Gesture(w,p.Expression==1?2:1)-actual:w.Twist.Kind==TwistKind.OneViewer&&viewer!=w.Twist.ViewerActor?-actual:default;
    }
    public static bool CoverHair(WorldState w,int viewer)=>!w.Experiment.Resolved&&(w.Twist.Kind==TwistKind.Reverse&&w.Player(viewer)?.Customer==true||w.Twist.Kind==TwistKind.BlindBuild&&w.Player(viewer)?.Customer==false);
}
public sealed partial class Session
{
    public bool TwistsEnabled=true;
    public int MatchRounds=1;
    public Func<WorldState,TwistKind>? TwistPicker;public Func<int,int>? FamilyPicker;
    TargetCard? familyTarget;int[] reverseOptions=[];int reverseGuess=-1;string recipeChinese="",recipeEnglish="";
    TwistKind lastTwist;
    readonly Dictionary<int,(int Prop,float At)> asked=new();
    readonly Dictionary<int,Vector2> fogPoints=new();
    Head? demonstration;
    float dreamAt;
    public int[] OwnOptions(int actor)=>State.Player(actor)?.Customer==true&&State.Twist.Kind==TwistKind.Reverse?(int[])reverseOptions.Clone():[];
    public (string Chinese,string English) OwnRecipe(int actor)=>State.Player(actor)?.Customer==true&&State.Twist.Kind==TwistKind.SplitInfo?(recipeChinese,recipeEnglish):("","");
    public bool GuessTarget(int actor,int option)
    {if(State.Twist.Kind!=TwistKind.Reverse||State.Player(actor)?.Customer!=true||State.Experiment.Resolved||option<0||option>=reverseOptions.Length)return false;reverseGuess=option;return true;}
    public void RestageTwistForQA()=>StageTwists();
    void StageTwists()
    {
        familyTarget=null;reverseOptions=[];reverseGuess=-1;recipeChinese=recipeEnglish="";demonstration=null;asked.Clear();fogPoints.Clear();dreamAt=State.Time+8;
        State.MatchRounds=MatchRounds;State.Twist=new();if(!State.Experiment.IsB||!TwistsEnabled||State.Round==1)return;
        var kind=TwistPicker?.Invoke(State)??TwistCards.Draw(State,RandomNumberGenerator.GetInt32(int.MaxValue),lastTwist);
        if(TwistCards.Card(kind) is not {} card||!TwistCards.Eligible(card,State))return;
        State.Twist=new(){Kind=kind,Started=State.Time};lastTwist=kind;var t=State.Twist;
        var barbers=State.Players.Where(p=>p.Active&&!p.NetworkAway&&!p.Customer).ToArray();
        if(kind==TwistKind.Family){t.FamilyActor=barbers[(State.Round-2)%barbers.Length].Id;familyTarget=FamilyPicker!=null?TargetCards.Cards[FamilyPicker(State.Round)%TargetCards.Cards.Length]:TargetCards.Cards.Where(c=>c.Id!=targetCard?.Id).OrderBy(_=>RandomNumberGenerator.GetInt32(int.MaxValue)).First();}
        if(kind==TwistKind.OneViewer)t.ViewerActor=barbers[(State.Round-2)%barbers.Length].Id;
        if(kind==TwistKind.Reverse&&targetCard!=null)reverseOptions=TargetCards.Cards.Where(c=>c.Id!=targetCard.Id).OrderBy(_=>RandomNumberGenerator.GetInt32(int.MaxValue)).Take(3).Append(targetCard).OrderBy(_=>RandomNumberGenerator.GetInt32(int.MaxValue)).Select(c=>c.Id).ToArray();
        if(kind==TwistKind.SplitInfo){recipeChinese="先冻住再剪；最后再固定委托物";recipeEnglish="Freeze before cutting; secure the commission last";}
        if(kind==TwistKind.MiniDemo){t.DemoTool=RandomNumberGenerator.GetInt32(2);CreateDemo();GiveCustomerTool(t.DemoTool);}
        if(kind==TwistKind.CustomerHelper)GiveCustomerTool(t.HelperTool);
    }
    void GiveCustomerTool(int definition)
    {if(State.Player(State.Experiment.CustomerActor) is {} p){Drop(p);var tool=new ToolState{Id=1800,Definition=definition,Holder=p.Id,Charge=-1};State.Tools.Add(tool);p.Held=tool.Id;}}
    void CreateDemo()
    {
        var h=demonstration?.Clone()??Head.Create(State.NextHead++,0);h.Miniature=true;h.Loose=true;h.GeometryScale=MiniatureModel.Scale;h.Position=State.Time-State.Twist.Started<10?new(0,1.8f,1.05f):new(-3,2.1528f,-3.6f);h.Velocity=default;h.Locked=State.Time-State.Twist.Started>=10;State.Heads.Add(h);
    }
    bool DemoActive(PlayerState p)=>p.Customer&&State.Twist.Kind==TwistKind.MiniDemo&&State.Time-State.Twist.Started<10;
    bool TwistToolAllowed(PlayerState p,int definition)=>!(p.Customer&&State.Twist.Kind==TwistKind.CustomerHelper&&definition!=State.Twist.HelperTool)&&!(DemoActive(p)&&definition!=State.Twist.DemoTool);
    bool FogInput(PlayerState p,Buttons buttons)
    {
        if(State.Twist.Kind!=TwistKind.FogMirror||!p.Customer||State.Time>=State.Twist.FogUntil||!buttons.HasFlag(Buttons.Primary)){fogPoints.Remove(p.Id);return false;}
        if(!MirrorAim(p,out var point)){fogPoints.Remove(p.Id);return false;}
        point=new(MathF.Round(point.X*200)/200,MathF.Round(point.Y*200)/200);
        if(fogPoints.TryGetValue(p.Id,out var last)&&Vector2.Distance(last,point)>.005f){State.Twist.Lines.Add(new(last,point));if(State.Twist.Lines.Count>256)State.Twist.Lines.RemoveAt(0);}fogPoints[p.Id]=point;return true;
    }
    public static bool MirrorAim(PlayerState p,out Vector2 point)
    {
        point=default;var aim=Aim(p);if(Math.Abs(aim.Z)<.001f)return false;float along=(MirrorPoint.Z-Eye(p).Z)/aim.Z;
        if(along<0||along>Tools.Get(3).Range)return false;var hit=Eye(p)+aim*along-MirrorPoint;point=new(hit.X,hit.Y);return Math.Abs(hit.X)<=.55f&&Math.Abs(hit.Y)<=.425f;
    }
    void TwistTool(PlayerState p,ToolState tool)
    {if(State.Twist.Kind==TwistKind.FogMirror&&tool.Definition is 3 or 10&&MirrorAim(p,out _)){if(State.Time>=State.Twist.FogUntil)State.Twist.Lines.Clear();State.Twist.FogUntil=State.Time+20;}}
    public bool PropQuestion(PropState prop)
    {
        var t=State.Twist;if(t.Kind!=TwistKind.LimitedQuestions)return true;if(t.Questions>=6)return false;
        if(!asked.TryGetValue(prop.Holder,out var prior)||prior.Prop!=prop.Id){asked[prop.Holder]=(prop.Id,State.Time);t.Questions++;t.QuestionAt=State.Time;return true;}
        return false;
    }
    void TickTwists(float dt)
    {
        var t=State.Twist;if(t.Kind==TwistKind.None||State.Experiment.Resolved)return;
        if(TwistCards.Card(t.Kind) is {} card&&!TwistCards.Eligible(card,State)){t.Cancelled="conditions changed";t.Kind=TwistKind.None;return;}
        if(t.Kind==TwistKind.MiniDemo){var h=State.Heads.FirstOrDefault(h=>h.Miniature);if(h==null){CreateDemo();h=State.Heads.First(h=>h.Miniature);}if(State.Time-t.Started>=10&&!h.Locked){h.Locked=true;h.Position=new(-3,2.1528f,-3.6f);h.Velocity=default;foreach(var p in State.Players.Where(p=>p.Customer))if(State.Tools.Any(x=>x.Id==p.Held&&x.Id==1800))Drop(p);}demonstration=h.Clone();}
        if(State.Phase!=Phase.Build)return;
        foreach(var id in asked.Keys.ToArray())if(!State.Props.Any(p=>p.Holder==id&&p.Gesture>=0&&Vector3.Distance(p.Position,State.SharedHead.Position)<1.5f))asked.Remove(id);
        if(t.Kind==TwistKind.LimitedQuestions&&State.Experiment.CustomerActor!=0)foreach(var prop in State.Props.Where(p=>p.Holder!=0&&p.Gesture>=0&&Vector3.Distance(p.Position,State.SharedHead.Position)<1.5f))PropQuestion(prop);
        if(t.Kind==TwistKind.SleepingAI&&State.Time>=dreamAt&&targetCard!=null){dreamAt=State.Time+8;var rule=targetCard.Rules[(int)(State.Time/8)%targetCard.Rules.Length];t.HintChinese="……"+DreamWord(rule,true)+"……";t.HintEnglish="… "+DreamWord(rule,false)+" …";t.HintUntil=State.Time+4;CustomerRemarks.Say(State.Experiment,Remark.StyleHot,State.Time);}
        if(t.Kind==TwistKind.Fan&&MathF.Sin((State.Time-t.Started)*MathF.PI/4)>.55f){
            foreach(var patch in State.SharedHead.Patches.Where(p=>p.Resistance<.6f))HairSystem.Apply(State.SharedHead,patch,new(EffectKind.ApplyForce,dt*.035f,State.SharedHead.LocalDirection(Vector3.UnitX)));
            foreach(var prop in State.Props.Where(p=>p.Holder==0&&!p.Attached))prop.Velocity+=Vector3.UnitX*dt*.5f;
            DebrisSystem.Blow(State.Debris,new(-5,1,0),Vector3.UnitX,10,3);
        }
    }
    static string DreamWord(StyleRule r,bool zh) {var word=r.Attribute switch {StyleAttribute.Height=>r.Min>1?"高高的|tall":"低低的|low",StyleAttribute.Horns=>"角……好多|horns… more",StyleAttribute.Wet=>r.Max<.5f?"不要湿的|not wet":"雨……湿|rain… wet",StyleAttribute.Ice=>"冷……冰|cold… ice",StyleAttribute.Glue=>"粘住|sticky",StyleAttribute.Wig=>"假发|wig",StyleAttribute.Char=>"黑……烧|black… burnt",StyleAttribute.Young=>"新……长|new… growth",StyleAttribute.Side=>"一边……风|one side… wind",StyleAttribute.Front=>"前后|front… back",StyleAttribute.Object=>"东西……那里|thing… there",_=>"蓬松|fluffy"};return word.Split('|')[zh?0:1];}
    void RevealTwist(bool success)
    {
        var t=State.Twist;int extra=0;
        if(familyTarget!=null){t.ResultFamily=familyTarget;t.FamilyChecks=TargetCards.Judge(State,familyTarget);extra+=t.FamilyChecks.Count(c=>c.Match)*10;}
        t.GuessCorrect=t.Kind==TwistKind.Reverse&&reverseGuess>=0&&reverseOptions[reverseGuess]==targetCard?.Id;if(t.GuessCorrect)extra+=10;
        if(extra>0){var e=State.Experiment;int mainMatches=e.ResultStyle.Count(c=>c.Match)*10;int early=success?(int)(Math.Max(0,e.EarlySeconds)/10)*5:0;e.Tip=Math.Max(0,mainMatches+early+extra-Math.Max(0,State.Job.Penalty));}
    }
}
