using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hairball;
public partial class HeadView : Node3D
{
    public static bool HeadlessSimulationOnly;
    readonly MeshInstance3D shell=new();
    readonly List<MeshInstance3D> anchors=new(),flames=new();
    int revision=-1,materialHash;
    Vector3 priorPosition, motion;
    float priorTime;
    Vector3 contactPoint;
    float contactAt=-100,load;
    public void Touch(System.Numerics.Vector3 world,float time,float weight=1){contactPoint=Art.V(world);contactAt=time;load=Math.Clamp(weight,.1f,2);fur?.Contact(Art.V(world),time,weight);ObserveContact(world,time,weight);}
    MainlineFur? fur;
    public MainlineFur? PuppetFur=>fur;
    public void ResetPuppet(){if(fur==null)return;fur.Dispose();fur.Root.QueueFree();fur=null;}
    public System.Numerics.Vector3 Observer;
    const string MaterialShader=@"shader_type spatial;
render_mode cull_back;
uniform vec3 impulse=vec3(0.0);
uniform float clock=0.0;
uniform vec3 contact=vec3(0.0);
uniform float pulse=0.0;
void vertex(){float mobility=UV.x;float anchor=smoothstep(0.0,0.5,VERTEX.y); float distance_to_hit=length(VERTEX-contact); float ripple=sin(clock*22.0-distance_to_hit*9.0)*pulse*exp(-distance_to_hit*1.5); VERTEX+=impulse*mobility*anchor; VERTEX.y+=(sin(clock*5.0+VERTEX.x*2.0)*0.08+ripple)*mobility*anchor;}
void fragment(){ALBEDO=COLOR.rgb; ROUGHNESS=UV.y; SPECULAR=0.45;}";
    HairVolume? seenVolume;
    byte[] renderedDensity=[];
    float lastBuild=-100;
    bool barber,wig;
    public int referenceGoal=-1;
    public int SurfaceTriangles {get;private set;}
    public void RenderClock(float time)
    {if(!HeadlessSimulationOnly&&!look)legacyMaterial.SetShaderParameter("clock",time);}
    public HeadView()
    {
        AddChild(shell);
        legacyMaterial=new ShaderMaterial{Shader=new Shader{Code=MaterialShader}};shell.MaterialOverride=legacyMaterial;
        for(int i=0;i<32;i++)
        {anchors.Add(Art.Ball(this,Vector3.Zero,Vector3.One*.065f,new("f5dc86")));flames.Add(Art.Ball(this,Vector3.Zero,new(.08f,.16f,.08f),new("ffc85c")));}
    }
    public void Update(Head h,float time,uint layer=1,bool localPosition=false,bool editing=false)
    {
        if(!localPosition)Position=Art.V(h.Position);Rotation=localPosition?Vector3.Zero:Art.V(h.Rotation);Scale=Vector3.One*h.GeometryScale;shell.Layers=layer;if(HeadlessSimulationOnly)return;
        UpdateLook(h);
        ObserveMotion(h,time);
        float delta=Math.Clamp(time-priorTime,0,.1f);
        if(delta>0){var displacement=Art.V(h.Position)-priorPosition;motion=motion.Lerp((-displacement*15).LimitLength(1),.3f)*MathF.Exp(-delta*5);priorPosition=Art.V(h.Position);priorTime=time;}
        if(!look){var shader=legacyMaterial;shader.SetShaderParameter("impulse",motion);shader.SetShaderParameter("clock",time);
        shader.SetShaderParameter("contact",Art.V(h.ToLocal(Art.N(contactPoint))));shader.SetShaderParameter("pulse",load*MathF.Exp(-Math.Max(0,time-contactAt)*6));}
        int hash=17+(int)h.Material;foreach(var p in h.Patches)hash=unchecked(hash*31+(int)(p.Char*16)+(int)(p.Glue*16)*17+(p.Frozen?300:0)+(p.Burning?600:0)+(int)(p.Young*8)*1100+(int)(p.Wet*8)*1700+(int)(p.Resistance*16)*2300);
        bool isWig=h.Patches.Any(p=>p.Wig);
        bool replaced=seenVolume!=h.Volume&&!h.Volume.Data.AsSpan().SequenceEqual(renderedDensity);
        bool puppet=VisualQuality.Puppet&&referenceGoal<0;
        if(puppet){fur??=new(this,h.Facial||h.Miniature||h.Fragment,!localPosition&&!h.Miniature&&!h.Loose);fur.Sync(h,hash,layer);shell.Visible=false;SurfaceTriangles=fur.SurfaceTriangles;}
        else if(fur!=null){fur.Dispose();fur.Root.QueueFree();fur=null;shell.Visible=true;revision=-1;}
        if(!puppet&&(replaced||revision!=h.Volume.Revision||hash!=materialHash||barber!=h.Barber||wig!=isWig)&&(revision<0||Math.Abs(time-lastBuild)>=(editing?.05f:.09f)))
        {
            if(revision>=0&&revision!=h.Volume.Revision)motion=(motion+new Vector3(.28f,.12f,.16f)).LimitLength(1);
            Rebuild(h);revision=h.Volume.Revision;materialHash=hash;barber=h.Barber;wig=isWig;lastBuild=time;
            renderedDensity=(byte[])h.Volume.Data.Clone();seenVolume=h.Volume;
        }
        else if(!replaced)seenVolume=h.Volume;
        for(int i=0;i<Math.Min(32,h.Patches.Count);i++)
        {
            var p=h.Patches[i];var tip=p.Root+p.Direction*p.Length;
            if(!p.Anchored&&!p.Burning){anchors[i].Visible=flames[i].Visible=false;continue;}
            // Markers sit on the merged surface, never expose the underlying control columns.
            if(h.Volume.Raycast(tip+System.Numerics.Vector3.UnitY*4,-System.Numerics.Vector3.UnitY,8,out var surface,out _))tip=surface;
            anchors[i].Visible=p.Anchored&&h.Mass>.01f;anchors[i].Position=Art.V(tip);anchors[i].Layers=layer;
            flames[i].Visible=p.Burning&&h.Mass>.01f;flames[i].Position=Art.V(tip)+Vector3.Up*.06f;flames[i].Scale=new(1,1+MathF.Sin(time*13+i)*.3f,1);flames[i].Layers=layer;
            flames[i].Transparency=1-MaterialFeel.Facing(Observer,h.Position,h.ToWorld(tip));
        }
    }
    public override void _ExitTree(){fur?.Dispose();fur=null;}
    void Rebuild(Head h)
    {
        var materialCells=new Dictionary<(int,int,int),Patch>();
        var stateCells=new Dictionary<(int,int,int),Color>();var normalCache=new Dictionary<System.Numerics.Vector3,Vector3>();
        Vector3 Smooth(System.Numerics.Vector3 point){if(!normalCache.TryGetValue(point,out var n)){n=Art.V(h.Volume.Normal(point));normalCache[point]=n;}return n;}
        StateMin=Vector4.One;StateMax=Vector4.Zero;
        Patch Material(System.Numerics.Vector3 v){var key=((int)MathF.Floor(v.X/HairVolume.Step),(int)MathF.Floor(v.Y/HairVolume.Step),(int)MathF.Floor(v.Z/HairVolume.Step));if(!materialCells.TryGetValue(key,out var result)){result=HairSystem.MaterialAt(h,new System.Numerics.Vector3(key.Item1+.5f,key.Item2+.5f,key.Item3+.5f)*HairVolume.Step);materialCells[key]=result;}return result;}
        var geometry=HairShell.Build(h.Volume);SurfaceTriangles=geometry.Indices.Count/3;
        var vertices=new Vector3[geometry.Indices.Count];var normals=new Vector3[vertices.Length];var colors=new Color[vertices.Length];var uv=new Vector2[vertices.Length];
        for(int i=0;i<vertices.Length;i+=3)
        {
            var a=geometry.Vertices[geometry.Indices[i]];var b=geometry.Vertices[geometry.Indices[i+1]];var c=geometry.Vertices[geometry.Indices[i+2]];
            var center=(a+b+c)/3;var normal=System.Numerics.Vector3.Cross(b-a,c-a);
            // Godot front faces wind clockwise; orient from the density gradient explicitly.
            if(System.Numerics.Vector3.Dot(normal,h.Volume.Normal(center))>0)(b,c)=(c,b);
            normal=System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Cross(c-a,b-a));
            var p=Material(center);Color color=p.Wig?new("af70b9"):h.Barber?new("593e68"):new("79513b");
            if(h.Material==HeadMaterialKind.Wool&&!h.Barber)color=new("dfcdb1");
            if(h.Fragment)color=new(h.FragmentColor.X,h.FragmentColor.Y,h.FragmentColor.Z);
            var rgb=HairMaterials.Color(p,new(color.R,color.G,color.B));color=new(rgb.X,rgb.Y,rgb.Z);
            if(h.Id==-100&&referenceGoal==4&&GoalMaterials.InZone(4,center))color=new("25221f");
            vertices[i]=Art.V(a);vertices[i+1]=Art.V(b);vertices[i+2]=Art.V(c);
            for(int j=0;j<3;j++){var point=j==0?a:j==1?b:c;normals[i+j]=look?Smooth(point):Art.V(normal);colors[i+j]=look&&VisualQuality.LocalStates?LocalState(h,point,stateCells):color;uv[i+j]=new(MaterialFeel.Mobility(Material(point)),p.Frozen?.08f:p.Wet>.3f?.16f:p.Glue>.45f?.32f:p.Char>.45f?.98f:.8f);
            if(look&&VisualQuality.LocalStates){var v=colors[i+j];StateMin=new(Math.Min(StateMin.X,v.R),Math.Min(StateMin.Y,v.G),Math.Min(StateMin.Z,v.B),Math.Min(StateMin.W,v.A));StateMax=new(Math.Max(StateMax.X,v.R),Math.Max(StateMax.Y,v.G),Math.Max(StateMax.Z,v.B),Math.Max(StateMax.W,v.A));}}
        }
        var previous=shell.Mesh;
        if(vertices.Length==0)shell.Mesh=null;
        else {var arrays=new Godot.Collections.Array();arrays.Resize((int)Mesh.ArrayType.Max);arrays[(int)Mesh.ArrayType.Vertex]=vertices;arrays[(int)Mesh.ArrayType.Normal]=normals;arrays[(int)Mesh.ArrayType.Color]=colors;arrays[(int)Mesh.ArrayType.TexUV]=uv;var mesh=new ArrayMesh();mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);shell.Mesh=mesh;}
        previous?.Dispose();
    }
}
