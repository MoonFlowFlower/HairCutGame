using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using NVector=System.Numerics.Vector3;

namespace Hairball;
public static class Art
{
    public static readonly Color[] Team=[new("ed785f"),new("59b9d1"),new("93c970"),new("dca8dc")];
    public static readonly Color[] ToolColors=[new("ced8dd"),new("80dc68"),new("dbb66e"),new("58b0b0"),new("efad57"),new("8ee5ff"),new("ef8151"),new("777d93"),new("ef6847"),new("b5ca68"),new("65bbda")];
    static readonly Dictionary<string,StandardMaterial3D> materials=new();
    public static Vector3 V(NVector p)=>new(p.X,p.Y,p.Z);
    public static NVector N(Vector3 p)=>new(p.X,p.Y,p.Z);
    public static StandardMaterial3D Material(Color c,bool glow=false)
    {
        string key=c.ToHtml()+glow;
        if(materials.TryGetValue(key,out var m))return m;
        m=new(){AlbedoColor=c,Roughness=.78f};
        if(c.A<1){m.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;m.ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded;m.NoDepthTest=false;}
        if(glow){m.EmissionEnabled=true;m.Emission=c;m.EmissionEnergyMultiplier=.5f;}
        materials[key]=m;return m;
    }
    public static MeshInstance3D Box(Node3D parent,Vector3 pos,Vector3 size,Color color)
    {var n=new MeshInstance3D{Mesh=new BoxMesh{Size=size},Position=pos,MaterialOverride=Material(color)};parent.AddChild(n);return n;}
    public static MeshInstance3D Ball(Node3D parent,Vector3 pos,Vector3 size,Color color)
    {var n=new MeshInstance3D{Mesh=new SphereMesh{Radius=.5f,Height=1,RadialSegments=10,Rings=5},Position=pos,Scale=size,MaterialOverride=Material(color)};parent.AddChild(n);return n;}
    public static MeshInstance3D Cylinder(Node3D parent,Vector3 pos,float radius,float height,Color color,float top=-1)
    {var n=new MeshInstance3D{Mesh=new CylinderMesh{BottomRadius=radius,TopRadius=top<0?radius:top,Height=height,RadialSegments=6},Position=pos,MaterialOverride=Material(color)};parent.AddChild(n);return n;}
    public static Label3D Label(Node3D parent,string text,Vector3 pos,int size=36,Color? color=null)
    {var n=new LocalizedLabel3D{SourceText=text,Position=pos,FontSize=size,PixelSize=.004f,Modulate=color??Colors.White,OutlineSize=5,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled};if(!string.IsNullOrEmpty(text))n.SetMeta("visual_context_hint",true);parent.AddChild(n);return n;}
    public static void Collider(Node3D parent,Vector3 pos,Vector3 size,uint layer=1)
    {var b=new StaticBody3D{Position=pos,CollisionLayer=layer,CollisionMask=0};parent.AddChild(b);b.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=size}});}
    public static Node3D Person(Node3D parent,Color shirt,bool seated=false)
    {
        if(VisualQuality.Puppet)return PuppetActor.Create(parent,shirt,seated);
        var n=new Node3D();parent.AddChild(n);
        Box(n,new(0,.94f,0),new(.68f,.8f,.38f),shirt);
        Cylinder(n,new(0,1.38f,0),.15f,.25f,new("e8b28d"));
        Ball(n,new(0,1.64f,0),new(.8f,.75f,.7f),new("f0bf96"));
        Ball(n,new(0,1.58f,.38f),new(.18f,.2f,.18f),new("e5a17f"));
        for(int side=-1;side<=1;side+=2)
        {
            Ball(n,new(side*.18f,1.76f,.31f),new(.22f,.23f,.11f),Colors.White).Name=$"Eye_{side}";
            Ball(n,new(side*.18f,1.76f,.37f),new(.08f,.09f,.05f),new("273343")).Name=$"Pupil_{side}";
            var lid=Ball(n,new(side*.18f,1.83f,.365f),new(.235f,.13f,.10f),new("e8b28d"));lid.Name=$"Lid_{side}";lid.Visible=false;
            var sweat=Ball(n,new(side*.34f,1.65f,.29f),new(.045f,.095f,.035f),new("87d6dc"));sweat.Name=$"Sweat_{side}";sweat.Visible=false;

            Ball(n,new(side*.42f,1.64f,0),new(.16f,.25f,.18f),new("e5a17f"));
            var arm=Box(n,new(side*.46f,1.0f,.03f),new(.2f,.62f,.24f),shirt);arm.Name=$"Arm_{side}";arm.RotationDegrees=new(0,0,side*12);
            Box(n,new(side*.2f,.38f,seated?.18f:0),new(.23f,.65f,seated?.55f:.26f),new("344151")).Name=$"Leg_{side}";
            Box(n,new(side*.2f,.1f,.14f),new(.27f,.19f,.4f),new("263143"));
        }
        Box(n,new(0,1.45f,.49f),new(.19f,.025f,.018f),new("654438")).Name="Mouth";
        if(seated) Box(n,new(0,.94f,.28f),new(.78f,.7f,.08f),new("ecdfbc")).Name="SeatedApron";
        return n;
    }
    public static void Layers(Node node,uint layer)
    {if(node is VisualInstance3D visual)visual.Layers=layer;foreach(var child in node.GetChildren())Layers(child,layer);}
    public static Node3D Tool(Node3D parent,int id)
    {
        if(VisualQuality.Puppet&&id==0){
            var root=new Node3D();parent.AddChild(root);
            TargetLabActors.Clipper(root,new(0,.02f,-.06f),1.4f);
            root.AddChild(new Marker3D{Name="Muzzle",Position=new(0,0,-.5f)});return root;
        }
        var n=new Node3D();parent.AddChild(n);Color c=id<ToolColors.Length?ToolColors[id]:new("65bbda");
        Box(n,new(0,0,0),new(.25f,.23f,.48f),c);
        Box(n,new(0,-.18f,.1f),new(.12f,.25f,.14f),new("344151"));
        var barrel=Cylinder(n,new(0,0,-.34f),id==2||id==3?.17f:.055f,id==7?.8f:.3f,new("3c4855"));barrel.RotationDegrees=new(90,0,0);
        if(id is 1 or 5 or 8) Cylinder(n,new(.13f,.06f,.1f),.12f,.38f,c.Lightened(.18f));
        if(id==7){var scope=Cylinder(n,new(0,.17f,-.06f),.065f,.35f,new("283443"));scope.RotationDegrees=new(90,0,0);}
        if(id==9) for(int i=0;i<6;i++)Box(n,new(0,0,-.3f-i*.1f),new(.3f,.045f,.055f),new("d7dfad"));
        if(id==0)Box(n,new(0,0,-.28f),new(.23f,.07f,.12f),new("eff0df"));
        if(id==4)
        {
            var stick=Cylinder(n,new(0,.06f,.32f),.045f,.24f,new("fff0b0"));stick.RotationDegrees=new(90,0,0);
            foreach(int side in new[]{-1,1})Box(n,new(side*.132f,.02f,-.02f),new(.025f,.13f,.24f),new("563f48"));
            var nozzle=Cylinder(n,new(0,0,-.47f),.07f,.18f,new("ffcf78"),.028f);nozzle.RotationDegrees=new(-90,0,0);
            Ball(n,new(0,-.02f,-.55f),new(.045f,.07f,.04f),new("ffe3a1"));
        }
        if(id==1){Box(n,new(.265f,.035f,.12f),new(.022f,.30f,.13f),new("142a35"));Box(n,new(.278f,.035f,.12f),new(.025f,.28f,.11f),new("9feb67")).Name="BottleFluid";}
        if(id==1){Box(n,new(.252f,.12f,.1f),new(.015f,.16f,.12f),new("eaffc9"));Box(n,new(.262f,.12f,.1f),new(.018f,.035f,.09f),new("397947"));Box(n,new(.263f,.12f,.1f),new(.019f,.1f,.03f),new("397947"));}
        n.AddChild(new Marker3D{Name="Muzzle",Position=new(0,0,id==4?-.56f:id==9?-.86f:-.5f)});
        return n;
    }
    public static void Shop(Node3D parent)
    {
        for(int x=-6;x<6;x++)for(int z=-6;z<6;z++)Box(parent,new(x+.5f,-.08f,z+.5f),new(1,.15f,1),(x+z)%2==0?new("d4d0b8"):new("729695"));
        Collider(parent,new(0,-.2f,0),new(12,.4f,12));
        foreach(var x in new[]{-6f,6f}){Box(parent,new(x,2,0),new(.2f,4,12),new("e7c5a0"));Collider(parent,new(x,2,0),new(.2f,4,12));}
        foreach(var z in new[]{-6f,6f}){Box(parent,new(0,2,z),new(12,4,.2f),new("c9b4a0"));Collider(parent,new(0,2,z),new(12,4,.2f));}
        Box(parent,new(0,2.7f,-5.86f),new(5.8f,1.05f,.12f),new("273e4b"));
        Label(parent,"4 BARBERS / 1 HEAD",new(0,2.85f,-5.75f),52,new("ffe4a7"));
        for(int side=-1;side<=1;side+=2)
        {
            Box(parent,new(side*5.3f,.78f,.7f),new(.85f,.35f,8.5f),new("986d51"));
            Collider(parent,new(side*5.3f,.5f,.7f),new(.85f,1,8.5f));
            Box(parent,new(side*5.86f,2.3f,.5f),new(.08f,1.5f,8),new("3d6e79"));
            Label(parent,side<0?"SAFE-ISH SUPPLIES":"REGRETTABLE SHORTCUTS",new(side*5.1f,3.2f,0),32);
        }
        for(int i=0;i<1;i++)
        {
            Vector3 s=V(Session.WorkCenter);var color=Team[i];
            Cylinder(parent,s+new Vector3(0,.12f,0),.72f,.2f,new("526274"));
            Cylinder(parent,s+new Vector3(0,.3f,0),.14f,.55f,new("b6c4c3"));
            var lift=new Node3D{Name=$"ChairLift_{i}",Position=s};parent.AddChild(lift);
            Box(lift,new(0,.64f,0),new(1.05f,.18f,.85f),color);
            Box(lift,new(0,1.02f,-.36f),new(1.05f,.85f,.2f),color);
            // Debris rests on visible geometry, not the player-blocking proxy.
            Collider(lift,new(0,.64f,0),new(1.05f,.18f,.85f),32);
            Collider(lift,new(0,1.02f,-.36f),new(1.05f,.85f,.2f),32);
            var chairBody=new StaticBody3D{Name=$"ChairCollider_{i}",Position=s+new Vector3(0,.85f,0),CollisionLayer=2,CollisionMask=0};parent.AddChild(chairBody);chairBody.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=new(1.1f,1.3f,.9f)}});
            for(int side=-1;side<=1;side+=2)Box(lift,new(side*.58f,.9f,0),new(.15f,.15f,.75f),new("455767"));
            var control=V(Session.ChairControl(i));Box(parent,control,new(.24f,.3f,.18f),new("4ba9b5"));
            Label(parent,"CHAIR  R ↑ / F ↓",control+Vector3.Up*.25f,20).Name=$"ChairHint_{i}";
            for(int lane=0;lane<4;lane++)
            {var mark=Box(parent,new Vector3(0,.012f,2.4f).Rotated(Vector3.Up,lane*Mathf.Pi/2),new(.65f,.015f,.9f),Team[lane]);mark.Rotation=new(0,lane*Mathf.Pi/2,0);}

        }
        Label(parent,"WIGS • AIM + E TO WEAR / PLACE\nGlue holds. Wind steals.",new(0,1.65f,5),28);
        foreach(int side in new[]{-1,1})
        {
            var cart=new Vector3(side*2.8f,.8f,-side*1.3f);
            Box(parent,cart,new(.75f,.30f,1.35f),new("657f82"));Collider(parent,cart-new Vector3(0,.3f,0),new(.75f,.9f,1.35f));
        }
        Box(parent,new(3.5f,.8f,-3.7f),new(1.6f,.35f,1.0f),new("a27856"));Collider(parent,new(3.5f,.5f,-3.7f),new(1.6f,1.0f,1.0f));
        var light=new DirectionalLight3D{RotationDegrees=new(-55,-25,0),LightColor=new("fff0d4"),LightEnergy=.65f,ShadowEnabled=true};parent.AddChild(light);
        parent.AddChild(new WorldEnvironment{Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new("b5ced5"),AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=new("d0e4ed"),AmbientLightEnergy=.3f,TonemapMode=Godot.Environment.ToneMapper.Linear}});
        parent.AddChild(new SceneLook{Name="SceneLook"});
    }
}


