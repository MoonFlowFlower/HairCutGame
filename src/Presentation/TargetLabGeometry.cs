using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using NVector=System.Numerics.Vector3;

namespace Hairball;

// Disposable visual target fixture. None of these assets replace production art or hair presets.
public static class TargetLabGeometry
{
    public const float DensityScale=2;
    public static float DensityScaleFor(bool giant)=>giant?DensityScale:4;
    public static StandardMaterial3D Mat(string color,float rough=.8f,float metal=0)=>new(){AlbedoColor=new(color),Roughness=rough,Metallic=metal,MetallicSpecular=.25f};
    public static MeshInstance3D Mesh(Node3D p,Mesh mesh,Vector3 at,Material mat)
    {var n=new MeshInstance3D{Mesh=mesh,Position=at,MaterialOverride=mat};p.AddChild(n);return n;}
    public static MeshInstance3D Ball(Node3D p,Vector3 at,Vector3 size,Material mat)
    {var n=Mesh(p,new SphereMesh{Radius=.5f,Height=1,RadialSegments=32,Rings=20},at,mat);n.Scale=size;return n;}
    public static MeshInstance3D Cylinder(Node3D p,Vector3 at,float radius,float height,Material mat)
        =>Mesh(p,new CylinderMesh{TopRadius=radius,BottomRadius=radius,Height=height,RadialSegments=48},at,mat);
    public static void Limb(Node3D p,Vector3 a,Vector3 b,float radius,Material mat)
    {
        var v=b-a;var n=Mesh(p,new CapsuleMesh{Radius=radius,Height=v.Length()+radius*2,RadialSegments=24,Rings=8},(a+b)*.5f,mat);
        n.Quaternion=new Quaternion(Vector3.Up,v.Normalized());
    }
    public static MeshInstance3D Round(Node3D p,Vector3 at,Vector3 size,float radius,Material mat)
    {
        // Six subdivided box faces projected onto a filleted box: real silhouette bevels.
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var indices=new List<int>();
        const int steps=10;var half=size*.5f;radius=Math.Min(radius,Math.Min(half.X,Math.Min(half.Y,half.Z))*.98f);var inner=half-Vector3.One*radius;
        for(int face=0;face<6;face++)
        {
            int start=vertices.Count,axis=face/2;float sign=face%2==0?1:-1;
            for(int j=0;j<=steps;j++)for(int i=0;i<=steps;i++)
            {
                float u=-1+2f*i/steps,v=-1+2f*j/steps;
                var q=axis==0?new Vector3(sign*half.X,u*half.Y,v*half.Z):axis==1?new Vector3(u*half.X,sign*half.Y,v*half.Z):new Vector3(u*half.X,v*half.Y,sign*half.Z);
                var c=q.Clamp(-inner,inner);var norm=(q-c).Normalized();vertices.Add(c+norm*radius);normals.Add(norm);
            }
            for(int j=0;j<steps;j++)for(int i=0;i<steps;i++)
            {int a=start+j*(steps+1)+i,b=a+1,c=a+steps+1,d=c+1;Triangle(a,b,c);Triangle(b,d,c);}
        }
        void Triangle(int a,int b,int c){if((vertices[b]-vertices[a]).Cross(vertices[c]-vertices[a]).Dot(normals[a]+normals[b]+normals[c])>0)(b,c)=(c,b);indices.AddRange([a,b,c]);}
        return Mesh(p,Array(vertices.ToArray(),normals.ToArray(),indices.ToArray()),at,mat);
    }
    public static ArrayMesh Array(Vector3[] positions,Vector3[] normals,int[]? indices=null)
    {var a=new Godot.Collections.Array();a.Resize((int)Godot.Mesh.ArrayType.Max);a[(int)Godot.Mesh.ArrayType.Vertex]=positions;a[(int)Godot.Mesh.ArrayType.Normal]=normals;if(indices!=null)a[(int)Godot.Mesh.ArrayType.Index]=indices;var mesh=new ArrayMesh();mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles,a);return mesh;}
    public static void Curve(Node3D parent,Vector3[] path,float radius,Material mat)
    {for(int i=1;i<path.Length;i++)Limb(parent,path[i-1],path[i],radius,mat);}
    static float Union(float a,float b,float k){float h=Math.Clamp(.5f+.5f*(a-b)/k,0,1);return b+(a-b)*h+k*h*(1-h);}
    public static HairVolume Hair(bool giant)
    {
        float scale=DensityScaleFor(giant);var v=new HairVolume();v.Fill(sample=>HairField(sample/scale,giant)*scale);return v;
    }
    static float HairField(NVector p,bool giant)
    {
            if(!giant)return NormalField(p);
            float d=NormalField(p);
            // A continuous asymmetrical swept pompadour, narrowing at the roots and crest.
            int count=giant?29:15;
            if(giant)d=Union(d,Math.Min(HairVolume.Ellipsoid(p,new(.05f,.52f,-.08f),new(.68f,.67f,.54f)),1.05f-.12f*p.Z-p.Y),.12f);
            for(int i=0;i<count;i++)
            {
                float t=(float)i/(count-1);
                float u=1-t;
                var center=giant?new NVector(-.28f,.28f,.08f)*u*u*u+new NVector(-1.1f,.82f,.06f)*3*u*u*t+new NVector(-.55f,1.98f,-.08f)*3*u*t*t+new NVector(.32f,1.38f,-.03f)*t*t*t:new NVector(-.29f+.72f*t,.3f+.31f*MathF.Sin(t*2.3f),-.015f-.1f*t);
                float r=giant?.30f+.09f*MathF.Sin(t*MathF.PI)-.14f*t*t:.29f+.09f*MathF.Sin(t*MathF.PI);
                d=Union(d,HairVolume.Ellipsoid(p,center,new(r,giant?r*.83f:.28f,giant?.47f-.12f*t:r*.83f)),.10f);
            }
            // Broad continuous combed flutes are geometry, visible with the featureless gray material.
            float sweep=.33f*MathF.Sin(p.Y*2.8f);
            float phase=giant?(p.Y-.40f*MathF.Sin(p.X*3.1f))*25:(p.X-sweep)*17;
            float groove=.016f*(.5f+.5f*MathF.Cos(phase));
            d-=groove*Math.Clamp((p.Z-.13f)*5,0,1)*Math.Clamp((p.Y-.13f)*5,0,1);
            // Open forehead, rounded temple taper and scalp boundary are shared with the head shape.
            float forehead=Math.Min(p.Z-.10f,.23f+.15f*p.X-p.Y);
            // Match the imported scalp; the large crest begins above the normal hairline.
            float rootLimit=Math.Clamp((p.Y-.12f)/.40f,0,1);
            float sides=(.31f+.48f*rootLimit)-MathF.Abs(p.X);
            float frontLimit=.16f+.46f*rootLimit-p.Z;
            return Math.Min(d,Math.Min(sides,Math.Min(frontLimit,p.Y+.13f)));
    }
    static float NormalField(NVector p)
    {
        // Sized to Snow's imported scalp. Modest swept quiff, not a scaled-down crazy blob.
        float d=HairVolume.Ellipsoid(p,new(0,.055f,-.025f),new(.30f,.29f,.27f));
        for(int i=0;i<16;i++)
        {
            float t=i/15f;
            var center=new NVector(-.20f+.40f*t,.21f+.13f*MathF.Sin(t*MathF.PI),.12f-.17f*t);
            d=Union(d,HairVolume.Ellipsoid(p,center,new(.15f-.035f*t,.145f,.17f)),.055f);
        }
        // The asymmetrical hairline opens both brows; sideburns end above the ears.
        float frontCut=Math.Min(p.Z-.115f,.115f+.09f*p.X-p.Y);
        float bottomCut=p.Y+.13f;
        d=Math.Min(d,Math.Min(-frontCut,bottomCut));
        // Deliberately shallow grouped combing survives featureless gray inspection.
        float groove=.008f*(.5f+.5f*MathF.Cos((p.X-.22f*MathF.Sin(p.Y*3))*41));
        return d-groove*Math.Clamp((p.Z-.10f)*8,0,1)*Math.Clamp(p.Y*8,0,1);
    }
    public static HairVolume AnimalHair(bool giant)
    {
        float scale=DensityScaleFor(giant);var v=new HairVolume();v.Fill(p=>FleeceField(p/scale,giant)*scale);return v;
    }
    static float FleeceField(NVector p,bool giant)
    {
        float s=giant?1.6f:1; p/=s;
        float d=HairVolume.Ellipsoid(p,new(0,.03f,0),new(.32f,.24f,.31f));
        for(int i=0;i<9;i++)
        {
            float a=i*MathF.Tau/9;
            d=Union(d,HairVolume.Ellipsoid(p,new(.23f*MathF.Cos(a),.10f+.05f*MathF.Sin(a*3),.22f*MathF.Sin(a)),new(.16f,.19f,.16f)),.05f);
        }
        return Math.Min(d,p.Y+.10f)*s;
    }
    static float DisplayField(NVector p,bool giant,bool animal)=>animal?FleeceField(p,giant):HairField(p,giant);
    public static ArrayMesh HairMesh(HairVolume volume,bool giant,bool animal=false)
    {
        var shell=HairShell.Build(volume);var v=new List<Vector3>();var n=new List<Vector3>();
        for(int i=0;i<shell.Indices.Count;i+=3)
        {
            int ia=shell.Indices[i],ib=shell.Indices[i+1],ic=shell.Indices[i+2];var a=shell.Vertices[ia];var b=shell.Vertices[ib];var c=shell.Vertices[ic];
            if(NVector.Dot(NVector.Cross(b-a,c-a),volume.Normal((a+b+c)/3))>0)(ib,ic)=(ic,ib);
            foreach(int index in new[]{ia,ib,ic})
            {
                var p=shell.Vertices[index]/DensityScaleFor(giant);v.Add(new(p.X,p.Y,p.Z));float e=giant?.045f:.020f;
                var normal=new Vector3(DisplayField(p-new NVector(e,0,0),giant,animal)-DisplayField(p+new NVector(e,0,0),giant,animal),DisplayField(p-new NVector(0,e,0),giant,animal)-DisplayField(p+new NVector(0,e,0),giant,animal),DisplayField(p-new NVector(0,0,e),giant,animal)-DisplayField(p+new NVector(0,0,e),giant,animal)).Normalized();n.Add(normal);
            }
        }
        return Array(v.ToArray(),n.ToArray());
    }
    public static void Cape(Node3D parent,Material mat)
    {
        const int rings=24,sides=64;var v=new List<Vector3>();var n=new List<Vector3>();var indices=new List<int>();
        Vector3 Point(float t,float angle)
        {
            t=Math.Clamp(t,0,1);
            float radius=.18f+.38f*MathF.Pow(MathF.Sin(t*MathF.PI*.5f),.65f);
            radius+=.036f*MathF.Cos(angle*9)*MathF.Pow(t,1.4f);
            return new(MathF.Cos(angle)*radius,1.565f-.81f*t+.04f*MathF.Sin(angle)*t,.06f+.14f*t+MathF.Sin(angle)*radius*(.7f+.1f*t));
        }
        for(int j=0;j<=rings;j++)for(int i=0;i<=sides;i++)
        {float t=(float)j/rings,a=MathF.Tau*i/sides;v.Add(Point(t,a));n.Add((Point(t,a+.001f)-Point(t,a-.001f)).Cross(Point(t+.001f,a)-Point(t-.001f,a)).Normalized());}
        for(int j=0;j<rings;j++)for(int i=0;i<sides;i++)
        {int a=j*(sides+1)+i,b=a+1,c=a+sides+1,d=c+1;Add(a,b,c);Add(b,d,c);}
        void Add(int a,int b,int c){if((v[b]-v[a]).Cross(v[c]-v[a]).Dot(n[a])>0)(b,c)=(c,b);indices.AddRange([a,b,c]);}
        // This is a single draped open garment surface; its inside is intentionally visible at the hem.
        var cloth=(StandardMaterial3D)mat.Duplicate();cloth.CullMode=BaseMaterial3D.CullModeEnum.Disabled;
        Mesh(parent,Array(v.ToArray(),n.ToArray(),indices.ToArray()),Vector3.Zero,cloth);
    }
}
