using Godot;
using System;
using static Hairball.TargetLabGeometry;
namespace Hairball;

public static class TargetLabActors
{
    public static readonly StandardMaterial3D Skin=Mat("dfa88a",.76f),Cream=Mat("c5b9a4",.95f),Teal=Mat("264e52",.66f),Rose=Mat("83474b",.74f),Brass=Mat("b49461",.38f,.65f),Dark=Mat("252d30",.9f),White=Mat("e2d7c3"),Mouth=Mat("65352e");
    public static Node3D Face(Node3D parent,Vector3 center,float scale,Material hair,bool customer)
    {
        var f=new Node3D{Position=center,Scale=Vector3.One*scale};parent.AddChild(f);
        Ball(f,new(0,-.015f,0),new(.79f,.86f,.69f),Skin);
        Ball(f,new(0,-.23f,.075f),new(.61f,.42f,.60f),Skin);
        for(int side=-1;side<=1;side+=2)
        {
            Ball(f,new(side*.39f,-.07f,0),new(.13f,.25f,.16f),Skin);
            Ball(f,new(side*.405f,-.07f,.073f),new(.06f,.13f,.04f),Mat("b77c60"));
            float gaze=customer?-.02f:.015f,up=customer?.073f:.061f;
            Ball(f,new(side*.175f,.045f,.313f),new(.223f,.161f,.064f),White);
            Ball(f,new(side*.171f+gaze,up,.345f),new(.084f,.106f,.027f),Mat("40534c"));
            Ball(f,new(side*.171f+gaze,up,.357f),new(.044f,.071f,.014f),Dark);
            Ball(f,new(side*.160f+gaze,up+.017f,.367f),new(.017f,.024f,.009f),White);
            Curve(f,[new(side*.28f,.179f,.30f),new(side*.19f,.21f+(customer&&side<0?.04f:0),.34f),new(side*.09f,.19f,.33f)],.025f,hair);
        }
        Ball(f,new(0,-.055f,.371f),new(.165f,.14f,.13f),Skin);
        Ball(f,new(0,-.225f,.358f),new(.34f,customer?.17f:.115f,.082f),Mat("472c29"));
        Round(f,new(0,-.187f,.405f),new(.27f,.040f,.016f),.012f,White);
        Ball(f,new(0,-.278f,.393f),new(.15f,.032f,.018f),Mat("bd8273"));
        for(int s=-1;s<=1;s+=2)Ball(f,new(s*.15f,-.185f,.369f),new(.038f,.041f,.032f),Mouth);
        return f;
    }
    public static void Customer(Node3D p,Material hair)
    {
        // Matching generously rounded upholstery and cape; the face is deliberately uncovered.
        Cylinder(p,new(0,.075f,-.08f),.58f,.13f,Brass);
        Cylinder(p,new(0,.33f,-.08f),.16f,.5f,Dark);
        Round(p,new(0,.63f,-.08f),new(1.04f,.22f,.92f),.105f,Rose);
        Round(p,new(0,1.08f,-.40f),new(.99f,.99f,.23f),.11f,Rose);
        Round(p,new(0,1.61f,-.38f),new(.55f,.25f,.21f),.10f,Rose);
        for(int s=-1;s<=1;s+=2)
        {
            Limb(p,new(s*.51f,.65f,.2f),new(s*.51f,.97f,.20f),.045f,Brass);
            Round(p,new(s*.52f,1.01f,.07f),new(.16f,.15f,.77f),.07f,Rose);
            Limb(p,new(s*.23f,.69f,.35f),new(s*.23f,.27f,.65f),.13f,Dark);
            Round(p,new(s*.23f,.21f,.78f),new(.27f,.20f,.49f),.09f,Dark);
            Ball(p,new(s*.50f,1.075f,.31f),new(.20f,.10f,.26f),Skin);
        }
        Round(p,new(0,.23f,.68f),new(.91f,.065f,.34f),.03f,Brass);
        Cape(p,Cream);
        Cylinder(p,new(0,1.57f,.005f),.19f,.065f,Dark);
        Face(p,new(0,1.94f,0),1,hair,true);
    }
    public static Node3D Clipper(Node3D p,Vector3 at,float scale=1)
    {
        var t=new Node3D{Position=at,Scale=Vector3.One*scale};p.AddChild(t);
        Round(t,Vector3.Zero,new(.17f,.34f,.15f),.067f,Teal);
        Round(t,new(0,.17f,-.022f),new(.20f,.055f,.16f),.017f,Brass);
        for(int i=0;i<9;i++)Round(t,new((i-4)*.021f,.205f,-.044f),new(.013f,.047f,.11f),.005f,White);
        Round(t,new(0,.025f,.079f),new(.054f,.061f,.013f),.011f,Brass);
        for(int i=0;i<3;i++)Round(t,new(0,-.075f-i*.026f,.076f),new(.09f,.009f,.012f),.004f,Dark);
        return t;
    }
    public static void Teammate(Node3D p,Vector3 at,float yaw,Material hair,bool right,bool giant)
    {
        var a=new Node3D{Position=at,RotationDegrees=new(0,yaw,0)};p.AddChild(a);
        Ball(a,new(0,1.05f,0),new(.63f,.80f,.43f),Cream);
        Round(a,new(0,.91f,.19f),new(.51f,.67f,.13f),.063f,Teal);
        Round(a,new(0,.92f,.27f),new(.24f,.18f,.045f),.02f,Mat("3e6866"));
        for(int s=-1;s<=1;s+=2)
        {
            Limb(a,new(s*.17f,.74f,0),new(s*.20f,.17f,.025f),.10f,Dark);
            Round(a,new(s*.20f,.12f,.10f),new(.24f,.21f,.39f),.10f,Dark);
            Limb(a,new(s*.15f,1.42f,.13f),new(s*.18f,1.08f,.235f),.025f,Teal);
        }
        Face(a,new(0,1.70f,0),.78f,hair,false);
        Ball(a,new(0,1.92f,-.055f),new(.66f,.37f,.54f),hair);
        Ball(a,new(-.10f,2.01f,.055f),new(.44f,.24f,.41f),hair);
        // Raised elbows/hands direct attention into the shared customer instead of hiding the face.
        var shoulder=new Vector3(right?-.28f:.28f,1.28f,.01f);
        var elbow=a.ToLocal(right?new(1.55f,1.77f,.18f):giant?new(-1.05f,2.84f,.2f):new(-.95f,1.87f,.13f));
        var hand=a.ToLocal(right?new(.93f,giant?2.49f:2.19f,.38f):giant?new(-.56f,3.16f,.66f):new(-.49f,2.18f,.50f));
        Limb(a,shoulder,elbow,.115f,Cream);Limb(a,elbow,hand,.075f,Skin);Ball(a,hand,new(.15f,.19f,.15f),Skin);
        var clip=Clipper(a,hand+new Vector3(0,.13f,-.015f),.85f);clip.RotationDegrees=new(right?-15:-45,0,right?-50:20);
        Limb(a,-shoulder+new Vector3(0,2.56f,0),new(right?.51f:-.51f,.97f,.18f),.10f,Cream);
        Limb(a,new(right?.51f:-.51f,.97f,.18f),new(right?.35f:-.35f,.81f,.38f),.075f,Skin);
    }
}
