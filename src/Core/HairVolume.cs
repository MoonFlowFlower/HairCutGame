using System;
using System.Collections.Generic;
using System.Numerics;

namespace Hairball.Core;

// Signed density lattice: positive is material. Cells are never rendered as blocks.
// The same field drives brushes, ray hits, scoring, collision proxies and the outer shell.
public sealed partial class HairVolume
{
    public const float Step=.14f, Band=.28f, UnitVolume=.035f;
    public const int NX=35, NY=31, NZ=27, Count=NX*NY*NZ;
    public static readonly Vector3 Min=new(-2.38f,-.7f,-1.82f);
    public byte[] Data=new byte[Count];
    public int Revision;
    int cachedRevision=-1;
    byte[]? cachedData;
    float mass;
    int minX,minY,minZ,maxX,maxY,maxZ;
    public float Mass { get {
        if(cachedRevision!=Revision || cachedData!=Data){
            mass=0;minX=NX;minY=NY;minZ=NZ;maxX=maxY=maxZ=0;
            for(int i=0;i<Data.Length;i++){
                byte value=Data[i];if(value==0)continue;
                mass+=Math.Clamp((Decode(value)+Step*.5f)/Step,0,1)*Step*Step*Step/UnitVolume;
                int x=i%NX,y=(i/NX)%NY,z=i/(NX*NY);minX=Math.Min(minX,x);minY=Math.Min(minY,y);minZ=Math.Min(minZ,z);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);maxZ=Math.Max(maxZ,z);
            }
            cachedData=Data;cachedRevision=Revision;
        }return mass;
    } }
    public (int x0,int y0,int z0,int x1,int y1,int z1) Bounds(int padding=1)
    { _=Mass;return(Math.Max(1,minX-padding),Math.Max(1,minY-padding),Math.Max(1,minZ-padding),Math.Min(NX-2,maxX+padding),Math.Min(NY-2,maxY+padding),Math.Min(NZ-2,maxZ+padding)); }
    public HairVolume Clone()=>new(){Data=(byte[])Data.Clone(),Revision=Revision};
    public static int Index(int x,int y,int z)=>(z*NY+y)*NX+x;
    public static Vector3 Position(int x,int y,int z)=>Min+new Vector3(x,y,z)*Step;
    public static float Decode(byte b)=>(b-127.5f)*(Band/127.5f);
    public static byte Encode(float d)=>(byte)Math.Clamp((int)MathF.Round(d*127.5f/Band+127.5f),0,255);
    public float At(int x,int y,int z)=>x<0||y<0||z<0||x>=NX||y>=NY||z>=NZ?-Band:Decode(Data[Index(x,y,z)]);
    public float Sample(Vector3 p)
    {
        var q=(p-Min)/Step;int x=(int)MathF.Floor(q.X),y=(int)MathF.Floor(q.Y),z=(int)MathF.Floor(q.Z);
        float tx=q.X-x,ty=q.Y-y,tz=q.Z-z;
        float Lerp(float a,float b,float t)=>a+(b-a)*t;
        return Lerp(Lerp(Lerp(At(x,y,z),At(x+1,y,z),tx),Lerp(At(x,y+1,z),At(x+1,y+1,z),tx),ty),Lerp(Lerp(At(x,y,z+1),At(x+1,y,z+1),tx),Lerp(At(x,y+1,z+1),At(x+1,y+1,z+1),tx),ty),tz);
    }
    public Vector3 Normal(Vector3 p)
    {
        const float e=.05f;var n=new Vector3(Sample(p-new Vector3(e,0,0))-Sample(p+new Vector3(e,0,0)),Sample(p-new Vector3(0,e,0))-Sample(p+new Vector3(0,e,0)),Sample(p-new Vector3(0,0,e))-Sample(p+new Vector3(0,0,e)));
        return n.LengthSquared()>.000001f?Vector3.Normalize(n):Vector3.UnitY;
    }
    public void Fill(Func<Vector3,float> field)
    {
        for(int z=0;z<NZ;z++)for(int y=0;y<NY;y++)for(int x=0;x<NX;x++)
            Data[Index(x,y,z)]=x==0||y==0||z==0||x==NX-1||y==NY-1||z==NZ-1?(byte)0:Encode(field(Position(x,y,z)));
        Revision++;
    }
    public static float Ellipsoid(Vector3 p,Vector3 center,Vector3 radii)=>(1-((p-center)/radii).Length())*Math.Min(radii.X,Math.Min(radii.Y,radii.Z));
    public static HairVolume Create(bool barber=false)
    {
        var v=new HairVolume();float height=barber?.5f:.73f;
        v.Fill(p=>{
            // A swept cap with broad side/back volume and an open face; no independent tufts.
            float hair=Ellipsoid(p,new(-.035f,.22f,-.055f),new(.61f,height,.53f));
            float scalp=Ellipsoid(p,new(0,0,0),new(.405f,.37f,.36f));
            float fringe=.27f+.13f*p.X;
            float face=Math.Min(p.Z-.18f,fringe-p.Y);
            return Math.Min(hair,Math.Min(-scalp,-face));
        });return v;
    }
    public bool Raycast(Vector3 origin,Vector3 direction,float range,out Vector3 point,out float distance)=>SurfaceRaycast(origin,direction,range,out point,out distance);
    public bool LegacyRaycast(Vector3 origin,Vector3 direction,float range,out Vector3 point,out float distance)
    {
        // Cheap bounding slab avoids marching through the whole salon for every head.
        float enter=.02f,exit=range;var max=Position(NX-1,NY-1,NZ-1);
        for(int axis=0;axis<3;axis++)
        {
            float o=axis==0?origin.X:axis==1?origin.Y:origin.Z,d=axis==0?direction.X:axis==1?direction.Y:direction.Z;
            float lo=axis==0?Min.X:axis==1?Min.Y:Min.Z,hi=axis==0?max.X:axis==1?max.Y:max.Z;
            if(Math.Abs(d)<.00001f){if(o<lo||o>hi){point=default;distance=0;return false;}continue;}
            float a=(lo-o)/d,b=(hi-o)/d;if(a>b)(a,b)=(b,a);enter=Math.Max(enter,a);exit=Math.Min(exit,b);
        }
        for(float t=enter;t<=exit;t+=Step*.35f)if(Sample(origin+direction*t)>0)
        {
            float lo=Math.Max(enter,t-Step*.35f),hi=t;for(int i=0;i<6;i++){float mid=(lo+hi)*.5f;if(Sample(origin+direction*mid)>0)hi=mid;else lo=mid;}
            distance=hi;point=origin+direction*hi;return true;
        }
        point=default;distance=0;return false;
    }
    public IEnumerable<Vector3> Samples(int stride=2)
    {for(int z=1;z<NZ-1;z+=stride)for(int y=1;y<NY-1;y+=stride)for(int x=1;x<NX-1;x+=stride)if(At(x,y,z)>0)yield return Position(x,y,z);}

    // Returns real material units added/removed, allowing the transfer cannon to conserve volume.
    public float Brush(HairEffect effect,Vector3 center,float radius,Vector3 rayOrigin=default,float range=0,float budget=float.PositiveInfinity)
    {
        if(!HairSystem.Finite(center)||!HairSystem.Finite(effect.Direction)||!float.IsFinite(effect.Amount)||radius<=0)return 0;
        if(effect.Kind is not (EffectKind.AddHair or EffectKind.Transfer or EffectKind.RemoveHair or EffectKind.Detach or EffectKind.CutPlane or EffectKind.Puncture or EffectKind.ApplyForce))return 0;
        if(effect.Kind==EffectKind.ApplyForce&&effect.Amount<=.000001f)return 0;
        float before=Mass;var original=Data;var next=(byte[])Data.Clone();float spent=0;
        var normal=Normal(center);float amount=Math.Clamp(effect.Amount,0,.4f);
        var bounds=Bounds((int)MathF.Ceiling(amount/Step)+1);
        if(effect.Kind is EffectKind.AddHair or EffectKind.Transfer){
            var lo=(center-new Vector3(radius+Band+.4f)-Min)/Step;var hi=(center+new Vector3(radius+Band+.4f)-Min)/Step;
            bounds=(Math.Max(1,Math.Min(bounds.x0,(int)lo.X)),Math.Max(1,Math.Min(bounds.y0,(int)lo.Y)),Math.Max(1,Math.Min(bounds.z0,(int)lo.Z)),Math.Min(NX-2,Math.Max(bounds.x1,(int)hi.X+1)),Math.Min(NY-2,Math.Max(bounds.y1,(int)hi.Y+1)),Math.Min(NZ-2,Math.Max(bounds.z1,(int)hi.Z+1)));
        }
        for(int z=bounds.z0;z<=bounds.z1;z++)for(int y=bounds.y0;y<=bounds.y1;y++)for(int x=bounds.x0;x<=bounds.x1;x++)
        {
            int i=Index(x,y,z);var pos=Position(x,y,z);float old=Decode(original[i]),d=old;
            float dist=Vector3.Distance(pos,center),weight=Math.Clamp(1-dist/(radius+.18f),0,1);
            switch(effect.Kind)
            {
                case EffectKind.AddHair: case EffectKind.Transfer:
                    // Smooth union with a broad brush attached to the hit surface. Growth builds bulges.
                    float ball=radius-Vector3.Distance(pos,center+normal*(amount-radius*.65f));
                    const float blend=.13f;float h=Math.Clamp(.5f+.5f*(ball-old)/blend,0,1);
                    d=old+(ball-old)*h+blend*h*(1-h);break;
                case EffectKind.RemoveHair: case EffectKind.Detach:
                    // Shave locally, including sideways cuts; never shorten an entire root column.
                    d=Math.Min(old,Vector3.Distance(pos,center-normal*amount*.5f)-Math.Max(radius,amount));break;
                case EffectKind.CutPlane:
                    var planeNormal=effect.Direction.LengthSquared()>.001f?Vector3.Normalize(effect.Direction):Vector3.UnitY;
                    float axial=Vector3.Dot(pos-center,planeNormal);float lateral=(pos-center-planeNormal*axial).Length();
                    d=Math.Min(old,Math.Max(lateral-radius,-axial));break;
                case EffectKind.Puncture:
                    d=Math.Min(old,HairSystem.DistanceToSegment(pos,rayOrigin,rayOrigin+effect.Direction*range)-Math.Max(.15f,radius));break;
                case EffectKind.ApplyForce:
                    if(weight>0)d=Sample(pos-effect.Direction*amount*weight);break;
                default:return 0;
            }
            byte encoded=Encode(d);float delta=(Math.Clamp((Decode(encoded)+Step*.5f)/Step,0,1)-Math.Clamp((old+Step*.5f)/Step,0,1))*Step*Step*Step/UnitVolume;
            if(Math.Abs(delta)+spent>budget)continue;
            spent+=Math.Abs(delta);next[i]=encoded;
        }
        if(original.AsSpan().SequenceEqual(next))return 0;
        if(effect.Kind==EffectKind.ApplyForce)ConserveTransport(original,next);
        Data=next;Revision++;return Mass-before;
    }
    // Redistribute quantized surface occupancy lost by interpolation, within the edited region.
    static void ConserveTransport(byte[] original,byte[] next)
    {
        float Occupancy(byte b)=>Math.Clamp((Decode(b)+Step*.5f)/Step,0,1);
        float error=0;var candidates=new List<int>();
        for(int i=0;i<Count;i++){error+=Occupancy(original[i])-Occupancy(next[i]);if(original[i]!=next[i]&&(Math.Abs(Decode(next[i]))<Step*.55f||Math.Abs(Decode(original[i]))<Step*.55f))candidates.Add(i);}
        for(int pass=0;pass<128&&Math.Abs(error)>.016f;pass++)
        {bool changed=false;foreach(int i in candidates){int direction=error>0?1:-1;int b=Math.Clamp(next[i]+direction,0,255);float delta=Occupancy((byte)b)-Occupancy(next[i]);if(Math.Abs(delta)<.0001f||Math.Abs(delta)>Math.Abs(error)*2)continue;next[i]=(byte)b;error-=delta;changed=true;}if(!changed)break;}
    }
    public void Erode(float depth,Func<Vector3,float> influence)
    {
        bool changed=false;
        for(int z=1;z<NZ-1;z++)for(int y=1;y<NY-1;y++)for(int x=1;x<NX-1;x++)
        {int i=Index(x,y,z);if(Data[i]==0)continue;byte b=Encode(Decode(Data[i])-depth*influence(Position(x,y,z)));if(b!=Data[i]){Data[i]=b;changed=true;}}
        if(changed)Revision++;
    }
}

public sealed class HairShell
{
    public readonly List<Vector3> Vertices=new();
    public readonly List<int> Indices=new();
    public static readonly int[][] Tetrahedra=[[0,1,3,7],[0,3,2,7],[0,2,6,7],[0,6,4,7],[0,4,5,7],[0,5,1,7]];
    // A consistent tetrahedral split resolves thin surfaces and tunnel rims without ambiguous
    // four-way edges. Shared edge vertices form a single closed, faceted boundary mesh.
    public static HairShell Build(HairVolume v)
    {
        var mesh=new HairShell();var edges=new Dictionary<(int,int),int>();
        var values=new float[8];var positions=new Vector3[8];var nodes=new int[8];
        int Vertex(int a,int b)
        {
            var key=(Math.Min(nodes[a],nodes[b]),Math.Max(nodes[a],nodes[b]));
            if(edges.TryGetValue(key,out int index))return index;
            index=mesh.Vertices.Count;edges[key]=index;mesh.Vertices.Add(Vector3.Lerp(positions[a],positions[b],values[a]/(values[a]-values[b])));return index;
        }
        void Triangle(int a,int b,int c){mesh.Indices.Add(a);mesh.Indices.Add(b);mesh.Indices.Add(c);}
        Span<int> inside=stackalloc int[4];Span<int> outside=stackalloc int[4];
        var bounds=v.Bounds();
        for(int z=bounds.z0-1;z<=bounds.z1;z++)for(int y=bounds.y0-1;y<=bounds.y1;y++)for(int x=bounds.x0-1;x<=bounds.x1;x++)
        {
            int positive=0;
            for(int i=0;i<8;i++){int xx=x+(i&1),yy=y+((i>>1)&1),zz=z+((i>>2)&1);values[i]=v.At(xx,yy,zz);positions[i]=HairVolume.Position(xx,yy,zz);nodes[i]=HairVolume.Index(xx,yy,zz);if(values[i]>0)positive++;}
            if(positive==0||positive==8)continue;
            foreach(var tetra in Tetrahedra)
            {
                int ni=0,no=0;foreach(int i in tetra)if(values[i]>0)inside[ni++]=i;else outside[no++]=i;
                if(ni==1)Triangle(Vertex(inside[0],outside[0]),Vertex(inside[0],outside[1]),Vertex(inside[0],outside[2]));
                else if(ni==3)Triangle(Vertex(outside[0],inside[0]),Vertex(outside[0],inside[1]),Vertex(outside[0],inside[2]));
                else if(ni==2){int a=Vertex(inside[0],outside[0]),b=Vertex(inside[0],outside[1]),c=Vertex(inside[1],outside[1]),d=Vertex(inside[1],outside[0]);Triangle(a,b,c);Triangle(a,c,d);}
            }
        }
        return mesh;
    }
}
