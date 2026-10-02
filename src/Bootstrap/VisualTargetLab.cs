using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using static Hairball.TargetLabGeometry;
using NVector=System.Numerics.Vector3;
namespace Hairball;

// Isolated presentation slice. Never constructs/ticks Session, networking, scores, or audio.
public partial class VisualTargetLab : Node3D
{
    public Dictionary<string,string> Options=new();
    public Action<int> Finished=null!;
    readonly Camera3D camera=new(){Fov=73,Near=.045f,Current=true};
    readonly StandardMaterial3D hair=Mat("838987",.91f),smallHair=Mat("727b79",.91f);
    HairVolume[] volumes=[Hair(false),Hair(true)];
    readonly List<object> results=new();readonly List<double> frames=new(),cpu=new(),gpu=new();readonly List<int> draws=new();
    readonly Stopwatch clock=new();
    MeshInstance3D hairMesh=null!;ArrayMesh[] meshes=[];Node3D held=null!,team=null!;CanvasLayer hud=null!;Label objective=null!,prompt=null!,style=null!;
    MeshInstance3D targetHair=null!;SubViewport targetView=null!;Camera3D targetCamera=null!;
    string directory="",shot="B";int styleIndex=0,captureIndex;double changedAt,lastFrame;bool recording,gray=true,freeLook;float yaw,pitch;
    Vector3 HairOrigin=new(0,1.94f,0);bool animal;
    public override void _Ready()
    {
        directory=Path.GetFullPath(Options.GetValueOrDefault("target-output","artifacts/visual-target"));Directory.CreateDirectory(directory);
        GetViewport().PositionalShadowAtlasSize=4096;
        animal=Options.ContainsKey("target-animal");
        if(animal){HairOrigin=new(0,2.14f,.02f);volumes=[AnimalHair(false),AnimalHair(true)];}
        gray=!Options.ContainsKey("target-color");ApplyColor();BuildRoom();
        var customer=new Node3D{Name=animal?"OneAnimalCustomer":"OneHumanCustomer"};AddChild(customer);
        if(animal)TargetLabAssets.Animal(customer);else TargetLabAssets.Customer(customer);
        team=new Node3D();AddChild(team);SetTeam();
        meshes=[HairMesh(volumes[0],false,animal),HairMesh(volumes[1],true,animal)];hairMesh=Mesh(this,meshes[0],HairOrigin,hair);hairMesh.Name="SharedDensityHair";hairMesh.Layers=2;
        AddChild(camera);held=new Node3D{Position=new(.42f,-.28f,-.88f),Scale=Vector3.One*.62f,RotationDegrees=new(-14,-18,-21)};camera.AddChild(held);
        TargetLabActors.Clipper(held,Vector3.Zero,1.10f);
        TargetLabAssets.FpsHand(held);
        BuildHud();SetStyle(0);SetShot("B");Input.MouseMode=Input.MouseModeEnum.Visible;
        RenderingServer.ViewportSetMeasureRenderTime(GetViewport().GetViewportRid(),true);clock.Start();
        recording=!Options.ContainsKey("target-interactive");if(recording)SelectCapture();
        GD.Print("VISUAL_TARGET_LAB_READY "+directory+" Gray="+gray);
    }
    void ApplyColor(){hair.AlbedoColor=new(gray?"838987":animal?"e7cfa9":"4e2d23");smallHair.AlbedoColor=new(gray?"727b79":"44382f");if(targetView!=null)targetView.RenderTargetUpdateMode=SubViewport.UpdateMode.Once;}
    void BuildRoom()
    {
        StandardMaterial3D plaster=Mat("596c68"),panel=Mat("304c4b"),wood=Mat("9c714e",.72f),trim=Mat("536b66"),floor=Mat("b09a7d");
        Round(this,new(0,-.10f,0),new(8,.2f,8),.08f,floor);
        Round(this,new(0,2.15f,-2),new(7.8f,4.3f,.24f),.1f,plaster);
        Round(this,new(0,.67f,-1.81f),new(7.65f,1.3f,.18f),.075f,panel);
        Round(this,new(0,1.35f,-1.71f),new(7.7f,.10f,.17f),.046f,trim);
        for(int i=-5;i<=5;i++)Round(this,new(i*.63f,.67f,-1.7f),new(.035f,1.19f,.06f),.017f,trim);
        Round(this,new(-3.7f,2.1f,.6f),new(.23f,4.2f,5.4f),.1f,plaster);
        Round(this,new(3.7f,2.1f,.6f),new(.23f,4.2f,5.4f),.1f,plaster);
        Round(this,new(0,4.35f,.2f),new(7.6f,.2f,5),.08f,Mat("9c9582"));
        for(int i=-1;i<=1;i++)Round(this,new(i*2.9f,4.17f,.2f),new(.19f,.20f,5),.08f,wood);
        // Quiet cool window provides depth and separation, without a bright competing vista.
        Round(this,new(2.08f,2.6f,-1.77f),new(2.06f,1.95f,.18f),.09f,trim);
        var glass=Mat("638b96",.9f);glass.EmissionEnabled=true;glass.Emission=new("638b96");glass.EmissionEnergyMultiplier=.22f;
        Round(this,new(2.08f,2.6f,-1.66f),new(1.82f,1.70f,.045f),.019f,glass);
        Round(this,new(2.08f,2.6f,-1.61f),new(.08f,1.76f,.05f),.02f,trim);
        Round(this,new(2.08f,2.6f,-1.61f),new(1.89f,.08f,.05f),.02f,trim);
        Round(this,new(2.08f,1.62f,-1.59f),new(2.22f,.13f,.39f),.05f,wood);
        TargetLabAssets.Station(this);
        var spare=TargetLabActors.Clipper(this,new(-2.32f,1.24f,-.85f));spare.RotationDegrees=new(90,20,0);
        Cylinder(this,new(-1.75f,1.25f,-1.04f),.09f,.25f,TargetLabActors.Cream);
        // A simple upholstered floor mat helps the seated focal group read as one shape.
        Round(this,new(0,.013f,.14f),new(2.18f,.04f,2.12f),.018f,Mat("575d53"));
        AddChild(new WorldEnvironment{Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new("6a7776"),AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=new("a9c1ca"),AmbientLightEnergy=.34f,TonemapMode=Godot.Environment.ToneMapper.Filmic,TonemapWhite=4,SsaoEnabled=true,SsaoRadius=.35f,SsaoIntensity=.85f}});
        AddLight(new(-2.1f,3.5f,3.0f),new(0,1.9f,0),new("ffdfb8"),7.5f,6,53,false);
        AddLight(new(3.0f,3.0f,2.0f),new(0,2,0),new("b7d8ef"),1.6f,6,65,false);
        AddLight(new(1.25f,3.8f,-1.25f),new(0,2.4f,0),new("ffe0b8"),4.6f,5,60,false);
        AddChild(new OmniLight3D{Position=new(-.65f,2.55f,1.25f),LightColor=new("ffe3bf"),LightEnergy=2.3f,OmniRange=4,OmniAttenuation=.8f,LightCullMask=2});
        AddChild(new DirectionalLight3D{RotationDegrees=new(-55,-30,0),LightColor=new("d6e1db"),LightEnergy=.28f});
    }
    void AddLight(Vector3 at,Vector3 target,Color color,float energy,float range,float angle,bool shadow)
    {var light=new SpotLight3D{Position=at,LightColor=color,LightEnergy=energy,SpotRange=range,SpotAngle=angle,SpotAngleAttenuation=2.0f,SpotAttenuation=.7f,ShadowEnabled=shadow,ShadowBias=.05f,ShadowNormalBias=.65f,ShadowBlur=4,ShadowOpacity=.65f};AddChild(light);light.LookAt(target);}
    Label Text(Control p,string text,Vector2 at,int size,Color color)
    {var l=new Label{Text=text,Position=at,MouseFilter=Control.MouseFilterEnum.Ignore};l.AddThemeFontOverride("font",LanguageSettings.Font);l.AddThemeFontSizeOverride("font_size",size);l.AddThemeColorOverride("font_color",color);p.AddChild(l);return l;}
    Panel Card(Control p,Vector2 at,Vector2 size)
    {var panel=new Panel{Position=at,Size=size,MouseFilter=Control.MouseFilterEnum.Ignore};var s=new StyleBoxFlat{BgColor=new Color(.07f,.13f,.14f,.88f),CornerRadiusTopLeft=12,CornerRadiusTopRight=12,CornerRadiusBottomLeft=12,CornerRadiusBottomRight=12};panel.AddThemeStyleboxOverride("panel",s);p.AddChild(panel);return panel;}
    void BuildHud()
    {
        hud=new CanvasLayer();AddChild(hud);var root=new Control{MouseFilter=Control.MouseFilterEnum.Ignore};root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);hud.AddChild(root);
        var task=Card(root,new(26,24),new(252,78));Text(task,"共同目标",new(16,10),14,new("b9c4b5"));objective=Text(task,"剪出向上回卷的大背头",new(16,33),19,new("fff2d7"));
        var time=Card(root,new(588,24),new(104,44));Text(time,"02:00",new(22,7),22,new("fff2d7"));
        var target=Card(root,new(1100,24),new(154,176));Text(target,"目标发型",new(15,10),14,new("b9c4b5"));style=Text(target,"回卷 · 高蓬松",new(15,143),16,new("fff2d7"));
        targetView=new SubViewport{Size=new(256,180),OwnWorld3D=true,TransparentBg=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Once};AddChild(targetView);
        var portrait=new Node3D();targetView.AddChild(portrait);targetHair=Mesh(portrait,meshes[styleIndex],Vector3.Zero,hair);
        if(!animal)TargetLabAssets.Add(portrait,"snow_customer",-HairOrigin,Vector3.One);
        else TargetLabAssets.Add(portrait,"quaternius_alpaca",new Vector3(0,0,-.86f)-HairOrigin,Vector3.One*.42f);
        targetCamera=new Camera3D{Position=new(1.3f,1.2f,3.7f),Fov=40,Current=true};portrait.AddChild(targetCamera);targetCamera.LookAt(new(0,.53f,0));
        portrait.AddChild(new DirectionalLight3D{RotationDegrees=new(-35,-25,0),LightColor=new("ffdfb8"),LightEnergy=1.7f});
        portrait.AddChild(new WorldEnvironment{Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=Colors.Transparent,AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=new("c0dce6"),AmbientLightEnergy=.6f}});
        target.AddChild(new TextureRect{Position=new(4,35),Size=new(146,103),Texture=targetView.GetTexture(),StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,MouseFilter=Control.MouseFilterEnum.Ignore});
        var tool=Card(root,new(1087,729),new(167,47));Text(tool,"推剪  ·  LMB",new(16,10),18,new("fff2d7"));
        prompt=Text(root,"",new(541,676),17,new("fff2d7"));
        var dot=new ColorRect{Position=new(638,398),Size=new(4,4),Color=new Color(1,1,.9f,.7f),MouseFilter=Control.MouseFilterEnum.Ignore};root.AddChild(dot);
        Text(root,"Snow Rig © Blender Foundation · CC-BY 4.0 · 已修改  |  F6 资产鸣谢",new(26,776),11,new("bac5bf"));
    }
    void SetTeam()
    {
        foreach(var child in team.GetChildren())child.Free();
        TargetLabAssets.Teammate(team,styleIndex==1&&!animal?new(-1.48f,.986f,-.45f):new(-1.18f,0,-.18f),25,smallHair,false);
        TargetLabAssets.Teammate(team,new(1.17f,0,-.22f),-35,smallHair,true);
    }
    void SetStyle(int index)
    {
        styleIndex=index;SetTeam();hairMesh.Mesh=meshes[index];
        objective.Text=animal?"修整圆润蓬松的毛团":index==0?"修整干净的侧分短发":"剪出向上回卷的大背头";
        style.Text=animal?"绒毛 · 蓬松":index==0?"侧分 · 清爽":"回卷 · 高蓬松";
        targetHair.Mesh=meshes[index];targetCamera.Position=index==0?new(1.0f,.65f,2.4f):new(1.3f,1.2f,3.7f);
        targetCamera.LookAt(index==0?new(0,.19f,0):new(0,.53f,0));targetView.RenderTargetUpdateMode=SubViewport.UpdateMode.Once;
        if(!freeLook)SetShot(shot);
    }
    void SetShot(string name)
    {
        shot=name;freeLook=false;Input.MouseMode=Input.MouseModeEnum.Visible;
        camera.Name=name=="A"?"BeautyMatchCamera":name=="B"?"RealGameplayCamera":"StoreScreenshotCamera";
        (camera.Position,camera.Fov)=name switch{"A"=>(new Vector3(.45f,2.10f,styleIndex==0||animal?3.35f:4.05f),54f),"C"=>(new Vector3(1.7f,2.45f,4.65f),49f),_=>(new Vector3(.20f,1.7f,2.65f),73f)};
        camera.LookAt(name=="B"?new(0,2.18f,0):styleIndex==0?new(0,1.52f,0):new(0,1.98f,0));held.Visible=name!="C";hud.Visible=name!="C";
        yaw=camera.Rotation.Y;pitch=camera.Rotation.X;prompt.Text="";
    }
    void SelectCapture(){SetStyle(captureIndex/3);SetShot(new[]{"A","B","C"}[captureIndex%3]);frames.Clear();cpu.Clear();gpu.Clear();draws.Clear();changedAt=clock.Elapsed.TotalSeconds;lastFrame=changedAt;}
    public override void _Process(double delta)
    {
        if(freeLook)
        {var move=new Vector3((Input.IsPhysicalKeyPressed(Key.D)?1:0)-(Input.IsPhysicalKeyPressed(Key.A)?1:0),0,(Input.IsPhysicalKeyPressed(Key.S)?1:0)-(Input.IsPhysicalKeyPressed(Key.W)?1:0));move=move.Rotated(Vector3.Up,yaw);camera.Position=(camera.Position+move*(float)delta*1.6f).Clamp(new Vector3(-3,1.7f,.8f),new Vector3(3,1.7f,5));}
        if(!recording)return;double now=clock.Elapsed.TotalSeconds;
        if(now-changedAt>1.0)
        {frames.Add((now-lastFrame)*1000);cpu.Add(RenderingServer.GetFrameSetupTimeCpu()+RenderingServer.ViewportGetMeasuredRenderTimeCpu(GetViewport().GetViewportRid()));gpu.Add(RenderingServer.ViewportGetMeasuredRenderTimeGpu(GetViewport().GetViewportRid()));draws.Add(GetViewport().GetRenderInfo(Viewport.RenderInfoType.Visible,Viewport.RenderInfo.DrawCallsInFrame));}
        lastFrame=now;if(now-changedAt<3.0)return;
        SaveShot();captureIndex++;
        if(captureIndex<6){SelectCapture();return;}
        recording=false;File.WriteAllText(Path.Combine(directory,"metrics.json"),JsonSerializer.Serialize(new{engine=Engine.GetVersionInfo()["string"].AsString(),renderer=RenderingServer.GetCurrentRenderingMethod(),device=RenderingServer.GetVideoAdapterName(),gray,fixture=animal?"alpaca":"human",simulation=false,shots=results},new JsonSerializerOptions{WriteIndented=true}));
        GD.Print("VISUAL_TARGET_CAPTURE_OK "+directory);Finished(0);
    }
    void SaveShot()
    {
        string file=(styleIndex==0?"ordinary":"giant")+"-"+shot;
        using var image=GetViewport().GetTexture().GetImage();image.SavePng(Path.Combine(directory,file+".png"));image.Resize(384,240,Image.Interpolation.Lanczos);image.SavePng(Path.Combine(directory,file+"-thumb.png"));
        if(frames.Count==0)return;
        var origin=camera.Position-HairOrigin;var direction=-camera.Basis.Z;
        float densityScale=DensityScaleFor(styleIndex==1);
        bool hit=volumes[styleIndex].Raycast(new NVector(origin.X,origin.Y,origin.Z)*densityScale,new(direction.X,direction.Y,direction.Z),3*densityScale,out _,out float distance);distance/=densityScale;
        var sorted=frames.Order().ToArray();results.Add(new{style=styleIndex==0?"ordinary":"giant",camera=shot,fov=camera.Fov,position=camera.Position.ToString(),eyeHeight=camera.Position.Y,centerRayHairHitWithinClipperRange=hit,centerRayDistance=hit?(float?)distance:null,samples=frames.Count,frameMs=frames.Average(),p99Ms=sorted[(int)((sorted.Length-1)*.99)],fps=1000/frames.Average(),cpuMs=cpu.Average(),gpuMs=gpu.Average(),drawCalls=draws.Average(),hairTriangles=meshes[styleIndex].SurfaceGetArrayLen(0)/3,renderPrimitives=GetViewport().GetRenderInfo(Viewport.RenderInfoType.Visible,Viewport.RenderInfo.PrimitivesInFrame)});
    }
    public override void _UnhandledInput(InputEvent e)
    {
        if(e is InputEventMouseMotion motion&&freeLook){yaw-=motion.Relative.X*.0025f;pitch=Math.Clamp(pitch-motion.Relative.Y*.0025f,-1.15f,1.15f);camera.Rotation=new(pitch,yaw,0);}
        if(e is not InputEventKey{Pressed:true,Echo:false} k)return;
        if(k.Keycode==Key.F1)SetShot("A");if(k.Keycode==Key.F2)SetShot("B");if(k.Keycode==Key.F3)SetShot("C");
        if(k.Keycode==Key.F4)SetStyle(1-styleIndex);if(k.Keycode==Key.F5){gray=!gray;ApplyColor();}
        if(k.Keycode==Key.Tab){SetShot("B");shot="free";freeLook=true;Input.MouseMode=Input.MouseModeEnum.Captured;}
        if(k.Keycode==Key.Escape){freeLook=false;Input.MouseMode=Input.MouseModeEnum.Visible;}
        if(k.Keycode==Key.F12)SaveShot();
        if(k.Keycode==Key.F6)
        {
            var credits=new AcceptDialog{Title="第三方资产鸣谢",DialogText=Godot.FileAccess.GetFileAsString("res://assets/third_party/visual_target/ATTRIBUTION.txt")};
            AddChild(credits);credits.PopupCentered(new(760,350));
        }
    }
}
