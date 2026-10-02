using Godot;
using System;
using System.Diagnostics;
using System.Collections.Generic;
namespace Hairball;

// Bounded lab presentation, not authoritative HairVolume force transport.
// The spring guides have fixed roots. They only deform the existing implicit
// follicles; all occupancy/cut decisions remain in their original coordinates.
public sealed class PuppetFurMotion : IDisposable
{
    public const int Columns=48,Rows=24,Width=Columns+1,Height=Rows+1;
    public const uint ContactLayer=1u<<18;
    public const float MaxOffset=.08f,ProbeRadius=.003f;
    public const float MaxContactPushWorld=.070f;
    public static readonly float[] ContactSamples={.5f,.75f,1f};
    public readonly PuppetLabHair Hair;
    public readonly ImageTexture Field;
    public readonly Vector3[] RestTips=new Vector3[Width*Height],Offsets=new Vector3[Width*Height],Velocities=new Vector3[Width*Height];
    public readonly float[] TipWeights=new float[Width*Height];
    public readonly bool[] Active=new bool[Width*Height];
    readonly Vector3[] rays=new Vector3[Width*Height];
    readonly float[] ends=new float[Width*Height];
    readonly float[] originalEnds=new float[Width*Height];
    readonly float[] cutSpringTime=new float[Width*Height];
    readonly byte[] probeMask=new byte[Width*Height];
    readonly float[] pixels=new float[Width*Height*4];
    readonly byte[] bytes=new byte[Width*Height*16];
    readonly Image uploadImage;
    readonly ShaderMaterial[] materials;
    readonly PuppetVolumeFur renderer;
    readonly Aabb baseBounds,finBaseBounds,motionBounds;
    readonly float contactBoundsMargin;
    readonly SphereShape3D probe=new(){Radius=ProbeRadius};
    readonly PhysicsShapeQueryParameters3D query;
    readonly Stopwatch timer=new();
    Transform3D frameTransform;
    int revision=-1;
    public bool ResponseEnabled=true;
    public bool CutFeedbackEnabled=true;
    public int CutEvents {get;private set;} public int CutImpulses {get;private set;}
    public int LastCutLocalGuides {get;private set;}
    public int LastCutGuide {get;private set;}=-1;
    public Vector3 LastCutWorldPoint {get;private set;} public Vector3 LastCutWorldDirection {get;private set;}
    public float LastCutRemovedMass {get;private set;} public float CutAge {get;private set;}=100;
    public const float CutDamping=.22f,CutFrequency=12f,MaxCutVelocity=2f;
    public int ActiveGuides {get;private set;} public int Contacts {get;private set;} public int Queries {get;private set;}
    public ulong LastCollider {get;private set;}
    public double LastStepMs {get;private set;} public double LastProbeRefreshMs {get;private set;}
    public float MaxTipOffset {get;private set;} public float RmsTipOffset {get;private set;} public float MaxTipSpeed {get;private set;}
    public float MaxContactCorrection {get;private set;} public bool Finite {get;private set;}=true;
    public int BoundedContacts {get;private set;} public int InfeasibleContactPlanes {get;private set;}
    public float MaxRequiredContactOffset {get;private set;}
    public bool BoundsContainFullMotion {get;private set;}=true;
    public PuppetFurMotion(PuppetLabHair hair)
    {
        Hair=hair;frameTransform=hair.Root.GlobalTransform;materials=new List<ShaderMaterial>(hair.VolumeFur!.MotionMaterials).ToArray();
        renderer=hair.VolumeFur!;baseBounds=renderer.Node.CustomAabb;finBaseBounds=renderer.Fins?.CustomAabb??default;
        var inv=frameTransform.Basis.Inverse();
        // Frobenius norm is a conservative bound for any affine inverse,
        // including rotated or non-uniformly scaled heads.
        contactBoundsMargin=MaxContactPushWorld*MathF.Sqrt(inv.X.LengthSquared()+inv.Y.LengthSquared()+inv.Z.LengthSquared());
        renderer.Node.CustomAabb=baseBounds.Grow(contactBoundsMargin);
        motionBounds=renderer.Node.CustomAabb;
        if(renderer.Fins!=null)renderer.Fins.CustomAabb=finBaseBounds.Grow(contactBoundsMargin);
        for(int y=0;y<Height;y++)for(int x=0;x<Width;x++){
            int i=y*Width+x;float u=x/(float)Columns,v=y/(float)Rows;
            rays[i]=PuppetVolumeFur.RootRay(u,v);ends[i]=PuppetVolumeFur.ProxyExtent(u,v);
            originalEnds[i]=renderer.OriginalEnd(rays[i]);
        }
        query=new(){Shape=probe,CollisionMask=ContactLayer,CollideWithAreas=false,CollideWithBodies=true,Margin=0};
        RefreshGuides();
        uploadImage=Image.CreateFromData(Width,Height,false,Image.Format.Rgbaf,bytes);
        Field=ImageTexture.CreateFromImage(uploadImage);
        foreach(var material in materials){material.SetShaderParameter("motion_field",Field);material.SetShaderParameter("motion_grid",new Vector2(Columns,Rows));material.SetShaderParameter("motion_enabled",true);}
        Hair.CutCommitted+=OnCutCommitted;
    }
    void OnCutCommitted(Vector3 worldPoint,Vector3 worldNormal,float radiusLocal,float removedMass)
    {
        CutEvents++;LastCutWorldPoint=worldPoint;LastCutWorldDirection=-worldNormal.Normalized();LastCutRemovedMass=removedMass;LastCutLocalGuides=0;LastCutGuide=-1;
        frameTransform=Hair.Root.GlobalTransform;
        // A cut may remove a guide or the entire renderer. Refresh first so
        // removed material never receives a kick or reappears on recovery.
        if(revision!=Hair.Volume.Revision)RefreshGuides();
        if(!ResponseEnabled||!CutFeedbackEnabled||ActiveGuides==0)return;
        var point=frameTransform.AffineInverse()*worldPoint;
        var direction=(frameTransform.Basis.Inverse()*LastCutWorldDirection).Normalized();
        float support=Math.Max(.16f,radiusLocal+.11f);
        float strength=.25f+.75f*(1-MathF.Exp(-removedMass/1.5f));
        float strongest=-1;
        for(int i=0;i<Active.Length;i++)if(Active[i]){
            var root=PuppetVolumeFur.RootCenter+rays[i]*.98f;
            var segment=RestTips[i]-root;
            float along=Math.Clamp((point-root).Dot(segment)/Math.Max(segment.LengthSquared(),1e-8f),.15f,1);
            float distance=(root+segment*along).DistanceTo(point);
            float local=Math.Clamp(1-distance/support,0,1);local=local*local*(3-2*local);
            if(local>.05f)LastCutLocalGuides++;
            // Cartoon cut recoil keeps a response floor for surviving short
            // hair: length-only scaling made Trimmed almost imperceptible.
            // The local tap dominates a small whole-head follow. Root pinning
            // and the displacement cap still apply to the existing curves.
            float weight=(.08f+.92f*local)*(.55f+.45f*MathF.Sqrt(TipWeights[i]));
            if(weight>strongest){strongest=weight;LastCutGuide=i;}
            Velocities[i]=(Velocities[i]+direction*(1.65f*strength*weight)).LimitLength(MaxCutVelocity);
            cutSpringTime[i]=2.2f;
        }
        CutImpulses++;CutAge=0;
    }
    void RefreshGuides()
    {
        var watch=Stopwatch.StartNew();ActiveGuides=0;
        for(int i=0;i<rays.Length;i++){
            float end=PuppetVolumeFur.TraceExtent(Hair.Volume,rays[i]);
            Active[i]=end>1.04f;
            probeMask[i]=0;
            if(!Active[i]){Offsets[i]=Velocities[i]=Vector3.Zero;cutSpringTime[i]=0;TipWeights[i]=0;continue;}
            float originalEnd=originalEnds[i];
            bool originalTip=Hair.Settings.BundleField&&end>=originalEnd-.002f/rays[i].Length();
            // Probe the conservative original tip envelope. A trimmed guide
            // uses the shortened endpoint and receives no new outer coat.
            float r=end+(originalTip?.040f:-.002f)/rays[i].Length();
            RestTips[i]=PuppetVolumeFur.RootCenter+rays[i]*r;
            for(int sample=0;sample<ContactSamples.Length;sample++)if(ContactSamples[sample]==1||PuppetVolumeFur.SampleExact(Hair.Volume,ProbeRest(i,ContactSamples[sample]))>0)probeMask[i]|=(byte)(1<<sample);
            float h=Math.Clamp((r-.98f)/Math.Max(.02f,ends[i]-.98f),0,1);
            float oldWeight=TipWeights[i];TipWeights[i]=h*h;
            if(oldWeight>0&&TipWeights[i]<oldWeight){float ratio=TipWeights[i]/oldWeight;Offsets[i]*=ratio;Velocities[i]*=ratio;}
            ActiveGuides++;
        }
        revision=Hair.Volume.Revision;LastProbeRefreshMs=watch.Elapsed.TotalMilliseconds;
        BoundsContainFullMotion=Hair.Core.Mesh.GetSurfaceCount()==0||motionBounds.Encloses(Hair.Core.Mesh.GetAabb().Grow(.040f+MaxOffset+contactBoundsMargin));
    }
    public int NearestGuide(Vector3 point)
    {
        int best=-1;float distance=float.PositiveInfinity;
        for(int i=0;i<Active.Length;i++)if(Active[i]&&RestTips[i].DistanceSquaredTo(point)<distance){best=i;distance=RestTips[i].DistanceSquaredTo(point);}
        return best;
    }
    public Vector3 TipWorld(int i)=>frameTransform*(RestTips[i]+Offsets[i]);
    Vector3 ProbeRest(int i,float along)=>(PuppetVolumeFur.RootCenter+rays[i]*.98f).Lerp(RestTips[i],along);
    public bool ProbeActive(int i,float along)=>(probeMask[i]&(along==.5f?1:along==.75f?2:4))!=0;
    public Vector3 ProbeWorld(int i,float along)=>frameTransform*(ProbeRest(i,along)+Offsets[i]*(along*along));
    Vector3 BoundContact(Vector3 value,Vector3 normal,float required)
    {
        // The push-out already satisfies the contact plane. Do not turn a
        // sub-ulp dot-product disagreement into a jump to the motion limit.
        if(value.LengthSquared()<=MaxOffset*MaxOffset)return value;
        var bounded=value.LimitLength(MaxOffset);
        if(bounded.Dot(normal)>=required-1e-7f)return bounded;
        BoundedContacts++;MaxRequiredContactOffset=Math.Max(MaxRequiredContactOffset,required);
        if(required>MaxOffset){InfeasibleContactPlanes++;return normal*MaxOffset;}
        // Intersect the engine contact half-space with the displacement ball.
        // A plain radial clamp after push-out re-entered the collider. Keep
        // the contact-plane component and use the remaining tangential room.
        var tangent=value-normal*value.Dot(normal);
        if(tangent.LengthSquared()<1e-10f)tangent=normal.Cross(Math.Abs(normal.Y)<.9f?Vector3.Up:Vector3.Right);
        return normal*required+tangent.Normalized()*MathF.Sqrt(Math.Max(0,MaxOffset*MaxOffset-required*required));
    }
    public void Step(float dt,Vector3 windWorld,PhysicsDirectSpaceState3D space,Vector3? sphereCenter=null,float sphereRadius=0,bool contactActive=true)
    {
        timer.Restart();LastProbeRefreshMs=0;frameTransform=Hair.Root.GlobalTransform;
        if(revision!=Hair.Volume.Revision)RefreshGuides();
        dt=Math.Clamp(dt,.0001f,.05f);Contacts=Queries=BoundedContacts=InfeasibleContactPlanes=0;LastCollider=0;MaxTipOffset=MaxTipSpeed=MaxContactCorrection=MaxRequiredContactOffset=0;float sum=0;
        CutAge+=dt;
        var inverse=frameTransform.Basis.Inverse();var wind=inverse*windWorld;
        bool canContact=contactActive&&ActiveGuides>0;
        if(sphereCenter.HasValue){
            float sphereLocalRadius=(sphereRadius+ProbeRadius)*MathF.Sqrt(inverse.X.LengthSquared()+inverse.Y.LengthSquared()+inverse.Z.LengthSquared());
            canContact&=motionBounds.Grow(sphereLocalRadius).HasPoint(frameTransform.AffineInverse()*sphereCenter.Value);
        }
        var contactNormals=frameTransform.Basis.Transposed();
        float broadRadius=sphereRadius+ProbeRadius+.001f,broadRadiusSquared=broadRadius*broadRadius;
        Span<float> decay=stackalloc float[14],cosine=stackalloc float[14],sine=stackalloc float[14];
        for(int band=0;band<14;band++){
            float z=band>=7?CutDamping:.72f,w=(band>=7?CutFrequency:13f)+(band%7)*.7f,wd=w*MathF.Sqrt(1-z*z);
            decay[band]=MathF.Exp(-z*w*dt);cosine[band]=MathF.Cos(wd*dt);sine[band]=MathF.Sin(wd*dt)/wd;
        }
        for(int i=0;i<Active.Length;i++){
            if(!Active[i])continue;
            var n=rays[i].Normalized();var target=(wind-n*wind.Dot(n))*(.065f*TipWeights[i]);
            if(target.Length()>MaxOffset)target=target.Normalized()*MaxOffset;
            if(ResponseEnabled){
                // Exact underdamped linear spring update for a constant target
                // over this physics tick. No frame-dependent Euler explosion.
                bool cutSpring=cutSpringTime[i]>0;int band=i%7+(cutSpring?7:0);
                float w=(cutSpring?CutFrequency:13f)+(i%7)*.7f,z=cutSpring?CutDamping:.72f,e=decay[band],c=cosine[band],s=sine[band];
                var y=Offsets[i]-target;var velocity=Velocities[i];
                Offsets[i]=target+e*(y*c+(velocity+y*(z*w))*s);
                Velocities[i]=e*(velocity*c-(velocity*(z*w)+y*(w*w))*s);
            }else Offsets[i]=Velocities[i]=Vector3.Zero;
            cutSpringTime[i]=Math.Max(0,cutSpringTime[i]-dt);
            // Match the shader's root-fixed quadratic bend, including its
            // occupied middle. Tip-only queries missed visible stem contact.
            if(canContact)foreach(float along in ContactSamples){
            if(!ProbeActive(i,along))continue;
            var probeWorld=ProbeWorld(i,along);
            // Optional exact bound for the one diagnostic sphere. This only
            // rejects impossible pairs; the engine still supplies contacts.
            if(sphereCenter.HasValue&&probeWorld.DistanceSquaredTo(sphereCenter.Value)>broadRadiusSquared)continue;
            query.Transform=new Transform3D(Basis.Identity,probeWorld);Queries++;
            var hit=space.GetRestInfo(query);
            if(hit.Count>0){
                Contacts++;LastCollider=hit["collider_id"].AsUInt64();
                var normal=hit["normal"].AsVector3();var point=hit["point"].AsVector3();
                float push=Math.Max(0,(point+normal*(ProbeRadius+.0003f)-probeWorld).Dot(normal));
                MaxContactCorrection=Math.Max(MaxContactCorrection,push);
                if(ResponseEnabled){
                    var transformedNormal=contactNormals*normal;float normalScale=transformedNormal.Length();
                    var localNormal=transformedNormal/normalScale;
                    float required=Offsets[i].Dot(localNormal)+push/(normalScale*along*along);
                    Offsets[i]=BoundContact(Offsets[i]+localNormal*(push/(normalScale*along*along)),localNormal,required);
                    float vn=Velocities[i].Dot(localNormal);
                    if(vn<0)Velocities[i]-=localNormal*vn;
                }
            }
            }
            float length=Offsets[i].Length();
            if(length>MaxOffset){var direction=Offsets[i]/length;Offsets[i]=direction*MaxOffset;float outward=Velocities[i].Dot(direction);if(outward>0)Velocities[i]-=direction*outward;}
            var displacement=frameTransform.Basis*Offsets[i];
            MaxTipOffset=Math.Max(MaxTipOffset,displacement.Length());sum+=displacement.LengthSquared();
            MaxTipSpeed=Math.Max(MaxTipSpeed,(frameTransform.Basis*Velocities[i]).Length());
            Finite&=float.IsFinite(length)&&float.IsFinite(Velocities[i].LengthSquared());
        }
        RmsTipOffset=ActiveGuides>0?MathF.Sqrt(sum/ActiveGuides):0;
        // Equal values at the periodic seam. These are fixed guide identities,
        // not freshly generated roots after each upload or cut.
        for(int y=0;y<Height;y++){Offsets[y*Width+Columns]=Offsets[y*Width];Velocities[y*Width+Columns]=Velocities[y*Width];cutSpringTime[y*Width+Columns]=cutSpringTime[y*Width];}
        // RGB is actual current endpoint deflection. A is its rest height,
        // so cutting shortens the bending interval instead of reducing the
        // collision correction to nearly zero at a still-visible cut tip.
        for(int i=0;i<Offsets.Length;i++){pixels[i*4]=Offsets[i].X;pixels[i*4+1]=Offsets[i].Y;pixels[i*4+2]=Offsets[i].Z;pixels[i*4+3]=MathF.Sqrt(TipWeights[i]);}
        Buffer.BlockCopy(pixels,0,bytes,0,bytes.Length);
        uploadImage.SetData(Width,Height,false,Image.Format.Rgbaf,bytes);Field.Update(uploadImage);
        foreach(var material in materials){
            material.SetShaderParameter("motion_contact_enabled",ResponseEnabled&&canContact&&sphereCenter.HasValue);
            material.SetShaderParameter("motion_contact_max_push",MaxContactPushWorld);
            if(canContact&&sphereCenter.HasValue){var p=sphereCenter.Value;material.SetShaderParameter("motion_contact_sphere",new Vector4(p.X,p.Y,p.Z,sphereRadius));}
        }
        LastStepMs=timer.Elapsed.TotalMilliseconds;
    }
    public void Dispose()
    {
        Hair.CutCommitted-=OnCutCommitted;
        foreach(var material in materials){material.SetShaderParameter("motion_enabled",false);material.SetShaderParameter("motion_contact_enabled",false);material.SetShaderParameter("motion_field",default(Variant));}
        // An ordinary cut-to-empty frees the render nodes and clears
        // Hair.VolumeFur before this controller is retired on reset. Restore
        // only the renderer we actually owned, if its nodes are still alive.
        if(GodotObject.IsInstanceValid(renderer.Node))renderer.Node.CustomAabb=baseBounds;
        if(renderer.Fins!=null&&GodotObject.IsInstanceValid(renderer.Fins))renderer.Fins.CustomAabb=finBaseBounds;
        Field.Dispose();uploadImage.Dispose();query.Dispose();probe.Dispose();
    }
}
