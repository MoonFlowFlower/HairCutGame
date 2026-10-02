using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using static Hairball.TargetLabGeometry;
using NV=System.Numerics.Vector3;
namespace Hairball;

// This fixture uses the existing density extractor. No production constants are modified.
public sealed class PuppetLabHair
{
    public const float Scale=3.5f;
    public static readonly Vector3 Origin=new(0,1.98f,.0f);
    public HairVolume Volume=new();public Node3D Root;public MeshInstance3D Core;
    public int Triangles;public int State;public int Fibers, FiberTriangles,ShortFibers;public float MaskCoverage;public double LastCutBuildMs {get;private set;}public MeshInstance3D? Fuzz;int round;
    readonly List<(Vector3 Point,Vector3 Normal)> cuts=new();
    // The authored field is immutable. Re-evaluating it for every detached
    // fragment stalled the live cut even though debris has no fiber renderer.
    static readonly Lazy<HairVolume> authoredVolume=new(()=>{
        var v=new HairVolume();v.Fill(p=>Field(p/Scale)*Scale);return v;
    });
    readonly HairVolume original;
    public readonly PuppetHairSettings Settings;
    public int TrimVertices {get;private set;} public int InteriorVertices {get;private set;}
    public PuppetHairWarning Warning {get;private set;}=new();
    public event Action<IReadOnlyList<HairVolume>,Transform3D>? Detached;
    // Only a committed material edit emits this. Misses, empty clicks and
    // state/reset fixtures cannot impersonate a successful haircut.
    public event Action<Vector3,Vector3,float,float>? CutCommitted;
    readonly List<GeometryInstance3D> shells=new();
    public PuppetVolumeFur? VolumeFur {get;private set;}
    public int ShellCount=>VolumeFur?.Count??shells.Count;
    public int ShellTriangles=>VolumeFur?.Triangles??Triangles*shells.Count;
    public IReadOnlyList<GeometryInstance3D> ShellLayers=>shells;
    public float MaximumFiberOffset {get;private set;}
    public float MaximumShortFiberOffset {get;private set;}
    public Vector3 MaximumFiberRoot {get;private set;}
    public Vector3 MaximumFiberTip {get;private set;}
    public string DensityHash=>Convert.ToHexString(SHA256.HashData(Volume.Data));
    public PuppetLabHair(Node3D parent,int state=0,HairVolume? fixture=null,PuppetHairSettings? settings=null)
    {
        Settings=settings??PuppetHairSettings.Current;
        State=state;Root=new Node3D{Name="DensityHair",Position=Origin,Scale=Vector3.One*.9f};parent.AddChild(Root);
        original=authoredVolume.Value;
        Volume=fixture??original.Clone();
        if(state==1)Volume.Brush(new HairEffect(EffectKind.CutPlane,1,NV.UnitY),new NV(0,.265f*Scale,0),2.0f*Scale);
        if(state==5)Volume.Brush(new HairEffect(EffectKind.RemoveHair,.12f,NV.UnitZ),new NV(.02f,.32f,.32f)*Scale,.235f*Scale);
        RefreshWarning();
        Core=Mesh(Root,BuildMesh(),Vector3.Zero,Mat("795285",.96f));Core.Layers=2;Core.GIMode=GeometryInstance3D.GIModeEnum.Disabled;
    }
    static float Union(float a,float b,float k){float h=Math.Clamp(.5f+.5f*(a-b)/k,0,1);return b+(a-b)*h+k*h*(1-h);}
    public static float Field(NV p)
    {
        float d=HairVolume.Ellipsoid(p,new(0,.095f,-.085f),new(.515f,.29f,.35f));
        // Continuous swept locks: the groove hierarchy is real density.
        for(int lockId=0;lockId<7;lockId++)for(int i=0;i<22;i++)
        {
            float t=i/21f,u=1-t,x=-.44f+lockId*.151f;
            float crown=lockId switch{0=>.40f,1=>.63f,2=>.79f,3=>.66f,4=>.65f,5=>.49f,_=>.33f};
            float fringe=lockId switch{0=>.015f,1=>.185f,2=>.25f,3=>.24f,4=>.22f,5=>.12f,_=>-.04f};
            NV a=new NV(x-.045f,.04f,-.28f)*u*u*u+new NV(x-.15f,crown,-.12f)*3*u*u*t+new NV(x+.32f,crown*.80f,.34f)*3*u*t*t+new NV(x-.085f,fringe,.33f)*t*t*t;
            a.X+=.022f*MathF.Sin(t*MathF.Tau+lockId)*MathF.Sin(t*MathF.PI);
            float width=.088f+.035f*MathF.Sin(t*MathF.PI);
            d=Union(d,HairVolume.Ellipsoid(p,a,new(width,.143f,.143f)),.025f);
        }
        float scalp=HairVolume.Ellipsoid(p,new(0,-.24f,.015f),new(.445f,.45f,.322f));
        float face=Math.Min(p.Z-.060f,.17f-.27f*Math.Abs(p.X)+.025f*MathF.Sin(p.X*7)-p.Y);
        d=-Union(-d,face,.045f);
        return Math.Min(d,Math.Min(-scalp,p.Y+.16f));
    }
    public ArrayMesh BuildMesh()
    {
        var shell=HairShell.Build(Volume);var vs=new Vector3[shell.Vertices.Count];var ns=new Vector3[vs.Length];var colors=new Color[vs.Length];var uv2=new Vector2[vs.Length];var indices=shell.Indices.ToArray();
        TrimVertices=InteriorVertices=0;
        for(int i=0;i<vs.Length;i++)
        {
            var point=shell.Vertices[i];var p=point/Scale;vs[i]=new(p.X,p.Y,p.Z);var n=Volume.Normal(point);ns[i]=new(n.X,n.Y,n.Z);
            int kind=Classify(original,cuts,State,vs[i],ns[i]);
            if(kind==1)TrimVertices++;if(kind==2)InteriorVertices++;
            colors[i]=new Color(1,1,1,Settings.Enabled?kind*.5f:IsShort(vs[i],ns[i])?1:0);
            uv2[i]=new Vector2(Warning.Weight(point),0);
            // Only flatten normals of a known authored cut plane, never move vertices.
            if(Settings.Enabled&&Settings.Smooth&&kind==1)
            {if(State==1)ns[i]=Vector3.Up;else foreach(var plane in cuts)if(Math.Abs((vs[i]-plane.Point).Dot(plane.Normal))<.018f&&ns[i].Dot(plane.Normal)>.9f){ns[i]=plane.Normal;break;}}
        }
        for(int i=0;i<shell.Indices.Count;i+=3)
        {
            int a=indices[i],b=indices[i+1],c=indices[i+2];
            if((vs[b]-vs[a]).Cross(vs[c]-vs[a]).Dot(ns[a]+ns[b]+ns[c])>0)(indices[i+1],indices[i+2])=(indices[i+2],indices[i+1]);
        }
        Triangles=indices.Length/3;
        // Complete shaving (and sub-surface density remnants) legitimately has
        // no triangles. Replace the old geometry with a zero-surface mesh;
        // Godot rejects AddSurfaceFromArrays with an empty vertex array.
        var mesh=new ArrayMesh();
        if(vs.Length==0||indices.Length==0)return mesh;
        var arr=new Godot.Collections.Array();arr.Resize((int)Godot.Mesh.ArrayType.Max);arr[(int)Godot.Mesh.ArrayType.Vertex]=vs;arr[(int)Godot.Mesh.ArrayType.Normal]=ns;arr[(int)Godot.Mesh.ArrayType.Color]=colors;arr[(int)Godot.Mesh.ArrayType.TexUV2]=uv2;arr[(int)Godot.Mesh.ArrayType.Index]=indices;mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles,arr);return mesh;
    }
    public void SetLook(int visualRound,bool on=true)
    {
        round=visualRound;
        Core.MaterialOverride=on&&round>=2?PuppetLabSurface.Hair(false,State,round>=3,Settings):Mat("795285",.96f);
        if(Fuzz==null&&round>=2&&Settings.Lod<2&&!Settings.VolumeShells)BuildFuzz();if(Fuzz!=null)Fuzz.Visible=on&&round>=2&&Settings.Fuzz;
        BuildShells(on);
    }
    public static float Weight(Vector3 p,int state)
    {
        static float Smooth(float a,float b,float x){float t=Math.Clamp((x-a)/(b-a),0,1);return t*t*(3-2*t);}
        return state==2?Smooth(.24f,.42f,p.X+.20f*p.Y)*Smooth(-.03f,.13f,p.Y):state is 3 or 4?Smooth(-.025f,.025f,p.X):0;
    }
    bool IsShort(Vector3 p,Vector3 n)
    {if(State==1&&p.Y>.245f&&n.Y>.65f)return true;foreach(var plane in cuts)if(Math.Abs((p-plane.Point).Dot(plane.Normal))<.025f&&(p-plane.Point).Length()<.27f&&n.Dot(plane.Normal)>.70f)return true;return false;}
    static int Classify(HairVolume before,IEnumerable<(Vector3 Point,Vector3 Normal)> planes,int state,Vector3 p,Vector3 n)
    {
        // A newly exposed sample was inside the pre-edit volume. The normal alone
        // cannot tell an original underside from a carved wall.
        bool exposed=before.Sample(new NV(p.X,p.Y,p.Z)*Scale)>HairVolume.Step*.22f;
        if(state==1&&p.Y>.245f&&n.Y>.65f)return 1;
        foreach(var plane in planes)
            if(Math.Abs((p-plane.Point).Dot(plane.Normal))<.025f&&(p-plane.Point).Length()<.27f&&n.Dot(plane.Normal)>.70f)return 1;
        return exposed?2:0;
    }
    sealed record FuzzData(Vector3[] Positions,Vector3[] Normals,Color[] Colors,Vector2[] Effects,int[] Indices,int Fibers,int ShortFibers,float Coverage,float MaximumOffset,float MaximumShortOffset,Vector3 MaximumRoot,Vector3 MaximumTip);
    public bool IsFuzzBuilding {get;private set;}
    public double LastCutReadyMs {get;private set;}
    void BuildFuzz()=>InstallFuzz(GenerateFuzz(Volume,original,cuts.ToArray(),State,round,Settings,Warning));
    void InstallFuzz(FuzzData data)
    {
        bool visible=Fuzz?.Visible??true;Fuzz?.Free();Fuzz=null;
        Fibers=data.Fibers;ShortFibers=data.ShortFibers;MaskCoverage=data.Coverage;FiberTriangles=data.Indices.Length/3;
        MaximumFiberOffset=data.MaximumOffset;MaximumShortFiberOffset=data.MaximumShortOffset;MaximumFiberRoot=data.MaximumRoot;MaximumFiberTip=data.MaximumTip;
        // Clear stale fuzz even when the rebuild has nothing to render. A
        // nonempty core can also generate zero fibers below the pile cutoff.
        if(data.Positions.Length==0||data.Indices.Length==0)return;
        var arr=new Godot.Collections.Array();arr.Resize((int)Godot.Mesh.ArrayType.Max);arr[(int)Godot.Mesh.ArrayType.Vertex]=data.Positions;arr[(int)Godot.Mesh.ArrayType.Normal]=data.Normals;arr[(int)Godot.Mesh.ArrayType.Color]=data.Colors;arr[(int)Godot.Mesh.ArrayType.TexUV2]=data.Effects;arr[(int)Godot.Mesh.ArrayType.Index]=data.Indices;
        var mesh=new ArrayMesh();mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles,arr);
        Fuzz=Mesh(Root,mesh,Vector3.Zero,PuppetLabSurface.Hair(true,State,round>=3,Settings));Fuzz.Layers=2;Fuzz.Name="BoundedOpaqueFuzz";Fuzz.Visible=visible&&Settings.Fuzz;Fuzz.GIMode=GeometryInstance3D.GIModeEnum.Disabled;Fuzz.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
    }
    async void RebuildFuzzAsync(System.Diagnostics.Stopwatch timer)
    {
        IsFuzzBuilding=true;var snapshot=Volume.Clone();var planes=cuts.ToArray();int state=State,quality=round;
        try
        {
            var data=await System.Threading.Tasks.Task.Run(()=>GenerateFuzz(snapshot,original,planes,state,quality,Settings,Warning));
            if(GodotObject.IsInstanceValid(Root)&&Root.IsInsideTree()){InstallFuzz(data);LastCutReadyMs=timer.Elapsed.TotalMilliseconds;}
        }
        catch(Exception e){GD.PushError("B2 fuzz rebuild failed: "+e);}
        finally{IsFuzzBuilding=false;}
    }
    static FuzzData GenerateFuzz(HairVolume volume,HairVolume original,(Vector3 Point,Vector3 Normal)[] planes,int state,int round,PuppetHairSettings settings,PuppetHairWarning warning)
    {
        int Fibers=0,ShortFibers=0;float MaskCoverage=0,areaTotal=0,areaMasked=0,maxOffset=0,maxShortOffset=0;Vector3 maxRoot=default,maxTip=default;var shell=HairShell.Build(volume);var rand=new Random(30971);
        bool Short(Vector3 p,Vector3 n){if(state==1&&p.Y>.245f&&n.Y>.65f)return true;foreach(var plane in planes)if(Math.Abs((p-plane.Point).Dot(plane.Normal))<.025f&&(p-plane.Point).Length()<.27f&&n.Dot(plane.Normal)>.70f)return true;return false;}
        var vs=new List<Vector3>();var ns=new List<Vector3>();var colors=new List<Color>();var effects=new List<Vector2>();var ix=new List<int>();
        void PlushTuft(Vector3 p,Vector3 n,int kind,bool inward)
        {
            bool cut=kind>0;int segments=cut||inward?2:4;
            float bundleLength=(cut?(kind==2?.007f:.009f):inward?.003f:PuppetShellFur.TuftLength)*(.75f+.4f*(float)rand.NextDouble());
            var flow=new Vector3(.3f+.18f*MathF.Sin(p.Y*5),.15f-p.Z,.8f);
            var tangent=(flow-n*n.Dot(flow)).Normalized();
            if(tangent.LengthSquared()<.5f)tangent=n.Cross(Math.Abs(n.Y)<.9f?Vector3.Up:Vector3.Right).Normalized();
            var cross=n.Cross(tangent).Normalized();
            float angle=((float)rand.NextDouble()-.5f)*3.8f;
            var bend=tangent*MathF.Cos(angle)+cross*MathF.Sin(angle);
            float phase=(float)rand.NextDouble()*MathF.Tau;
            float warned=warning.Weight(new NV(p.X,p.Y,p.Z)*Scale);
            // A tuft shares a gentle bend but contains individually tapered
            // opaque filaments. No subpixel texture can merge into a wide card.
            for(int filament=0;filament<5;filament++){
                float theta=(float)rand.NextDouble()*MathF.Tau;
                var side=cross*MathF.Cos(theta)+tangent*MathF.Sin(theta);
                var spread=side*((cut||inward?.001f:.006f)*MathF.Sqrt((float)rand.NextDouble()));
                float len=bundleLength*(.7f+.5f*(float)rand.NextDouble());
                float width=(cut||inward?.0005f:.0008f)*(.65f+.6f*(float)rand.NextDouble());
                float tone=.94f+.12f*(float)rand.NextDouble();int offset=vs.Count;
                for(int s=0;s<=segments;s++){
                    float t=s/(float)segments;
                    var center=p+spread*(1-.45f*t)+n*(.0007f+len*.92f*t*(1-.18f*t*t))+bend*(len*.48f*t*t)+side*(len*.23f*MathF.Sin(t*MathF.PI*1.25f+phase)*t);
                    float w=width*(1-.96f*t*t);
                    for(int sign=-1;sign<=1;sign+=2){
                        var vertex=center+side*(sign*w);vs.Add(vertex);ns.Add((n+side*(sign*.08f)+bend*(t*.15f)).Normalized());
                        colors.Add(new(tone*(.93f+.07f*t),tone*(.93f+.07f*t),tone*(.93f+.07f*t),kind*.5f));
                        effects.Add(new(warned,t));
                        float distance=vertex.DistanceTo(p);if(cut)maxShortOffset=Math.Max(maxShortOffset,distance);
                        if(distance>maxOffset){maxOffset=distance;maxRoot=p;maxTip=vertex;}
                    }
                }
                for(int s=0;s<segments;s++){int z=offset+s*2;ix.Add(z);ix.Add(z+1);ix.Add(z+2);ix.Add(z+1);ix.Add(z+3);ix.Add(z+2);}
                Fibers++;if(cut)ShortFibers++;
            }
        }
        var normals=new Vector3[shell.Vertices.Count];var kinds=new int[normals.Length];
        if(settings.Round>=3)for(int i=0;i<normals.Length;i++){var q=shell.Vertices[i];var n=volume.Normal(q);normals[i]=new(n.X,n.Y,n.Z);kinds[i]=Classify(original,planes,state,new Vector3(q.X,q.Y,q.Z)/Scale,normals[i]);}
        for(int k=0;k<shell.Indices.Count;k+=3)
        {
            NV an=shell.Vertices[shell.Indices[k]]/Scale,bn=shell.Vertices[shell.Indices[k+1]]/Scale,cn=shell.Vertices[shell.Indices[k+2]]/Scale;
            var a=new Vector3(an.X,an.Y,an.Z);var b=new Vector3(bn.X,bn.Y,bn.Z);var c=new Vector3(cn.X,cn.Y,cn.Z);
            float area=(b-a).Cross(c-a).Length()*.5f;
            var center=(a+b+c)/3;var centern=volume.Normal(new NV(center.X,center.Y,center.Z)*Scale);
            int centerKind=Classify(original,planes,state,center,new Vector3(centern.X,centern.Y,centern.Z));
            float expectation=area*(settings.Round>=2?(centerKind>0?95000:18000):10000)*(settings.Lod==1?.38f:1);
            // The preserved flat candidate uses edge fuzz. The 00:44 candidate
            // uses small curved tufts above a shallow shell root coating.
            if(settings.ShellFur)expectation=area*(centerKind>0?1800:650)*(settings.Fuzz?1:0);
            if(settings.PlushTufts)expectation=area*(centerKind>0?14000:5000)*(settings.Fuzz?1:0);
            if(new Vector3(centern.X,centern.Y,centern.Z).Dot(center-new Vector3(0,.05f,0))>=-.01f){areaTotal+=area;areaMasked+=area*Weight(center,state);}
            int count=(int)expectation+(rand.NextDouble()<expectation%1?1:0);
            for(int j=0;j<count;j++)
            {
                float u=MathF.Sqrt((float)rand.NextDouble()),v=(float)rand.NextDouble();var p=a*(1-u)+b*u*(1-v)+c*u*v;
                Vector3 n;
                if(settings.Round>=3)n=(normals[shell.Indices[k]]*(1-u)+normals[shell.Indices[k+1]]*u*(1-v)+normals[shell.Indices[k+2]]*u*v).Normalized();
                else {var np=volume.Normal(new NV(p.X,p.Y,p.Z)*Scale);n=new Vector3(np.X,np.Y,np.Z);}
                bool inward=n.Dot(p-new Vector3(0,.05f,0))<-.01f;
                if((!settings.Enabled&&inward)||p.Y<-.12f)continue;
                int kind=settings.Round>=3&&kinds[shell.Indices[k]]==kinds[shell.Indices[k+1]]&&kinds[shell.Indices[k]]==kinds[shell.Indices[k+2]]?kinds[shell.Indices[k]]:Classify(original,planes,state,p,n);
                if(settings.Enabled&&((kind==1&&!settings.Trim)||(kind==2&&!settings.Carve)))continue;
                bool useShort=settings.Enabled&&(kind==1?settings.Trim:kind==2&&settings.Carve);
                bool cut=settings.Enabled?useShort:Short(p,n);
                if(settings.PlushTufts){PlushTuft(p,n,kind,inward);continue;}
                int segments=settings.Round>=3?(cut?1:3):settings.Round>=2?(cut?2:5):7;
                if(cut)ShortFibers++;
                bool wisp=!cut&&rand.NextDouble()<(settings.Round>=2?.07:.15);
                float baseLength=!settings.Enabled?(cut?.003f:wisp?.068f:.038f):cut?(settings.Round>=2?(kind==2?.0055f:.008f):kind==2?.0035f:.006f):inward?.004f:wisp?(settings.Round>=2?.041f:.068f):(settings.Round>=2?.030f:.038f);
                if(settings.ShellFur){wisp=false;baseLength=cut?(kind==2?.0026f:.0038f):inward?.004f:PuppetShellFur.NormalPile;}
                float len=baseLength*(.65f+.7f*(float)rand.NextDouble());
                float phase=(float)rand.NextDouble()*MathF.Tau;float mask=round>=3?Weight(p,state):0;
                len*=state==2?Mathf.Lerp(1,.15f,mask):state==3?Mathf.Lerp(1,.45f,mask):1;
                var flow=new Vector3(.25f+MathF.Sin(p.Y*17+p.X*10)*.34f,.15f-p.Z*1.6f+MathF.Cos(p.X*13)*.18f,.80f);
                var tangent=(flow-n*n.Dot(flow)).Normalized();
                if(tangent.LengthSquared()<.5f)tangent=n.Cross(Vector3.Forward).Normalized();var cross=n.Cross(tangent).Normalized();
                float tone=(settings.Round>=2?.87f:.79f)+(settings.Round>=2?.18f:.27f)*(float)rand.NextDouble(),curl=cut?.0004f:wisp?.011f:.0065f;
                if(settings.ShellFur)curl=cut?.0002f:.0012f;
                if(state is 2 or 3)curl*=1-mask*.91f;if(state==4)curl*=1-mask;
                var points=new Vector3[segments+1];
                for(int s=0;s<=segments;s++)
                    {float t=s/(float)segments;points[s]=p+n*((settings.Round>=2&&cut?.002f:.0013f)+len*t*(settings.Enabled&&cut?.72f:wisp?.24f:.48f)+curl*.25f*MathF.Sin(t*MathF.PI))+tangent*(len*t*(settings.Enabled&&cut?.45f:.82f)+curl*MathF.Sin(t*MathF.Tau))+cross*(MathF.Sin(t*MathF.Tau*.90f+phase)-MathF.Sin(phase))*curl*t;}
                for(int ribbon=0;ribbon<2;ribbon++)
                {
                    int offset=vs.Count;var side=ribbon==0?cross:tangent;float width=(cut?(settings.Enabled?.0009f:.0010f):settings.Round>=3?.00065f:.00082f)*(.65f+.6f*(float)rand.NextDouble());
                    float warned=warning.Weight(new NV(p.X,p.Y,p.Z)*Scale);
                    for(int s=0;s<=segments;s++)
                    {
                        float t=s/(float)segments,w=width*(1-t*.83f);var color=new Color(tone*(.85f+t*.15f),tone*(.85f+t*.15f),tone*(.85f+t*.15f),settings.Enabled?kind*.5f:1);
                        for(int sign=-1;sign<=1;sign+=2){var tip=points[s]+side*sign*w;vs.Add(tip);ns.Add(settings.ShellFur?n:(n+side*sign*.65f).Normalized());colors.Add(color);effects.Add(new(warned,0));float offsetFromRoot=tip.DistanceTo(p);if(cut)maxShortOffset=Math.Max(maxShortOffset,offsetFromRoot);if(offsetFromRoot>maxOffset){maxOffset=offsetFromRoot;maxRoot=p;maxTip=tip;}}
                    }
                    for(int s=0;s<segments;s++){int z=offset+s*2;ix.Add(z);ix.Add(z+1);ix.Add(z+2);ix.Add(z+1);ix.Add(z+3);ix.Add(z+2);}
                }
                Fibers++;
            }
        }
        MaskCoverage=areaTotal>0?areaMasked/areaTotal:0;
        return new FuzzData(vs.ToArray(),ns.ToArray(),colors.ToArray(),effects.ToArray(),ix.ToArray(),Fibers,ShortFibers,MaskCoverage,maxOffset,maxShortOffset,maxRoot,maxTip);
    }
    public bool Cut(Vector3 worldPoint,Vector3 normal,float radius=.23f)
    {
        if(IsFuzzBuilding)return false;
        var timer=System.Diagnostics.Stopwatch.StartNew();
        var n=(Root.GlobalBasis.Transposed()*normal).Normalized();var center=Root.ToLocal(worldPoint)-n*.055f;var p=center*Scale;float mass=Volume.Mass;
        Volume.Brush(new HairEffect(EffectKind.CutPlane,.10f,new NV(n.X,n.Y,n.Z)),new NV(p.X,p.Y,p.Z),radius*Scale);
        if(Math.Abs(Volume.Mass-mass)<.00001f)return false;cuts.Add((center,n));if(cuts.Count>32)cuts.RemoveAt(0);
        FinalizeVisualEdit();Core.Mesh=BuildMesh();BuildShells(true);
        CutCommitted?.Invoke(worldPoint,normal,radius,Math.Max(0,mass-Volume.Mass));
        if(Settings.VolumeShells)LastCutReadyMs=timer.Elapsed.TotalMilliseconds;
        else if(round>=2){if(Fuzz?.MaterialOverride is ShaderMaterial m){m.SetShaderParameter("cut_pending",true);m.SetShaderParameter("cut_center",center);m.SetShaderParameter("cut_normal",n);}RebuildFuzzAsync(timer);}
        LastCutBuildMs=timer.Elapsed.TotalMilliseconds;return true;
    }
    public static bool Supported(NV p)=>Math.Abs(HairVolume.Ellipsoid(p/Scale,new(0,-.24f,.015f),new(.445f,.45f,.322f)))<HairVolume.Step/Scale;
    void RefreshWarning(){Warning=Settings.Round>=3&&Settings.Warning&&Settings.Lod<2?PuppetHairWarning.Evaluate(Volume,Supported):new();}
    void FinalizeVisualEdit()
    {
        if(Settings.Round>=3){var pieces=Volume.DetachUnsupported(Supported,false);if(pieces.Count>0)Detached?.Invoke(pieces,Root.GlobalTransform);}
        RefreshWarning();
    }
    public void SeverWarningFixture()
    {
        // The QA last cut is fixed independently of whether warning presentation is on.
        var neck=new NV(.28f,.40f,0)*Scale;
        Volume.Brush(new HairEffect(EffectKind.RemoveHair,0,NV.UnitY),neck,.065f*Scale);
        var timer=System.Diagnostics.Stopwatch.StartNew();FinalizeVisualEdit();Core.Mesh=BuildMesh();BuildShells(true);if(Settings.VolumeShells)LastCutReadyMs=timer.Elapsed.TotalMilliseconds;else RebuildFuzzAsync(timer);LastCutBuildMs=timer.Elapsed.TotalMilliseconds;
    }
    public static HairVolume WarningFixture(bool redundant=false)
    {
        var volume=new HairVolume();volume.Fill(q=>{
            var p=q/Scale;float body=Math.Min(Field(p),.21f-p.X);
            float neck=.023f-HairSystem.DistanceToSegment(p,new(.19f,.40f,0),new(.40f,.40f,0));
            if(redundant)neck=Math.Max(neck,.080f-HairSystem.DistanceToSegment(p,new(.17f,.32f,.12f),new(.42f,.40f,.12f)));
            float lobe=HairVolume.Ellipsoid(p,new(.48f,.46f,0),new(.17f,.23f,.22f));
            return Math.Max(body,Math.Max(neck,lobe))*Scale;
        });return volume;
    }
    void BuildShells(bool on)
    {
        if(Settings.VolumeShells){
            Core.Visible=!on;
            if(Core.Mesh.GetSurfaceCount()==0){foreach(var shell in shells)shell.Free();VolumeFur=null;shells.Clear();return;}
            if(VolumeFur==null&&Settings.Shells>0){VolumeFur=new(Root,original,Volume,Settings.Shells,Settings.BundleField,Settings.TuftVolume,Settings.TuftReference,(ArrayMesh)Core.Mesh,Settings);shells.Add(VolumeFur.Node);if(VolumeFur.Fins!=null)shells.Add(VolumeFur.Fins);}
            else if(VolumeFur!=null&&VolumeFur.UploadedRevision!=Volume.Revision)VolumeFur.Update(Volume,(ArrayMesh)Core.Mesh);
            if(VolumeFur!=null){VolumeFur.Node.Visible=on;if(VolumeFur.Fins!=null)VolumeFur.Fins.Visible=on;}
            return;
        }
        foreach(var shell in shells)shell.Free();shells.Clear();
        if(Core.Mesh.GetSurfaceCount()==0)return;
        int count=Settings.Round>=3&&on?(Settings.Lod==0?Settings.Shells:Settings.Lod==1?Math.Min(1,Settings.Shells):0):0;
        if(Settings.ShellFur&&on)count=Settings.Shells;
        if(Settings.ShellFur){
            for(int i=1;i<=count;i++){
                var layer=Mesh(Root,Core.Mesh,Vector3.Zero,PuppetShellFur.Material(i,count,Settings.PlushTufts));
                layer.Name="R7Shell"+i;layer.Layers=2;layer.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
                layer.GIMode=GeometryInstance3D.GIModeEnum.Disabled;layer.ExtraCullMargin=Settings.PlushTufts?.08f:.035f;shells.Add(layer);
            }
            return;
        }
        for(int i=1;i<=count;i++){var material=PuppetLabSurface.Hair(false,State,true,Settings);material.SetShaderParameter("shell_index",i);material.SetShaderParameter("shell_count",count);var layer=Mesh(Root,Core.Mesh,Vector3.Zero,material);layer.Layers=2;layer.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;layer.GIMode=GeometryInstance3D.GIModeEnum.Disabled;shells.Add(layer);}
    }
    public bool Raycast(Vector3 worldOrigin,Vector3 worldDirection,out Vector3 point,out float distance)
    {
        var o=Root.ToLocal(worldOrigin)*Scale;var d=(Root.GlobalBasis.Inverse()*worldDirection).Normalized();
        bool hit=Volume.Raycast(new NV(o.X,o.Y,o.Z),new NV(d.X,d.Y,d.Z),8,out var p,out _);
        point=Root.ToGlobal(new Vector3(p.X,p.Y,p.Z)/Scale);distance=point.DistanceTo(worldOrigin);return hit&&distance<=3;
    }
}
