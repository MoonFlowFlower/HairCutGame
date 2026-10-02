using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using NV=System.Numerics.Vector3;
namespace Hairball;

// Stable follicle coordinates and instanced shells THROUGH the original hair.
// The editable signed lattice is an occupancy clip, never a distance estimate.
// Cutting updates that texture; it does not create roots on the new surface.
public sealed class PuppetVolumeFur
{
    public readonly MultiMeshInstance3D Node;
    public readonly MeshInstance3D? Fins;
    public int FinTriangles=>Fins?.Mesh is ArrayMesh mesh&&mesh.GetSurfaceCount()>0?mesh.SurfaceGetArrayIndexLen(0)/3:0;
    public IEnumerable<ShaderMaterial> MotionMaterials {
        get { yield return (ShaderMaterial)Node.MaterialOverride;if(Fins!=null)yield return (ShaderMaterial)Fins.MaterialOverride; }
    }
    public readonly ImageTexture3D Density=new();
    public int Count {get;}
    public int Triangles=>(Node.Multimesh.Mesh is ArrayMesh mesh?mesh.SurfaceGetArrayIndexLen(0)/3:12)*Count;
    public int UploadedRevision {get;private set;}
    public int Uploads {get;private set;}
    readonly HairVolume originalVolume;
    readonly PuppetFurSpace space;
    public PuppetFurSpace Space=>space;
    readonly bool surfaceSamples;
    public float OriginalEnd(Vector3 ray)=>TraceExtent(originalVolume,ray,space);
    public bool WasOriginalInterior(Vector3 point)=>SampleExact(originalVolume,point,space)>HairVolume.Band*.5f;
    static ArrayMesh? rootMesh;
    static ArrayMesh? contactRootMesh;
    static Vector2[]? canonicalExtents;
    static Texture2DArray? coveragePattern;
    static ImageTexture? originalExtent;
    static ImageTexture? proxyExtent;
    static ImageTexture3D? originalDensity;
    sealed record SourceResources(ArrayMesh Roots,ImageTexture Extent,ImageTexture Proxy,ImageTexture3D Density);
    static readonly Dictionary<string,SourceResources> mainlineSources=new();
    static ArrayMesh? cartesianCanonical;
    static ImageTexture? cartesianExtent;
    static ImageTexture UnitExtent(){using var image=Image.CreateEmpty(2,2,false,Image.Format.Rf);image.Fill(Colors.White);return ImageTexture.CreateFromImage(image);}
    static readonly Vector3 Center=new(0,-.24f,.015f),Radii=new(.445f,.45f,.322f);
    public PuppetVolumeFur(Node3D parent,HairVolume original,HairVolume current,int count,bool bundles=false,bool tufts=false,bool curveReference=false,ArrayMesh? surface=null,PuppetHairSettings? settings=null,PuppetFurSpace? coordinates=null,bool includeFins=true,HairVolume? finOriginal=null)
    {
        settings??=PuppetHairSettings.Current;
        space=coordinates??PuppetFurSpace.Lab;
        bool lab=space==PuppetFurSpace.Lab;
        string key=lab?"":Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(original.Data))+space.ToString();
        mainlineSources.TryGetValue(key,out var cachedSource);
        Count=count;originalVolume=original;surfaceSamples=settings.SurfaceSamples;
        var canonical=space.Cartesian?(cartesianCanonical??=new ArrayMesh()):lab?(rootMesh??=BuildRoots(original)):cachedSource?.Roots??BuildRoots(original,space);
        var slices=Slices(current);
        if(Density.Create(Image.Format.Rgba8,HairVolume.NX,HairVolume.NY,HairVolume.NZ,false,slices)!=Error.Ok)
            throw new InvalidOperationException("Cannot upload fur occupancy");
        foreach(var image in slices)image.Dispose();
        UploadedRevision=current.Revision;Uploads=1;
        var material=new ShaderMaterial{Shader=GD.Load<Shader>(settings.SoftVolumeSampling?"res://shaders/puppet_volume_fur_soft.gdshader":"res://shaders/puppet_volume_fur.gdshader")};
        material.SetShaderParameter("density_grid",Density);
        material.SetShaderParameter("density_scale",space.DensityScale);
        material.SetShaderParameter("density_offset",space.DensityOffset);
        material.SetShaderParameter("root_packing",space.RootPacking);
        material.SetShaderParameter("cartesian_fur",space.Cartesian);
        material.SetShaderParameter("root_pattern",PuppetShellFur.VolumeRootPattern);
        material.SetShaderParameter("coverage_pattern",coveragePattern??=BuildCoverage());
        material.SetShaderParameter("use_bundles",bundles);
        material.SetShaderParameter("use_tuft_volume",tufts);
        material.SetShaderParameter("curve_reference",curveReference);
        material.SetShaderParameter("curve_ribbons",settings.TuftRibbons);
        material.SetShaderParameter("surface_samples",surfaceSamples);
        material.SetShaderParameter("surface_sample_spacing",.030f/Math.Max(count,1));
        material.SetShaderParameter("clump_depth",settings.ClumpDepth);
        material.SetShaderParameter("volume_undercoat",settings.VolumeUndercoat);
        if(bundles){
            if(tufts){
                material.SetShaderParameter("tuft_volume",PuppetTuftVolume.Field);
                material.SetShaderParameter("tuft_roots",PuppetTuftVolume.Roots);
            }else material.SetShaderParameter("bundle_pattern",PuppetBundleField.Texture);
            var extent=space.Cartesian?(cartesianExtent??=UnitExtent()):lab?(originalExtent??=BuildExtent(original)):cachedSource?.Extent??BuildExtent(original,space);
            var proxy=space.Cartesian?extent:lab?(proxyExtent??=BuildProxyExtent(canonical)):cachedSource?.Proxy??BuildProxyExtent(canonical);
            material.SetShaderParameter("original_extent",extent);
            material.SetShaderParameter("proxy_extent",proxy);
            ImageTexture3D source;
            if(lab&&originalDensity!=null)source=originalDensity;
            else if(cachedSource!=null)source=cachedSource.Density;
            else{
                source=new ImageTexture3D();var initial=Slices(original);
                if(source.Create(Image.Format.Rgba8,HairVolume.NX,HairVolume.NY,HairVolume.NZ,false,initial)!=Error.Ok)throw new InvalidOperationException("Cannot upload original occupancy");
                foreach(var image in initial)image.Dispose();
                if(lab)originalDensity=source;
            }
            material.SetShaderParameter("original_density",source);
            if(!lab&&cachedSource==null){if(mainlineSources.Count>=8){using var keys=mainlineSources.Keys.GetEnumerator();keys.MoveNext();mainlineSources.Remove(keys.Current);}mainlineSources[key]=new(canonical,extent,proxy,source);}
        }
        if(curveReference)Count=count=1;
        var sampleMesh=surfaceSamples?BuildContactRoots(surface??throw new InvalidOperationException("Surface samples need the current extracted surface"),false,space.RootPacking):(lab?(contactRootMesh??=BuildContactRoots(canonical)):BuildContactRoots(canonical));
        var instances=new MultiMesh{TransformFormat=MultiMesh.TransformFormatEnum.Transform3D,UseCustomData=true,Mesh=curveReference?PuppetTuftVolume.Reference(original,settings.TuftRibbons,coordinates):sampleMesh,InstanceCount=count};
        for(int i=0;i<count;i++){
            instances.SetInstanceTransform(i,Transform3D.Identity);
            float t=(i+.5f)/count,spacing=.030f/Math.Max(count,1);
            if(surfaceSamples&&settings.VolumeUndercoat){
                // Spend samples near thin surviving boundaries, retaining the
                // full 30 mm interior. Custom G is the represented interval,
                // so moving samples does not silently thicken their opacity.
                t=MathF.Pow(t,1.45f);
                spacing=.030f*(MathF.Pow((i+1f)/count,1.45f)-MathF.Pow(i/(float)count,1.45f));
            }
            instances.SetInstanceCustomData(i,new Color(t,spacing,0,1));
        }
        Node=new MultiMeshInstance3D{Name="PersistentVolumeShells",Multimesh=instances,MaterialOverride=material,Layers=2,
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,GIMode=GeometryInstance3D.GIModeEnum.Disabled,
            CustomAabb=new Aabb(new(-.85f,-.35f,-.7f),new(1.7f,1.6f,1.4f))};
        if(!lab)Node.CustomAabb=new Aabb(space.ToMaterial(Art.V(HairVolume.Min)),new Vector3(HairVolume.NX,HairVolume.NY,HairVolume.NZ)*HairVolume.Step/space.DensityScale).Grow(.15f);
        parent.AddChild(Node);
        if(settings.TuftHybrid&&includeFins){
            var finMesh=settings.DebrisVolume?PuppetTuftVolume.FragmentReference(original,(surface??throw new InvalidOperationException("Debris requires a current surface")).GetAabb()):PuppetTuftVolume.Reference(finOriginal??original,true,coordinates);
            if(finMesh==null||finMesh.GetSurfaceCount()==0)return;
            var finMaterial=(ShaderMaterial)material.Duplicate();
            finMaterial.SetShaderParameter("curve_reference",true);finMaterial.SetShaderParameter("curve_ribbons",true);
            Fins=new MeshInstance3D{Name="PersistentVolumeCurveFins",Mesh=finMesh,MaterialOverride=finMaterial,Layers=2,
                CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,GIMode=GeometryInstance3D.GIModeEnum.Disabled,CustomAabb=Node.CustomAabb};
            parent.AddChild(Fins);
        }
    }
    public static Vector3 RootRay(float u,float v)
    {
        float a=u*MathF.Tau,b=v*MathF.PI;
        return Radii*new Vector3(MathF.Cos(a)*MathF.Sin(b),MathF.Sin(a)*MathF.Sin(b),MathF.Cos(b));
    }
    public static Vector3 RootCenter=>Center;
    public void ReleaseTextures()=>Density.Dispose();
    public static float ProxyExtent(float u,float v)
    {
        if(rootMesh==null)return 1;
        var values=canonicalExtents??=rootMesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.TexUV2].AsVector2Array();
        float fx=Math.Clamp(u,0,1)*96,fy=Math.Clamp(v,0,1)*48;
        int x=Math.Min((int)fx,95),y=Math.Min((int)fy,47);float a=fx-x,b=fy-y;
        float lo=Mathf.Lerp(values[y*97+x].X,values[y*97+x+1].X,a);
        float hi=Mathf.Lerp(values[(y+1)*97+x].X,values[(y+1)*97+x+1].X,a);
        return Mathf.Lerp(lo,hi,b);
    }
    public static float SampleExact(HairVolume volume,Vector3 point,PuppetFurSpace? coordinates=null)
    {
        var q=((coordinates??PuppetFurSpace.Lab).ToDensity(point)-new Vector3(-2.38f,-.7f,-1.82f))/.14f;
        int x=(int)MathF.Floor(q.X),y=(int)MathF.Floor(q.Y),z=(int)MathF.Floor(q.Z);
        if(x<0||y<0||z<0||x>=34||y>=30||z>=26)return -HairVolume.Band;
        var f=q-new Vector3(x,y,z);int ax,ay,az,bx,by,bz;float hi,mid,lo;
        if(f.X>=f.Y){
            if(f.Y>=f.Z){ax=1;ay=0;az=0;bx=1;by=1;bz=0;hi=f.X;mid=f.Y;lo=f.Z;}
            else if(f.X>=f.Z){ax=1;ay=0;az=0;bx=1;by=0;bz=1;hi=f.X;mid=f.Z;lo=f.Y;}
            else{ax=0;ay=0;az=1;bx=1;by=0;bz=1;hi=f.Z;mid=f.X;lo=f.Y;}
        }else{
            if(f.X>=f.Z){ax=0;ay=1;az=0;bx=1;by=1;bz=0;hi=f.Y;mid=f.X;lo=f.Z;}
            else if(f.Y>=f.Z){ax=0;ay=1;az=0;bx=0;by=1;bz=1;hi=f.Y;mid=f.Z;lo=f.X;}
            else{ax=0;ay=0;az=1;bx=0;by=1;bz=1;hi=f.Z;mid=f.Y;lo=f.X;}
        }
        float a=volume.At(x,y,z),b=volume.At(x+ax,y+ay,z+az),c=volume.At(x+bx,y+by,z+bz),d=volume.At(x+1,y+1,z+1);
        return a+(b-a)*hi+(c-b)*mid+(d-c)*lo;
    }
    public static float TraceExtent(HairVolume volume,Vector3 ray,PuppetFurSpace? coordinates=null)
    {
        float last=0;
        for(int i=0;i<=140;i++){
            float r=1+i*.014f;if(SampleExact(volume,Center+ray*r,coordinates)>0)last=r;
        }
        if(last==0)return 0;
        float lo=last,hi=last+.014f;
        for(int i=0;i<7;i++){float m=(lo+hi)*.5f;if(SampleExact(volume,Center+ray*m,coordinates)>0)lo=m;else hi=m;}
        return (lo+hi)*.5f;
    }
    static ImageTexture BuildExtent(HairVolume original,PuppetFurSpace? coordinates=null)
    {
        const int w=256,h=128;var data=new float[w*h];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)data[y*w+x]=TraceExtent(original,RootRay((x+.5f)/w,(y+.5f)/h),coordinates);
        var bytes=new byte[data.Length*4];Buffer.BlockCopy(data,0,bytes,0,bytes.Length);
        using var image=Image.CreateFromData(w,h,false,Image.Format.Rf,bytes);
        return ImageTexture.CreateFromImage(image);
    }
    public void Update(HairVolume volume,ArrayMesh? surface=null)
    {
        var slices=Slices(volume);Density.Update(slices);foreach(var image in slices)image.Dispose();
        if(surfaceSamples){
            // Move only the sampling proxy. Original tuft/root textures and
            // explicit curve geometry stay fixed in material coordinates.
            var previous=Node.Multimesh.Mesh;
            Node.Multimesh.Mesh=BuildContactRoots(surface??throw new InvalidOperationException("Missing updated surface proxy"),false,space.RootPacking);
            previous.Dispose();
        }
        UploadedRevision=volume.Revision;Uploads++;
    }
    static ImageTexture BuildProxyExtent(ArrayMesh mesh)
    {
        var uv=mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.TexUV2].AsVector2Array();var values=new float[uv.Length];
        for(int i=0;i<values.Length;i++)values[i]=uv[i].X;
        var bytes=new byte[values.Length*4];Buffer.BlockCopy(values,0,bytes,0,bytes.Length);
        using var image=Image.CreateFromData(97,49,false,Image.Format.Rf,bytes);return ImageTexture.CreateFromImage(image);
    }
    static Texture2DArray BuildCoverage()
    {
        // Filter coverage, NOT the Gaussian field before thresholding. The
        // latter erases every follicle once its mip average falls below .60.
        // Eight thresholds retain the taper while all mips preserve area.
        using var roots=PuppetShellFur.VolumeRootPattern.GetImage();
        var source=roots.GetData();int w=roots.GetWidth(),h=roots.GetHeight();
        var images=new Godot.Collections.Array<Image>();
        for(int layer=0;layer<8;layer++){
            float threshold=.60f+.20f*layer/7;var bytes=new byte[w*h];
            for(int i=0;i<bytes.Length;i++){
                float t=Math.Clamp((source[i*4]/255f-threshold+.03f)/.06f,0,1);
                bytes[i]=(byte)MathF.Round(t*t*(3-2*t)*255);
            }
            var image=Image.CreateFromData(w,h,false,Image.Format.R8,bytes);
            image.GenerateMipmaps();images.Add(image);
        }
        var texture=new Texture2DArray();
        if(texture.CreateFromImages(images)!=Error.Ok)throw new InvalidOperationException("Cannot upload prefiltered follicle coverage");
        foreach(var image in images)image.Dispose();
        return texture;
    }
    static Godot.Collections.Array<Image> Slices(HairVolume v)
    {
        var images=new Godot.Collections.Array<Image>();
        for(int z=0;z<HairVolume.NZ;z++){
            var bytes=new byte[HairVolume.NX*HairVolume.NY*4];
            for(int y=0;y<HairVolume.NY;y++)for(int x=0;x<HairVolume.NX;x++){
                var n=new Vector3(v.At(x-1,y,z)-v.At(x+1,y,z),v.At(x,y-1,z)-v.At(x,y+1,z),v.At(x,y,z-1)-v.At(x,y,z+1));
                if(n.LengthSquared()>.000001f)n=n.Normalized();
                int i=(y*HairVolume.NX+x)*4;
                bytes[i]=(byte)((n.X*.5f+.5f)*255);bytes[i+1]=(byte)((n.Y*.5f+.5f)*255);bytes[i+2]=(byte)((n.Z*.5f+.5f)*255);bytes[i+3]=v.Data[HairVolume.Index(x,y,z)];
            }
            images.Add(Image.CreateFromData(HairVolume.NX,HairVolume.NY,false,Image.Format.Rgba8,bytes));
        }
        return images;
    }
    static ArrayMesh BuildRoots(HairVolume original,PuppetFurSpace? coordinates=null)
    {
        const int nx=96,ny=48;var vs=new List<Vector3>();var ns=new List<Vector3>();var uv=new List<Vector2>();var length=new List<Vector2>();var indices=new List<int>();
        for(int y=0;y<=ny;y++)for(int x=0;x<=nx;x++){
            float u=x/(float)nx,v=y/(float)ny,a=u*MathF.Tau,b=v*MathF.PI;
            // Poles are in the face/back, not on the crown under inspection.
            var dir=new Vector3(MathF.Cos(a)*MathF.Sin(b),MathF.Sin(a)*MathF.Sin(b),MathF.Cos(b));
            var radial=Radii*dir;float end=1;
            for(int i=0;i<=140;i++){
                float r=1+i*.014f;var p=(coordinates??PuppetFurSpace.Lab).ToDensity(Center+radial*r);
                if(original.Sample(new NV(p.X,p.Y,p.Z))>0)end=r;
            }
            // Conservative coverage of the extracted tetrahedra. Fragments
            // outside the authoritative mesh are clipped in the shader.
            end+=.075f;
            vs.Add(Center+radial);ns.Add((dir/Radii).Normalized());uv.Add(new(u,v));length.Add(new(end,0));
        }
        // A ray just outside a narrow lock has end=1. Interpolating it with a
        // neighboring occupied ray cuts the proxy INSIDE the extracted lock.
        // Cover each angular cell conservatively, including its neighbors;
        // exact tetrahedral occupancy still defines every visible boundary.
        var sampled=length.ToArray();
        for(int y=0;y<=ny;y++)for(int x=0;x<=nx;x++){
            float end=sampled[y*(nx+1)+x].X;
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){
                int xx=(x+dx+nx)%nx,yy=Math.Clamp(y+dy,0,ny);
                end=Math.Max(end,sampled[yy*(nx+1)+xx].X);
            }
            length[y*(nx+1)+x]=new(end,0);
        }
        for(int y=0;y<ny;y++)for(int x=0;x<nx;x++){
            int a=y*(nx+1)+x,b=a+1,c=a+nx+1,d=c+1;
            indices.AddRange(new[]{a,c,b,b,c,d});
        }
        var array=new Godot.Collections.Array();array.Resize((int)Mesh.ArrayType.Max);
        array[(int)Mesh.ArrayType.Vertex]=vs.ToArray();array[(int)Mesh.ArrayType.Normal]=ns.ToArray();
        array[(int)Mesh.ArrayType.TexUV]=uv.ToArray();array[(int)Mesh.ArrayType.TexUV2]=length.ToArray();array[(int)Mesh.ArrayType.Index]=indices.ToArray();
        var mesh=new ArrayMesh();mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,array);return mesh;
    }
    static ArrayMesh BuildContactRoots(ArrayMesh canonical,bool scalpGrid=true,float rootPacking=1)
    {
        // Preserve the canonical 97x49 extent grid. Only the render copy is
        // deindexed so all three vertices receive the SAME contact anchor.
        // Independent radial projections make a chord through a sphere.
        var source=canonical.SurfaceGetArrays(0);
        var positions=source[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        var normals=source[(int)Mesh.ArrayType.Normal].AsVector3Array();
        var uv=scalpGrid?source[(int)Mesh.ArrayType.TexUV].AsVector2Array():new Vector2[positions.Length];
        var extents=source[(int)Mesh.ArrayType.TexUV2].AsVector2Array();
        var indices=source[(int)Mesh.ArrayType.Index].AsInt32Array();
        var vs=new Vector3[indices.Length];var ns=new Vector3[indices.Length];
        var tex=new Vector2[indices.Length];var lengths=new Vector2[indices.Length];
        var colors=new Color[indices.Length];var ix=new int[indices.Length];
        for(int i=0;i<indices.Length;i+=3){
            int a=indices[i],b=indices[i+1],c=indices[i+2];
            var anchor=(positions[a]+positions[b]+positions[c])/3-(scalpGrid?Center:Vector3.Zero);
            float end=scalpGrid?(extents[a].X+extents[b].X+extents[c].X)/3:4;
            anchor/=rootPacking;
            var color=new Color(anchor.X*.5f+.5f,anchor.Y*.5f+.5f,anchor.Z*.5f+.5f,end/4);
            for(int j=0;j<3;j++){
                int n=i+j,k=indices[n];vs[n]=positions[k];ns[n]=normals[k];tex[n]=uv[k];lengths[n]=extents[k];colors[n]=color;ix[n]=n;
            }
        }
        var arrays=new Godot.Collections.Array();arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex]=vs;arrays[(int)Mesh.ArrayType.Normal]=ns;
        arrays[(int)Mesh.ArrayType.TexUV]=tex;arrays[(int)Mesh.ArrayType.TexUV2]=lengths;
        arrays[(int)Mesh.ArrayType.Color]=colors;arrays[(int)Mesh.ArrayType.Index]=ix;
        var mesh=new ArrayMesh();mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);return mesh;
    }
}
