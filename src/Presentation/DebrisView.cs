using Godot;
using Hairball.Core;
using System;
using System.Linq;

namespace Hairball;

// One draw batch for airborne chips, one for resting chips. No particle has a node/body.
public partial class DebrisView : Node3D
{
    MultiMesh flying=null!,resting=null!;
    MultiMesh keyChunks=null!;
    int visualRevision=-1;
    ShaderMaterial keyMaterial=null!;
    public int KeyCount=>keyChunks.VisibleInstanceCount;
    int lastHash=int.MinValue;
    float lastTime=-1,extrapolation;
    public int FlyingCount=>flying.VisibleInstanceCount;
    public int RestingCount=>resting.VisibleInstanceCount;
    public override void _Ready()
    {
        var chip=new ArrayMesh();var a=new Godot.Collections.Array();a.Resize((int)Mesh.ArrayType.Max);
        var p=new[]{new Vector3(-.5f,0,-.35f),new Vector3(.45f,0,-.25f),new Vector3(.25f,.15f,.45f),new Vector3(-.4f,.08f,.3f),new Vector3(0,.35f,0)};
        var vertices=new[]{p[0],p[1],p[4],p[1],p[2],p[4],p[2],p[3],p[4],p[3],p[0],p[4],p[0],p[3],p[2],p[0],p[2],p[1]};var normals=new Vector3[vertices.Length];
        for(int i=0;i<vertices.Length;i+=3){var normal=(vertices[i+2]-vertices[i]).Cross(vertices[i+1]-vertices[i]).Normalized();normals[i]=normals[i+1]=normals[i+2]=normal;}
        a[(int)Mesh.ArrayType.Vertex]=vertices;a[(int)Mesh.ArrayType.Normal]=normals;chip.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,a);
        MultiMesh Make(int count)
        {
            var multi=new MultiMesh{TransformFormat=MultiMesh.TransformFormatEnum.Transform3D,UseColors=true,Mesh=chip,InstanceCount=count,VisibleInstanceCount=0};
            AddChild(new MultiMeshInstance3D{Multimesh=multi,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,MaterialOverride=new StandardMaterial3D{VertexColorUseAsAlbedo=true,CullMode=BaseMaterial3D.CullModeEnum.Disabled,Roughness=.9f}});return multi;
        }
        flying=Make(256);resting=Make(DebrisState.MaxPiles*8);
        keyChunks=Make(32);keyChunks.Mesh=new SphereMesh{Radius=.5f,Height=1,RadialSegments=12,Rings=8};
        keyMaterial=new(){Shader=GD.Load<Shader>("res://shaders/debris_flocked.gdshader")};GetChildren().OfType<MultiMeshInstance3D>().Last().MaterialOverride=keyMaterial;
        if(VisualQuality.Puppet){
            keyMaterial=new(){Shader=GD.Load<Shader>("res://shaders/puppet_clippings.gdshader")};keyMaterial.SetShaderParameter("tuft_volume",PuppetTuftVolume.Field);
            var tuft=new SphereMesh{Radius=.5f,Height=1,RadialSegments=12,Rings=8};
            foreach(var batch in GetChildren().OfType<MultiMeshInstance3D>()){batch.Multimesh.Mesh=tuft;batch.MaterialOverride=keyMaterial;}
        }
    }
    static Color ShownMaterial(DebrisMaterial material)
    {
        if(!VisualQuality.Puppet)return new(material.Color.X,material.Color.Y,material.Color.Z);
        // Recover the already-transmitted base family from the existing state
        // tint; do not change saved debris mass, material, or wire packets.
        var patch=new Patch{Wet=material.Wet,Char=material.Char,Glue=material.Glue,Young=material.Young,Temperature=material.Heat};
        var wool=HairMaterials.Color(patch,new(.875f,.804f,.694f));
        float woolDistance=System.Numerics.Vector3.DistanceSquared(wool,material.Color);
        float hairDistance=new[]{new System.Numerics.Vector3(.47f,.32f,.23f),new(.35f,.24f,.41f),new(.68f,.44f,.73f)}.Min(c=>System.Numerics.Vector3.DistanceSquared(HairMaterials.Color(patch,c),material.Color));
        return new(material.Wet,material.Heat< -10?1:0,material.Char,woolDistance<hairDistance?1:0);
    }
    static float Noise(int seed){uint n=unchecked((uint)seed*747796405u+2891336453u);n=((n>>((int)(n>>28)+4))^n)*277803737u;return ((n>>22)^n)%10000/10000f;}
    public void Sync(DebrisState state,float time,float dt,bool replay)
    {
        if(visualRevision!=VisualQuality.Revision){visualRevision=VisualQuality.Revision;lastHash=int.MinValue;keyMaterial.SetShaderParameter("features",new Vector3(VisualQuality.Flocking?1:0,VisualQuality.Rim?1:0,VisualQuality.Micro?1:0));}
        bool enhanced=VisualQuality.Puppet||VisualQuality.Phase>=7&&VisualQuality.Profile!=HairProfile.Current;
        if(!VisualQuality.Debris){flying.VisibleInstanceCount=resting.VisibleInstanceCount=keyChunks.VisibleInstanceCount=0;return;}
        if(time!=lastTime){lastTime=time;extrapolation=0;}else extrapolation=Math.Min(.2f,extrapolation+dt);
        int index=0;
        foreach(var f in state.Flights)
        {
            int count=Math.Clamp((int)MathF.Ceiling(f.Mass*30),1,4);
            for(int i=0;i<count&&index<256;i++,index++)
            {
                int seed=f.Id*17+i*97;float phase=f.Age+(replay?0:extrapolation);
                var pos=Art.V(f.Position+f.Velocity*(replay?0:extrapolation));pos+=new Vector3(Noise(seed)-.5f,MathF.Sin(phase*5+seed)*.25f,Noise(seed+1)-.5f)*Math.Min(.14f,f.Age*.2f);pos.Y=Math.Max(.015f,pos.Y);
                float size=Math.Clamp(MathF.Pow(f.Mass*.035f/count,1f/3),.025f,.13f);
                var basis=Basis.FromEuler(new(phase*4+seed,phase*3,phase*2)).Scaled(new(size,size*.45f,size*.7f));
                flying.SetInstanceTransform(index,new(basis,pos));flying.SetInstanceColor(index,ShownMaterial(f.Material));
            }
        }
        flying.VisibleInstanceCount=index;
        int hash=HashCode.Combine(17,enhanced);foreach(var p in state.Piles)hash=HashCode.Combine(hash,p.Id,p.Mass,p.Position,p.Material.Color);
        if(hash==lastHash)return;lastHash=hash;index=0;
        int keyIndex=0;
        if(enhanced)foreach(var p in state.Piles.OrderByDescending(p=>p.Mass).Take(32).Where(p=>p.Mass>.08f))
        {
            float size=Math.Clamp(MathF.Pow(p.Mass*.035f,1f/3),.04f,.3f);
            // Existing authority pile position/mass; no additional physics bodies or gameplay mass.
            keyChunks.SetInstanceTransform(keyIndex,new(Basis.FromEuler(new(0,Noise(p.Id)*Mathf.Tau,0)).Scaled(new(size*1.45f,size*.4f,size*1.1f)),Art.V(p.Position)+Vector3.Up*size*.2f));
            keyChunks.SetInstanceColor(keyIndex++,ShownMaterial(p.Material));
        }
        keyChunks.VisibleInstanceCount=keyIndex;
        foreach(var p in state.Piles)
        {
            int count=Math.Clamp(1+(int)MathF.Sqrt(p.Mass*70),1,enhanced?2:8);float spread=Math.Clamp(MathF.Sqrt(p.Mass*.035f)*1.4f,.025f,.25f);
            float height=Math.Clamp(MathF.Log(1+p.Mass)*.018f,.003f,.12f);
            for(int i=0;i<count;i++,index++)
            {
                int seed=p.Id*31+i*137;float radius=spread*MathF.Sqrt(Noise(seed+1)),angle=Noise(seed)*Mathf.Tau;
                float surface=height*(1-radius/Math.Max(.001f,spread));
                var pos=Art.V(p.Position)+new Vector3(MathF.Cos(angle)*radius,.006f+surface,MathF.Sin(angle)*radius);
                float size=Math.Clamp(.018f+MathF.Pow(p.Mass/count*.035f,1f/3)*.6f,.025f,.11f);
                var transform=new Transform3D(Basis.FromEuler(new(.1f*Noise(seed+4),Noise(seed+3)*Mathf.Tau,0)).Scaled(new(size,.018f,size*.65f)),pos);
                if(i==0&&p.Mass>3)transform=new(Basis.Identity.Scaled(new(spread*2.3f,height*3,spread*2.3f)),Art.V(p.Position)+Vector3.Up*.003f);
                resting.SetInstanceTransform(index,transform);
                var color=p.Material.Color*(.82f+Noise(seed+5)*.3f);resting.SetInstanceColor(index,VisualQuality.Puppet?ShownMaterial(p.Material):new(color.X,color.Y,color.Z));
            }
        }
        resting.VisibleInstanceCount=index;
    }
}
