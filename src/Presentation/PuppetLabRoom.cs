using Godot;
using System;
using static Hairball.TargetLabGeometry;
namespace Hairball;

public static class PuppetLabRoom
{
    public static void Build(Node3D p,int round)
    {
        var plaster=Mat("bda384",.96f);var teal=Mat("3d6562",.94f);var wood=Mat("907052",.87f);var edge=Mat("654e3e",.87f);var brass=Mat("b39363",.5f,.45f);
        plaster.ResourceName="plaster";wood.ResourceName="oak";edge.ResourceName="oak";
        Round(p,new(0,-.12f,0),new(8,.23f,7),.04f,Mat("7f715f",.91f));
        // Quiet alternating old oak boards and seams, deliberately lower contrast than the head.
        for(int x=-7;x<=7;x++)for(int z=-3;z<=3;z++)
            Round(p,new(x*.51f,.002f,z*.95f+(x%2)*.47f),new(.497f,.025f,.937f),.009f,Mat((x+z)%3==0?"9f8b70":(x+z)%3==1?"968269":"8e7c65",.87f));
        Round(p,new(0,2.1f,-2.0f),new(8,4.2f,.2f),.05f,plaster);
        Round(p,new(0,.70f,-1.84f),new(8,1.4f,.13f),.03f,teal);
        Round(p,new(0,1.41f,-1.76f),new(8,.075f,.12f),.02f,wood);
        for(int i=-10;i<=10;i++)Round(p,new(i*.37f,.7f,-1.748f),new(.031f,1.3f,.034f),.01f,Mat("4c6b62"));
        Round(p,new(-3.65f,2.1f,.35f),new(.15f,4.2f,4.8f),.04f,plaster);
        Round(p,new(3.65f,2.1f,.35f),new(.15f,4.2f,4.8f),.04f,plaster);
        Round(p,new(0,4.12f,.25f),new(7.5f,.15f,4.7f),.04f,Mat("b8a78d"));
        // Window is geometry and soft blue glass, no photographic backdrop.
        Round(p,new(1.8f,2.5f,-1.84f),new(1.6f,2.1f,.15f),.12f,edge);
        var glass=Mat("98b9be",.85f);glass.EmissionEnabled=true;glass.Emission=new("688b96");glass.EmissionEnergyMultiplier=.35f;
        var pane=Round(p,new(1.8f,2.5f,-1.744f),new(1.40f,1.89f,.06f),.055f,glass);if(round>=4){var sky=new ShaderMaterial{Shader=GD.Load<Shader>("res://shaders/puppet_window.gdshader")};pane.SetMeta("b2_baseline",glass);pane.SetMeta("b2_candidate",sky);pane.MaterialOverride=sky;}
        Round(p,new(1.8f,2.5f,-1.68f),new(.065f,1.91f,.06f),.015f,wood);
        Round(p,new(1.8f,2.5f,-1.68f),new(1.40f,.065f,.06f),.015f,wood);
        Round(p,new(1.8f,1.48f,-1.57f),new(1.75f,.12f,.42f),.036f,wood);
        // Hero station cabinets.
        foreach(float x in new[]{-2.15f,2.90f})
        {
            Round(p,new(x,.59f,-1.25f),new(1.2f,1.1f,.62f),.04f,teal);Round(p,new(x,1.16f,-1.21f),new(1.32f,.12f,.73f),.035f,wood);
            for(int j=0;j<3;j++){Round(p,new(x,.30f+j*.29f,-.92f),new(1.08f,.25f,.054f),.017f,Mat("507571"));Round(p,new(x,.31f+j*.29f,-.876f),new(.24f,.032f,.052f),.015f,brass);}
        }
        // Round-backed wall mirror, more atmosphere than detail.
        Round(p,new(-2.1f,2.3f,-1.77f),new(1.12f,1.69f,.12f),.24f,wood);
        Round(p,new(-2.1f,2.3f,-1.69f),new(.98f,1.54f,.045f),.21f,Mat("84938b",.64f,.22f));
        for(int j=0;j<2;j++)
        {
            float y=1.96f+j*.69f;Round(p,new(-.88f,y,-1.70f),new(.94f,.08f,.38f),.025f,wood);
            for(int i=0;i<4;i++)Bottle(p,new(-1.19f+i*.22f,y+.045f,-1.64f),i+j);
        }
        for(int i=0;i<5;i++)Bottle(p,new(-2.56f+i*.20f,1.22f,-1.25f),i+1);
        Plant(p,new(2.15f,1.56f,-1.50f),.68f);Plant(p,new(-2.73f,1.22f,-1.16f),.62f);Plant(p,new(3.03f,1.22f,-1.30f),.9f);
        for(int i=0;i<3;i++){var towel=Round(p,new(2.7f,1.25f+i*.075f,-1.15f),new(.50f,.074f,.36f),.035f,Mat(i%2==0?"bcad87":"7e9991",.98f));towel.RotationDegrees=new(0,i*7-7,0);}
        // Uneven ceramic tiles and physical clock give the set a made-by-hand scale.
        var clock=Ball(p,new(-2.1f,3.37f,-1.73f),new(.44f,.44f,.06f),brass);
        Ball(p,new(-2.1f,3.37f,-1.691f),new(.385f,.385f,.024f),Mat("d8c89f",.95f));
        PuppetLabModels.Tube(p,[new(-2.1f,3.37f,-1.675f),new(-2.17f,3.44f,-1.675f)],.012f,edge);PuppetLabModels.Tube(p,[new(-2.1f,3.37f,-1.673f),new(-2.05f,3.51f,-1.673f)],.009f,edge);
        foreach(float x in new[]{-1.55f,1.43f})
        {
            Cylinder(p,new(x,3.70f,-.5f),.017f,.70f,edge);
            Mesh(p,new CylinderMesh{TopRadius=.10f,BottomRadius=.32f,Height=.22f,RadialSegments=48},new(x,3.27f,-.5f),brass);
            var bulb=Mat("ffe2a4");bulb.EmissionEnabled=true;bulb.Emission=new("ffd491");bulb.EmissionEnergyMultiplier=1.4f;
            Ball(p,new(x,3.16f,-.5f),new(.25f,.09f,.25f),bulb);
        }
        // No UI used as decorative cover. A small physical print anchors the shop.
        Round(p,new(.25f,2.72f,-1.81f),new(.76f,.91f,.06f),.025f,wood);
        Round(p,new(.25f,2.72f,-1.77f),new(.68f,.83f,.016f),.013f,Mat("cbbb9b"));
        var label=new Label3D{Text="GOOD HAIR\nODD COMPANY",FontSize=36,PixelSize=.0022f,Position=new(.25f,2.75f,-1.75f),Modulate=new("425b59"),OutlineSize=0};p.AddChild(label);
        if(round>=4)
        {
            // A second working shelf adds depth and an individual, cluttered shop rhythm.
            Round(p,new(2.92f,2.55f,-1.67f),new(.92f,.075f,.43f),.025f,wood);
            for(int i=0;i<4;i++)Bottle(p,new(2.57f+i*.21f,2.59f,-1.6f),i+2);
            Plant(p,new(3.17f,2.62f,-1.56f),.8f);
            var curlMat=Mat("aa91aa",.98f);
            var jar=Mat("628e92",.58f);Cylinder(p,new(2.46f,1.41f,-1.16f),.092f,.32f,jar);Cylinder(p,new(2.46f,1.58f,-1.16f),.097f,.030f,brass);
            for(int j=0;j<3;j++){float x=-1.36f+j*.27f;Round(p,new(x,.85f,-.78f),new(.085f,.58f,.07f),.025f,wood);Cylinder(p,new(x,1.18f,-.78f),.091f,.07f,brass);for(int i=0;i<9;i++)Ball(p,new(x+.061f*MathF.Cos(i*2.4f),1.225f+.012f*(i%3),-.78f+.061f*MathF.Sin(i*2.4f)),new(.028f,.08f,.028f),curlMat);}
            // A striped barber pole is a recognizable silhouette beside the mirror.
            Cylinder(p,new(-2.9f,2.27f,-1.55f),.103f,.70f,Mat("d2c5ad",.66f));
            for(int i=0;i<7;i++){float a=i*.94f;var band=Round(p,new(-2.9f+.077f*MathF.Sin(a),1.99f+i*.087f,-1.475f),new(.16f,.045f,.025f),.015f,Mat(i%2==0?"aa5b54":"4b7d87",.67f));band.RotationDegrees=new(0,0,-22);}
            Ball(p,new(-2.9f,2.655f,-1.55f),new(.20f,.14f,.20f),brass);Ball(p,new(-2.9f,1.88f,-1.55f),new(.20f,.14f,.20f),brass);
        }
        SetLighting(p,round,true);
    }
    public static void SetLighting(Node3D p,int round,bool enabled)
    {
        foreach(var node in p.GetChildren())if(node is Light3D or WorldEnvironment or ReflectionProbe)node.Free();
        foreach(var node in p.GetChildren())if(node is MeshInstance3D m&&m.HasMeta("b2_baseline"))m.MaterialOverride=m.GetMeta(enabled?"b2_candidate":"b2_baseline").AsGodotObject() as Material;
        if(!enabled)round=1;
        var env=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new("a3937f"),AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=new("c2d5de"),AmbientLightEnergy=.45f,TonemapMode=Godot.Environment.ToneMapper.Filmic,TonemapWhite=4};
        bool hero=round>=3&&RenderingServer.GetCurrentRenderingMethod()=="forward_plus";
        if(hero){env.AmbientLightEnergy=.34f;env.AmbientLightColor=new("b1b7d0");env.TonemapMode=Godot.Environment.ToneMapper.Aces;env.TonemapWhite=6;env.SsaoEnabled=true;env.SsaoRadius=.16f;env.SsaoIntensity=1.15f;env.SsaoLightAffect=.25f;env.SsaoDetail=.5f;env.SsilEnabled=true;env.SsilIntensity=.75f;env.SsilRadius=1.5f;env.GlowEnabled=true;env.GlowIntensity=.55f;env.GlowBloom=.025f;env.GlowHdrThreshold=1.3f;}
        p.AddChild(new WorldEnvironment{Environment=env});
        Light(p,new(-1.6f,3.4f,2.5f),new(0,1.9f,0),"ffddb2",hero?2.7f:1.55f,6.5f,57,true,hero?2u:7u);
        Light(p,new(2.3f,3.0f,1.4f),new(0,2.1f,0),"b3ccf3",hero?.68f:.7f,5.5f,65,false,hero?2u:7u);
        Light(p,new(.95f,3.0f,-.90f),new(0,2.35f,0),"ffc9a1",hero?3.8f:1.7f,5,57,false,hero?2u:7u);
        if(hero)
        {
            Light(p,new(-2.5f,2.7f,1.6f),new(-1.2f,1.5f,-.4f),"ffe1bd",2.1f,5,62,true,4u);
            Light(p,new(1.8f,2.8f,-1.25f),new(.1f,1.2f,.85f),"c8e0f3",2.5f,5,64,false);
            foreach(float x in new[]{-1.55f,1.43f})p.AddChild(new OmniLight3D{Position=new(x,3.03f,-.5f),LightColor=new("ffbf78"),LightEnergy=1.2f,OmniRange=2.8f});
        }
        p.AddChild(new DirectionalLight3D{RotationDegrees=new(-52,-28,0),LightColor=new("d8d9cc"),LightEnergy=.18f});
    }
    static void Light(Node3D p,Vector3 at,Vector3 target,string color,float energy,float range,float angle,bool shadow,uint mask=7)
    {var l=new SpotLight3D{Position=at,LightColor=new(color),LightEnergy=energy,SpotRange=range,SpotAngle=angle,SpotAttenuation=.65f,SpotAngleAttenuation=1.2f,ShadowEnabled=shadow,ShadowBias=.05f,ShadowNormalBias=RenderingServer.GetCurrentRenderingMethod()=="forward_plus"?.45f:1.5f,ShadowBlur=2,ShadowOpacity=.72f,LightSize=RenderingServer.GetCurrentRenderingMethod()=="forward_plus"?.65f:0,LightCullMask=mask};p.AddChild(l);l.LookAt(target);}
    static void Bottle(Node3D p,Vector3 at,int id)
    {string[] colors=["697e79","b49a71","a77567","838090","618990"];float h=.17f+(id%3)*.045f;Cylinder(p,at+new Vector3(0,h*.5f,0),.057f,h,Mat(colors[id%5],.6f));Cylinder(p,at+new Vector3(0,h+.025f,0),.033f,.05f,Mat("565b54",.85f));Round(p,at+new Vector3(0,h*.54f,.056f),new(.073f,.075f,.005f),.002f,Mat("c8b999"));}
    static void Plant(Node3D p,Vector3 at,float size)
    {var n=new Node3D{Position=at,Scale=Vector3.One*size};p.AddChild(n);Mesh(n,new CylinderMesh{TopRadius=.16f,BottomRadius=.11f,Height=.23f},new(0,.115f,0),Mat("a26f51"));for(int i=0;i<9;i++){float a=i*2.399f;var v=new Vector3(MathF.Cos(a),0,MathF.Sin(a));var leaf=Ball(n,new Vector3(0,.32f+i*.025f,0)+v*.16f,new(.12f,.33f,.039f),Mat(i%2==0?"637b4c":"788a54"));leaf.RotationDegrees=new(25,i*137,35);}}
}
