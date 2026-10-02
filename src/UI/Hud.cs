using Godot;
using Hairball.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using static Hairball.Core.L;

namespace Hairball;
public partial class Hud : CanvasLayer
{
    public Action<string,string,int>? Start;
    public Action? Begin,ReturnMenu,Restart,ResetLab;
    public Action<int>? Choose;
    Control root=null!,menu=null!;
    VBoxContainer menuBox=null!;
    Label top=null!,goal=null!,tool=null!,prompt=null!,notice=null!,scores=null!,debug=null!;
    Label spotlight=null!,orderBanner=null!;
    HBoxContainer choices=null!;
    ColorRect fringe=null!;
    readonly System.Collections.Generic.List<ColorRect> splashes=new();
    Button startButton=null!,returnButton=null!,resetButton=null!;
    bool built;
    Label controls=null!,compactControls=null!;
    OptionButton inGameLanguage=null!;
    int choiceKey=-1;
    public bool DebugVisible;
    public bool BPlaytest;
    public string NetworkStatus="";
    public bool MenuVisible=>menu.Visible;
    public VBoxContainer VoiceSettingsHost { get; private set; }=null!;
    public Action? MenuHidden;
    readonly List<Action> translations=new();
    readonly List<OptionButton> languagePickers=new();
    Label menuMessage=null!;
    string messageSource="";
    (WorldState World,int Id,bool Authority,bool Lab,SubViewport Portrait,int Bytes)? lastUpdate;
    string preferencePath=LanguageSettings.Path;
    public override void _Ready()
    {
        root=new Control();AddChild(root);root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);root.MouseFilter=Control.MouseFilterEnum.Ignore;
        var theme=new Theme();theme.DefaultFontSize=20;theme.DefaultFont=LanguageSettings.Font;root.Theme=theme;
        top=Text(new(24,18),new(850,55),26);goal=Text(new(24,77),new(880,115),20);
        tool=Text(new(24,655),new(950,88),20);prompt=Text(new(350,455),new(650,60),22);
        notice=Text(new(24,605),new(1000,60),18);debug=Text(new(24,210),new(550,190),16);
        scores=Text(new(180,205),new(920,365),23);scores.HorizontalAlignment=HorizontalAlignment.Center;
        spotlight=Text(new(260,390),new(760,170),18);spotlight.HorizontalAlignment=HorizontalAlignment.Center;
        orderBanner=Text(new(370,150),new(640,75),18);orderBanner.HorizontalAlignment=HorizontalAlignment.Center;
        var cross=Text(new(625,377),new(30,30),24);cross.Text="+";cross.HorizontalAlignment=HorizontalAlignment.Center;
        controls=Text(new(24,754),new(1080,40),16);Bind(controls,"WASD move • Space jump • R/F chair   Mouse aim   E pick up / place   LMB use   RMB alternate   G drop   Esc cursor   F3 debug");
        compactControls=Text(new(24,770),new(950,25),13);Bind(compactControls,"E interact · G drop · Space jump · R/F chair · Tab board · H ghost · Esc options");
        choices=new(){Position=new(25,500),Size=new(940,85)};root.AddChild(choices);
        startButton=new(){Text="START MATCH [Enter]",Position=new(490,480),Size=new(300,52)};root.AddChild(startButton);startButton.Pressed+=()=>Begin?.Invoke();
        returnButton=new(){Name="LeaveRoomButton",Text="MENU",Position=new(1125,735),Size=new(125,40)};root.AddChild(returnButton);returnButton.Pressed+=ConfirmLeave;
        resetButton=new(){Text="RESET LAB [F5]",Position=new(1020,280),Size=new(230,45),Visible=false};root.AddChild(resetButton);resetButton.Pressed+=()=>ResetLab?.Invoke();
        inGameLanguage=LanguagePicker();inGameLanguage.Position=new(1030,338);inGameLanguage.Size=new(225,40);root.AddChild(inGameLanguage);
        fringe=new(){Position=Vector2.Zero,Size=new(1280,8),Color=new(1,.3f,.1f,.4f),MouseFilter=Control.MouseFilterEnum.Ignore};root.AddChild(fringe);
        for(int i=0;i<6;i++){var spot=new ColorRect{Position=new(i%2==0?30+i*15:1090-i*12,170+i*65),Size=new(85,37),Rotation=i*.31f,Color=new(.65f,.8f,.4f,.65f),Visible=false,MouseFilter=Control.MouseFilterEnum.Ignore};root.AddChild(spot);splashes.Add(spot);}
        menu=new Control{Theme=theme};AddChild(menu);menu.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var bg=new ColorRect{Color=new("18313e")};menu.AddChild(bg);bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var menuScroll=new ScrollContainer{Position=new(320,55),Size=new(640,680)};menu.AddChild(menuScroll);menuBox=new(){CustomMinimumSize=new(610,0),SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};menuScroll.AddChild(menuBox);menuBox.AddThemeConstantOverride("separation",12);
        foreach(var source in new[]{"HAIRBALL","A salon for questionable engineering.\n1–4 barbers · 3 rounds · 75 seconds"})
        {
            var label=new Label{HorizontalAlignment=HorizontalAlignment.Center};menuBox.AddChild(label);
            if(BPlaytest)translations.Add(()=>label.Text=source=="HAIRBALL"?"PROJECT HAIRBALL · B":L.Locale=="zh"?$"朋友试玩版 · 1–4 人合作\n一个顾客 · 停机坪 · {ExperimentState.BuildDuration:0} 秒施工":$"FRIENDS PLAYTEST · 1–4 PLAYERS\nOne customer · Helipad · {ExperimentState.BuildDuration:0} seconds to build");
            else Bind(label,source);
        }
        var languageRow=new HBoxContainer();menuBox.AddChild(languageRow);
        languageRow.AddChild(new Label{Text="语言 / Language",SizeFlagsHorizontal=Control.SizeFlags.ExpandFill});languageRow.AddChild(LanguagePicker());
        VoiceSettingsHost=new VBoxContainer{Name="MenuVoiceSettings"};menuBox.AddChild(VoiceSettingsHost);
        if(BPlaytest){SetupSecretMenu();SetupPresentationMenu();SetupCustomerMenu();SetupTwistMenu();SetupWorldMenu();}
        var address=new LineEdit{Text="127.0.0.1"};menuBox.AddChild(address);translations.Add(()=>address.PlaceholderText=T("Host address"));
        var port=new SpinBox{MinValue=1024,MaxValue=65535,Value=7777};menuBox.AddChild(port);
        foreach(var pair in new[]{("SOLO — start cutting","solo"),("HOST — open shared salon","host"),("JOIN — direct ENet","join"),("HAIR LAB — experiment without a timer","lab"),("LAUNDRY LAB — risky finishing touches","laundry")})
        {if(BPlaytest&&pair.Item2 is "lab" or "laundry")continue;var b=new Button{CustomMinimumSize=new(580,46)};menuBox.AddChild(b);translations.Add(()=>b.Text=T(pair.Item1));b.Pressed+=()=>Start?.Invoke(pair.Item2,address.Text,(int)port.Value);}
        var instructions=new Label{HorizontalAlignment=HorizontalAlignment.Center,AutowrapMode=TextServer.AutowrapMode.WordSmart};menuBox.AddChild(instructions);
        if(BPlaytest)translations.Add(()=>instructions.Text=L.Locale=="zh"?"桌上直升机用 E 拿起，瞄准头发再按 E 放置。\n用胶水或冷冻加固；试放不会通关。\n倒计时或完工铃让顾客起身，护送到门口仍有支撑才完成。\n联机：输入房主地址和端口后加入。\n房主等朋友到齐，再按 Enter 开始。":"E picks up the table helicopter; aim at hair and E places it.\nGlue or freeze for support. Trial placement never ends construction.\nThe timer or bell starts departure. Escort it supported to the door.\nTo join, enter the host address and port.\nHost: wait for friends, then press Enter to start.");
        else Bind(instructions,"Pick up physical tools from the side benches.\nAim at the cyan target around your customer.\nAll players work on the same customer.\nAccidents cost the shared shop money.");
        menuMessage=new Label{AutowrapMode=TextServer.AutowrapMode.WordSmart,HorizontalAlignment=HorizontalAlignment.Center};menuBox.AddChild(menuMessage);
        translations.Add(()=>returnButton.Text=L.Locale=="zh"?"离开房间":"LEAVE ROOM");translations.Add(()=>resetButton.Text=T("RESET LAB [F5]"));
        built=true;L.Changed+=RefreshLanguage;RefreshLanguage();ShowMenu(true);
    }
    void Bind(Label label,string source)=>translations.Add(()=>label.Text=T(source));
    OptionButton LanguagePicker()
    {
        var picker=new OptionButton{CustomMinimumSize=new(225,40),TooltipText="语言 / Language"};
        picker.AddItem("简体中文",0);picker.AddItem("English",1);
        picker.ItemSelected+=index=>LanguageSettings.Select(index==0?"zh":"en",preferencePath);languagePickers.Add(picker);return picker;
    }
    void RefreshLanguage()
    {
        foreach(var refresh in translations)refresh();
        foreach(var picker in languagePickers)picker.Select(L.Locale=="zh"?0:1);
        menuMessage.Text=L.Text(messageSource);choiceKey=-1;
        if(lastUpdate is {} x)Update(x.World,x.Id,x.Authority,x.Lab,x.Portrait,x.Bytes);
    }
    public override void _ExitTree(){L.Changed-=RefreshLanguage;}
    public void VerifyEgoVisibility(WorldState world,int id,SubViewport portrait)
    {
        var phase=world.Phase;string locale=L.Locale;
        foreach(string language in new[]{"zh","en"})
        {
            L.SetLocale(language);world.Phase=Phase.Build;Update(world,id,true,false,portrait,0);
            if(spotlight.Visible)throw new Exception("Personal awards exposed during construction");
            world.Phase=Phase.Results;Update(world,id,true,false,portrait,0);
            if(!spotlight.Visible||spotlight.Text==""||!scores.Visible)throw new Exception("Shared result / post-job awards missing");
        }
        world.Phase=phase;L.SetLocale(locale);Update(world,id,true,false,portrait,0);GD.Print("EGO_PASS bilingual awards stay hidden during build and secondary at results");
    }
    public void VerifyLanguageSwitch(WorldState world,int id,SubViewport portrait)
    {
        string original=L.Locale;string testPath="user://language-check-"+Guid.NewGuid().ToString("N")+".cfg";
        var state=Wire.Encode(world);
        try
        {
            preferencePath=testPath;
            ShowMenu(true);
            languagePickers[1].EmitSignal(OptionButton.SignalName.ItemSelected,0L);
            if(L.Locale!="zh"||!menuBox.GetChildren().OfType<Button>().Any(b=>b.Text.StartsWith("单人游戏")))throw new Exception("Chinese menu did not refresh from picker");
            ShowMenu(false);Update(world,id,true,false,portrait,0);
            if(!goal.Text.Contains("共享")||!top.Text.Contains("回合"))throw new Exception("Chinese HUD did not refresh");
            foreach(var phase in Enum.GetValues<Phase>())
            {var copy=Wire.Decode<WorldState>(state);copy.Phase=phase;Update(copy,id,true,false,portrait,0);}
            languagePickers[0].EmitSignal(OptionButton.SignalName.ItemSelected,1L);
            Update(world,id,true,false,portrait,0);
            if(!goal.Text.Contains("SHARED")||languagePickers.Any(p=>p.Selected!=1))throw new Exception("English switch or picker synchronization failed");
            L.SetLocale("zh");LanguageSettings.Load(testPath);
            if(L.Locale!="en")throw new Exception("Saved English preference was not restored");
            if(!LanguageSettings.Font.HasChar('中')||!LanguageSettings.Font.HasChar('发'))throw new Exception("Missing Chinese glyphs");
            if(!state.SequenceEqual(Wire.Encode(world)))throw new Exception("Language switch mutated gameplay facts");
            GD.Print("LOCALIZATION_CHECK_OK menu picker / live HUD / every phase / synchronized selectors / persistence / CJK glyphs / unchanged gameplay");
        }
        finally
        {preferencePath=LanguageSettings.Path;DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(testPath));L.SetLocale(original);Update(world,id,true,false,portrait,0);}
    }
    Label Text(Vector2 pos,Vector2 size,int font)
    {
        var l=new Label{Position=pos,Size=size,AutowrapMode=TextServer.AutowrapMode.WordSmart,MouseFilter=Control.MouseFilterEnum.Ignore};
        l.AddThemeFontSizeOverride("font_size",font);l.AddThemeColorOverride("font_color",new("fff1d0"));l.AddThemeColorOverride("font_shadow_color",new("172632"));l.AddThemeConstantOverride("shadow_offset_x",2);l.AddThemeConstantOverride("shadow_offset_y",2);root.AddChild(l);return l;
    }
    public void ShowMenu(bool visible,string message="")
    {if(!built)return;if(!visible)MenuHidden?.Invoke();menu.Visible=visible;root.Visible=!visible;messageSource=message;menuMessage.Text=L.Text(message);}
    public void Update(WorldState w,int id,bool authority,bool lab,SubViewport portrait,int bytes)
    {
        lastUpdate=(w,id,authority,lab,portrait,bytes);
        if(!root.Visible)return;
        var p=w.Player(id);top.Text=T("{0}   /   ROUND {1}/3   /   {2}   {3:00}s",T("HAIRBALL"),w.Round,T(w.Phase.ToString().ToUpperInvariant()),Math.Max(0,w.Remaining));
        startButton.Visible=authority && w.Phase is Phase.Lobby or Phase.Complete;
        startButton.Text=T(w.Phase==Phase.Complete?"PLAY AGAIN [Enter]":"START MATCH [Enter]");
        resetButton.Visible=lab;scores.Visible=w.Phase is Phase.Results or Phase.Complete or Phase.Highlight or Phase.Validation or Phase.Lobby;
        if(p==null){goal.Text=T("Connecting to the host…");return;}
        spotlight.Visible=w.Phase is Phase.Results or Phase.Complete;
        spotlight.Text=w.Job.Awards.Count==0?"":T("SPOTLIGHT — stories, not a winner")+"\n"+string.Join("\n",w.Job.Awards.Select(a=>T(a.Title)+" · "+PlayerName(a.Name)));
        orderBanner.Visible=w.Order.Pending;
        orderBanner.Text=w.Order.Pending?T("{0} is ordering {1} · shared ${2}\n{3:0.0}s to cancel at the red terminal button",PlayerName(w.Players.FirstOrDefault(q=>q.Id==w.Order.Actor)?.Name??""),T(SpecialOrders.Names[w.Order.Option]),SpecialOrders.Prices[w.Order.Option],Math.Max(0,w.Order.CommitAt-w.Time)):"";
        foreach(var spot in splashes){spot.Visible=p.Obscured>0;spot.Modulate=new(1,1,1,Math.Min(1,p.Obscured));}
        var def=Goals.All[w.Job.Goal];
        bool compact=w.Phase==Phase.Build&&Input.MouseMode==Input.MouseModeEnum.Captured&&!DebugVisible;
        controls.Visible=!compact;compactControls.Visible=compact;returnButton.Visible=inGameLanguage.Visible=!compact;resetButton.Visible=lab&&!compact;
        top.AddThemeFontSizeOverride("font_size",compact?19:26);goal.AddThemeFontSizeOverride("font_size",compact?17:20);
        goal.Position=new(24,compact?53:77);goal.Size=new(compact?580:880,115);
        tool.Position=new(24,compact?697:655);tool.Size=new(compact?620:950,88);tool.AddThemeFontSizeOverride("font_size",compact?16:20);
        prompt.Position=new(400,compact?650:455);prompt.AddThemeFontSizeOverride("font_size",compact?17:22);
        notice.Visible=!compact||w.Notice.Contains("Reservoir full")||w.Notice.Contains("Not enough room")||w.Events.Any(e=>w.Time-e.Time<2&&e.Value>=20);
        if(compact)top.Text=lab?T("ROUND {0}/3 · HAIR LAB",w.Round):T("ROUND {0}/3 · {1:00}s",w.Round,Math.Max(0,w.Remaining));
        goal.Text=T("SHARED JOB • {0}\n{1}",T(def.Name),T(def.Instruction));
        if(w.Phase==Phase.Choice)goal.Text=T("VOTE TOGETHER • 1 / 2 / 3\nYour vote: {0}",p.Vote<0?T("Not voted"):T(Goals.All[p.Vote].Name));
        if(w.Phase==Phase.Arrival)goal.Text=T("ONE SHARED CUSTOMER • EVERYONE HELPS")+"\n"+T(w.Customers[0].Profile=="llama"?"Llama customer • watch those cheeks!":"Human customer • steady hands, please!");
        var held=w.Tools.FirstOrDefault(t=>t.Id==p.Held);
        tool.Text=held==null?T("EMPTY HANDS  •  Aim at a bench tool and press E"):$"{T(Tools.Get(held.Definition).Name)}   {(held.Charge<0?"∞":T("{0} charges",held.Charge))}   {(held.Definition==2?T("Stored hair {0:0.0}/20",p.Reservoir):"")}\n{T(Tools.Get(held.Definition).Hint)}";
        if(held?.Definition is 0 or 1)tool.Text+="\n"+T("LMB normal • RMB fine • Wheel size: {0}",T(new[]{"Small","Medium","Large"}[Math.Clamp(p.BrushSize,0,2)]));
        if(compact&&held!=null)tool.Text=$"{T(Tools.Get(held.Definition).Name)}  {(held.Charge<0?"∞":held.Charge.ToString())}"+(held.Definition is 0 or 1?"\n"+T("LMB normal • RMB fine • Wheel size: {0}",T(new[]{"Small","Medium","Large"}[Math.Clamp(p.BrushSize,0,2)])):held.Definition==2?"\n"+T("Stored hair {0:0.0}/20",p.Reservoir):"");
        if(p.CarryLadder>=0)tool.Text=T("CARRYING LADDER • G / E to place on clear floor");
        notice.Text=L.Text(w.Notice);
        prompt.Text="";
        if(w.Tools.FirstOrDefault(t=>t.Holder==0 && Session.LookingAt(p,t.Position,.45f,2.5f)) is {} nearby)prompt.Text=$"E  •  {T(SpecialOrders.ToolName(nearby))}";
        else if(w.Heads.Any(h=>h.Loose && !h.Miniature && h.AttachedTo<0 && Session.LookingAt(p,h.Position,.8f,3)))prompt.Text=T("E  •  PLACE / WEAR HAIR BUNDLE");
        if(p.CarryLadder>=0)prompt.Text=T("G • PLACE LADDER — clear floor required");
        else if(w.Ladders.Any(l=>l.Holder==0&&Session.LookingAt(p,l.Position+System.Numerics.Vector3.UnitY*.9f,.65f,3)))prompt.Text=T("E • CARRY LADDER / walk up steps to stand");
        else if(w.Customers.FirstOrDefault(c=>Session.LookingAt(p,Session.ChairControl(c.Slot),.4f,3)) is {} chair)prompt.Text=T("R / F • RAISE / LOWER CHAIR ({0:0.00} m)",chair.ChairHeight);
        for(int option=0;option<3;option++)if(!w.Experiment.IsB&&w.Phase==Phase.Build&&Session.LookingAt(p,SpecialOrders.Button(option),.22f,2.5f))
        {
            prompt.Text=option==2?T("E · Cancel pending order"):T("E · Order {0} · shared ${1} / wallet ${2}",T(SpecialOrders.Names[option]),SpecialOrders.Prices[option],w.Job.Wallet);
            if(w.Order.Notice!=""){notice.Text=T(w.Order.Notice);notice.Visible=true;}
        }
        if(held?.Special>0)tool.Text=T(SpecialOrders.ToolName(held))+" · "+T(held.Charge>0?"ONE USE · LMB burst · affects nearby hair":"EMPTY · drop with G");
        if(p.CarriedProp>=0){tool.Text=T("JOB PROP • E place · R/F tilt · G drop");prompt.Text="";}
        else if(w.Props.Any(x=>x.Holder==0&&Session.LookingAt(p,x.Position,.25f,2.5f)))prompt.Text=T("E • CARRY JOB PROP");
        if(p.CarriedModel>=0){tool.Text=T("PRACTICE MODEL • E / G put down");prompt.Text="";}
        else if(w.Heads.Any(h=>h.Miniature&&h.Holder==0&&MiniatureModel.InReach(p,h)))prompt.Text=T("E • CARRY PRACTICE MODEL");
        int key=w.Phase==Phase.Choice?w.Round*100+p.Slot:-1;
        choices.Visible=w.Phase==Phase.Choice;
        if(key!=choiceKey)
        {
            foreach(var child in choices.GetChildren()){choices.RemoveChild(child);child.QueueFree();}
            if(key>=0)for(int i=0;i<3;i++)
            {int selected=w.Job.Choices[i];var b=new Button{Text=$"[{i+1}] {T(Goals.All[selected].Name)}\n{T(Goals.All[selected].Validation)}",Icon=GoalCardImage(selected),CustomMinimumSize=new(360,88)};b.AddThemeFontSizeOverride("font_size",17);choices.AddChild(b);b.Pressed+=()=>Choose?.Invoke(selected);}
            choiceKey=key;
        }
        if(w.Phase==Phase.Choice)for(int i=0;i<choices.GetChildCount();i++){int selected=w.Job.Choices[i];var button=choices.GetChild<Button>(i);button.Text=$"[{i+1}] {T(Goals.All[selected].Name)}  ·  {w.Players.Count(q=>q.Active&&q.Vote==selected)}\n{T(Goals.All[selected].Validation)}";}
        scores.Text=w.Phase switch
        {
            Phase.Lobby=>T("SHARED SALON\n{0}\n\n{1}",string.Join("\n",w.Players.Where(p=>p.Active).Select(p=>PlayerName(p.Name))),T(authority?"Start when everyone has joined.":"Waiting for host to start.")),
            Phase.Results=>T("SHARED RESULT\nShape {0:0} / 100 · Validation {1}\nIncidents -{2} · Job {3:+0;-0;0}\nSpecial orders -{5} · SHOP WALLET {4}",w.Job.Result.Shape,T(w.Job.Result.Function>50?"PASSED":"FAILED"),w.Job.Penalty,w.Job.Delta,w.Job.Wallet,w.Order.Spent)+"\n"+T("Shape {0:0} · Material {1:0} · Function {2:0} · Total {3:0}",w.Job.Result.Shape,w.Job.Result.State,w.Job.Result.Function,w.Job.Result.Final),
            Phase.Complete=>T(w.Job.Wallet<=0?"SHOP BANKRUPT · {0}":"THREE SHARED JOBS COMPLETE · WALLET {0}",w.Job.Wallet),
            Phase.Highlight=>T("BEST MOMENT\n{0}",L.Text(w.HighlightTitle)),
            Phase.Validation=>T("{0}  •  SPECTATOR\n{1} / {2} still holding",T(def.Validation),w.Props.Count(x=>!x.Failed),w.Props.Count),
            _=>""
        };
        var ownHead=w.Barber(p.Slot);int burning=ownHead.Patches.Count(x=>x.Burning),frozen=ownHead.Patches.Count(x=>x.Frozen);
        fringe.Visible=burning>0||frozen>0||ownHead.Mass>40;fringe.Color=burning>0?new(1,.4f,.1f,.65f):frozen>0?new(.5f,.9f,1,.6f):new(.5f,.3f,.6f,.6f);
        debug.Visible=DebugVisible;
        Head? aimedHead=null;System.Numerics.Vector3 aimedPoint=default;float aimDistance=5;
        foreach(var h in w.Heads.Where(h=>!h.Barber||h.Owner!=id))if(h.Raycast(Session.Eye(p),Session.Aim(p),aimDistance,out var point,out var distance))
        {aimedHead=h;aimedPoint=point;aimDistance=distance;}
        string patchInfo=T("No hair surface under aim");
        if(aimedHead!=null){var material=HairSystem.MaterialAt(aimedHead,aimedPoint);patchInfo=T("Head {0} • Volume {1:0.0}\nHeat {2:0} Glue {3:0.0} Anchored {4}",aimedHead.Id,aimedHead.Mass,material.Temperature,material.Glue,T(material.Anchored?"Yes":"No"));}
        debug.Text=T("Peer {0}  {1}\nFacts revision {2} • compressed {3} bytes\nHeads {4} / patches {5}\nGoal {6} • Shop damages {7}\n{8}\nLAB: F5 reset • F6 wig • F7 target",id,T(authority?"HOST AUTHORITY":"CLIENT"),w.Revision,bytes,w.Heads.Count,w.Heads.Sum(h=>h.Patches.Count),w.Job.Goal,w.Job.Penalty,patchInfo);
        debug.Text+="\n"+NetworkStatus;
        UpdateExperiment(w,p);
    }
}

