using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hairball;

// Production adapter: the Session owns every voxel. Only render resources and
// spring guides live here; cuts never create a new surface coat or alter hits.
public sealed class MainlineFur : IDisposable
{
    public readonly Node3D Root;
    public PuppetVolumeFur? Renderer {get;private set;}
    public int CutImpulses {get;private set;}
    public float PeakMeters {get;private set;}
    public float OffsetMeters {get;private set;}
    public int Uploads {get;private set;}
    public double LastUpdateMs {get;private set;}
    public int SurfaceTriangles {get;private set;}
    const int Columns=24,Rows=12,Width=Columns+1,Height=Rows+1;
    readonly PuppetFurSpace space;
    readonly bool detailed;
    readonly HairVolume source=new();
    byte[] prior=[];
    int stateHash=int.MinValue;
    readonly ImageTexture3D states=new();
    bool statesReady;
    readonly ImageTexture motion=new();
    readonly Image motionImage;
    readonly Vector3[] offsets=new Vector3[Width*Height],velocities=new Vector3[Width*Height],tips=new Vector3[Width*Height];
    readonly float[] weights=new float[Width*Height],pixels=new float[Width*Height*4];
    readonly byte[] motionBytes=new byte[Width*Height*16];
    float contactTime=-100;
    Head current=null!;
    readonly float[] originalEnds=new float[Width*Height];
    public MainlineFur(Node3D parent,bool cartesian=false,bool detailed=true)
    {
        space=cartesian?new(1,Vector3.Zero,4,true):PuppetFurSpace.Mainline;this.detailed=detailed;
        Root=new(){Name="MainlinePuppetFur",Position=space.DensityOffset};parent.AddChild(Root);
        Array.Fill(originalEnds,-1);
        motionImage=Image.CreateFromData(Width,Height,false,Image.Format.Rgbaf,motionBytes);motion.SetImage(motionImage);
    }
    public void Sync(Head h,int hash,uint layer)
    {
        current=h;
        if(hash!=stateHash){UpdateStates(h);stateHash=hash;}
        if(!h.Volume.Data.AsSpan().SequenceEqual(prior)){
            var watch=System.Diagnostics.Stopwatch.StartNew();bool first=prior.Length==0;float removed=0;Vector3 point=Vector3.Zero;
            if(first){var canonical=!h.Loose&&!h.Facial?HairVolume.Create(h.Barber):h.Volume;Array.Copy(canonical.Data,source.Data,source.Data.Length);}
            for(int i=0;i<source.Data.Length;i++){
                byte value=h.Volume.Data[i];
                if(!first&&prior[i]>=128&&value<prior[i]){float amount=prior[i]-value;removed+=amount;point+=Art.V(HairVolume.Position(i%HairVolume.NX,i/HairVolume.NX%HairVolume.NY,i/(HairVolume.NX*HairVolume.NY)))*amount;}
            }
            source.Revision++;
            using var proxy=BuildProxy(h.Volume);SurfaceTriangles=proxy.GetSurfaceCount()>0?proxy.SurfaceGetArrayIndexLen(0)/3:0;
            if(SurfaceTriangles==0){ClearRenderer();Array.Clear(offsets);Array.Clear(velocities);Array.Clear(weights);}
            else {
                if(Renderer==null){Renderer=new(Root,source,h.Volume,detailed?8:4,true,true,false,proxy,PuppetHairSettings.Current,space,detailed&&!space.Cartesian,!h.Loose&&!h.Facial?HairVolume.Create(h.Barber):null);Bind();}
                else Renderer.Update(h.Volume,proxy);
                RefreshGuides(h.Volume);
                if(removed>0){CutImpulses++;Kick(space.ToMaterial(point/removed),Vector3.Up*.5f-space.ToMaterial(point/removed).DirectionTo(PuppetVolumeFur.RootCenter),.7f,1.7f);}
            }
            prior=(byte[])h.Volume.Data.Clone();Uploads++;LastUpdateMs=watch.Elapsed.TotalMilliseconds;
        }
        if(Renderer!=null){Renderer.Node.Layers=layer;if(Renderer.Fins!=null)Renderer.Fins.Layers=layer;}
    }
    void Bind()
    {
        foreach(var material in Renderer!.MotionMaterials){
            material.SetShaderParameter("production_states",true);material.SetShaderParameter("material_states",states);
            material.SetShaderParameter("motion_field",motion);material.SetShaderParameter("motion_grid",new Vector2(Columns,Rows));
            material.SetShaderParameter("motion_enabled",true);
            Color color=current.Material==HeadMaterialKind.Wool?new("d6c5a8"):new(.44f,.32f,.53f);
            material.SetShaderParameter("fur_color",color);
        }
    }
    ArrayMesh BuildProxy(HairVolume volume)
    {
        var shell=HairShell.Build(volume);var mesh=new ArrayMesh();if(shell.Indices.Count==0)return mesh;
        var vs=shell.Vertices.Select(v=>space.ToMaterial(Art.V(v))).ToArray();var ns=shell.Vertices.Select(v=>Art.V(volume.Normal(v))).ToArray();var ix=shell.Indices.ToArray();
        for(int i=0;i<ix.Length;i+=3)if((vs[ix[i+1]]-vs[ix[i]]).Cross(vs[ix[i+2]]-vs[ix[i]]).Dot(ns[ix[i]]+ns[ix[i+1]]+ns[ix[i+2]])>0)(ix[i+1],ix[i+2])=(ix[i+2],ix[i+1]);
        var a=new Godot.Collections.Array();a.Resize((int)Mesh.ArrayType.Max);a[(int)Mesh.ArrayType.Vertex]=vs;a[(int)Mesh.ArrayType.Normal]=ns;a[(int)Mesh.ArrayType.TexUV2]=new Vector2[vs.Length];a[(int)Mesh.ArrayType.Index]=ix;
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,a);return mesh;
    }
    void UpdateStates(Head h)
    {
        const int size=16;var slices=new Godot.Collections.Array<Image>();
        for(int z=0;z<size;z++){
            var data=new byte[size*size*4];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){
                var point=HairVolume.Min+new System.Numerics.Vector3((x+.5f)/size*4.76f,(y+.5f)/size*4.2f,(z+.5f)/size*3.64f);
                var patch=HairSystem.MaterialAt(h,point);int i=(y*size+x)*4;
                data[i]=(byte)(Math.Clamp(patch.Wet,0,1)*255);data[i+1]=patch.Frozen?(byte)255:(byte)0;data[i+2]=(byte)(Math.Clamp(patch.Char,0,1)*255);data[i+3]=(byte)(Math.Clamp(patch.Glue,0,1)*255);
            }
            slices.Add(Image.CreateFromData(size,size,false,Image.Format.Rgba8,data));
        }
        if(!statesReady){if(states.Create(Image.Format.Rgba8,size,size,size,false,slices)!=Error.Ok)throw new InvalidOperationException("Cannot upload B hair states");statesReady=true;}else states.Update(slices);
        foreach(var image in slices)image.Dispose();if(Renderer!=null)Bind();
    }
    void RefreshGuides(HairVolume volume)
    {
        if(space.Cartesian)return;
        for(int y=0;y<Height;y++)for(int x=0;x<Width;x++){
            int i=y*Width+x;var ray=PuppetVolumeFur.RootRay(x/(float)Columns,y/(float)Rows);float end=PuppetVolumeFur.TraceExtent(volume,ray,space);
            float original=originalEnds[i]<0?(originalEnds[i]=Renderer!.OriginalEnd(ray)):originalEnds[i];weights[i]=end>0?Math.Clamp((end-.98f)/Math.Max(.02f,original-.98f),.08f,1):0;
            tips[i]=PuppetVolumeFur.RootCenter+ray*end;
            if(weights[i]==0)offsets[i]=velocities[i]=Vector3.Zero;
        }
    }
    void Kick(Vector3 point,Vector3 direction,float radius,float strength)
    {
        if(!VisualQuality.Dynamics)return;direction=direction.Normalized();
        for(int i=0;i<tips.Length;i++)if(weights[i]>0){float d=tips[i].DistanceTo(point);float local=MathF.Exp(-d*d/(radius*radius));velocities[i]=(velocities[i]+direction*(strength*local)).LimitLength(2);}
    }
    public void Contact(Vector3 worldPoint,float time,float weight)
    {
        if(time<=contactTime+.001f||weight<.5f||Renderer==null)return;contactTime=time;
        var p=Root.ToLocal(worldPoint);Kick(p,(PuppetVolumeFur.RootCenter-p).Normalized()+Vector3.Up*.3f,.45f,.8f*weight);
    }
    public void Impulse(Vector3 worldVelocity)
    {
        if(Renderer==null||!VisualQuality.Dynamics)return;var v=Root.GlobalBasis.Inverse()*worldVelocity;
        for(int i=0;i<velocities.Length;i++)if(weights[i]>0)velocities[i]=(velocities[i]+v).LimitLength(2);
    }
    public void Step(float delta)
    {
        if(Renderer==null)return;float dt=Math.Clamp(delta,0,.05f),wet=0,stiff=0;foreach(var p in current.Patches){wet+=p.Wet;stiff+=Math.Max(p.Frozen?1:0,p.Glue);}wet/=current.Patches.Count;stiff/=current.Patches.Count;
        float w=12+stiff*12,z=.22f+stiff*.5f+wet*.10f,wd=w*MathF.Sqrt(1-z*z),e=MathF.Exp(-z*w*dt),c=MathF.Cos(wd*dt),s=MathF.Sin(wd*dt)/wd;
        OffsetMeters=0;
        for(int i=0;i<offsets.Length;i++){
            var p=offsets[i];var v=velocities[i];offsets[i]=e*(p*c+(v+p*(z*w))*s);velocities[i]=e*(v*c-(v*(z*w)+p*w*w)*s);
            float limit=.072f/Math.Max(.05f,current.GeometryScale)/(1+stiff*3);
            if(offsets[i].Length()>limit){offsets[i]=offsets[i].LimitLength(limit);float outward=velocities[i].Dot(offsets[i].Normalized());if(outward>0)velocities[i]-=offsets[i].Normalized()*outward;}
            if(!VisualQuality.Dynamics||weights[i]==0)offsets[i]=velocities[i]=Vector3.Zero;
            if(i%Width==Columns){offsets[i]=offsets[i-Columns];velocities[i]=velocities[i-Columns];}
            pixels[i*4]=offsets[i].X;pixels[i*4+1]=offsets[i].Y;pixels[i*4+2]=offsets[i].Z;pixels[i*4+3]=weights[i];OffsetMeters=Math.Max(OffsetMeters,offsets[i].Length()*current.GeometryScale);
        }
        PeakMeters=Math.Max(PeakMeters,OffsetMeters);Buffer.BlockCopy(pixels,0,motionBytes,0,motionBytes.Length);motionImage.SetData(Width,Height,false,Image.Format.Rgbaf,motionBytes);motion.Update(motionImage);
    }
    void ClearRenderer(){if(Renderer==null)return;if(Root.IsInsideTree()){if(GodotObject.IsInstanceValid(Renderer.Node))Renderer.Node.Free();if(Renderer.Fins!=null&&GodotObject.IsInstanceValid(Renderer.Fins))Renderer.Fins.Free();}Renderer.ReleaseTextures();Renderer=null;}
    public void Dispose(){ClearRenderer();states.Dispose();motion.Dispose();motionImage.Dispose();}
}
