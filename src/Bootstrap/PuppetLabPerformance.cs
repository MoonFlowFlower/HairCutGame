using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
namespace Hairball;

// A local visual performance, never a Session rule or a networked action.
public partial class PuppetLab
{
    readonly Dictionary<int,PuppetLabHair> performanceHair=new();
    readonly Dictionary<int,Node3D> performanceFaces=new();Node3D? activePerformanceFace;
    readonly List<double> reelTimes=new();
    PuppetLabHair? beforePerformance;bool performing,recording,reelBusy,ending;float performanceTime,idleTime;int lastBeat=-1;double nextReelFrame,performanceStart;
    void PreparePerformance()
    {
        if(performanceHair.Count==0)for(int s=0;s<5;s++){var h=new PuppetLabHair(this,s);h.SetLook(round);h.Root.Visible=false;performanceHair.Add(s,h);}
        if(performanceFaces.Count==0)for(int s=1;s<=4;s++){var f=PuppetLabModels.Face(subject,new(0,1.97f,.035f),1,s);f.Name="PerformanceFace"+s;foreach(var m in Descendants(f).OfType<MeshInstance3D>()){m.Layers=2;m.GIMode=GeometryInstance3D.GIModeEnum.Disabled;}f.Visible=false;performanceFaces.Add(s,f);surface.Collect(f);}
        surface.Enable(round>=2);
        surface.SetTransport(PuppetLabSurface.TransportEnabled);
    }
    void StartPerformance(bool record=false)
    {
        SetShot("gameplay");PreparePerformance();subject.GetNode<Node3D>("OriginalPuppetFace").Hide();
        beforePerformance=hair;hair.Root.Visible=false;hair=performanceHair[0];hair.Root.Visible=true;
        performing=true;recording=record;performanceTime=0;lastBeat=-1;performanceStart=clock.Elapsed.TotalSeconds+(record?CaptureWarmup:0);nextReelFrame=performanceStart;
        if(record){Directory.CreateDirectory(Path.Combine(output,"frames"));reelTimes.Clear();}
    }
    void StopPerformance()
    {
        if(beforePerformance!=null){hair.Root.Visible=false;hair=beforePerformance;hair.Root.Visible=true;beforePerformance=null;}
        performing=false;recording=false;performanceTime=0;lastBeat=-1;
        foreach(var f in performanceFaces.Values)f.Hide();activePerformanceFace=null;
        if(subject!=null){var f=subject.GetNodeOrNull<Node3D>("OriginalPuppetFace");if(f!=null){f.Show();f.Rotation=Vector3.Zero;f.Position=new(0,1.97f,.035f);}}
        if(hair!=null){hair.Root.Position=PuppetLabHair.Origin;hair.Root.Rotation=Vector3.Zero;hair.Root.Scale=Vector3.One*.9f;}
        if(friend!=null){var f=friend.GetNode<Node3D>("PearShapedStylist");f.Position=new(-1.18f,0,-.45f);f.RotationDegrees=new(0,18,0);f.GetNodeOrNull<Node3D>("RescueSprayer")?.Hide();f.GetNodeOrNull<Node3D>("StylistTool")?.Show();}
        if(held!=null)held.Rotation=Vector3.Zero;
        if(effects!=null){effects.Spraying=false;effects.Visible=false;}
    }
    void AnimatePerformance(float dt)
    {
        // A/B and silhouette masks must share the exact head pose across
        // frames; the interactive R7 preview retains the existing idle motion.
        if(args.ContainsKey("r7-review")&&!args.ContainsKey("interactive"))return;
        if(round<4||capture||(args.ContainsKey("puppet-check")&&!performing)||gray||baseline||ending)return;
        idleTime+=dt;float t=idleTime;
        var face=subject.GetNode<Node3D>("OriginalPuppetFace");float yaw=.024f*MathF.Sin(t*.65f),roll=.012f*MathF.Sin(t*1.1f),lift=.004f*MathF.Sin(t*2);
        if(performing)
        {
            performanceTime=(float)Math.Max(0,clock.Elapsed.TotalSeconds-performanceStart);
            if(recording&&performanceTime>=13.5f&&!reelBusy){FinishReel();return;}
            t=performanceTime%13.5f;
            int beat=t<2.5f?0:t<5.4f?1:t<8.4f?2:t<11.2f?3:4;
            if(beat!=lastBeat)
            {
                lastBeat=beat;int next=beat switch{0=>0,1=>2,2=>3,3=>4,_=>1};
                hair.Root.Visible=false;hair=performanceHair[next];hair.Root.Visible=true;state=next;
                activePerformanceFace?.Hide();activePerformanceFace=performanceFaces[beat switch{0=>1,1=>2,2=>2,3=>4,_=>3}];activePerformanceFace.Show();
                effects.SetState(next);effects.Visible=beat is 1 or 2 or 3;effects.Spraying=beat==2;
                var actor=friend.GetNode<Node3D>("PearShapedStylist");actor.GetNode<Node3D>("RescueSprayer").Visible=beat>=2;actor.GetNode<Node3D>("StylistTool").Visible=beat<2;
                GD.Print($"PUPPET_PERFORMANCE beat={beat} seconds={performanceTime:F3} state={next}");
            }
            face=activePerformanceFace!;
            yaw=beat==0?.10f*MathF.Sin(t*1.5f):beat==1?.055f*MathF.Sin(t*15):beat==4?-.18f:.025f*MathF.Sin(t*2);
            roll=beat==1?.055f*MathF.Sin(t*18):beat==2?-.10f:beat==3?.08f:beat==4?-.055f:.02f*MathF.Sin(t*2);
            lift=beat==1?.012f*MathF.Sin(t*18):beat==2?.008f*MathF.Sin(t*7):.004f*MathF.Sin(t*3);
            var stylist=friend.GetNode<Node3D>("PearShapedStylist");float ease=1-MathF.Exp(-dt*9);stylist.RotationDegrees=stylist.RotationDegrees.Lerp(new Vector3(0,18+(beat==4?18:0),beat==2?3:beat==1?6:2*MathF.Sin(t*2)),ease);
            stylist.Position=stylist.Position.Lerp(beat==2?new Vector3(-1.38f,0,-.03f):new Vector3(-1.18f,0,-.45f),ease);
            effects.SprayFrom=stylist.GetNode<Node3D>("RescueSprayer").ToGlobal(new(.15f,.22f,0));effects.SprayTo=hair.Root.ToGlobal(new(.27f,.36f,.52f));
            held.Rotation=new(0,0,beat==1?-.035f*MathF.Sin(t*10):beat==4?-.045f:0);
        }
        face.Position=new(0,1.97f+lift,.035f);face.Rotation=new(0,yaw,roll);
        float blinkPhase=idleTime%4.2f,blink=blinkPhase>.16f?1:1-.90f*MathF.Sin(blinkPhase/.16f*MathF.PI);
        foreach(string eyeName in new[]{"EyeAssemblyLeft","EyeAssemblyRight"}){var eye=face.GetNode<Node3D>(eyeName);eye.Scale=new(1,blink,1);eye.GetNode<Node3D>("Gaze").Position=new(performing&&lastBeat==4?-.033f:.009f*MathF.Sin(t*1.3f),performing&&lastBeat==1?.010f:0,0);}
        hair.Root.GlobalTransform=face.GlobalTransform*new Transform3D(Basis.Identity,new Vector3(0,.23f,-.035f));
        effects.GlobalTransform=hair.Root.GlobalTransform;
        if(recording&&!reelBusy&&clock.Elapsed.TotalSeconds>=nextReelFrame)SaveReelFrame();
    }
    async void SaveReelFrame()
    {
        reelBusy=true;await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        double now=clock.Elapsed.TotalSeconds;using var img=GetViewport().GetTexture().GetImage();img.Resize(960,600,Image.Interpolation.Lanczos);
        img.SaveJpg(Path.Combine(output,"frames",$"frame{reelTimes.Count:D4}.jpg"),.94f);reelTimes.Add(now);nextReelFrame=now+.05;reelBusy=false;
    }
    void FinishReel()
    {
        recording=false;var text=new StringBuilder("ffconcat version 1.0\n");
        for(int i=0;i<reelTimes.Count;i++){double duration=i+1<reelTimes.Count?reelTimes[i+1]-reelTimes[i]:.05;text.Append($"file 'frames/frame{i:D4}.jpg'\nduration {duration.ToString("F6",System.Globalization.CultureInfo.InvariantCulture)}\n");}
        text.Append($"file 'frames/frame{reelTimes.Count-1:D4}.jpg'\n");File.WriteAllText(Path.Combine(output,"reel.ffconcat"),text.ToString());
        File.WriteAllText(Path.Combine(output,"reel.json"),JsonSerializer.Serialize(new{actualRealtime=true,frames=reelTimes.Count,times=reelTimes,duration=reelTimes.Count>1?reelTimes[^1]-reelTimes[0]:0,width=960,height=600,cameraFov=camera.Fov,eyeHeight=camera.Position.Y}));
        GD.Print("PUPPET_REALTIME_REEL_COMPLETE "+output);QuitCleanly();
    }
    async void QuitCleanly()
    {
        if(ending)return;ending=true;capture=false;
        foreach(var p in room.GetChildren().OfType<ReflectionProbe>().ToArray())p.Free();
        await ToSignal(GetTree().CreateTimer(.25),SceneTreeTimer.SignalName.Timeout);GetTree().Quit();
    }
}
