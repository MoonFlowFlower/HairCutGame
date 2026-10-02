using Godot;
using Hairball.Core;
using System.Linq;
namespace Hairball;
public partial class Hud
{
    public bool TwistsOn=true;
    public int[] GuessOptions=[];
    public string RecipeChinese="",RecipeEnglish="";
    Label? twistLabel;
    void SetupTwistMenu()
    {
        var toggle=new CheckBox{ButtonPressed=TwistsOn};menuBox.AddChild(toggle);toggle.Toggled+=v=>TwistsOn=v;translations.Add(()=>toggle.Text=L.Locale=="zh"?"变局卡（房主，第 2 局起）":"Twist cards (host, from round 2)");
        var view=new CheckBox{ButtonPressed=PresentationSettings.AllowViewChanges};menuBox.AddChild(view);view.Toggled+=v=>{PresentationSettings.AllowViewChanges=v;PresentationSettings.Save();};translations.Add(()=>view.Text=L.Locale=="zh"?"允许镜像/布罩/暗场视图（默认关闭）":"Allow flipped/covered/dark views (default off)");
    }
    void UpdateTwistHud(WorldState w,PlayerState p)
    {
        twistLabel??=Text(new(12,175),new(440,190),17);var card=TwistCards.Card(w.Twist.Kind);twistLabel.Visible=card!=null&&!menu.Visible;
        if(card==null)return;bool zh=L.Locale=="zh";twistLabel.Text=card.Title+" · "+card.Category+"\n"+card.Rule+"\n"+card.Condition;
        if(w.Twist.Kind==TwistKind.LimitedQuestions)twistLabel.Text+="\n"+(zh?"提问":"Questions")+$": {w.Twist.Questions}/6";
        if(w.Twist.Kind==TwistKind.OneViewer)twistLabel.Text+="\n"+(zh?"观察者":"Observer")+": "+w.Player(w.Twist.ViewerActor)?.Name;
        if(w.Twist.Kind==TwistKind.Family)twistLabel.Text+="\n"+(zh?"家属":"Relative")+": "+w.Player(w.Twist.FamilyActor)?.Name;
        if(w.Twist.Kind==TwistKind.MiniDemo)twistLabel.Text+="\n"+L.T(Tools.Get(w.Twist.DemoTool).Name)+$" · {System.Math.Max(0,10-(w.Time-w.Twist.Started)):0.0}s";
        if(p.Customer&&w.Twist.Kind==TwistKind.Contrarian)twistLabel.Text+="\n"+(zh?"你的点头在别人眼里是摇头！":"Your nod looks like a shake to everyone else!");
        if(p.Customer&&w.Twist.Kind==TwistKind.Reverse&&GuessOptions.Length==4)twistLabel.Text+="\n"+string.Join("\n",GuessOptions.Select((id,i)=>$"{i+1}: {TargetCards.Cards[id-1].Title}"));
        if(p.Customer&&w.Twist.Kind==TwistKind.SplitInfo)twistLabel.Text+="\n"+(zh?RecipeChinese:RecipeEnglish);
        if(w.Time<w.Twist.HintUntil)twistLabel.Text+="\n"+(zh?w.Twist.HintChinese:w.Twist.HintEnglish);
        if(w.Experiment.Resolved&&w.Twist.ResultFamily is {} family)scores.Text+="\n"+(zh?"家属目标":"RELATIVE TARGET")+": "+family.Face(true)+"\n"+string.Join(" · ",w.Twist.FamilyChecks.Select(c=>(c.Match?"✓ ":"× ")+StyleAttributes.Text(c.Rule)));
        if(w.Experiment.Resolved&&w.Twist.Kind==TwistKind.Reverse)scores.Text+="\n"+(w.Twist.GuessCorrect?(zh?"猜对了 · 小费 +10":"Correct guess · tip +10"):(zh?"没有猜中":"Guess missed"));
    }
}
