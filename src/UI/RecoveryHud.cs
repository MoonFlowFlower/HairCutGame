using Godot;
using Hairball.Core;
using System;

namespace Hairball;
public partial class Hud
{
    public Action? NetworkContinue,NetworkRetry,NetworkLeave;
    Control? recoveryPanel;
    Label recoveryTitle=null!,recoveryBody=null!,networkWarning=null!;
    Button recoveryContinue=null!,recoveryRetry=null!,recoveryLeave=null!;
    ConfirmationDialog? leaveDialog;
    string Bi(string zh,string en)=>L.Locale=="zh"?zh:en;
    public bool RecoveryVisible=>recoveryPanel?.Visible==true;
    public void VerifyRecoveryUi()
    {
        string original=L.Locale;var oldContinue=NetworkContinue;var oldRetry=NetworkRetry;
        try
        {
            int continued=0,retried=0;NetworkContinue=()=>continued++;NetworkRetry=()=>retried++;
            foreach(string locale in new[]{"zh","en"})
            {
                L.SetLocale(locale);ShowMenu(false);ShowNetworkRecovery(ConnectionHealth.Waiting,23,true,"");
                if(!RecoveryVisible||!recoveryBody.Text.Contains("23")||!recoveryContinue.Visible)throw new Exception("Missing pause controls");
                recoveryContinue.EmitSignal(Button.SignalName.Pressed);
                ShowNetworkRecovery(ConnectionHealth.Failed,0,false,"version");
                if(!recoveryRetry.Visible||recoveryContinue.Visible||!recoveryBody.Text.Contains(locale=="zh"?"版本":"versions"))throw new Exception("Missing version failure explanation");
                recoveryRetry.EmitSignal(Button.SignalName.Pressed);
                ShowNetworkRecovery(ConnectionHealth.Reconnecting,19,false,"");if(!recoveryRetry.Visible)throw new Exception("Active recovery retry missing");recoveryRetry.EmitSignal(Button.SignalName.Pressed);
                ConfirmLeave();if(!leaveDialog!.Visible)throw new Exception("Leave confirmation missing");leaveDialog.Hide();
                ShowNetworkRecovery(ConnectionHealth.Normal,0,false,"");if(RecoveryVisible)throw new Exception("Recovery overlay persisted");
            }
            if(continued!=2||retried!=4)throw new Exception("Recovery buttons not wired");
            GD.Print("RECOVERY_UI_CHECK_OK bilingual waiting / failure / retry / continue / leave cancellation");
        }
        finally{NetworkContinue=oldContinue;NetworkRetry=oldRetry;L.SetLocale(original);HideNetworkRecovery();}
    }
    void BuildRecovery()
    {
        recoveryPanel=new Control{Visible=false,Theme=root.Theme};AddChild(recoveryPanel);recoveryPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade=new ColorRect{Color=new(0.025f,.055f,.07f,.82f)};recoveryPanel.AddChild(shade);shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var box=new VBoxContainer{Position=new(300,245),Size=new(680,360)};box.AddThemeConstantOverride("separation",18);recoveryPanel.AddChild(box);
        recoveryTitle=new Label{HorizontalAlignment=HorizontalAlignment.Center,AutowrapMode=TextServer.AutowrapMode.WordSmart};recoveryTitle.AddThemeFontSizeOverride("font_size",30);box.AddChild(recoveryTitle);
        recoveryBody=new Label{HorizontalAlignment=HorizontalAlignment.Center,AutowrapMode=TextServer.AutowrapMode.WordSmart,CustomMinimumSize=new(640,90)};box.AddChild(recoveryBody);
        recoveryContinue=new Button();recoveryRetry=new Button();recoveryLeave=new Button();
        foreach(var b in new[]{recoveryContinue,recoveryRetry,recoveryLeave}){b.CustomMinimumSize=new(600,46);box.AddChild(b);}
        recoveryContinue.Pressed+=()=>NetworkContinue?.Invoke();recoveryRetry.Pressed+=()=>NetworkRetry?.Invoke();recoveryLeave.Pressed+=ConfirmLeave;
        networkWarning=new Label{Position=new(320,190),Size=new(720,50),HorizontalAlignment=HorizontalAlignment.Center,Visible=false,Theme=root.Theme};AddChild(networkWarning);
        networkWarning.AddThemeColorOverride("font_color",new Color(1,.8f,.25f));
    }
    void ConfirmLeave()
    {
        leaveDialog??=new ConfirmationDialog();if(leaveDialog.GetParent()==null){AddChild(leaveDialog);leaveDialog.Confirmed+=()=>ReturnMenu?.Invoke();}
        leaveDialog.Title=Bi("离开房间？","Leave the room?");leaveDialog.DialogText=Bi("离开将结束你在本房间的连接。房主离开会关闭房间。","Leaving ends your connection. If you are the host, the room will close.");
        leaveDialog.GetOkButton().Text=Bi("离开房间","Leave room");leaveDialog.GetCancelButton().Text=Bi("取消","Cancel");leaveDialog.PopupCentered(new(560,200));
    }
    public void HideNetworkRecovery(){if(recoveryPanel!=null)recoveryPanel.Visible=false;if(networkWarning!=null)networkWarning.Visible=false;}
    public void ShowNetworkRecovery(ConnectionHealth state,int seconds,bool host,string reason)
    {
        if(recoveryPanel==null)BuildRecovery();
        networkWarning.Visible=state==ConnectionHealth.Unstable;
        networkWarning.Text=Bi("网络不稳定，正在恢复同步……","Network unstable. Restoring synchronization…");
        recoveryPanel!.Visible=state is ConnectionHealth.Waiting or ConnectionHealth.Reconnecting or ConnectionHealth.Synchronizing or ConnectionHealth.Failed or ConnectionHealth.Leaving;
        if(!recoveryPanel.Visible)return;
        recoveryTitle.Text=state switch{
            ConnectionHealth.Waiting=>Bi("已暂停，正在等待队友","Paused — waiting for teammates"),
            ConnectionHealth.Synchronizing=>Bi("正在同步施工现场","Synchronizing the salon"),
            ConnectionHealth.Failed=>Bi("暂时无法恢复连接","Could not restore the connection"),
            ConnectionHealth.Leaving=>Bi("正在离开房间","Leaving the room"),
            _=>Bi("连接中断，正在自动重连","Connection interrupted — reconnecting")};
        recoveryBody.Text=state==ConnectionHealth.Failed?reason switch{
            "version"=>Bi("游戏版本不一致，请双方更新到同一测试包。","Game versions differ. Both players must update to the same build."),
            "room_closed"=>Bi("房主已关闭房间。请等房主重新建房后再加入。","The host closed this room. Join again after the host creates a room."),
            "expired"=>Bi("原席位保留已结束。可以重新加入房间。","Your seat reservation expired. You can join the room again."),
            "duplicate"=>Bi("此玩家已有一个有效连接。","This player already has an active connection."),
            "full"=>Bi("房间已满，请稍后重试。","The room is full. Please try again later."),
            _=>Bi("等待恢复已超时。原因尚不确定；请确认房主和网络仍在线。","Recovery timed out. The cause is unknown; check that the host and network are available.")}
            :state==ConnectionHealth.Leaving?"":seconds>0?Bi($"最多再等待 {seconds} 秒。\n暂停期间不会消耗施工时间。",$"Waiting up to {seconds} more seconds.\nPaused time does not consume the construction timer."):Bi("正在建立连接并校验现场，请稍候。","Connecting and verifying the scene. Please wait.");
        recoveryContinue.Visible=host&&state==ConnectionHealth.Waiting;recoveryContinue.Text=Bi("不再等待，继续游戏","Continue without waiting");
        recoveryRetry.Visible=!host&&state is (ConnectionHealth.Failed or ConnectionHealth.Reconnecting or ConnectionHealth.Synchronizing);recoveryRetry.Text=Bi("重试连接","Retry connection");
        recoveryLeave.Visible=state!=ConnectionHealth.Leaving;recoveryLeave.Text=Bi("离开房间 / 返回主菜单","Leave room / Main menu");
    }
}
