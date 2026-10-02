using System;
using System.Collections.Generic;
namespace Hairball;

// Local render options only. Never serialized into Session or network snapshots.
public sealed record PuppetHairSettings
{
    public static PuppetHairSettings Current { get; set; } = new();
    public int Round { get; init; }
    public bool Enabled => Round > 0;
    public bool Microfiber { get; init; } = true;
    public bool Fuzz { get; init; } = true;
    public bool Trim { get; init; } = true;
    public bool Carve { get; init; } = true;
    public bool Smooth { get; init; } = true;
    public bool Warning { get; init; } = true;
    public bool Prefall { get; init; } = true;
    public bool StableSampling { get; init; } = true;
    public bool CleanFabric { get; init; } = true;
    public bool SoftHairLighting { get; init; } = true;
    public string VisualMode { get; init; } = "R6_FIBERS";
    public bool ShellFur => VisualMode == "R7_SHELL" && Round >= 3 && Lod < 2;
    public bool VolumeShells => (VisualMode == "R7_VOLUME" || VisualMode.StartsWith("R7_TUFT_")) && Round >= 3 && (Lod < 2 || DebrisVolume);
    public bool DebrisVolume { get; init; }
    public bool BundleField { get; init; }
    public bool TuftVolume { get; init; }
    public bool TuftReference => VisualMode is "R7_TUFT_REFERENCE" or "R7_TUFT_RIBBONS";
    public bool TuftRibbons => VisualMode=="R7_TUFT_RIBBONS";
    public bool TuftHybrid => VisualMode is "R7_TUFT_HYBRID" or "R7_TUFT_SURFACE";
    public bool SurfaceSamples => VisualMode=="R7_TUFT_SURFACE";
    public bool SoftVolumeSampling { get; init; }
    public bool ClumpDepth { get; init; }
    public bool VolumeUndercoat { get; init; }
    public bool SoftFabric { get; init; }
    public bool DiagnosticNonShell => TuftReference;
    public bool FlatShell { get; init; }
    public bool PlushTufts => ShellFur && !FlatShell;
    public int Shells { get; init; }
    public int Lod { get; init; } // 0 hero, 1 nearby, 2 background, 3 debris
    public static void Configure(Dictionary<string,string> args)
    {
        Current = new PuppetHairSettings {
            Round = int.Parse(args.GetValueOrDefault("hair-opt-round", "0")),
            Shells = Math.Clamp(int.Parse(args.GetValueOrDefault("hair-shells", "0")), 0, args.ContainsKey("r7-volume")?128:args.ContainsKey("r7-shell")?16:4),
            VisualMode = args.ContainsKey("r7-surface-samples")?"R7_TUFT_SURFACE":args.ContainsKey("r7-tuft-hybrid")?"R7_TUFT_HYBRID":args.ContainsKey("r7-tuft-ribbons")?"R7_TUFT_RIBBONS":args.ContainsKey("r7-tuft-reference")?"R7_TUFT_REFERENCE":args.ContainsKey("r7-volume") ? "R7_VOLUME" : args.ContainsKey("r7-shell") ? "R7_SHELL" : "R6_FIBERS",
            BundleField = args.ContainsKey("r7-bundle-field")||args.ContainsKey("r7-tuft-volume"),
            TuftVolume = args.ContainsKey("r7-tuft-volume"),
            SoftVolumeSampling = args.ContainsKey("fur-soft-sampling"),
            ClumpDepth = args.ContainsKey("fur-clump-depth"),
            VolumeUndercoat = args.ContainsKey("fur-volume-undercoat"),
            SoftFabric = args.ContainsKey("puppet-soft-fabric"),
            FlatShell = args.ContainsKey("r7-flat"),
            Microfiber = !args.ContainsKey("no-hair-microfiber"), Fuzz = !args.ContainsKey("no-hair-fuzz"),
            Trim = !args.ContainsKey("old-cut-interior"), Carve = !args.ContainsKey("old-cut-interior"),
            Smooth = !args.ContainsKey("no-hair-smoothing"), Warning = !args.ContainsKey("no-hair-warning"),
            Prefall = !args.ContainsKey("no-hair-prefall"),
            StableSampling = !args.ContainsKey("legacy-hair-sampling"),
            CleanFabric = !args.ContainsKey("legacy-fabric"),
            SoftHairLighting = !args.ContainsKey("legacy-hair-lighting")
        };
    }
}
