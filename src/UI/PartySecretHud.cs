using Godot;
using Hairball.Core;
using System;
namespace Hairball;
public partial class Hud
{
    public bool SecretTasksOn=true;
    public PrivateTask? OwnSecret;
    Label? secretLabel,statusLabel;
    Button? secretToggle;
    bool secretHidden;
    CheckButton? secretSetting;
    void SetupSecretMenu()
    {
        var check=new CheckButton{ButtonPressed=SecretTasksOn};secretSetting=check;menuBox.AddChild(check);
        check.Toggled+=value=>SecretTasksOn=value;
        translations.Add(()=>check.Text=L.T("Secret tasks (host setting)"));
    }
    public void VerifySecretControls(WorldState w,PlayerState p)
    {
        if(secretSetting==null)throw new InvalidOperationException("secret setting missing");
        secretSetting.EmitSignal(BaseButton.SignalName.Toggled,false);if(SecretTasksOn)throw new InvalidOperationException("secret off toggle failed");secretSetting.EmitSignal(BaseButton.SignalName.Toggled,true);
        var saved=OwnSecret;bool enabled=w.Experiment.SecretsEnabled;w.Experiment.SecretsEnabled=true;OwnSecret=new(p.Id,w.Round,SecretKind.Rescue,false);UpdateSecretHud(w,p);
        secretToggle!.EmitSignal(BaseButton.SignalName.Pressed);UpdateSecretHud(w,p);if(secretLabel!.Visible)throw new InvalidOperationException("secret task did not fold");secretToggle.EmitSignal(BaseButton.SignalName.Pressed);UpdateSecretHud(w,p);if(!secretLabel.Visible)throw new InvalidOperationException("secret task did not unfold");OwnSecret=saved;w.Experiment.SecretsEnabled=enabled;
        GD.Print("PARTY_SECRET_UI_OK menu on/off and own task fold/unfold");
    }
    void UpdateSecretHud(WorldState w,PlayerState p)
    {
        if(secretLabel==null){
            secretLabel=Text(new(970,420),new(285,100),16);secretLabel.ZIndex=8;
            secretToggle=new Button{Position=new(1030,380),Size=new(225,34),ZIndex=8};root.AddChild(secretToggle);secretToggle.Pressed+=()=>secretHidden=!secretHidden;
            statusLabel=Text(new(400,580),new(680,55),20);statusLabel.ZIndex=8;
        }
        bool show=w.Phase==Phase.Build&&w.Experiment.SecretsEnabled&&OwnSecret?.Round==w.Round;
        secretToggle!.Visible=show;secretToggle.Text=L.T(secretHidden?"Show secret task":"Hide secret task");secretLabel.Visible=show&&!secretHidden;
        secretLabel.Text=OwnSecret is {} t?L.T("Your secret task")+"\n"+SecretTasks.Text(t.Kind)+"\n"+L.T(t.Complete?"Done":"No score · revealed at results"):"";
        statusLabel!.Text=w.Time<p.FreezeUntil?L.T("Frozen · teammate tap to rescue")+$" · {p.FreezeUntil-w.Time:0.0}s":w.Time<p.GlueUntil?L.T("Sticky hands · heat to rescue")+$" · {p.GlueUntil-w.Time:0.0}s":w.Time<p.FireUntil?L.T("Hair on fire · water to rescue")+$" · {p.FireUntil-w.Time:0.0}s":"";
        statusLabel.Visible=w.Phase==Phase.Build&&statusLabel.Text!="";
    }
}
