using Godot;
using System;
using System.Collections.Generic;
using static Hairball.TargetLabGeometry;
namespace Hairball;

// Original B2 toy shapes. No Snow assets or production actor nodes.
public static class PuppetLabModels
{
    public static bool FineSurfaceSampling {get;set;}
    static MeshInstance3D Ball(Node3D parent,Vector3 at,Vector3 size,Material material)
    {
        var mesh=Mesh(parent,new SphereMesh{Radius=.5f,Height=1,RadialSegments=FineSurfaceSampling?96:32,Rings=FineSurfaceSampling?64:20},at,material);mesh.Scale=size;return mesh;
    }
    public static StandardMaterial3D Skin=Mat("c3a18b",.94f),Nose=Mat("b97664",.96f),Cream=Mat("b8bca2",.96f),Teal=Mat("36666b",.94f),Dark=Mat("24202e",.52f),White=Mat("f3dfbc",.59f),Mouth=Mat("321a29",.98f),Tongue=Mat("b76172",.95f),Brow=Mat("453344",.97f);
    public static ArrayMesh Surface(Func<float,float,Vector3> point,int rings,int sides,bool closed=false)
    {
        if(FineSurfaceSampling){rings*=2;sides*=2;}
        var vs=new List<Vector3>();var ns=new List<Vector3>();var ix=new List<int>();
        for(int j=0;j<=rings;j++)for(int i=0;i<=sides;i++)
        {float t=j/(float)rings,a=i*MathF.Tau/sides;var p=point(t,a);vs.Add(p);var du=point(Math.Min(1,t+.0005f),a)-point(Math.Max(0,t-.0005f),a);var dv=point(t,a+.0005f)-point(t,a-.0005f);var n=du.Cross(dv).Normalized();ns.Add(n);}
        for(int j=0;j<rings;j++)for(int i=0;i<sides;i++){int a=j*(sides+1)+i,b=a+1,c=a+sides+1,d=c+1;ix.AddRange([a,b,c,b,d,c]);}
        return Array(vs.ToArray(),ns.ToArray(),ix.ToArray());
    }
    public static MeshInstance3D Tube(Node3D p,Vector3[] path,float radius,Material mat,int sides=10)
    {
        var vs=new List<Vector3>();var ns=new List<Vector3>();var ix=new List<int>();
        for(int j=0;j<path.Length;j++)
        {var tangent=(path[Math.Min(j+1,path.Length-1)]-path[Math.Max(0,j-1)]).Normalized();var u=tangent.Cross(Math.Abs(tangent.Z)<.9f?Vector3.Back:Vector3.Up).Normalized();var v=tangent.Cross(u);for(int i=0;i<=sides;i++){var n=u*MathF.Cos(i*MathF.Tau/sides)+v*MathF.Sin(i*MathF.Tau/sides);vs.Add(path[j]+radius*n);ns.Add(n);}}
        for(int j=0;j<path.Length-1;j++)for(int i=0;i<sides;i++){int a=j*(sides+1)+i,b=a+1,c=a+sides+1,d=c+1;ix.AddRange([a,b,c,b,d,c]);}
        // Explicit normals/winding independent of the path orientation.
        for(int i=0;i<ix.Count;i+=3)if((vs[ix[i+1]]-vs[ix[i]]).Cross(vs[ix[i+2]]-vs[ix[i]]).Dot(ns[ix[i]])>0)(ix[i+1],ix[i+2])=(ix[i+2],ix[i+1]);
        return Mesh(p,Array(vs.ToArray(),ns.ToArray(),ix.ToArray()),Vector3.Zero,mat);
    }
    public static Node3D Face(Node3D parent,Vector3 at,float size=1,int expression=0,bool friend=false,bool production=false)
    {
        var f=new Node3D{Name="OriginalPuppetFace",Position=at,Scale=Vector3.One*size};parent.AddChild(f);
        float open=expression==1?.110f:expression==2?.195f:expression>=3?.045f:.115f;
        float mouthY=expression>=3?-.20f:-.185f;
        float MouthY(float a)=>mouthY+open*MathF.Sin(a)+(expression==1?.038f:expression==2?-.01f:.012f)*MathF.Cos(a)*MathF.Cos(a);
        Vector3 FacePoint(float t,float a)
        {
            float c=MathF.Cos(a),s=MathF.Sin(a),width=.46f*(1+.08f*MathF.Sin(a)-.025f*MathF.Cos(a*3));
            float x=Mathf.Lerp(.262f*c,width*c,t),y=Mathf.Lerp(MouthY(a),.46f*s,t);
            float z=.355f*MathF.Sqrt(Math.Max(0,1-t*t))-.048f*MathF.Exp(-t*12);
            z+=.030f*MathF.Sin(t*MathF.PI)*MathF.Cos(a*2)+.009f*MathF.Sin(x*16+y*7)*MathF.Sin(t*MathF.PI);
            return new(x,y,z);
        }
        var skin=(StandardMaterial3D)Skin.Duplicate();skin.CullMode=BaseMaterial3D.CullModeEnum.Disabled;
        Mesh(f,Surface(FacePoint,40,96),Vector3.Zero,skin).Name="FaceSurface";
        // Back hemisphere closes the handmade head without covering the mouth aperture.
        var back=Surface((t,a)=>{float q=t*MathF.PI*.5f;return new(.465f*MathF.Cos(a)*MathF.Cos(q),.46f*MathF.Sin(a)*MathF.Cos(q),-.33f*MathF.Sin(q));},32,72);
        Mesh(f,back,Vector3.Zero,skin);
        var cavity=(StandardMaterial3D)Mouth.Duplicate();cavity.CullMode=BaseMaterial3D.CullModeEnum.Disabled;
        Mesh(f,Surface((t,a)=>new(.262f*MathF.Cos(a)*(1-t),Mathf.Lerp(MouthY(a),mouthY,t),.316f-.18f*MathF.Sin(t*MathF.PI*.5f)),20,64),Vector3.Zero,cavity).Name="MouthSurface";
        Ball(f,new(0,mouthY-open*.57f,.295f),new(.235f,open*.68f,.067f),Tongue);
        if(expression!=3){var tooth=Round(f,new(-.020f,mouthY+open-.024f,.318f),new(.090f,.048f,.023f),.013f,White);tooth.RotationDegrees=new(0,0,-6);}
        for(int side=-1;side<=1;side+=2)
        {
            Ball(f,new(side*.46f,-.015f,-.012f),new(.17f,.245f,.15f),Skin);
            Ball(f,new(side*.475f,-.015f,.062f),new(.085f,.14f,.029f),Nose);
            float squint=expression==4?.57f:expression==3?.81f:1;
            float ey=.119f+(side<0?.017f:0);float es=side<0?1.025f:.955f;
            Ball(f,new(side*.194f,ey-.007f,.342f),new(.267f*es,.283f*squint,.070f),Skin);
            var center=new Vector3(side*.194f,ey,.367f);var eyes=new Node3D{Name=side<0?"EyeAssemblyLeft":"EyeAssemblyRight",Position=center};f.AddChild(eyes);
            var eye=Ball(eyes,Vector3.Zero,new(.245f*es,.270f*squint,.13f),White);eye.RotationDegrees=new(0,side*7,-side*8);eye.Name=side<0?"EyeLeft":"EyeRight";
            float px=side*.186f-.014f,py=ey+.010f;
            var gaze=new Node3D{Name="Gaze"};eyes.AddChild(gaze);
            Ball(gaze,new Vector3(px,py,.425f)-center,new(.108f,.127f*squint,.018f),Mat("665447",.43f));
            var pupil=Ball(gaze,new Vector3(px-.006f,py,.434f)-center,new(.082f,.104f*squint,.019f),Dark);pupil.Name=side<0?"PupilLeft":"PupilRight";
            Ball(gaze,new Vector3(px-.024f,py+.031f,.446f)-center,new(.026f,.032f,.007f),White);
            Ball(gaze,new Vector3(px+.018f,py-.018f,.446f)-center,new(.009f,.011f,.006f),White);
            var path=new Vector3[13];for(int i=0;i<13;i++){float t=i/12f;float mood=expression==3?-.075f+.13f*t:expression==4?-.040f:expression==2?.055f:0;path[i]=new(side*(.09f+t*.235f),.309f+.045f*MathF.Sin(t*MathF.PI)+(side<0?.025f:0)+mood,.283f+.028f*MathF.Sin(t*MathF.PI));}
            if(!production)Tube(f,path,.027f,Brow);
            // Flat, softly colored cheek patch rather than a separate spherical muzzle.
            Ball(f,new(side*.325f,-.052f,.283f),new(.100f,.066f,.017f),Mat("c39780",1));
            for(int dot=0;dot<3;dot++)Ball(f,new(side*(.296f+.023f*dot),-.034f-(dot%2)*.021f,.303f-dot*.011f),new(.010f,.008f,.004f),Mat("9d624b",1));
        }
        var nose=Ball(f,new(-.008f,-.003f,.419f),new(.165f,.140f,.145f),Nose);nose.RotationDegrees=new(0,0,-8);
        // Tiny stitched darts and an imperfect seam are construction detail, not body fur.
        for(int s=-1;s<=1;s+=2)for(int i=0;i<9;i++){float y=-.25f+i*.045f;var path=new[]{new Vector3(s*.425f,y,.105f),new Vector3(s*.433f,y+.012f,.114f)};Tube(f,path,.0019f,Mat("d3a378",1),5);}
        return f;
    }
    public static Node3D Customer(Node3D parent,int expression=0)
    {
        var n=new Node3D{Name="HeroCustomer"};parent.AddChild(n);
        TargetLabAssets.Add(n,"polyhaven_barber_chair",new(0,0,-.12f),new(1.5f,1.13f,1.13f));
        // Seated 2.55m visual height including hair: broad head, short quiet body.
        for(int s=-1;s<=1;s+=2)
        {
            Limb(n,new(s*.20f,.77f,.25f),new(s*.22f,.38f,.57f),.115f,Teal);
            Round(n,new(s*.22f,.29f,.64f),new(.29f,.22f,.42f),.105f,Mat("796c61"));
            Limb(n,new(s*.30f,1.34f,0),new(s*.55f,1.04f,.20f),.128f,Cream);
            Limb(n,new(s*.55f,1.04f,.20f),new(s*.57f,1.07f,.40f),.095f,Skin);
            Hand(n,new(s*.57f,1.085f,.38f),s,.60f,false);
        }
        Cape(n,Cream);Cylinder(n,new(0,1.548f,.0f),.19f,.08f,Teal);
        // Sewn collar, binding and a small tactile patch identify real cloth.
        var collar=new Vector3[65];for(int i=0;i<collar.Length;i++){float a=i*MathF.Tau/64;collar[i]=new(.197f*MathF.Cos(a),1.544f,.04f+.16f*MathF.Sin(a));}Tube(n,collar,.016f,Teal);
        for(int s=-1;s<=1;s+=2){var seam=new Vector3[25];for(int i=0;i<seam.Length;i++){float t=i/24f;seam[i]=new(s*(.12f+.24f*t),1.47f-.63f*t,.215f+.265f*MathF.Sin(t*MathF.PI*.5f));}Tube(n,seam,.0033f,Mat("e2cc92",.96f),5);}
        Face(n,new(0,1.97f,.035f),1,expression);
        // Small woven-style tag on the cape; unobtrusive and part of the physical garment.
        var badge=Round(n,new(.265f,1.13f,.448f),new(.12f,.14f,.020f),.022f,Teal);badge.RotationDegrees=new(0,0,-8);
        for(int i=0;i<7;i++)Tube(n,[new(.217f+i*.016f,1.184f,.462f),new(.221f+i*.016f,1.176f,.465f)],.0018f,Cream,5);
        return n;
    }
    public static Node3D Hand(Node3D p,Vector3 at,int side,float scale=1,bool grip=true)
    {
        var h=new Node3D{Position=at,Scale=Vector3.One*scale};p.AddChild(h);
        Ball(h,Vector3.Zero,new(.23f,.26f,.17f),Skin);
        for(int i=0;i<3;i++)
        {float y=.074f-i*.071f;var path=new Vector3[13];for(int j=0;j<13;j++){float t=j/12f;path[j]=new(Mathf.Lerp(-side*.067f,side*.063f,t),y-.021f*MathF.Sin(t*MathF.PI),.047f+.059f*MathF.Sin(t*MathF.PI));}Tube(h,path,.030f,Skin);Ball(h,path[^1],Vector3.One*.060f,Skin);}
        var thumb=Ball(h,new(-side*.096f,.030f,.064f),new(.10f,.15f,.10f),Skin);thumb.RotationDegrees=new(0,0,side*24);
        return h;
    }
    public static Node3D Hands(Node3D camera,int round)
    {
        var root=new Node3D{Name="FirstPersonHandsAndClipper"};camera.AddChild(root);
        var right=new Node3D{Position=round>=3?new(.43f,-.31f,-.77f):new(.40f,-.26f,-.70f),RotationDegrees=new(-8,-15,-19),Scale=Vector3.One*(round>=3?.86f:1)};root.AddChild(right);
        Limb(right,new(.11f,-.43f,.21f),new(.045f,-.12f,.05f),.093f,Cream);
        Cylinder(right,new(.03f,-.13f,.035f),.102f,.105f,Teal);
        for(int i=0;i<14;i++){float a=i*MathF.Tau/14;Tube(right,[new(.03f+.104f*MathF.Cos(a),-.152f,.035f+.104f*MathF.Sin(a)),new(.03f+.104f*MathF.Cos(a),-.141f,.035f+.104f*MathF.Sin(a))],.0022f,Cream,5);}
        TargetLabActors.Clipper(right,new(0,.106f,-.038f),1.12f);
        Hand(right,new(.037f,-.055f,.069f),1,.85f);
        var left=new Node3D{Position=round>=3?new(-.48f,-.38f,-.80f):new(-.43f,-.32f,-.74f),RotationDegrees=new(-12,14,27),Scale=Vector3.One*(round>=3?.87f:1)};root.AddChild(left);
        Limb(left,new(-.10f,-.35f,.24f),new(0,-.08f,.04f),.093f,Cream);Hand(left,new(0,-.025f,0),-1,.85f);
        Round(left,new(0,.17f,-.01f),new(.08f,.32f,.037f),.027f,Teal);
        for(int i=0;i<12;i++)Round(left,new(.049f,.04f+i*.024f,-.01f),new(.105f,.014f,.038f),.006f,Teal);
        return root;
    }
    public static void Friend(Node3D p)
    {
        var f=new Node3D{Name="PearShapedStylist",Position=new(-1.18f,0,-.45f),RotationDegrees=new(0,18,0)};p.AddChild(f);
        var coral=Mat("b46278",.96f);var blue=Mat("36575a",.95f);
        for(int s=-1;s<=1;s+=2){Limb(f,new(s*.14f,.66f,0),new(s*.15f,.17f,.03f),.10f,blue);Round(f,new(s*.15f,.11f,.08f),new(.24f,.17f,.32f),.076f,Dark);}
        Ball(f,new(0,1.00f,0),new(.60f,.86f,.44f),coral);
        Round(f,new(0,.94f,.20f),new(.46f,.53f,.11f),.05f,blue);
        Round(f,new(0,.96f,.265f),new(.22f,.13f,.025f),.01f,Mat("627b79"));
        for(int s=-1;s<=1;s+=2){Tube(f,[new(s*.20f,.75f,.265f),new(s*.20f,1.12f,.265f),new(s*.17f,1.33f,.10f)],.012f,Mat("bfaa75",.96f));Ball(f,new(s*.15f,1.145f,.272f),Vector3.One*.031f,Mat("bfaa75",.6f));}
        // Different head, almond eyes, little muzzle and two folded felt ears.
        Ball(f,new(0,1.66f,0),new(.69f,.79f,.57f),coral);
        for(int s=-1;s<=1;s+=2)
        {
            var ear=Ball(f,new(s*.24f,2.025f,-.01f),new(.19f,.31f,.14f),coral);ear.RotationDegrees=new(0,0,-s*25);
            Ball(f,new(s*.13f,1.75f,.256f),new(.20f,.23f,.069f),White);Ball(f,new(s*.12f+.01f,1.76f,.288f),new(.068f,.091f,.030f),Dark);
        }
        Ball(f,new(0,1.595f,.288f),new(.37f,.245f,.09f),Mat("e0a08f"));Ball(f,new(0,1.63f,.346f),new(.12f,.095f,.045f),Dark);
        Ball(f,new(0,1.49f,.300f),new(.21f,.112f,.033f),Mouth);
        Limb(f,new(.28f,1.22f,0),new(.45f,1.65f,.44f),.09f,coral);Limb(f,new(.45f,1.65f,.44f),new(.52f,1.91f,.93f),.075f,coral);Ball(f,new(.52f,1.91f,.93f),new(.17f,.18f,.14f),coral);
        var clip=TargetLabActors.Clipper(f,new(.50f,2.03f,.91f),.8f);clip.Name="StylistTool";clip.RotationDegrees=new(0,0,28);
        var spray=new Node3D{Name="RescueSprayer",Position=new(.50f,2.02f,.91f),RotationDegrees=new(0,0,-20),Visible=false};f.AddChild(spray);
        Cylinder(spray,Vector3.Zero,.085f,.31f,Mat("ac422c",.6f));Cylinder(spray,new(0,.02f,0),.088f,.12f,Mat("dec791",.88f));Cylinder(spray,new(0,.17f,0),.057f,.045f,Teal);
        Tube(spray,[new(0,.2f,0),new(.08f,.22f,0),new(.15f,.22f,0)],.022f,Teal);Round(spray,new(-.03f,.208f,0),new(.09f,.025f,.05f),.009f,Dark);
        Limb(f,new(-.26f,1.26f,0),new(-.35f,.97f,.19f),.09f,coral);
    }
}
