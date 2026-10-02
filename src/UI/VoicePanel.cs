using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Hairball;
public partial class VoicePanel : CanvasLayer
{
    const string SettingsPath="user://voice.cfg";
    public bool Enabled=true,PushToTalk,Missing;public float Gain=1,Output=1,Gate=.008f;public string Device="Default";
    public Action? Changed;
    public Action? PreviewChanged;
    public bool PreviewHeld { get; private set; }
    public VoiceSpecies PreviewSpecies { get; private set; }
    readonly Dictionary<int,float> volumes=new();readonly HashSet<int> muted=new();
    PanelContainer panel=null!;VBoxContainer list=null!;ColorRect shade=null!;Label status=null!,previewStatus=null!;Button previewButton=null!;string roster="";bool open;
    VBoxContainer? menuPreview;
    public bool SettingsOpen=>open;
    public void AttachMenuPreview(VBoxContainer target){menuPreview=target;Rebuild();}
    public void CancelPreview()=>SetPreviewHeld(false);
    Input.MouseModeEnum priorMouseMode;
    string T(string en,string zh)=>L.Locale=="zh"?zh:en;
    public float Volume(int actor)=>muted.Contains(actor)?0:volumes.GetValueOrDefault(actor,1);
    public override void _Ready()
    {
        Layer=35;var cfg=new ConfigFile();if(cfg.Load(SettingsPath)==Error.Ok){Enabled=cfg.GetValue("voice","enabled",true).AsBool();PushToTalk=cfg.GetValue("voice","ptt",false).AsBool();Device=cfg.GetValue("voice","device","Default").AsString();Gain=Math.Clamp((float)cfg.GetValue("voice","gain",1).AsDouble(),0,4);Output=Math.Clamp((float)cfg.GetValue("voice","output",1).AsDouble(),0,2);Gate=Math.Clamp((float)cfg.GetValue("voice","gate",.008).AsDouble(),.001f,.2f);for(int i=1;i<=32;i++){volumes[i]=Math.Clamp((float)cfg.GetValue("peers",i.ToString(),1).AsDouble(),0,2);if(cfg.GetValue("muted",i.ToString(),false).AsBool())muted.Add(i);}}
        status=new Label{Position=new(18,730),Size=new(1000,26),MouseFilter=Control.MouseFilterEnum.Ignore};status.AddThemeFontSizeOverride("font_size",16);status.AddThemeFontOverride("font",LanguageSettings.Font);AddChild(status);
        shade=new ColorRect{Color=new(0,0,0,.65f),Visible=false};shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);AddChild(shade);
        panel=new PanelContainer{Position=new(250,75),Size=new(690,650),Visible=false};panel.AddThemeFontOverride("font",LanguageSettings.Font);panel.AddThemeStyleboxOverride("panel",new StyleBoxFlat{BgColor=new("172c35"),ContentMarginLeft=18,ContentMarginRight=18,ContentMarginTop=14,ContentMarginBottom=14});AddChild(panel);var scroll=new ScrollContainer();panel.AddChild(scroll);list=new VBoxContainer{SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};scroll.AddChild(list);
        L.Changed+=Rebuild;Rebuild();
    }
    public override void _ExitTree(){L.Changed-=Rebuild;}
    public override void _Input(InputEvent e){if(e is InputEventMouseButton m&&m.ButtonIndex==MouseButton.Left&&!m.Pressed)SetPreviewHeld(false);if(e is InputEventKey k&&k.Pressed&&!k.Echo&&(k.Keycode==Key.F8||k.PhysicalKeycode==Key.F8)){ShowSettings(!open);GetViewport().SetInputAsHandled();}}
    public override void _Notification(int what){if(what==NotificationWMWindowFocusOut)SetPreviewHeld(false);}
    public void ShowSettings(bool value){if(value&&!open)priorMouseMode=Input.MouseMode;SetPreviewHeld(false);open=value;panel.Visible=shade.Visible=value;Input.MouseMode=value?Input.MouseModeEnum.Visible:priorMouseMode;}
    void SetPreviewHeld(bool value){value&=menuPreview?.IsVisibleInTree()==true&&!open;if(value==PreviewHeld)return;PreviewHeld=value;previewButton.Text=value?T("Listening… release to stop","正在试听…松开停止"):T("Hold to test voice","按住试听变声");SetPreviewSignal(false);PreviewChanged?.Invoke();}
    public void SetPreviewSignal(bool signal){if(previewStatus!=null)previewStatus.Text=PreviewHeld?(signal?T("Local preview · not sent to other players","本机试听 · 不发送给其他玩家"):T("Speak into your microphone · check input / permissions if silent","请对麦克风说话 · 无声时检查输入设备和权限")):T("Choose a species, then hold to hear your customer voice.","选择物种后按住试听，听听自己当顾客时的声音。");}
    public void SetSpeaking(IEnumerable<string> names){var text=string.Join(", ",names);status.Text=!Enabled?T("Voice off · F8 settings","语音关闭 · F8 设置"):text.Length>0?T("Speaking: ","正在说话：")+text:Missing?T("No microphone signal · F8 · Windows Settings → Privacy → Microphone","未收到麦克风信号 · F8 · Windows 设置 → 隐私 → 麦克风"):T("Voice ready · F8 settings","语音就绪 · F8 设置");}
    void Save(){var c=new ConfigFile();c.SetValue("voice","enabled",Enabled);c.SetValue("voice","ptt",PushToTalk);c.SetValue("voice","device",Device);c.SetValue("voice","gain",Gain);c.SetValue("voice","output",Output);c.SetValue("voice","gate",Gate);foreach(var (id,v) in volumes){c.SetValue("peers",id.ToString(),v);c.SetValue("muted",id.ToString(),muted.Contains(id));}c.Save(SettingsPath);}
    void Label(string text)=>list.AddChild(new Label{Text=text});
    void Toggle(string text,bool value,Action<bool> action){var b=new CheckButton{Text=text,ButtonPressed=value};list.AddChild(b);b.Toggled+=v=>{action(v);Save();};}
    void Slider(string text,float value,float max,float step,Action<float> action){var row=new HBoxContainer();list.AddChild(row);row.AddChild(new Label{Text=text,CustomMinimumSize=new(245,0)});var s=new HSlider{MinValue=0,MaxValue=max,Step=step,Value=value,CustomMinimumSize=new(320,26)};row.AddChild(s);s.ValueChanged+=v=>{action((float)v);Save();};}
    readonly List<(int Id,string Name)> names=new();
    public void UpdateNames(IEnumerable<(int Id,string Name)> source){var n=source.ToList();string signature=string.Join("|",n);if(signature==roster)return;roster=signature;names.Clear();names.AddRange(n);Rebuild();}
    void Rebuild()
    {
        if(list==null)return;SetPreviewHeld(false);foreach(var child in list.GetChildren()){list.RemoveChild(child);child.QueueFree();}
        Label(T("IN-GAME VOICE · F8","游戏内语音 · F8"));Toggle(T("Voice enabled","开启语音"),Enabled,v=>{Enabled=v;Changed?.Invoke();});
        Toggle(T("Push to talk: hold V (off = voice activation)","按键说话：按住 V（关闭时自动语音激活）"),PushToTalk,v=>PushToTalk=v);
        var device=new OptionButton();foreach(string d in AudioServer.GetInputDeviceList())device.AddItem(d);list.AddChild(device);for(int i=0;i<device.ItemCount;i++)if(device.GetItemText(i)==Device)device.Select(i);device.ItemSelected+=i=>{Device=device.GetItemText((int)i);Save();Changed?.Invoke();};
        Slider(T("Input gain","输入增益"),Gain,4,.05f,v=>Gain=v);Slider(T("Noise gate","噪声门"),Gate,.08f,.001f,v=>Gate=Math.Max(.001f,v));Slider(T("Output volume","输出音量"),Output,2,.05f,v=>Output=v);
        foreach(var (id,name) in names){Slider(name,volumes.GetValueOrDefault(id,1),2,.05f,v=>volumes[id]=v);Toggle(T("Mute ","静音 ")+name,muted.Contains(id),v=>{if(v)muted.Add(id);else muted.Remove(id);});}
        Label(T("Use headphones. No audio is saved.\nWindows: Settings → Privacy & security → Microphone.\nFiltered voice keeps tone, replaces words with synthetic syllables.","建议戴耳机。任何音频都不保存。\nWindows：设置 → 隐私和安全性 → 麦克风。\n顾客变声只保留语气，用合成音节替换话语。"));var close=new Button{Name="VoiceSettingsClose",Text=T("Close / F8","关闭 / F8")};list.AddChild(close);close.Pressed+=()=>ShowSettings(false);
        if(menuPreview==null)return;foreach(var child in menuPreview.GetChildren()){menuPreview.RemoveChild(child);child.QueueFree();}
        var previewRow=new HBoxContainer{Name="VoicePreviewRow"};menuPreview.AddChild(previewRow);
        previewRow.AddChild(new Label{Text=T("Voice test","变声测试"),CustomMinimumSize=new(135,0)});
        var species=new OptionButton{Name="VoicePreviewSpecies",CustomMinimumSize=new(200,36)};species.AddItem(T("Human · gibberish","人类 · 歪比巴卜"),(int)VoiceSpecies.Human);species.AddItem(T("Bird · chirps","鸟类 · 啾啾"),(int)VoiceSpecies.Bird);species.Select((int)PreviewSpecies);previewRow.AddChild(species);
        species.ItemSelected+=i=>{PreviewSpecies=(VoiceSpecies)species.GetItemId((int)i);if(PreviewHeld)PreviewChanged?.Invoke();};
        previewButton=new Button{Name="VoicePreviewButton",Text=T("Hold to test voice","按住试听变声"),CustomMinimumSize=new(220,36),FocusMode=Control.FocusModeEnum.None};previewRow.AddChild(previewButton);
        // A hold gesture must follow each pointer press, including after the menu
        // was hidden mid-press; BaseButton's internal press attempt can survive that hide.
        previewButton.GuiInput+=e=>{if(e is InputEventMouseButton mouse&&mouse.ButtonIndex==MouseButton.Left)SetPreviewHeld(mouse.Pressed);};previewButton.MouseExited+=()=>SetPreviewHeld(false);
        previewStatus=new Label{AutowrapMode=TextServer.AutowrapMode.WordSmart};menuPreview.AddChild(previewStatus);SetPreviewSignal(false);
        var settings=new Button{Name="MenuVoiceSettingsButton",Text=T("Microphone & volume settings…","麦克风与音量设置…")};menuPreview.AddChild(settings);settings.Pressed+=()=>ShowSettings(true);
    }
}
