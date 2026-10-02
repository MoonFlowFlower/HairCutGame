using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Hairball;

// B2-only controlled render study. Never saves renderer/project settings.
public partial class PuppetLab
{
    string lightingProfile="r5";
    bool SpatialGi=>lightingProfile is "gi" or "fabric";
    double CaptureWarmup=>4;
    void ConfigureLightingStudy()
    {
        lightingProfile=args.GetValueOrDefault("lighting-profile",round>=6?"fabric":"r5");
        if(lightingProfile is not ("r5" or "direct" or "gi" or "fabric"))throw new ArgumentException("Unknown B2 lighting profile");
        if(lightingProfile!="r5"&&RenderingServer.GetCurrentRenderingMethod()!="forward_plus")throw new ArgumentException("B2 lighting study requires Forward+");
        PuppetLabSurface.TransportEnabled=lightingProfile=="fabric";
        if(lightingProfile!="r5")RenderingServer.PositionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.SoftMedium);
    }
    void ApplyLightingStudy()
    {
        surface.SetTransport(PuppetLabSurface.TransportEnabled&&!baseline);
        if(lightingProfile=="r5"||baseline||shot is "micro" or "hair-fuzz")return;
        foreach(var child in room.GetChildren())if(child is Light3D)child.Free();
        var env=room.GetChildren().OfType<WorldEnvironment>().Single().Environment;
        env.AmbientLightColor=new Color("b8c4d3");env.AmbientLightEnergy=.065f;
        env.SsilEnabled=false;env.SsaoRadius=.10f;env.SsaoIntensity=.80f;env.SsaoLightAffect=.08f;
        env.GlowIntensity=.28f;env.GlowBloom=0;env.GlowHdrThreshold=2.0f;
        env.Sky=new Sky{RadianceSize=Sky.RadianceSizeEnum.Size256,ProcessMode=Sky.ProcessModeEnum.Quality,SkyMaterial=new ProceduralSkyMaterial{
            SkyTopColor=new Color("899fb8"),SkyHorizonColor=new Color("cab5a0"),SkyEnergyMultiplier=.32f,
            GroundBottomColor=new Color("343734"),GroundHorizonColor=new Color("7a695a"),GroundEnergyMultiplier=.25f}};
        env.ReflectedLightSource=Godot.Environment.ReflectionSource.Sky;
        env.SdfgiEnabled=SpatialGi;env.SdfgiCascades=4;env.SdfgiMinCellSize=.075f;
        env.SdfgiUseOcclusion=true;env.SdfgiReadSkyLight=false;env.SdfgiBounceFeedback=.35f;
        env.SdfgiEnergy=1;env.SdfgiNormalBias=1.1f;env.SdfgiProbeBias=1.1f;
        foreach(var mesh in Descendants(room).OfType<GeometryInstance3D>())mesh.GIMode=GeometryInstance3D.GIModeEnum.Static;
        // Deforming/cuttable actors receive GI; never bake their old silhouettes into the room.
        foreach(var root in new[]{subject,friend,held,hair.Root,stress}.Where(n=>n!=null))
            foreach(var mesh in Descendants(root!).OfType<GeometryInstance3D>())mesh.GIMode=GeometryInstance3D.GIModeEnum.Disabled;
        Softbox(new(-1.75f,2.88f,2.65f),new(0,1.70f,0),new(1.65f,1.30f),"ffe5cb",11.5f,.58f);
        // A low rear softbox projected the pendant shade onto the ceiling.
        // Put the experimental fill above the fixture, retaining real shadows.
        Softbox(new(1.85f,args.ContainsKey("fur-lighting-fix")?3.65f:2.85f,-1.20f),new(-.25f,1.9f,.6f),new(1.20f,1.70f),"d0e5ff",7.2f,.45f);
        Softbox(new(2.8f,2.4f,2.5f),new(0,1.8f,0),new(2.0f,2.0f),"e4eafa",2.8f,.65f);
        foreach(float x in new[]{-1.55f,1.43f})room.AddChild(new OmniLight3D{Name="PracticalPendant",Position=new(x,3.01f,-.5f),
            LightCullMask=7,LightColor=new("ffc991"),LightEnergy=.55f,OmniRange=3.5f,OmniAttenuation=1.25f,OmniShadowMode=OmniLight3D.ShadowMode.Cube,
            ShadowEnabled=true,ShadowBias=.10f,ShadowNormalBias=1.0f,ShadowOpacity=1,LightSize=.16f});
        if(args.ContainsKey("no-ao"))env.SsaoEnabled=false;
        if(args.ContainsKey("no-direct-shadows"))foreach(var light in room.GetChildren().OfType<Light3D>())light.ShadowEnabled=false;
        if(args.GetValueOrDefault("disable-shadow-type")=="area")foreach(var light in room.GetChildren().OfType<AreaLight3D>())light.ShadowEnabled=false;
        if(args.GetValueOrDefault("disable-shadow-type")=="omni")foreach(var light in room.GetChildren().OfType<OmniLight3D>())light.ShadowEnabled=false;
    }
    void Softbox(Vector3 position,Vector3 target,Vector2 size,string color,float energy,float softness)
    {
        // Rectangular-light shadow projection left bands in the gray receiver study.
        // Keep that branch reproducible; the selected candidate uses PCSS spot shadows.
        if(args.GetValueOrDefault("light-rig","spot")=="spot")
        {
            var spot=new SpotLight3D{Name="B2SoftboxSpot",Position=position,LightColor=new Color(color),LightEnergy=energy,
                SpotRange=8,SpotAngle=72,SpotAttenuation=2,SpotAngleAttenuation=1,LightCullMask=7,LightSpecular=.65f,
                ShadowEnabled=true,ShadowOpacity=1,ShadowBias=.06f,ShadowNormalBias=.6f,ShadowBlur=1,LightSize=softness};
            room.AddChild(spot);spot.LookAt(target);return;
        }
        var light=new AreaLight3D{Name="B2Softbox",Position=position,AreaSize=size,AreaNormalizeEnergy=true,
            LightColor=new Color(color),LightEnergy=energy,AreaRange=8,AreaAttenuation=2,LightCullMask=7,
            ShadowEnabled=true,ShadowOpacity=1,ShadowBias=.10f,ShadowNormalBias=1.0f,ShadowBlur=1,LightSize=softness};
        room.AddChild(light);light.LookAt(target);
    }
}
