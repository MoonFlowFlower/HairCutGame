using Godot;
using Hairball.Core;
namespace Hairball;
public partial class Hud
{
    public TargetCard? PrivateTarget;
    public int PrivateTargetRound;
    public bool TargetHeld,ReturnCutOn=true;
    public Texture2D? ResultPhoto;
    Label? targetLabel,standLabel;
    TextureRect? resultPhoto;
    void SetupCustomerMenu()
    {
        var setting=new CheckButton{ButtonPressed=ReturnCutOn};menuBox.AddChild(setting);setting.Toggled+=v=>ReturnCutOn=v;
        translations.Add(()=>setting.Text=L.Locale=="zh"?"顾客回赠一刀（房主，5 秒）":"Return cut (host, 5 seconds)");
    }
    void UpdateHumanHud(WorldState w,PlayerState p)
    {
        bool zh=L.Locale=="zh";var e=w.Experiment;
        targetLabel??=Text(new(940,420),new(310,145),18);standLabel??=Text(new(340,200),new(700,90),24);
        resultPhoto??=new(){Position=new(680,350),Size=new(300,225),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,MouseFilter=Control.MouseFilterEnum.Ignore,ZIndex=12};
        if(resultPhoto.GetParent()==null)root.AddChild(resultPhoto);
        targetLabel.Visible=Main.PrivateTargetEligible(w,p.Id)&&w.Twist.Kind!=TwistKind.SplitInfo&&PrivateTargetRound==w.Round&&PrivateTarget!=null&&(TargetHeld||w.Phase==Phase.Arrival)&&!e.Resolved;
        targetLabel.Text=PrivateTarget==null?"":(zh?"你的目标卡 · 按住 Tab 查看\n":"YOUR TARGET · hold Tab to view\n")+PrivateTarget.Title+(PrivateTarget.Kind==TargetKind.Emotion?"":"\n"+string.Join("\n",System.Array.ConvertAll(PrivateTarget.Rules,r=>StyleAttributes.Text(r))));
        standLabel.Visible=e.StandActive;standLabel.Text=(zh?"顾客要起身！顾客连按 W · 理发师连按 E\n":"CUSTOMER WANTS TO STAND! Customer tap W · barbers tap E\n")+new string('■',System.Math.Clamp((int)(e.StandProgress*20),0,20))+new string('□',20-System.Math.Clamp((int)(e.StandProgress*20),0,20))+"  "+System.Math.Max(0,3-(w.Time-e.StandStarted)).ToString("0.0")+"s";
        resultPhoto.Visible=w.Phase==Phase.Results&&e.ResultTarget!=null||w.Phase==Phase.Complete&&w.ReturningHeads;resultPhoto.Texture=ResultPhoto;
        if(resultPhoto.Visible){scores.Size=new(610,480);scores.HorizontalAlignment=HorizontalAlignment.Left;}
        if(p.Customer&&!e.Resolved){compactControls.Text=zh?"左键点头 · 右键摇头 · R 看向视线 · E 拍开/拿道具 · W 起身 · Tab 目标卡":"LMB nod · RMB shake · R turn head · E slap/prop · W stand · Tab target";
            tool.Text=p.Standing?(zh?"顾客 · 自由走动，回椅子旁按 E 坐下":"CUSTOMER · walk freely, E near chair to sit"):(zh?"顾客 · 你只能看见自己的目标卡":"CUSTOMER · only you can see your target card");
            if(e.LeaveStarted>=0)prompt.Text=zh?"沿地面路线走到门口 · 20 秒后就地判定":"FOLLOW THE FLOOR ROUTE · judged here after 20 seconds";
        }
        if(w.Phase==Phase.Results){prompt.Visible=true;prompt.Position=new(330,200);prompt.ZIndex=12;}else prompt.Position=new(350,455);
        UpdateTwistHud(w,p);UpdateWorldHud(w,p);
        if(w.Phase==Phase.Results)prompt.Text=w.Time<Session.PieStart(w)?(zh?"顾客回赠一刀 · 理发师可以躲！":"RETURN CUT · BARBERS CAN DODGE!"):Session.PieTime(w)?(zh?"左键扔派 · 顾客右键扔狗屎 / E 点赞":"LMB throw pie · customer RMB poop / E like"):(zh?"准备合影！":"GET READY FOR THE GROUP PHOTO!");
    }
}
