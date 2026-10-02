using System;
using System.Collections.Generic;
namespace Hairball;

// Local presentation only. No fields are added to WorldState, Head or wire packets.
public enum HairProfile { Current, Human, Animal, Alien }
public static class VisualQuality
{
    public static HairProfile Profile=HairProfile.Current;
    public static int Phase;
    public static string Palette="natural";
    public static bool Scene, Flocking=true, Rim=true, Micro=true, Dynamics=true, Debris=true, Ssao=true, Glow=true;
    public static bool LocalStates=>Phase>=5;
    public static int Revision;
    public static bool Puppet;
    public static void Configure(Dictionary<string,string> args)
    {
        Phase=int.Parse(args.GetValueOrDefault("visual-phase","0"));
        Enum.TryParse(args.GetValueOrDefault("hair-profile","Current"),true,out Profile);
        Scene=Phase>=1&&!args.ContainsKey("no-scene");
        Palette=args.GetValueOrDefault("hair-color","natural");
        Flocking=!args.ContainsKey("no-flock");Rim=!args.ContainsKey("no-rim");Micro=!args.ContainsKey("no-micro");
        Dynamics=!args.ContainsKey("no-dynamics");Debris=!args.ContainsKey("no-debris");Ssao=!args.ContainsKey("no-ssao");Glow=!args.ContainsKey("no-glow");
        Puppet=!args.ContainsKey("legacy-b-art")&&!args.ContainsKey("v06-variant-a")&&!args.ContainsKey("visual-lookdev")&&!args.ContainsKey("visual-target-lab")&&(args.Count==0||args.ContainsKey("menu")||args.ContainsKey("v06-variant-b")||Godot.OS.HasFeature("hairball_variant_b"));
        if(Puppet){
            Scene=true;
            PuppetHairSettings.Current=new(){Round=6,Shells=8,VisualMode="R7_TUFT_SURFACE",BundleField=true,TuftVolume=true,SoftVolumeSampling=true,ClumpDepth=true,VolumeUndercoat=true,SoftFabric=true,Warning=false};
            PuppetLabSurface.TransportEnabled=true;
        }
        Revision++;
    }
    public static void Cycle(){Profile=(HairProfile)(((int)Profile+1)%4);Revision++;}
}
