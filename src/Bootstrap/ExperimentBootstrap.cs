using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace Hairball;
public partial class Main
{
    StreamWriter? experimentWriter;
    readonly System.Collections.Generic.HashSet<string> experimentFacts=new();
    bool DefaultVariantB=>!args.ContainsKey("v06-variant-a")&&(args.Count==0||args.ContainsKey("menu")||args.ContainsKey("v06-variant-b")||OS.HasFeature("hairball_variant_b"));
    void ConfigureExperiment()
    {
        experimentWriter?.Dispose();experimentWriter=null;
        if(args.ContainsKey("v06-variant-a")&&args.ContainsKey("v06-variant-b"))throw new ArgumentException("Select exactly one v0.6 variant");
        world.Experiment.Variant=DefaultVariantB?ExperimentVariant.B:args.ContainsKey("v06-variant-a")?ExperimentVariant.A:ExperimentVariant.Off;
        if(world.Experiment.IsB){simulation.BuildSeconds=ExperimentState.BuildDuration;string combos=ProjectSettings.GlobalizePath("user://gallery/combos.json");try{if(File.Exists(combos)&&new FileInfo(combos).Length<4096)foreach(var item in JsonSerializer.Deserialize<Combo[]>(File.ReadAllText(combos))??[])if(Enum.IsDefined(item))simulation.KnownCombos.Add(item);}catch(IOException){}catch(JsonException){}catch(UnauthorizedAccessException){}}
        GD.Print("EXPERIMENT_VARIANT "+world.Experiment.Variant);
        if(!authority||world.Experiment.Variant==ExperimentVariant.Off)return;
        string logRoot=OS.HasFeature("editor")?"artifacts":Path.Combine(OS.GetUserDataDir(),"playtests");
        string path=args.GetValueOrDefault("experiment-log",reportPath.Length>0?Path.ChangeExtension(reportPath,"events.jsonl"):Path.Combine(logRoot,"v06-"+world.Experiment.Variant+"-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".jsonl"));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        experimentWriter=new StreamWriter(path,false){AutoFlush=true};
        var options=new JsonSerializerOptions{IncludeFields=true};
        simulation.ExperimentLog=e=>experimentWriter.WriteLine(JsonSerializer.Serialize(new{Utc=DateTime.UtcNow.ToString("O"),e.Time,e.Round,e.Variant,e.Kind,e.Actor,e.Position,e.Detail,e.IncidentId},options));
        GD.Print("EXPERIMENT_LOG "+Path.GetFullPath(path));
    }
    // At tree shutdown the engine owns child destruction; do not enqueue audio
    // player destruction from inside _ExitTree while the mixer is stopping.
    public override void _ExitTree(){if(!quitting)WriteVoiceReport();experimentWriter?.Dispose();networkLog?.Dispose();}
    void ObserveExperiment()
    {
        if(!world.Experiment.IsB)return;
        void Seen(string fact){if(experimentFacts.Add(fact))GD.Print("V06_LIVE_FACT "+fact);}
        Seen("leave:"+world.Experiment.Leave);
        if(world.Experiment.Attention.Stage!=AttentionStage.Unaware)Seen("attention:"+world.Experiment.Attention.Stage);
        if(world.Players.Any(p=>p.Active&&p.Bracing))Seen("brace");
    }
}
