using Godot;
using Hairball.Core;
using System;
using System.Linq;
namespace Hairball;
public partial class Hud
{
    ColorRect? partyResultPanel;
    public MotionFrame? LiveMotion;
    public string ExperimentGoalText=>goal.Text;
    public string ExperimentToolText=>tool.Text;
    public string ExperimentPromptText=>prompt.Text;
    void UpdateExperiment(WorldState w,PlayerState p)
    {
        if(!w.Experiment.IsB)return;
        bool results=w.Phase is Phase.Results or Phase.Complete;
        tool.Visible=prompt.Visible=notice.Visible=!results;controls.Visible=false;compactControls.Visible=!results;
        if(partyResultPanel==null){partyResultPanel=new(){Position=new(16,255),Size=new(948,345),Color=new Color(.07f,.16f,.20f,.94f),MouseFilter=Control.MouseFilterEnum.Ignore,ZIndex=10};root.AddChild(partyResultPanel);}
        partyResultPanel.Visible=results;scores.ZIndex=11;scores.HorizontalAlignment=HorizontalAlignment.Left;
        bool zh=L.Locale=="zh";var e=w.Experiment;var heli=w.Props.FirstOrDefault(x=>x.Id==e.HelicopterId);
        UpdateSecretHud(w,p);
        choices.Visible=false;spotlight.Visible=false;
        top.Text="B · "+(w.Phase==Phase.Build?$"{Math.Max(0,w.Remaining):00}s · "+L.T(e.Leave.ToString()):L.T(w.Phase.ToString().ToUpperInvariant()));
        goal.Size=new(920,190);goal.Text=L.T("Put the helicopter on the hair. Keep it supported until the customer reaches the door.");
        goal.Text+="\n"+L.T("Place anywhere; test without ending the round.");
        if(heli?.Attached==true)
        {
            goal.Text+="\n"+LandingFeedback.Describe(e.LandingIssues,zh)+(PresentationSettings.Numbers?$" · {e.StableSeconds:0.0}/3s":"");
            var zones=ExperimentText.LandingZones(w,heli,e.LandingIssues).Where(x=>x.Issues!=LandingIssue.None&&MaterialFeel.Facing(Session.Eye(p),w.SharedHead.Position,x.Position)>.55f).Take(2);
            goal.Text+="\n"+string.Join("; ",zones.Select(x=>ExperimentText.ZoneName(x.Column,x.Row,zh)+": "+ExperimentText.ZoneAdvice(x.Issues,zh)));
        }
        else if(e.SlippedAt>=0)goal.Text+="\n"+L.T("Commission slipped")+$" · {e.SlippedAt:0.0}s · "+LandingFeedback.Describe(e.SlipIssues,zh);
        orderBanner.Visible=w.Time<e.ComboUntil;orderBanner.Text=ComboText.Text(e.Combo);orderBanner.Position=new(270,250);orderBanner.Size=new(740,70);orderBanner.AddThemeFontSizeOverride("font_size",26);
        goal.Text+="\n"+L.T("Customer tolerance")+(PresentationSettings.Numbers?$" {e.Tolerance:0}/100":" "+(e.Tolerance>=70?"!!":e.Tolerance>=40?"!":"○"))+" · "+(w.Time<e.RemarkUntil?CustomerRemarks.Text(e.Remark):"");
        if(e.Leave==LeaveStage.Rising)goal.Text+=" · "+L.T("Seat delay remaining")+$" {PartyLoop.MaxDelay-e.DelayUsed:0}s";
        compactControls.Text=L.T("E: pick up / place; hold E: brace; G: drop");
        controls.Text=L.T("Put the helicopter on the hair. Keep it supported until the customer reaches the door.")+"\n"+L.T("Place anywhere; test without ending the round.");
        if(p.Bracing){tool.Text=zh?"正在扶头 · 松开 E 释放 · 工具暂停":"BRACING · release E · tool paused";prompt.Text="";}
        else if(p.CarriedProp>=0)tool.Text=L.T("E: pick up / place; hold E: brace; G: drop");
        else if(w.Phase==Phase.Build&&Session.LookingAt(p,w.SharedHead.Position,.75f,1.9f))prompt.Text=zh?"按住 E · 扶稳头部":"HOLD E · BRACE HEAD";
        if(w.Phase==Phase.Build&&Session.LookingAt(p,PartyLoop.Bell,.25f,2.5f))prompt.Text=heli?.Attached==true?L.T("E: leave early"):L.T("Place the commission first");
        if(w.Tools.FirstOrDefault(t=>t.Id==p.Held) is {Definition:1} growth&&!p.Bracing)
        {
            string amount=growth.GrowthRemaining is >0 and <.01f?"<0.01":growth.GrowthRemaining.ToString("0.00");
            string status=growth.GrowthRemaining<=0?(zh?"材料用尽":"SUPPLY EMPTY"):w.Time-growth.LastUse<.23f?(growth.HitHair?(growth.EffectMass>0?(zh?"正在生长":"GROWING"):(zh?"命中 · 未生长":"CONTACT · NO GROWTH")):(zh?"未命中头发":"NO HAIR CONTACT")):(zh?"瞄准头发":"AIM AT HAIR");
            tool.Text=(PresentationSettings.Numbers?(zh?"本瓶剩余 ":"This bottle ")+amount+"/1.0 · ×"+GrowthSpray.Power(p.GrowthHeldSeconds).ToString("0.0"):(p.GrowthFine?(zh?"细喷":"FINE SPRAY"):p.GrowthHeldSeconds>.6f?(zh?"强喷":"POWER SPRAY"):(zh?"生发喷雾":"GROWTH SPRAY")))+"\n"+status;
        }
        if(w.Props.FirstOrDefault(t=>t.Id==p.CarriedProp) is {Mold:not MoldKind.None})tool.Text=L.T("Hold mold against hair; a teammate sprays. Nail to fix; E places; G drops.");
        if(p.CarriedHair>=0)tool.Text=zh?"手持假发 · E 戴上 · 按住 G 蓄力投掷":"WIG IN HAND · E attach · hold G to throw";
        if(w.Props.FirstOrDefault(t=>t.Id==p.CarriedProp&&t.Gesture>=0) is {} gesture)tool.Text=TargetCards.Props[gesture.Gesture].Name+" · "+(zh?"E 放下 · G 投掷 · 举给顾客看":"E place · G throw · show the customer");
        if(p.ThrowCharge>0)tool.Text="◀"+new string('■',(int)(p.ThrowCharge*10))+new string('□',10-(int)(p.ThrowCharge*10))+"▶ "+(zh?"松开 G 投掷":"release G to throw");
        if(PartyBodies.Empty(p)&&Session.LookingAt(p,Session.ChairControl(0),.5f,3))prompt.Text=zh?"左 / 右键转椅子 · R / F 升降":"LMB / RMB rotate chair · R / F raise/lower";
        if(Session.CanSlapAI(w,p))prompt.Text=zh?"左键 · 扇醒顾客 · 按住 E 扶头":"LMB · SLAP CUSTOMER AWAKE · hold E to brace";
        if(w.Time<p.DownUntil)prompt.Text=zh?"倒地 · 等待队友按 E 拍醒":"DOWNED · teammate E slaps you awake";
        else if(w.Players.Any(q=>q.Id!=p.Id&&w.Time<q.DownUntil&&Session.LookingAt(p,q.Position+System.Numerics.Vector3.UnitY*.8f,.6f,2.5f)))prompt.Text=zh?"E · 拍醒队友":"E · SLAP TEAMMATE AWAKE";
        if(w.Tools.FirstOrDefault(t=>t.Id==p.Held) is {Definition:2})tool.Text=L.T("LMB: recycle 60% (charred 30%); RMB: patch with stored hair")+$" · {p.Reservoir:0.00}/20";
        if(w.Phase is Phase.Results or Phase.Complete){
            partyResultPanel.Size=new(948,495);scores.AddThemeFontSizeOverride("font_size",16);
            scores.Size=new(930,480);scores.Position=new(34,270);scores.Text=(e.Success?(zh?"完成 · 护送到门口":"COMPLETE · SUPPORTED AT DOOR"):(zh?"未完成\n":"INCOMPLETE\n")+LandingFeedback.Describe(e.LandingIssues,zh,true))+"\n"+PartyResults.Reaction(e.Curtain)+"\n"+L.T("Base pay")+$" ${e.BasePay} · "+L.T("Tip")+$" ${e.Tip} · "+L.T("Shop wallet")+$" ${w.Job.Wallet}"+"\n"+string.Join("\n",e.ResultActions.Select(PartyResults.Text))+"\n"+(e.ResultTarget==null?"":e.ResultTarget.Title+"\n"+string.Join("\n",e.ResultStyle.Select(c=>(c.Match?"✓ ":"✗ ")+StyleAttributes.Text(c.Rule)))+"\n")+string.Join(" · ",e.ResultHair.Select(h=>h.Name+(zh?" 本场头发减少 ":" hair reduced ")+h.LossPercent.ToString("0")+"%"))+"\n"+(e.ResultTasks.Count>0?L.T("Secret tasks · no score")+"\n"+string.Join("\n",e.ResultTasks.Select(SecretTasks.Reveal)):"");}
        UpdateHumanHud(w,p);
        if(w.Phase==Phase.Lobby&&w.Players.Any(q=>q.Active&&!q.VoiceEnabled))notice.Text=zh?"有玩家关闭语音：请用手势沟通":"A player disabled voice: use gestures";
    }
}

