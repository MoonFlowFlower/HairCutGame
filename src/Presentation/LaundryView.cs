using Godot;
using Hairball.Core;
using System;
using System.Linq;

namespace Hairball;

public partial class LaundryView : Node3D
{
    readonly Node3D[] garments=new Node3D[3],ties=new Node3D[3],repairs=new Node3D[3];
    readonly LaundryContact[] contacts=new LaundryContact[3];
    Node3D fan=null!,rotor=null!;
    float lastSample=-100;
    int revision=-1;
    HairVolume? volume;
    public int GarmentCount=>garments.Length;
    public float MotionAmount {get;private set;}
    public override void _Ready()
    {
        for(int i=0;i<3;i++)
        {
            var pivot=new Node3D();AddChild(pivot);ties[i]=pivot;
            Art.Box(pivot,new(0,.015f,0),new(.055f,.13f,.04f),new("f4d17e"));
            float hang=.28f+(i==2?.16f:0);
            Art.Box(pivot,new(0,-hang*.5f,.025f),new(.018f,hang,.018f),new("e4cdb0"));
            var cloth=Underwear(i);pivot.AddChild(cloth);garments[i]=cloth;
            var repair=new Node3D();pivot.AddChild(repair);repairs[i]=repair;
            for(int j=0;j<3;j++)Art.Ball(repair,new((j-1)*.045f,.02f-j*.025f,-.03f),new(.065f,.085f,.055f),new("ffdf92"));
        }
        fan=new Node3D();GetParent().AddChild(fan);
        Art.Cylinder(fan,new(0,.06f,0),.27f,.1f,new("425b66"));Art.Cylinder(fan,new(0,.5f,0),.045f,.9f,new("6d858a"));
        var housing=Art.Cylinder(fan,new(0,1.02f,0),.29f,.11f,new("de8865"));housing.RotationDegrees=new(90,0,0);
        rotor=new Node3D{Position=new(0,1.02f,.09f)};fan.AddChild(rotor);
        for(int i=0;i<3;i++){var blade=Art.Box(rotor,new(0,0,0),new(.1f,.47f,.025f),new("c1e1dc"));blade.Rotation=new(0,0,i*Mathf.Tau/3);}
        Art.Ball(fan,new(0,1.02f,.14f),Vector3.One*.1f,new("ffdaa5"));
        for(int i=-1;i<=1;i++)Art.Box(fan,new(i*.10f,1.02f,.15f),new(.015f,.49f,.018f),new("304c58"));
        // A small sticky repair tray gives context without becoming another focal point.
        Art.Box(fan,new(.28f,.09f,.12f),new(.3f,.06f,.23f),new("ddb969"));
        for(int i=0;i<3;i++)Art.Box(fan,new(.24f+i*.05f,.13f,.11f),new(.025f,.03f,.17f),new("ffebbd"));
    }
    public override void _ExitTree(){if(IsInstanceValid(fan))fan.QueueFree();}
    public static Node3D Underwear(int index)
    {
        var n=new Node3D();Color color=(index%3) switch{0=>new("e97b83"),1=>new("6abacb"),_=>new("edc759")};
        var points=new[]{new Vector2(-.19f,0),new Vector2(.19f,0),new Vector2(.17f,-.30f),new Vector2(.065f,-.32f),new Vector2(.025f,-.16f),new Vector2(-.025f,-.16f),new Vector2(-.065f,-.32f),new Vector2(-.17f,-.30f)};
        var tris=Geometry2D.TriangulatePolygon(points);var verts=new System.Collections.Generic.List<Vector3>();
        foreach(int i in tris)verts.Add(new(points[i].X,points[i].Y,.017f));
        var mesh=new ArrayMesh();var a=new Godot.Collections.Array();a.Resize((int)Mesh.ArrayType.Max);a[(int)Mesh.ArrayType.Vertex]=verts.ToArray();a[(int)Mesh.ArrayType.Normal]=Enumerable.Repeat(Vector3.Back,verts.Count).ToArray();mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,a);
        var material=new StandardMaterial3D{AlbedoColor=color,Roughness=1,EmissionEnabled=true,Emission=color,EmissionEnergyMultiplier=.18f,CullMode=BaseMaterial3D.CullModeEnum.Disabled};n.AddChild(new MeshInstance3D{Mesh=mesh,MaterialOverride=material});
        Art.Box(n,new(0,-.022f,.023f),new(.39f,.045f,.035f),new("fff1d7"));
        for(int i=0;i<6;i++)
        {float x=(i%3-1)*.11f,y=-.095f-(i/3)*.105f;if(index==1)Art.Box(n,new(x,y,.026f),new(.05f,.015f,.012f),new("e2f5e7"));else Art.Ball(n,new(x,y,.027f),new(.035f,.035f,.012f),new("fff0d7"));}
        return n;
    }
    public void Sync(Head head,float time,float wind,bool active)
    {
        Visible=fan.Visible=active;if(!active)return;
        Position=Art.V(head.Position);Rotation=Art.V(head.Rotation);
        fan.Position=Art.V(Session.WorkCenter)+new Vector3(-1.4f,0,.4f);fan.Rotation=new(0,-.6f,0);rotor.Rotation=new(0,0,time*9);
        if(volume!=head.Volume||revision!=head.Volume.Revision||time-lastSample>.18f||time<lastSample)
        {for(int i=0;i<3;i++)contacts[i]=LaundryRig.Contact(head,i);lastSample=time;revision=head.Volume.Revision;volume=head.Volume;}
        float stress=LaundryRig.Tension(head,wind);MotionAmount=.08f+stress*.08f+wind*.32f;
        for(int i=0;i<3;i++)
        {
            var contact=contacts[i];float low=i==2?.16f:0;
            ties[i].Position=Art.V(contact.Point)+new Vector3(0,.015f,.18f);
            garments[i].Position=new(0,-.28f-low,.045f);
            garments[i].Rotation=new(MathF.Sin(time*(3.1f+wind*2)+i)*MotionAmount,MathF.Sin(time*2+i)*.12f,(i-1)*.09f+MathF.Sin(time*2.3f+i)*MotionAmount*.32f+(contact.OnHair?0:.5f));
            if(i==2){ties[i].Scale=new(1,1,1);}
            repairs[i].Visible=HairSystem.MaterialAt(head,contact.Point).Glue>.3f;
        }
    }
    public static void Expression(Node3D model,float time,float stress,bool active)
    {
        float blink=MathF.Sin(time*2.7f)> .989f?1:0;
        for(int side=-1;side<=1;side+=2)
        {
            var lid=model.GetNode<Node3D>($"Lid_{side}");lid.Visible=active;
            lid.Position=new(side*.18f,1.86f+stress*.015f-blink*.07f,.375f);lid.Scale=new(.235f,.08f+blink*.18f,.10f);lid.Rotation=new(0,0,-side*.18f);
            var pupil=model.GetNode<Node3D>($"Pupil_{side}");pupil.Position=new(side*.18f+(active?MathF.Sin(time*1.6f)*.014f:0),1.76f+(active?.025f:0),.38f);
            var sweat=model.GetNode<Node3D>($"Sweat_{side}");sweat.Visible=active;sweat.Position=new(side*.34f,1.7f-(time*.17f+side*.1f)% .16f,.32f);
        }
        var mouth=model.GetNode<Node3D>("Mouth");mouth.Scale=active?new(1,.7f+stress*2.8f,1):Vector3.One;mouth.Rotation=new(0,0,active?-.07f:0);
    }
}
