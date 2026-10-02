using Godot;
using Hairball.Core;
using System.Collections.Generic;
using System.Linq;
namespace Hairball;

// Reversible local environment layer, including no new collision bodies.
public partial class SceneLook : Node3D
{
    readonly Dictionary<MeshInstance3D,Material> original=new(),quiet=new();
    readonly List<Label3D> hints=new();
    Node3D additions=null!;DirectionalLight3D fill=null!;SpotLight3D key=null!;
    WorldEnvironment worldEnvironment=null!;Godot.Environment prior=null!,candidate=null!;
    int revision=-1;bool scene;
    readonly Dictionary<int,MeshInstance3D> feet=new();
    readonly List<Label3D> contextHints=new(),temporarilyHidden=new();
    float hintRefresh;
    static ShaderMaterial? blob;
    public static MeshInstance3D Shadow(Node3D parent,Vector3 position,Vector2 size)
    {
        blob??=new(){Shader=new Shader{Code="shader_type spatial; render_mode unshaded, cull_disabled, depth_draw_never; void fragment(){float r=length((UV-vec2(0.5))*2.0);ALBEDO=vec3(0.18,0.13,0.10);ALPHA=0.20*(1.0-smoothstep(0.05,1.0,r));}"}};
        var mesh=new MeshInstance3D{Mesh=new QuadMesh{Size=size},MaterialOverride=blob,Position=position,RotationDegrees=new(-90,0,0),CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};parent.AddChild(mesh);return mesh;
    }
    public override void _Ready()
    {
        var parent=(Node3D)GetParent();
        foreach(var mesh in parent.GetChildren().OfType<MeshInstance3D>())if(mesh.MaterialOverride is StandardMaterial3D material)
        {original[mesh]=material;var muted=(StandardMaterial3D)material.Duplicate();var c=material.AlbedoColor;float gray=c.R*.3f+c.G*.5f+c.B*.2f;muted.AlbedoColor=c.Lerp(new Color(gray*1.04f,gray,gray*.96f),.38f);quiet[mesh]=muted;}
        hints.AddRange(parent.GetChildren().OfType<Label3D>());
        worldEnvironment=parent.GetChildren().OfType<WorldEnvironment>().First();prior=worldEnvironment.Environment;candidate=(Godot.Environment)prior.Duplicate();
        candidate.BackgroundColor=new("e0ccaf");candidate.AmbientLightColor=new("e4ded2");candidate.AmbientLightEnergy=.45f;candidate.SsaoRadius=.22f;candidate.SsaoIntensity=.65f;
        fill=parent.GetChildren().OfType<DirectionalLight3D>().First();
        additions=new(){Name="VisualRoom"};AddChild(additions);
        Art.Box(additions,new(0,4.1f,0),new(12,.18f,12),new("e4d9c6")).CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
        foreach(float x in new[]{-4f,0f,4f})Art.Box(additions,new(x,3.97f,0),new(.10f,.14f,12),new("b6a58f"));
        foreach(float z in new[]{-3.5f,0f,3.5f})Art.Box(additions,new(0,3.97f,z),new(12,.14f,.10f),new("b6a58f"));
        foreach(float x in new[]{-3f,3f}){Art.Cylinder(additions,new(x,3.72f,-1),.34f,.10f,new("b99563"),.24f);Art.Cylinder(additions,new(x,3.66f,-1),.24f,.02f,new("fff0c7"));}
        foreach(var mesh in additions.GetChildren().OfType<MeshInstance3D>())mesh.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
        key=new(){Position=new(-1.6f,3.8f,2.2f),LightColor=new("ffe1b4"),LightEnergy=1.5f,SpotRange=7,SpotAngle=52,SpotAttenuation=.65f,ShadowEnabled=false};additions.AddChild(key);key.LookAt(new(0,1.9f,0));
        Shadow(additions,new(0,.013f,0),new(2.1f,1.8f));Apply();
        if(VisualQuality.Puppet){
            candidate.TonemapMode=Godot.Environment.ToneMapper.Aces;candidate.TonemapWhite=6;candidate.AmbientLightEnergy=.22f;
            candidate.AmbientLightColor=new("b8c4d3");candidate.SsaoRadius=.10f;candidate.SsaoIntensity=.8f;candidate.SsaoLightAffect=.08f;
            key.Position=new(-1.75f,2.88f,2.65f);key.LookAt(new(0,1.7f,0));key.LightEnergy=9;
            key.SpotRange=8;key.SpotAngle=72;key.SpotAttenuation=2;key.SpotAngleAttenuation=1;
            key.ShadowEnabled=true;key.ShadowBias=.06f;key.ShadowNormalBias=.6f;key.LightSize=.58f;
            fill.LightEnergy=.18f;RenderingServer.PositionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.SoftMedium);
            var rear=new SpotLight3D{Position=new(1.85f,3.65f,-1.2f),LightColor=new("d0e5ff"),LightEnergy=5.5f,SpotRange=8,SpotAngle=72,SpotAttenuation=2,SpotAngleAttenuation=1,ShadowEnabled=true,ShadowBias=.06f,ShadowNormalBias=.6f,LightSize=.45f};additions.AddChild(rear);rear.LookAt(new(-.25f,1.9f,.6f));
            var surface=new PuppetLabSurface();foreach(var mesh in original.Keys)surface.Collect(mesh,true);surface.Collect(additions,true);surface.Enable(true);
        }
    }
    void Apply()
    {
        if(revision==VisualQuality.Revision&&scene==VisualQuality.Scene)return;revision=VisualQuality.Revision;scene=VisualQuality.Scene;
        additions.Visible=scene;foreach(var pair in original)pair.Key.MaterialOverride=scene?quiet[pair.Key]:pair.Value;
        candidate.SsaoEnabled=VisualQuality.Ssao;worldEnvironment.Environment=scene?candidate:prior;
        fill.LightEnergy=scene?.35f:.65f;fill.LightColor=scene?new Color("e6e9ee"):new("fff0d4");
    }
    public void Sync(WorldState w,int localId)
    {
        foreach(var label in temporarilyHidden)if(IsInstanceValid(label)&&label.IsInsideTree()&&!label.IsQueuedForDeletion())label.Visible=true;temporarilyHidden.Clear();
        Apply();var local=w.Player(localId);
        foreach(var hint in hints)hint.Visible=!scene||(local!=null&&Session.LookingAt(local,Art.N(hint.GlobalPosition),.8f,2.8f));
        if(!scene){foreach(var shadow in feet.Values)shadow.Visible=false;return;}
        foreach(var p in w.Players.Where(p=>p.Active))
        {
            if(!feet.TryGetValue(p.Id,out var shadow)){shadow=Shadow(this,Vector3.Zero,new(.9f,.7f));feet[p.Id]=shadow;}
            shadow.Visible=scene&&!p.NetworkAway;shadow.Position=new(p.Position.X,.014f,p.Position.Z);float height=System.Math.Max(0,p.Position.Y);shadow.Scale=Vector3.One/(1+height*.5f);
        }
    }
    public void FinishHints(WorldState w,int localId,float dt)
    {
        if(!scene)return;hintRefresh-=dt;
        if(hintRefresh<=0){hintRefresh=.5f;contextHints.Clear();contextHints.AddRange(VisualLookDev.Descendants(GetParent()).OfType<Label3D>().Where(l=>l.HasMeta("visual_context_hint")||l.Name=="OrderTitle"||l.Name=="ComboWall"));}
        var local=w.Player(localId);if(local==null)return;
        foreach(var label in contextHints)if(IsInstanceValid(label)&&label.IsInsideTree()&&!label.IsQueuedForDeletion()&&label.Visible&&!Session.LookingAt(local,Art.N(label.GlobalPosition),.8f,3))
        {label.Visible=false;temporarilyHidden.Add(label);}
    }
    public object BakeAudit()=>new{roomMeshes=original.Count,primitiveMeshes=original.Keys.Count(m=>m.Mesh is PrimitiveMesh),uv2Meshes=original.Keys.Count(m=>m.Mesh is PrimitiveMesh p&&p.AddUV2),bakedLightmapLoaded=false,construction="runtime code-generated Art.Shop",renderer=RenderingServer.GetCurrentRenderingMethod(),pipelineChange="optional static scene export + UV2 ArrayMesh + offline editor RD bake + stable path mapping; exclude moving chair/props/heads"};
}
