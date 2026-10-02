using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using NVector=System.Numerics.Vector3;
namespace Hairball;

// Frozen production salon/HeadView, with explicit QA fixtures. No simulation tick/network/audio.
public partial class VisualLookDev : Node3D
{
    public Dictionary<string,string> Options=new();
    public Action<int> Finished=null!;
    readonly Session session=new(){ForceAI=true,CatEnabled=false,TwistsEnabled=false};
    SalonView salon=null!;Hud hud=null!;
    readonly Stopwatch clock=new();
    readonly List<double> frames=new(),cpu=new(),gpu=new(),sync=new();
    readonly List<int> draws=new(),objects=new();
    string fingerprint="",directory="";
    bool captured,done;
    double lastFrame;
    bool impulseSent,peakSaved;
    float restingOffset;
    readonly List<float[]> springSamples=new();
    double lastSpringSample;
    public override void _Ready()
    {
        session.State.Seed=729;session.State.Experiment.Variant=ExperimentVariant.B;
        int players=int.Parse(Options.GetValueOrDefault("visual-players","4"));
        for(int i=1;i<=players;i++)session.AddPlayer(i,"LookDev "+i);
        session.StartMatch();var w=session.State;w.Phase=Phase.Build;w.Time=8;w.Remaining=120;
        for(int i=0;i<w.Players.Count;i++){var p=w.Players[i];p.Position=new((i%2==0?-1:1)*1.35f,0,(i<2?-.2f:-1.3f));p.Yaw=0;w.Barber(p.Slot).Position=Session.Eye(p);}
        foreach(var face in w.Heads.Where(h=>h.Facial)){var parent=w.Heads.First(h=>h.Id==face.ParentHead);face.Position=parent.ToWorld(Head.FaceOffset(face.Region));}
        if(Options.TryGetValue("visual-fixture",out var fixture))
        {fixture=Path.GetFullPath(fixture);if(File.Exists(fixture)){session.State=Wire.Decode<WorldState>(File.ReadAllBytes(fixture));w=session.State;}else {Directory.CreateDirectory(Path.GetDirectoryName(fixture)!);File.WriteAllBytes(fixture,Wire.Encode(w));}}
        string state=Options.GetValueOrDefault("visual-state","normal");
        var h=w.SharedHead;
        if(Options.ContainsKey("visual-llama")){w.Customers[0].Profile="llama";foreach(var head in w.Heads.Where(v=>v.Id==0||v.ParentHead==0))head.Material=HeadMaterialKind.Wool;}
        if(state=="flat")h.Volume.Brush(new(EffectKind.CutPlane,1,NVector.UnitY),new(0,.56f,0),1.2f);
        if(state=="hole")h.Volume.Brush(new(EffectKind.Puncture,1,-NVector.UnitZ),new(0,.6f,.4f),.19f,new(0,.6f,1),3);
        if(state=="overgrown")h.Volume.Fill(p=>Math.Min(HairVolume.Ellipsoid(p,new(0,.8f,-.12f),new(1.0f,1.4f,.8f)),Math.Min(-HairVolume.Ellipsoid(p,NVector.Zero,new(.405f,.37f,.36f)),-Math.Min(p.Z-.18f,.27f+.13f*p.X-p.Y))));
        if(state is "wet" or "frost" or "burn" or "glue")foreach(var p in h.Patches)HairMaterials.Set(p,state switch{"wet"=>HairState.Wet,"frost"=>HairState.Frozen,"burn"=>HairState.Charred,_=>HairState.Glued});
        if(state=="mixed")foreach(var p in h.Patches)HairMaterials.Set(p,p.Root.X<-.22f?HairState.Wet:p.Root.X>.22f?HairState.Charred:p.Root.Y>.20f?HairState.Frozen:p.Root.Z>0?HairState.Glued:HairState.Normal);
        if(state=="debris")
        {
            for(int i=0;i<512;i++)DebrisSystem.Deposit(w.Debris,new(-4+(i%21)*.4f,.015f,-4+(i/21)*.4f),.1f+(i%7)*.04f,new(){Color=new(.47f,.32f,.23f)});
            for(int i=0;i<64;i++)DebrisSystem.Emit(w.Debris,new(-2+(i%8)*.5f,.7f+(i%3)*.2f,-1+(i/8)*.4f),new(.2f,0,.1f),.14f,new(){Color=new(.47f,.32f,.23f)});
        }
        var tool=w.Tools.First(t=>t.Definition==0);tool.Holder=1;w.Player(1)!.Held=tool.Id;
        directory=Path.GetFullPath(Options.GetValueOrDefault("visual-output","artifacts/lookdev"));Directory.CreateDirectory(directory);
        salon=new();AddChild(salon);hud=new(){BPlaytest=true};AddChild(hud);hud.ShowMenu(false);hud.Update(w,1,true,false,salon.Mirror,0);
        salon.Sync(w,1,0);SetCamera();Input.MouseMode=Input.MouseModeEnum.Visible;
        File.WriteAllText(Path.Combine(directory,"bake-audit.json"),JsonSerializer.Serialize(salon.GetNode<SceneLook>("SceneLook").BakeAudit(),new JsonSerializerOptions{WriteIndented=true}));
        RenderingServer.ViewportSetMeasureRenderTime(GetViewport().GetViewportRid(),true);
        fingerprint=Hash();clock.Start();GD.Print("LOOKDEV_STARTED "+directory);
    }
    void SetCamera(){salon.Camera.Current=true;salon.ReplayCamera.Current=false;salon.Camera.CullMask=uint.MaxValue;bool floor=Options.ContainsKey("visual-floor-camera");salon.Camera.Position=floor?new(1.7f,1.4f,3.5f):new(.95f,2.08f,3.5f);salon.Camera.LookAt(floor?new(0,.15f,.8f):Art.V(session.State.SharedHead.Position)+new Vector3(0,.25f,0));salon.Camera.Fov=55;}
    string Hash()=>Convert.ToHexString(SHA256.HashData(Wire.Encode(session.State)));
    public override void _UnhandledInput(InputEvent e)
    {
        if(e is not InputEventKey{Pressed:true,Echo:false} k)return;
        if(k.Keycode==Key.F9){VisualQuality.Cycle();GD.Print("LOOKDEV_PROFILE "+VisualQuality.Profile);}
        if(k.Keycode==Key.F10){VisualQuality.Scene=!VisualQuality.Scene;VisualQuality.Revision++;}
    }
    public override void _Process(double delta)
    {
        if(done)return;
        double now=clock.Elapsed.TotalSeconds,start=clock.Elapsed.TotalMilliseconds;
        salon.Sync(session.State,1,(float)delta);SetCamera();
        var mainHair=salon.GetNode<HeadView>("Hair_0");
        if(now<2)restingOffset=Math.Max(restingOffset,mainHair.VisualOffsetMeters);
        if(Options.ContainsKey("visual-impulse")&&now>=2.2&&!impulseSent){mainHair.VisualImpulse(new Vector3(.25f,.07f,0));impulseSent=true;}
        if(now-lastSpringSample>.016){var v=mainHair.VisualDisplacementMeters;springSamples.Add(new[]{(float)now,v.X,v.Y,v.Z});lastSpringSample=now;}
        if(impulseSent&&now>2.4&&!peakSaved){SaveImage();File.Move(Path.Combine(directory,"frame.png"),Path.Combine(directory,"event.png"),true);peakSaved=true;captured=false;}
        double work=clock.Elapsed.TotalMilliseconds-start;
        if(now>2&&!captured&&lastFrame>0)
        {
            frames.Add((now-lastFrame)*1000);sync.Add(work);
            cpu.Add(RenderingServer.GetFrameSetupTimeCpu()+RenderingServer.ViewportGetMeasuredRenderTimeCpu(GetViewport().GetViewportRid()));
            gpu.Add(RenderingServer.ViewportGetMeasuredRenderTimeGpu(GetViewport().GetViewportRid()));
            draws.Add(GetViewport().GetRenderInfo(Viewport.RenderInfoType.Visible,Viewport.RenderInfo.DrawCallsInFrame));
            objects.Add(GetViewport().GetRenderInfo(Viewport.RenderInfoType.Visible,Viewport.RenderInfo.ObjectsInFrame));
        }
        lastFrame=now;
        if(now<double.Parse(Options.GetValueOrDefault("visual-seconds","8"),System.Globalization.CultureInfo.InvariantCulture))return;
        done=true;
        try
        {
            string after=Hash();if(after!=fingerprint)throw new InvalidOperationException("Presentation changed authority");
            var sorted=frames.Order().ToArray();double p99=sorted.Length>0?sorted[(int)((sorted.Length-1)*.99)]:0;
            var result=new{phase=VisualQuality.Phase,profile=VisualQuality.Profile.ToString(),state=Options.GetValueOrDefault("visual-state","normal"),engine=Engine.GetVersionInfo()["string"].AsString(),renderer=RenderingServer.GetCurrentRenderingMethod(),gpuDevice=RenderingServer.GetVideoAdapterName(),resolution=GetViewport().GetVisibleRect().Size.ToString(),players=session.State.Players.Count,customers=1,fixture=true,simulationTicked=false,samples=frames.Count,wallSeconds=now,avgFps=frames.Count>0?1000/frames.Average():0,frameMs=frames.Count>0?frames.Average():0,onePercentLow=p99>0?1000/p99:0,cpuRenderSetupMs=cpu.Count>0?cpu.Average():0,gpuMs=gpu.Any(v=>v>0)?(double?)gpu.Average():null,presentationSyncMs=sync.Count>0?sync.Average():0,drawCalls=draws.Count>0?draws.Average():0,visibleObjects=objects.Count>0?objects.Average():0,hairTriangles=Descendants(salon).OfType<HeadView>().Sum(v=>v.SurfaceTriangles),debris=Descendants(salon).OfType<DebrisView>().Select(v=>new{flying=v.FlyingCount,resting=v.RestingCount,key=v.KeyCount}).ToArray(),stateMin=mainHair.StateMin.ToString(),stateMax=mainHair.StateMax.ToString(),restingOffsetMeters=restingOffset,peakOffsetMeters=mainHair.VisualPeakMeters,finalOffsetMeters=mainHair.VisualOffsetMeters,authorityUnchanged=after==fingerprint,authoritySha256=after};
            File.WriteAllText(Path.Combine(directory,"metrics.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
            File.WriteAllText(Path.Combine(directory,"spring.json"),JsonSerializer.Serialize(springSamples));
            SaveImage();GD.Print("LOOKDEV_OK "+JsonSerializer.Serialize(result));
            if(!Options.ContainsKey("visual-interactive"))Finished(0);else {done=false;captured=false;clock.Restart();frames.Clear();cpu.Clear();gpu.Clear();draws.Clear();objects.Clear();sync.Clear();lastFrame=0;}
        }catch(Exception ex){GD.PushError(ex.ToString());Finished(1);}
    }
    void SaveImage(){using var screenshot=GetViewport().GetTexture().GetImage();screenshot.SavePng(Path.Combine(directory,"frame.png"));screenshot.Resize(360,225,Image.Interpolation.Lanczos);screenshot.SavePng(Path.Combine(directory,"thumbnail.png"));captured=true;}
    public static IEnumerable<Node> Descendants(Node node){foreach(var c in node.GetChildren()){yield return c;foreach(var d in Descendants(c))yield return d;}}
}


