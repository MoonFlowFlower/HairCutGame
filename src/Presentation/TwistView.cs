using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Hairball;
public partial class SalonView
{
    public TargetCard? PrivateTarget;
    Node3D? fogArt,cloth;
    Label3D? backTarget;
    int fogCount=-1,fogRound=-1;
    readonly Dictionary<int,SpotLight3D> flashlights=new();
    CanvasLayer? flipLayer;
    ColorRect? flip;
    bool wasPowerOut;
    readonly Dictionary<Light3D,float> lightEnergy=new();
    readonly Dictionary<Godot.Environment,float> ambientEnergy=new();
    void SyncTwistPresentation(WorldState w,int localId)
    {
        bool resolved=w.Experiment.Resolved;
        if(flip==null){flipLayer=new(){Layer=0};AddChild(flipLayer);var copy=new BackBufferCopy{CopyMode=BackBufferCopy.CopyModeEnum.Viewport};flipLayer.AddChild(copy);flip=new(){MouseFilter=Control.MouseFilterEnum.Ignore,Material=new ShaderMaterial{Shader=new Shader{Code="shader_type canvas_item; uniform sampler2D screen_texture : hint_screen_texture, filter_nearest; void fragment(){ COLOR=texture(screen_texture,vec2(1.0-SCREEN_UV.x,SCREEN_UV.y)); }"}}};flipLayer.AddChild(flip);}
        flip.Size=GetViewport().GetVisibleRect().Size;flip.Visible=!resolved&&w.Twist.Kind==TwistKind.MirrorWorld&&w.Player(localId)?.Customer==true&&PresentationSettings.AllowViewChanges;
        bool power=!resolved&&w.Twist.Kind==TwistKind.PowerOut;
        if(power!=wasPowerOut){foreach(var node in Descendants(this)){if(node is Light3D light&&!flashlights.Values.Contains(light)&&light.GetParent() is not Camera3D){if(power){lightEnergy[light]=light.LightEnergy;light.LightEnergy=0;}else if(lightEnergy.TryGetValue(light,out float energy))light.LightEnergy=energy;}if(node is WorldEnvironment env&&env.Environment!=null){if(power){ambientEnergy[env.Environment]=env.Environment.AmbientLightEnergy;env.Environment.AmbientLightEnergy=.025f;}else if(ambientEnergy.TryGetValue(env.Environment,out float energy))env.Environment.AmbientLightEnergy=energy;}}wasPowerOut=power;}
        foreach(var p in w.Players.Where(p=>p.Active)){if(!flashlights.TryGetValue(p.Id,out var light)){light=new(){LightColor=new("fff0d1"),LightEnergy=3,SpotRange=9,SpotAngle=45,ShadowEnabled=true};AddChild(light);flashlights[p.Id]=light;}light.Visible=power;light.Position=Art.V(Session.Eye(p))+Vector3.Up*.2f;light.Rotation=new(-p.Pitch,p.Yaw,0);}
        if(fogArt==null){fogArt=new();AddChild(fogArt);}fogArt.Visible=w.Twist.Kind==TwistKind.FogMirror&&w.Time<w.Twist.FogUntil&&!resolved;
        if(fogArt.Visible&&(fogCount!=w.Twist.Lines.Count||fogRound!=w.Round)){foreach(var child in fogArt.GetChildren()){fogArt.RemoveChild(child);child.QueueFree();}fogCount=w.Twist.Lines.Count;fogRound=w.Round;var film=Art.Box(fogArt,Art.V(Session.MirrorPoint)+Vector3.Forward*.02f,new(1.08f,.83f,.005f),new("b6d8de"));var material=Art.Material(new Color(.65f,.8f,.83f,.55f),true);material.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;film.MaterialOverride=material;
            foreach(var line in w.Twist.Lines){var from=new Vector3(line.From.X,line.From.Y,-.028f)+Art.V(Session.MirrorPoint);var to=new Vector3(line.To.X,line.To.Y,-.028f)+Art.V(Session.MirrorPoint);var segment=Art.Cylinder(fogArt,(from+to)*.5f,.006f,(to-from).Length(),new("213b47"));segment.Quaternion=new Quaternion(Vector3.Up,(to-from).Normalized());}}
        if(fogArt.Visible&&fogArt.GetChildCount()>0&&fogArt.GetChild(0) is MeshInstance3D fog&&fog.MaterialOverride is StandardMaterial3D fogMaterial){var color=fogMaterial.AlbedoColor;color.A=Mathf.Clamp((w.Twist.FogUntil-w.Time)/20,0,1)*.7f;fogMaterial.AlbedoColor=color;}
        backTarget??=Art.Label(this,"",Vector3.Zero,15,new("ffe6a7"));backTarget.Text=PrivateTarget?.Face()??"";var pLocal=w.Player(localId);backTarget.Visible=!resolved&&w.Twist.Kind==TwistKind.SplitInfo&&pLocal is {Customer:false}&&PrivateTarget!=null&&w.SharedHead.ToLocal(Session.Eye(pLocal)).Z<-.3f;
        backTarget.Position=Art.V(w.SharedHead.ToWorld(new(0,.75f,-.46f)));backTarget.Rotation=Art.V(w.SharedHead.Rotation)+new Vector3(0,Mathf.Pi,0);
        if(cloth==null){cloth=new();AddChild(cloth);Art.Ball(cloth,Vector3.Zero,new(.7f,.9f,.65f),new("768bb3"));}
        cloth.Visible=TwistCards.CoverHair(w,localId);cloth.Position=Art.V(w.SharedHead.Position)+Vector3.Up*.35f;cloth.Rotation=Art.V(w.SharedHead.Rotation);
        if(cloth.Visible){var bounds=w.SharedHead.Volume.Samples().ToArray();float height=bounds.Length==0?.5f:Math.Clamp(bounds.Max(v=>v.Y),.5f,3);cloth.Scale=new(1,height,1);}
        ApplyTwistVisibility(w,localId);
    }
    void ApplyTwistVisibility(WorldState w,int localId)
    {
        bool cover=TwistCards.CoverHair(w,localId);foreach(var h in w.Heads.Where(h=>h.Id==0||h.ParentHead==0||h.AttachedTo==0))if(heads.TryGetValue(h.Id,out var hair))hair.Visible=!cover;
        // A private back label is not copied into gallery photos. Own portrait and customer mirror share the same masking policy.
        if(customerMirrorHair!=null)customerMirrorHair.Visible=!cover;foreach(var wig in mirrorWigs.Values)if(w.Player(localId)?.Customer==true)wig.Visible=!cover;
        if(cover){mirrorHair.Visible=false;Brush.Visible=false;}else mirrorHair.Visible=true;
        if(w.Twist.Kind==TwistKind.OneViewer&&w.Player(localId)?.Customer==false&&localId!=w.Twist.ViewerActor&&!w.Experiment.Resolved)foreach(var prop in w.Props.Where(p=>p.Holder==w.Experiment.CustomerActor&&p.Gesture>=0))if(props.TryGetValue($"{w.Round}:{prop.Id}",out var node))node.Visible=false;
    }
    static IEnumerable<Node> Descendants(Node root){foreach(var child in root.GetChildren()){yield return child;foreach(var node in Descendants(child))yield return node;}}
}
