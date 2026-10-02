using Godot;
using System;
using System.Linq;
using static Hairball.TargetLabGeometry;
namespace Hairball;

// The production rig retains its named expression/limb controls. Puppet art is
// attached to those controls, so standing, panic and remote body motion remain.
public partial class PuppetActor : Node3D
{
    Node3D face=null!;
    public static Node3D Create(Node3D parent,Color shirt,bool seated)
    {
        var n=new PuppetActor();parent.AddChild(n);var fabric=new StandardMaterial3D{AlbedoColor=shirt.Darkened(.18f),Roughness=.95f};
        Ball(n,new(0,.97f,0),new(.64f,.80f,.43f),fabric);
        Round(n,new(0,.95f,.19f),new(.49f,.58f,.12f),.06f,PuppetLabModels.Teal);
        Cylinder(n,new(0,1.34f,0),.15f,.24f,PuppetLabModels.Skin);
        for(int side=-1;side<=1;side+=2){
            var arm=new Node3D{Name=$"Arm_{side}",Position=new(side*.46f,1,.03f)};n.AddChild(arm);
            Limb(arm,new(0,.28f,0),new(0,-.15f,.02f),.10f,fabric);PuppetLabModels.Hand(arm,new(0,-.23f,.04f),side,.64f,false);
            var leg=new Node3D{Name=$"Leg_{side}",Position=new(side*.2f,.38f,seated?.18f:0)};n.AddChild(leg);
            Limb(leg,new(0,.24f,-.03f),new(0,-.20f,.06f),.105f,PuppetLabModels.Teal);
            Round(leg,new(0,-.26f,.15f),new(.27f,.19f,.38f),.07f,PuppetLabModels.Dark);
            n.AddChild(new Node3D{Name=$"Eye_{side}",Scale=new(.22f,.23f,.11f)});
            n.AddChild(new Node3D{Name=$"Lid_{side}",Visible=false});
            n.AddChild(new Node3D{Name=$"Pupil_{side}",Position=new(side*.18f,1.76f,.38f)});
            Ball(n,new(side*.34f,1.65f,.29f),new(.035f,.070f,.025f),Mat("87d6dc",.25f)).Name=$"Sweat_{side}";
        }
        n.AddChild(new Node3D{Name="Mouth"});
        n.face=PuppetLabModels.Face(n,new(0,1.64f,0),.86f,production:true);
        if(seated){var cape=new Node3D{Name="SeatedApron",Position=new(0,-.04f,0),Scale=new(1,.92f,1)};n.AddChild(cape);Cape(cape,PuppetLabModels.Cream);}
        Finish(n);foreach(string name in new[]{"FaceSurface","MouthSurface"})n.face.GetNode<MeshInstance3D>(name).SetInstanceShaderParameter("puppet_face",true);return n;
    }
    public static void Finish(Node node){var surface=new PuppetLabSurface();surface.Collect(node);surface.Enable(true);surface.SetTransport(true);}
    public override void _Process(double delta)
    {
        if(face==null)return;
        float opening=Math.Clamp((GetNode<Node3D>("Mouth").Scale.Y-1)*.5f,-.15f,1);
        foreach(string name in new[]{"FaceSurface","MouthSurface"})face.GetNode<MeshInstance3D>(name).SetInstanceShaderParameter("puppet_mouth",opening);
        foreach(int side in new[]{-1,1}){
            var eye=face.GetNode<Node3D>(side<0?"EyeAssemblyLeft":"EyeAssemblyRight");
            var control=GetNode<Node3D>($"Eye_{side}");var lid=GetNode<Node3D>($"Lid_{side}");
            float blink=lid.Visible?Math.Clamp(lid.Scale.Y/.26f,0,1):0;
            eye.Scale=new(control.Scale.X/.22f,control.Scale.Y/.23f*(1-blink*.58f),1);
            var p=GetNode<Node3D>($"Pupil_{side}").Position-new Vector3(side*.18f,1.76f,.38f);
            eye.GetNode<Node3D>("Gaze").Position=p/.86f;
        }
    }
}
