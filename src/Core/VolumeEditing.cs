using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Hairball.Core;

public sealed class VolumeEditResult
{
    public float Added, Removed, Absorbed;
    public float VisibleRemoved=>Math.Max(0,Removed-Absorbed);
    public readonly List<HairVolume> Detached=new();
}

public sealed partial class HairVolume
{
    public float GrowAlong(ref float[]? field,Vector3 center,Vector3 normal,float radius,float distance,float flatness=0)
    {
        float before=Mass;field??=Array.ConvertAll(Data,Decode);var source=(float[])field.Clone();bool changed=false;
        float SampleField(Vector3 p)
        {
            var q=(p-Min)/Step;int x=(int)MathF.Floor(q.X),y=(int)MathF.Floor(q.Y),z=(int)MathF.Floor(q.Z);float sum=0;
            for(int dz=0;dz<2;dz++)for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++)
            {int xx=x+dx,yy=y+dy,zz=z+dz;float value=xx<0||yy<0||zz<0||xx>=NX||yy>=NY||zz>=NZ?-Band:source[Index(xx,yy,zz)];sum+=value*(dx==0?1-(q.X-x):q.X-x)*(dy==0?1-(q.Y-y):q.Y-y)*(dz==0?1-(q.Z-z):q.Z-z);}return sum;
        }
        var lo=(center-new Vector3(radius+Band)-Min)/Step;var hi=(center+new Vector3(radius+Band)-Min)/Step;
        for(int z=Math.Max(1,(int)lo.Z);z<=Math.Min(NZ-2,(int)hi.Z+1);z++)for(int y=Math.Max(1,(int)lo.Y);y<=Math.Min(NY-2,(int)hi.Y+1);y++)for(int x=Math.Max(1,(int)lo.X);x<=Math.Min(NX-2,(int)hi.X+1);x++)
        {
            int i=Index(x,y,z);var pos=Position(x,y,z);var delta=pos-center;float axial=Vector3.Dot(delta,normal);float lateral=(delta-normal*axial).Length();
            // A broad spray can advance a flat inner disk, retaining useful thickness as
            // its hit point follows the growing front; precision growth keeps the old falloff.
            float w=Math.Clamp((1-lateral/radius)/(1-Math.Clamp(flatness,0,.85f)),0,1);w=w*w*(3-2*w);if(w<=0||Math.Abs(axial)>Band+distance)continue;
            field[i]=Math.Max(source[i],SampleField(pos-normal*(distance*w)));byte b=Encode(field[i]);if(b!=Data[i]){Data[i]=b;changed=true;}
        }
        if(changed)Revision++;return Mass-before;
    }
    public bool SurfaceRaycast(Vector3 origin,Vector3 direction,float range,out Vector3 point,out float distance)
    {
        point=default;distance=0;
        if(!HairSystem.Finite(origin)||!HairSystem.Finite(direction)||range<=0)return false;
        float enter=0,exit=range;var bounds=Bounds();
        var low=Position(bounds.x0-1,bounds.y0-1,bounds.z0-1);var high=Position(bounds.x1+1,bounds.y1+1,bounds.z1+1);
        for(int axis=0;axis<3;axis++)
        {
            float o=origin[axis],d=direction[axis];
            if(Math.Abs(d)<1e-8f){if(o<low[axis]||o>high[axis])return false;continue;}
            float a=(low[axis]-o)/d,b=(high[axis]-o)/d;enter=Math.Max(enter,Math.Min(a,b));exit=Math.Min(exit,Math.Max(a,b));
        }
        if(enter>exit)return false;
        var values=new float[8];var positions=new Vector3[8];
        Span<int> inside=stackalloc int[4];Span<int> outside=stackalloc int[4];
        Vector3 Vertex(int a,int b)=>Vector3.Lerp(positions[a],positions[b],values[a]/(values[a]-values[b]));
        float best=range+1;
        void Triangle(Vector3 a,Vector3 b,Vector3 c)
        {
            var e1=b-a;var e2=c-a;var p=Vector3.Cross(direction,e2);float det=Vector3.Dot(e1,p);
            if(Math.Abs(det)<1e-10f)return;float inv=1/det;var s=origin-a;float u=Vector3.Dot(s,p)*inv;
            if(u<-.00001f||u>1.00001f)return;var q=Vector3.Cross(s,e1);float v=Vector3.Dot(direction,q)*inv;
            if(v<-.00001f||u+v>1.00001f)return;float t=Vector3.Dot(e2,q)*inv;if(t>=0&&t<best)best=t;
        }
        // DDA visits every intersected cell, including thin shells between sample points.
        for(float t=enter;t<=exit;)
        {
            var grid=(origin+direction*(t+.00001f)-Min)/Step;
            int x=Math.Clamp((int)MathF.Floor(grid.X),0,NX-2),y=Math.Clamp((int)MathF.Floor(grid.Y),0,NY-2),z=Math.Clamp((int)MathF.Floor(grid.Z),0,NZ-2);
            int positive=0;
            for(int i=0;i<8;i++){int xx=x+(i&1),yy=y+((i>>1)&1),zz=z+((i>>2)&1);values[i]=At(xx,yy,zz);positions[i]=Position(xx,yy,zz);if(values[i]>0)positive++;}
            if(positive>0&&positive<8)foreach(var tetra in HairShell.Tetrahedra)
            {
                int ni=0,no=0;foreach(int i in tetra)if(values[i]>0)inside[ni++]=i;else outside[no++]=i;
                if(ni==1)Triangle(Vertex(inside[0],outside[0]),Vertex(inside[0],outside[1]),Vertex(inside[0],outside[2]));
                else if(ni==3)Triangle(Vertex(outside[0],inside[0]),Vertex(outside[0],inside[1]),Vertex(outside[0],inside[2]));
                else if(ni==2){var a=Vertex(inside[0],outside[0]);var b=Vertex(inside[0],outside[1]);var c=Vertex(inside[1],outside[1]);var d=Vertex(inside[1],outside[0]);Triangle(a,b,c);Triangle(a,c,d);}
            }
            if(best<=exit){distance=best;point=origin+direction*best;return true;}
            float next=float.PositiveInfinity;
            for(int axis=0;axis<3;axis++)if(Math.Abs(direction[axis])>1e-8f)
            {int cell=axis==0?x:axis==1?y:z;float boundary=Min[axis]+(cell+(direction[axis]>0?1:0))*Step;float crossing=(boundary-origin[axis])/direction[axis];if(crossing>t+.000001f)next=Math.Min(next,crossing);}
            if(!float.IsFinite(next))break;t=Math.Max(t+.00002f,next);
        }
        return false;
    }

    // Absolute stroke field: repeated evaluation does not compound a locked cut or
    // accumulate byte rounding. Growth is derived from the stroke baseline and elapsed time.
    public float Sculpt(HairVolume baseline,Vector3 center,Vector3 normal,float radius,float depth,bool growth)
    {
        float before=Mass;bool changed=false;
        var lo=(center-new Vector3(radius+Band+depth)-Min)/Step;var hi=(center+new Vector3(radius+Band+depth)-Min)/Step;
        for(int z=Math.Max(1,(int)lo.Z);z<=Math.Min(NZ-2,(int)hi.Z+1);z++)
        for(int y=Math.Max(1,(int)lo.Y);y<=Math.Min(NY-2,(int)hi.Y+1);y++)
        for(int x=Math.Max(1,(int)lo.X);x<=Math.Min(NX-2,(int)hi.X+1);x++)
        {
            int i=Index(x,y,z);var p=Position(x,y,z);var delta=p-center;float axial=Vector3.Dot(delta,normal);
            float lateral=(delta-normal*axial).Length();float old=Decode(Data[i]),value=old;
            if(growth)
            {
                float w=Math.Clamp(1-lateral/radius,0,1);w=w*w*(3-2*w);
                if(w>0&&axial> -Band)value=Math.Max(old,baseline.Sample(p-normal*(depth*w)));
            }
            else value=Math.Min(old,Math.Max(lateral-radius,-depth-axial));
            byte encoded=Encode(value);if(encoded!=Data[i]){Data[i]=encoded;changed=true;}
        }
        if(changed)Revision++;return Mass-before;
    }

    static readonly (int X,int Y,int Z)[] ConnectedOffsets=
    [(1,0,0),(0,1,0),(0,0,1),(1,1,0),(1,0,1),(0,1,1),(1,1,1),(-1,0,0),(0,-1,0),(0,0,-1),(-1,-1,0),(-1,0,-1),(0,-1,-1),(-1,-1,-1)];
    static readonly (int X,int Y,int Z)[] BandOffsets=[(1,0,0),(0,1,0),(0,0,1),(-1,0,0),(0,-1,0),(0,0,-1)];

    public List<HairVolume> DetachUnsupported(Func<Vector3,bool> support,bool loose)
    {
        var labels=new int[Count];var queue=new Queue<int>();var supported=new List<bool>{false};var sizes=new List<int>{0};int label=0;
        for(int start=0;start<Count;start++)if(Data[start]>=128&&labels[start]==0)
        {
            label++;supported.Add(false);sizes.Add(0);labels[start]=label;queue.Enqueue(start);
            while(queue.TryDequeue(out int i))
            {
                int x=i%NX,y=i/NX%NY,z=i/(NX*NY);sizes[label]++;supported[label]|=support(Position(x,y,z));
                foreach(var d in ConnectedOffsets)
                {
                    int xx=x+d.X,yy=y+d.Y,zz=z+d.Z;if(xx<0||yy<0||zz<0||xx>=NX||yy>=NY||zz>=NZ)continue;
                    int n=Index(xx,yy,zz);if(Data[n]>=128&&labels[n]==0){labels[n]=label;queue.Enqueue(n);}
                }
            }
        }
        if(loose&&label>0)supported[Enumerable.Range(1,label).MaxBy(i=>sizes[i])]=true;
        if(label>0&&supported.Skip(1).All(v=>v))return new();
        // Partition the narrow negative band as well, so no material units are duplicated.
        for(int i=0;i<Count;i++)if(labels[i]>0)queue.Enqueue(i);
        while(queue.TryDequeue(out int i))
        {
            int x=i%NX,y=i/NX%NY,z=i/(NX*NY);
            foreach(var d in BandOffsets)
            {int xx=x+d.X,yy=y+d.Y,zz=z+d.Z;if(xx<0||yy<0||zz<0||xx>=NX||yy>=NY||zz>=NZ)continue;int n=Index(xx,yy,zz);if(Data[n]>0&&labels[n]==0){labels[n]=labels[i];queue.Enqueue(n);}}
        }
        var pieces=new Dictionary<int,HairVolume>();var retained=new byte[Count];bool changed=false;
        for(int i=0;i<Count;i++)
        {
            int owner=labels[i];if(owner>0&&supported[owner])retained[i]=Data[i];
            else if(Data[i]>0){if(!pieces.TryGetValue(owner,out var piece)){piece=new();pieces[owner]=piece;}piece.Data[i]=Data[i];changed=true;}
        }
        if(changed){Data=retained;Revision++;}
        return pieces.Values.Where(v=>v.Mass>0).ToList();
    }
}
