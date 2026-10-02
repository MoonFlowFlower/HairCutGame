using Godot;
using System;
using static Hairball.TargetLabGeometry;
namespace Hairball;

// Imported, licensed art is confined to the opt-in visual slice. See THIRD_PARTY_ASSETS.md.
public static class TargetLabAssets
{
    const string Root="res://assets/third_party/visual_target/";
    public static Node3D Add(Node3D parent,string asset,Vector3 position,Vector3 scale,float yaw=0)
    {
        var n=GD.Load<PackedScene>(Root+asset+".glb").Instantiate<Node3D>();n.Name=asset;
        parent.AddChild(n);n.Position=position;n.Scale=scale;n.RotationDegrees=new(0,yaw,0);
        CleanMaterials(n);
        return n;
    }
    static void CleanMaterials(Node node)
    {
        if(node is MeshInstance3D mesh)
        for(int i=0;i<mesh.Mesh.GetSurfaceCount();i++)
        {
            mesh.LodBias=4;
            if(mesh.GetActiveMaterial(i) is not StandardMaterial3D src)continue;
            var m=(StandardMaterial3D)src.Duplicate();
            if(src.ResourceName.StartsWith("Snow_Skin")){m.AlbedoColor=new("fff3e7");m.Roughness=.65f;m.MetallicSpecular=.42f;}
            if(src.ResourceName=="Black")m.AlbedoColor=new("344b49");
            if(src.ResourceName=="Brown")m.AlbedoColor=new("6d4f35");
            if(src.ResourceName=="DarkGreen")m.AlbedoColor=new("53704a");
            if(src.ResourceName=="LightOrange")m.AlbedoColor=new("ad8b60");
            if(src.ResourceName=="Mirror"){m.AlbedoColor=new("708d91");m.Metallic=.25f;m.Roughness=.62f;}
            if(src.ResourceName=="Wood")m.AlbedoColor=new("735d45");
            if(src.ResourceName=="Cushin")m.AlbedoColor=new("8e6a4d");
            if(src.ResourceName=="Kitchen")m.AlbedoColor=new("506760");
            if(src.ResourceName=="White")m.AlbedoColor=new("c5b59a");
            if(src.ResourceName=="KitchenTop")m.AlbedoColor=new("997047");
            if(src.ResourceName=="Main")m.AlbedoColor=new("e4cdaa");
            if(src.ResourceName=="Main_Light")m.AlbedoColor=new("f1ddba");
            if(src.ResourceName=="Main_Dark")m.AlbedoColor=new("c7ab86");
            if(src.ResourceName=="Hooves")m.AlbedoColor=new("665445");
            if(src.ResourceName=="Muzzle")m.AlbedoColor=new("947c65");
            mesh.SetSurfaceOverrideMaterial(i,m);
        }
        foreach(var c in node.GetChildren())CleanMaterials(c);
    }
    public static void Paint(Node node,Material material)
    {
        if(node is MeshInstance3D m)m.MaterialOverride=material;
        foreach(var c in node.GetChildren())Paint(c,material);
    }
    public static void Customer(Node3D parent)
    {
        SubjectLayer(Add(parent,"snow_customer",Vector3.Zero,Vector3.One));
        Add(parent,"polyhaven_barber_chair",new(0,0,-.12f),new(1.32f,1.02f,1.04f));
    }
    public static void Animal(Node3D parent)=>SubjectLayer(Add(parent,"quaternius_alpaca",new(0,0,-.86f),Vector3.One*.42f));
    public static void SubjectLayer(Node n)
    {
        if(n is MeshInstance3D m)m.Layers=2;
        foreach(var child in n.GetChildren())SubjectLayer(child);
    }
    public static void Teammate(Node3D parent,Vector3 at,float yaw,Material hair,bool right)
    {
        var n=Add(parent,"snow_teammate",at,Vector3.One,yaw);
        SubjectLayer(n);
        if(right)TintShirt(n,Mat("718b7b",.9f));
        var h=Mesh(n,HairMesh(Hair(false),false),new(0,1.93f,0),hair);h.Scale=Vector3.One*.63f;
        var socket=n.FindChild("ClipperSocket",true,false) as Node3D;
        var tool=TargetLabActors.Clipper(n,(socket?.Position??new(.22f,1.53f,.27f))+new Vector3(-.02f,.08f,.025f),.85f);tool.RotationDegrees=new(-20,0,-35);
    }
    static void TintShirt(Node n,Material mat)
    {
        if(n is MeshInstance3D m && n.Name.ToString().Contains("shirt"))m.MaterialOverride=mat;
        foreach(var child in n.GetChildren())TintShirt(child,mat);
    }
    public static void FpsHand(Node3D parent)
    {
        var n=Add(parent,"snow_fps",new(.04f,-.105f,.10f),Vector3.One*1.40f);
        n.RotationDegrees=new(-90,0,-65);
    }
    public static void Station(Node3D parent)
    {
        Add(parent,"home_kitchen_3drawers",new(-2.12f,.0f,-1.01f),new(1.5f,.68f,.65f));
        var mirror=Add(parent,"home_bathroom_mirror2",new(-2.12f,2.0f,-1.76f),new(2.05f,2.05f,2.05f));
        var shelf=Add(parent,"home_shelf_1",new(3.05f,0,-1.10f),new(.8f,.72f,.8f));Paint(shelf,Mat("76573f",.85f));
        Add(parent,"home_houseplant_3",new(-2.73f,1.105f,-1.12f),Vector3.One*.65f);
        Add(parent,"home_houseplant_3",new(3.04f,.05f,-.18f),Vector3.One*1.3f);
        Add(parent,"home_stool",new(-1.48f,0,-.46f),new(.8f,.72f,.8f));
        for(int i=-1;i<=1;i+=2)
        {var lamp=Add(parent,"home_light_ceiling6",new(i*2.1f,3.80f,-.7f),Vector3.One*1.55f);Paint(lamp,Mat("ac8752",.5f,.4f));}
    }
}
