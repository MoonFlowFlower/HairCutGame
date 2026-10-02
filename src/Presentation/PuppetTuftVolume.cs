using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Hairball.Core;

namespace Hairball;

// A periodic head-local volume of finite curved fibres. This is a distinct
// topology experiment, not the scalp-root field and not a cut-surface coat.
// Geometry and root offsets are generated once with a fixed seed. A cut only
// clips them using the original/current authoritative occupancy textures.
public static class PuppetTuftVolume
{
    public static int Size=>PuppetHairSettings.Current.TuftHybrid?128:256;
    public static float FinSimplificationBound {get;private set;}
    public static float FinMeanSegments {get;private set;}
    public const float Period=.24f,RootRange=.10f;
    static ImageTexture3D? field,roots;
    readonly record struct Strand(Vector3 Root,Vector3[] Points,Vector3 Side,float Radius);
    static readonly List<Strand> strands=new();
    static ArrayMesh? reference,ribbonReference;
    static readonly Dictionary<string,ArrayMesh> mainlineReferences=new();
    public static bool BakeMainlineCache;
    readonly record struct FinSpan(int Start,int Count,Aabb Bounds);
    sealed record FinCpu(Vector3[] Vertices,Vector3[] Normals,Color[] Colors,Vector2[] Uvs,Vector2[] Widths,FinSpan[] Spans);
    static FinCpu? finCpu;
    static readonly Dictionary<(int,int,int),List<int>> finCells=new();
    public const int FragmentFinTriangleLimit=60000;
    const float FinCell=.08f;
    public static ImageTexture3D Field {get{Build();return field!;}}
    public static ImageTexture3D Roots {get{Build();return roots!;}}
    static Vector3 ToField(Vector3 p)
    {
        var delta=p-PuppetVolumeFur.RootCenter;var q=delta/new Vector3(.445f,.45f,.322f);float r=q.Length();
        float u=Mathf.PosMod(MathF.Atan2(q.Y,q.X)/MathF.Tau,1),v=MathF.Acos(Math.Clamp(q.Z/r,-1,1))/MathF.PI;
        return new(u*Period*16,v*Period*8,(r-.98f)*delta.Length()/r);
    }
    static Vector3 FromField(Vector3 p)
    {
        var ray=PuppetVolumeFur.RootRay(p.X/(Period*16),p.Y/(Period*8));
        return PuppetVolumeFur.RootCenter+ray*(.98f+p.Z/ray.Length());
    }
    static byte Encode(float v)=>(byte)Math.Clamp((int)MathF.Round(v*255),0,255);
    static Vector3[] GroupRoots(int count)
    {
        // Periodic best-candidate placement reduces accidental large voids
        // without introducing a Cartesian lattice or cut-dependent roots.
        var random=new Random(719431);var points=new Vector3[count];
        for(int i=0;i<count;i++){
            float best=-1;Vector3 selected=default;
            for(int attempt=0;attempt<32;attempt++){
                var p=new Vector3((float)random.NextDouble(),(float)random.NextDouble(),(float)random.NextDouble());
                float nearest=float.PositiveInfinity;
                for(int j=0;j<i;j++){
                    var d=(p-points[j]).Abs();d=d.Min(Vector3.One-d);
                    nearest=Math.Min(nearest,d.LengthSquared());
                    if(nearest<=best)break;
                }
                if(nearest>best){best=nearest;selected=p;}
            }
            points[i]=selected;
        }
        for(int i=0;i<count;i++)points[i]*=Period;
        return points;
    }
    static void Build(bool forceCpu=false)
    {
        if(field!=null)return;
        if(!forceCpu&&VisualQuality.Puppet&&Godot.FileAccess.FileExists("res://assets/b_mainline_fur/v1_field.res")&&Godot.FileAccess.FileExists("res://assets/b_mainline_fur/v1_roots.res")){
            field=GD.Load<ImageTexture3D>("res://assets/b_mainline_fur/v1_field.res");roots=GD.Load<ImageTexture3D>("res://assets/b_mainline_fur/v1_roots.res");return;
        }
        float step=Period/Size;
        var data=new byte[Size*Size*Size*4];var rootData=new byte[data.Length];
        for(int i=0;i<data.Length;i+=4){data[i]=data[i+1]=data[i+2]=128;rootData[i]=rootData[i+1]=rootData[i+2]=128;}
        var random=new Random(943712);float R()=>(float)random.NextDouble();
        var groupRoots=GroupRoots(1280);
        bool soft=PuppetHairSettings.Current.SoftVolumeSampling;
        Vector3 Direction(){float z=soft?.35f+R()*.65f:.72f+R()*.28f,a=R()*MathF.Tau,s=MathF.Sqrt(1-z*z);return new(s*MathF.Cos(a),s*MathF.Sin(a),z);}
        for(int group=0;group<groupRoots.Length;group++){
            // Consume the original root draws to keep all subsequent shape
            // parameters paired with the previous random-placement trial.
            _=new Vector3(R(),R(),R());var root=groupRoots[group];var axis=Direction();
            var u=axis.Cross(Math.Abs(axis.Y)<.9f?Vector3.Up:Vector3.Right).Normalized();var v=axis.Cross(u);
            float length=soft?.016f+R()*.022f:.018f+R()*.028f,phase=R()*MathF.Tau,bend=(R()-.5f)*.030f,turns=.30f+R()*.65f;
            for(int strand=0;strand<11;strand++){
                float angle=R()*MathF.Tau,spread=MathF.Sqrt(R())*.0035f;
                var side=u*MathF.Cos(angle)+v*MathF.Sin(angle);
                var origin=root+side*spread;
                float len=length*(.65f+R()*.65f),radius=.0009f+R()*.0005f,fan=(R()-.2f)*.008f;
                float strandPhase=phase+(R()-.5f)*1.0f,strandTurns=turns*(.75f+R()*.55f);
                Vector3 Point(float t)=>origin+axis*(len*t)+u*(bend*t*t)+side*(fan*t*t)
                    +(u*(MathF.Cos(t*MathF.Tau*strandTurns+strandPhase)-MathF.Cos(strandPhase))+v*(MathF.Sin(t*MathF.Tau*strandTurns+strandPhase)-MathF.Sin(strandPhase)))*(.006f*t);
                const int segments=7;
                var points=new Vector3[segments+1];for(int s=0;s<=segments;s++)points[s]=Point(s/(float)segments);
                strands.Add(new(origin,points,u,radius));
                for(int segment=0;segment<segments;segment++){
                    float t0=segment/(float)segments,t1=(segment+1)/(float)segments;
                    var a=Point(t0);var b=Point(t1);var ab=b-a;float den=ab.LengthSquared();
                    float reach=radius+step*.65f;
                    var low=(a.Min(b)-Vector3.One*reach)/step;var high=(a.Max(b)+Vector3.One*reach)/step;
                    for(int z=(int)MathF.Floor(low.Z);z<=(int)MathF.Ceiling(high.Z);z++)
                    for(int y=(int)MathF.Floor(low.Y);y<=(int)MathF.Ceiling(high.Y);y++)
                    for(int x=(int)MathF.Floor(low.X);x<=(int)MathF.Ceiling(high.X);x++){
                        var p=new Vector3(x+.5f,y+.5f,z+.5f)*step;
                        float along=Math.Clamp((p-a).Dot(ab)/den,0,1),t=Mathf.Lerp(t0,t1,along);
                        var radial=p-(a+ab*along);float distance=radial.Length();
                        float r=radius*(1-.88f*t*t);
                        float coverage=Math.Clamp((r+step*.5f-distance)/step,0,1);
                        if(coverage<=0)continue;
                        int xx=(x%Size+Size)%Size,yy=(y%Size+Size)%Size,zz=(z%Size+Size)%Size,i=((zz*Size+yy)*Size+xx)*4;
                        byte alpha=Encode(coverage);if(alpha<=data[i+3])continue;
                        var normal=distance>.000001f?radial/distance:u;
                        data[i]=Encode(normal.X*.5f+.5f);data[i+1]=Encode(normal.Y*.5f+.5f);data[i+2]=Encode(normal.Z*.5f+.5f);data[i+3]=alpha;
                        var rootDelta=(origin-p)/RootRange;
                        // Store premultiplied root delta so mip filtering does
                        // not drag the support towards empty texels.
                        rootData[i]=Encode(rootDelta.X*.5f*coverage+.5f);
                        rootData[i+1]=Encode(rootDelta.Y*.5f*coverage+.5f);
                        rootData[i+2]=Encode(rootDelta.Z*.5f*coverage+.5f);
                        rootData[i+3]=Encode(t*coverage);
                    }
                }
            }
        }
        field=Upload(data);roots=Upload(rootData);
        if(BakeMainlineCache){System.IO.Directory.CreateDirectory(ProjectSettings.GlobalizePath("res://assets/b_mainline_fur"));ResourceSaver.Save(field,"res://assets/b_mainline_fur/v1_field.res",ResourceSaver.SaverFlags.Compress);ResourceSaver.Save(roots,"res://assets/b_mainline_fur/v1_roots.res",ResourceSaver.SaverFlags.Compress);}
    }
    // Diagnostic geometry from the SAME finite curves used by the volume
    // bake. It isolates sampling error; this is explicitly not a Shell run.
    public static ArrayMesh Reference(HairVolume original,bool ribbons=false,PuppetFurSpace? coordinates=null)
    {
        var space=coordinates??PuppetFurSpace.Lab;bool lab=space==PuppetFurSpace.Lab;
        string cacheKey=lab?"":Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(original.Data))+space.ToString()+ribbons;
        if(!lab&&mainlineReferences.TryGetValue(cacheKey,out var cached))return cached;
        // Increment v1 if finite-curve generation or simplification changes.
        // Ordinary play never saves generated player hair into the project.
        string bakedPath="res://assets/b_mainline_fur/v1_"+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(cacheKey)))+".res";
        if(!lab&&Godot.FileAccess.FileExists(bakedPath)){var baked=GD.Load<ArrayMesh>(bakedPath);mainlineReferences[cacheKey]=baked;return baked;}
        Build();if(lab&&ribbons&&ribbonReference!=null)return ribbonReference;if(lab&&!ribbons&&reference!=null)return reference;
        if(strands.Count==0){field=null;roots=null;Build(true);}
        var tiles=new SortedSet<(int X,int Y,int Z)>();
        foreach(var sample in original.Samples(1)){
            var raw=space.ToMaterial(Art.V(sample));var p=ToField(raw);
            for(int z=(int)MathF.Floor((p.Z-.05f)/Period);z<=(int)MathF.Floor((p.Z+.05f)/Period);z++)
            for(int y=(int)MathF.Floor((p.Y-.08f)/Period);y<=(int)MathF.Floor((p.Y+.08f)/Period);y++)
            for(int x=(int)MathF.Floor((p.X-.08f)/Period);x<=(int)MathF.Floor((p.X+.08f)/Period);x++)tiles.Add((x,y,z));
        }
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var colors=new List<Color>();var indices=new List<int>();
        var uvs=new List<Vector2>();var widths=new List<Vector2>();
        var spans=new List<FinSpan>();
        int sides=ribbons?2:6,curveCount=0,segmentCount=0,minSegments=7,maxSegments=0;
        foreach(var tile in tiles){
            var offset=new Vector3(tile.X,tile.Y,tile.Z)*Period;
            int strandIndex=-1;
            foreach(var strand in strands){
                strandIndex++;
                var rootCoord=strand.Root+offset;
                if(rootCoord.X<0||rootCoord.X>=Period*16||rootCoord.Y<0||rootCoord.Y>Period*8)continue;
                var root=FromField(rootCoord);
                if(PuppetVolumeFur.SampleExact(original,root,space)<=0)continue;
                int start=vertices.Count;
                var full=new Vector3[8];
                for(int s=0;s<8;s++)full[s]=FromField(strand.Points[s]+offset);
                if(PuppetHairSettings.Current.SurfaceSamples&&(strandIndex/11)%4!=0){
                    // Keep fibres with an original boundary-crossing sample.
                    // Others are sparsified only in explicit geometry; the
                    // continuous volume still contains every original curve.
                    // This sign test does not interpret occupancy as distance.
                    bool crossesOriginalBoundary=false;
                    for(int s=1;s<full.Length;s++)if(PuppetVolumeFur.SampleExact(original,full[s],space)<=0){crossesOriginalBoundary=true;break;}
                    if(!crossesOriginalBoundary)continue;
                }
                var knots=ribbons&&PuppetHairSettings.Current.TuftHybrid?Simplify(full,PuppetHairSettings.Current.SurfaceSamples?.002f:.0035f):new List<int>{0,1,2,3,4,5,6,7};
                int rings=knots.Count;var simplified=new Vector3[rings];
                for(int s=0;s<rings;s++)simplified[s]=full[knots[s]];
                curveCount++;segmentCount+=rings-1;minSegments=Math.Min(minSegments,rings-1);maxSegments=Math.Max(maxSegments,rings-1);
                for(int s=0;s<rings;s++){
                    float t=knots[s]/7f,sample=knots[s];int low=Math.Min((int)sample,6);
                    var center=strand.Points[low].Lerp(strand.Points[low+1],sample-low)+offset;
                    var tangent=(strand.Points[Math.Min(low+1,7)]-strand.Points[Math.Max(low-1,0)]).Normalized();
                    var u=(strand.Side-tangent*strand.Side.Dot(tangent)).Normalized();var v=tangent.Cross(u);
                    for(int j=0;j<sides;j++){
                        float angle=j*MathF.Tau/sides;var normal=u*MathF.Cos(angle)+v*MathF.Sin(angle);
                        var p=ribbons?simplified[s]:FromField(center+normal*(strand.Radius*(1-.88f*t*t)));
                        var mappedNormal=(FromField(center+normal*.001f)-FromField(center)).Normalized();
                        if(ribbons)mappedNormal=(simplified[Math.Min(s+1,rings-1)]-simplified[Math.Max(s-1,0)]).Normalized();
                        vertices.Add(p);normals.Add(mappedNormal);
                        // The GPU contact plane needs one identical anchor
                        // for the whole curve. Per-vertex root deltas acquire
                        // different errors when COLOR is packed by the mesh.
                        // Original root membership above remains full precision.
                        var packedRoot=root/space.RootPacking;colors.Add(new(packedRoot.X*.5f+.5f,packedRoot.Y*.5f+.5f,packedRoot.Z*.5f+.5f,t));
                        uvs.Add(new(j==0?-1:1,t));widths.Add(new(strand.Radius*(1-.96f*t*t),0));
                    }
                }
                if(ribbons){
                    var bounds=new Aabb(vertices[start],Vector3.Zero);
                    for(int j=start+1;j<vertices.Count;j++)bounds=bounds.Expand(vertices[j]);
                    spans.Add(new(start,vertices.Count-start,bounds.Grow(.002f)));
                    for(int s=0;s<rings-1;s++){
                        int a=start+s*2;indices.AddRange(new[]{a,a+1,a+2,a+1,a+3,a+2});
                    }
                    continue;
                }
                for(int s=0;s<strand.Points.Length-1;s++)for(int j=0;j<sides;j++){
                    int a=start+s*sides+j,b=start+s*sides+(j+1)%sides,c=a+sides,d=b+sides;
                    indices.AddRange(new[]{a,b,c,b,d,c});
                }
                for(int j=1;j<sides-1;j++){
                    indices.AddRange(new[]{start,start+j+1,start+j});
                    int end=start+(strand.Points.Length-1)*sides;indices.AddRange(new[]{end,end+j,end+j+1});
                }
            }
        }
        var arrays=new Godot.Collections.Array();arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex]=vertices.ToArray();arrays[(int)Mesh.ArrayType.Normal]=normals.ToArray();
        arrays[(int)Mesh.ArrayType.Color]=colors.ToArray();arrays[(int)Mesh.ArrayType.Index]=indices.ToArray();
        if(ribbons){arrays[(int)Mesh.ArrayType.TexUV]=uvs.ToArray();arrays[(int)Mesh.ArrayType.TexUV2]=widths.ToArray();}
        var mesh=new ArrayMesh();if(vertices.Count>0)mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);
        if(lab&&ribbons){
            ribbonReference=mesh;
            finCpu=new(vertices.ToArray(),normals.ToArray(),colors.ToArray(),uvs.ToArray(),widths.ToArray(),spans.ToArray());
            for(int i=0;i<spans.Count;i++)foreach(var key in FinKeys(spans[i].Bounds)){
                if(!finCells.TryGetValue(key,out var members))finCells[key]=members=new();members.Add(i);
            }
        }else if(lab)reference=mesh;
        if(!lab){if(mainlineReferences.Count>=8)mainlineReferences.Remove(mainlineReferences.Keys.First());mainlineReferences[cacheKey]=mesh;}
        if(!lab&&BakeMainlineCache){System.IO.Directory.CreateDirectory(ProjectSettings.GlobalizePath("res://assets/b_mainline_fur"));if(ResourceSaver.Save(mesh,bakedPath,ResourceSaver.SaverFlags.Compress)!=Error.Ok)throw new InvalidOperationException("B fur cache save failed");}
        FinMeanSegments=curveCount>0?segmentCount/(float)curveCount:0;
        GD.Print($"TUFT_GEOMETRY_REFERENCE curves={curveCount} triangles={indices.Count/3} tiles={tiles.Count} ribbons={ribbons} segments={minSegments}..{maxSegments} mean={FinMeanSegments:F3} boundMm={FinSimplificationBound*1000:F3}; not Shell");
        return mesh;
    }
    static IEnumerable<(int,int,int)> FinKeys(Aabb bounds)
    {
        var low=bounds.Position/FinCell;var high=bounds.End/FinCell;
        for(int z=(int)MathF.Floor(low.Z);z<=(int)MathF.Floor(high.Z);z++)
        for(int y=(int)MathF.Floor(low.Y);y<=(int)MathF.Floor(high.Y);y++)
        for(int x=(int)MathF.Floor(low.X);x<=(int)MathF.Floor(high.X);x++)yield return (x,y,z);
    }
    public static ArrayMesh? FragmentReference(HairVolume original,Aabb pieceBounds)
    {
        _=Reference(original,true);var data=finCpu!;
        // Copy existing curves only. The current occupancy still clips every
        // point in the shader. This box query is a broad phase, not an SDF or
        // a new set of roots on the cut. Stable hashed selection bounds cost.
        var bounds=pieceBounds.Grow(.045f);var ids=new HashSet<int>();
        foreach(var key in FinKeys(bounds))if(finCells.TryGetValue(key,out var members))foreach(int id in members)if(data.Spans[id].Bounds.Intersects(bounds))ids.Add(id);
        var selected=new List<FinSpan>();int triangleCount=0,vertexCount=0;
        foreach(int id in ids.OrderBy(id=>unchecked((uint)id*2654435761u))){
            var span=data.Spans[id];int triangles=span.Count-2;
            if(triangleCount+triangles>FragmentFinTriangleLimit)continue;
            selected.Add(span);triangleCount+=triangles;vertexCount+=span.Count;
        }
        if(vertexCount==0)return null;
        var vs=new Vector3[vertexCount];var ns=new Vector3[vertexCount];var cs=new Color[vertexCount];
        var uv=new Vector2[vertexCount];var widths=new Vector2[vertexCount];var ix=new int[triangleCount*3];int v=0,k=0;
        foreach(var span in selected){
            Array.Copy(data.Vertices,span.Start,vs,v,span.Count);Array.Copy(data.Normals,span.Start,ns,v,span.Count);
            Array.Copy(data.Colors,span.Start,cs,v,span.Count);Array.Copy(data.Uvs,span.Start,uv,v,span.Count);Array.Copy(data.Widths,span.Start,widths,v,span.Count);
            for(int ring=0;ring<span.Count/2-1;ring++){
                int a=v+ring*2;ix[k++]=a;ix[k++]=a+1;ix[k++]=a+2;ix[k++]=a+1;ix[k++]=a+3;ix[k++]=a+2;
            }
            v+=span.Count;
        }
        var arrays=new Godot.Collections.Array();arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex]=vs;arrays[(int)Mesh.ArrayType.Normal]=ns;arrays[(int)Mesh.ArrayType.Color]=cs;
        arrays[(int)Mesh.ArrayType.TexUV]=uv;arrays[(int)Mesh.ArrayType.TexUV2]=widths;arrays[(int)Mesh.ArrayType.Index]=ix;
        var mesh=new ArrayMesh();mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);return mesh;
    }
    static List<int> Simplify(Vector3[] full,float tolerance)
    {
        var knots=new List<int>{0,full.Length-1};
        while(true){
            float worst=0;int split=-1,insert=0;
            for(int k=0;k<knots.Count-1;k++){
                int a=knots[k],b=knots[k+1];
                for(int s=a+1;s<b;s++){
                    float error=full[s].DistanceTo(full[a].Lerp(full[b],(s-a)/(float)(b-a)));
                    if(error>worst){worst=error;split=s;insert=k+1;}
                }
            }
            // Both polylines are affine between original knots, so the
            // maximum same-parameter centerline error occurs at a knot.
            // Retain exact original root/tip and split only where needed.
            if(worst<=tolerance){FinSimplificationBound=Math.Max(FinSimplificationBound,worst);return knots;}
            knots.Insert(insert,split);
        }
    }
    static ImageTexture3D Upload(byte[] data)
    {
        var images=new Godot.Collections.Array<Image>();int size=Size;var level=data;
        // Godot RD requires the complete XYZ mip chain as Z slices at each
        // level. useMipmaps=true alone does not create those missing images.
        while(true){
            int stride=size*size*4;
            for(int z=0;z<size;z++){
                var slice=new byte[stride];Buffer.BlockCopy(level,z*stride,slice,0,stride);
                images.Add(Image.CreateFromData(size,size,false,Image.Format.Rgba8,slice));
            }
            if(size==1)break;
            int next=size/2;var smaller=new byte[next*next*next*4];
            for(int z=0;z<next;z++)for(int y=0;y<next;y++)for(int x=0;x<next;x++)for(int c=0;c<4;c++){
                int sum=0;
                for(int dz=0;dz<2;dz++)for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++)sum+=level[(((z*2+dz)*size+y*2+dy)*size+x*2+dx)*4+c];
                smaller[((z*next+y)*next+x)*4+c]=(byte)((sum+4)/8);
            }
            size=next;level=smaller;
        }
        var texture=new ImageTexture3D();
        var error=texture.Create(Image.Format.Rgba8,Size,Size,Size,true,images);
        foreach(var image in images)image.Dispose();
        if(error!=Error.Ok)throw new InvalidOperationException("Cannot upload fixed tuft volume: "+error);
        var readback=texture.GetData();
        bool valid=readback.Count==Size*2-1&&readback[0].GetData().AsSpan().SequenceEqual(data.AsSpan(0,Size*Size*4));
        foreach(var image in readback)image.Dispose();
        if(!valid)throw new InvalidOperationException("Fixed tuft mip upload failed readback");
        return texture;
    }
}
