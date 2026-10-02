using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Hairball.Core;

public sealed record ToolDefinition(string Id, string Name, float Range, float Radius, int Charges, float Cooldown, string Hint, params HairEffect[] Effects);
public static class Tools
{
    public static readonly ToolDefinition[] All =
    [
        new("clipper", "Precision Clipper", 3, .16f, -1, .12f, "Shallow finishing removal • RMB fine", new HairEffect(EffectKind.RemoveHair, .12f)),
        new("growth", "Super Growth Spray", 4, .3f, -1, .12f, "Extrude soft Young hair • wind carries mist", new HairEffect(EffectKind.AddHair, .16f)),
        new("vacuum", "Transfer Cannon", 5, .4f, -1, .16f, "LMB suck / RMB transfer stored hair", new HairEffect(EffectKind.RemoveHair, .18f)),
        new("blower", "Industrial Blower", 6, .65f, -1, .15f, "LMB push • RMB press / organize existing hair", new HairEffect(EffectKind.ApplyForce, .36f)),
        new("glue", "Hot Glue Gun", 4, .3f, 100, .15f, "Join touching hair • fix shape • heat releases", new HairEffect(EffectKind.Glue, .35f)),
        new("nitrogen", "Liquid Nitrogen", 4, .4f, 100, .15f, "Freeze rigid • heavy impacts shatter", new HairEffect(EffectKind.ChangeTemperature, -55)),
        new("nail", "Anchor Gun", 10, .2f, 12, .5f, "Pin hair tips in space • stretch moving hair", new HairEffect(EffectKind.Anchor, 1)),
        new("sniper", "Hair Sniper", 22, .085f, 5, .8f, "Puncture through aligned targets / RMB zoom", new HairEffect(EffectKind.Puncture, .7f), new HairEffect(EffectKind.ApplyForce, 2)),
        new("flame", "Flamethrower", 5, .55f, 65, .15f, "Fast removal • heat thaws and fire spreads", new HairEffect(EffectKind.ChangeTemperature, 65), new HairEffect(EffectKind.Ignite, 1)),
        new("trimmer", "Hedge Trimmer", 3.5f, .7f, 100, .18f, "LMB level plane • RMB aim-facing sever", new HairEffect(EffectKind.CutPlane, 1)),
        new("water", "Water Mister", 4, .4f, -1, .15f, "Wet hair • damp heat and weaken fresh glue", new HairEffect(EffectKind.Wet,.3f)),
    ];
    public static ToolDefinition Get(int id) => All[Math.Clamp(id, 0, All.Length - 1)];
}

public interface ITargetVolume { bool Contains(Vector3 point, float tolerance = 0); }
public sealed record BoxVolume(Vector3 Center, Vector3 Half) : ITargetVolume
{
    public bool Contains(Vector3 p, float t = 0) { var d = Vector3.Abs(p - Center); return d.X <= Half.X + t && d.Y <= Half.Y + t && d.Z <= Half.Z + t; }
}
public sealed record CylinderVolume(Vector3 Center, float Radius, float HalfHeight, float Hole = 0) : ITargetVolume
{
    public bool Contains(Vector3 p, float t = 0) { p -= Center; float r = MathF.Sqrt(p.X * p.X + p.Z * p.Z); return r <= Radius + t && r >= Math.Max(0, Hole - t) && Math.Abs(p.Y) <= HalfHeight + t; }
}
public sealed record UnionVolume(params ITargetVolume[] Parts) : ITargetVolume { public bool Contains(Vector3 p, float t = 0) => Parts.Any(x => x.Contains(p, t)); }
public sealed record GoalDefinition(int Id, string Name, string Instruction, int Band, string Validation, ITargetVolume Target)
{
    public Vector3[] TargetSamples { get; } = Sample(Target);
    static Vector3[] Sample(ITargetVolume target)
    {
        var list = new List<Vector3>();
        for (float x = -1.2f; x <= 1.2f; x += .16f) for (float y = .12f; y <= 2.6f; y += .16f) for (float z = -.9f; z <= .9f; z += .16f)
            if (target.Contains(new(x, y, z))) list.Add(new(x, y, z));
        return list.ToArray();
    }
}
public static class Goals
{
    // All are intentionally the same prototype difficulty band; tolerances favor readable rough constructions.
    public static readonly GoalDefinition[] All =
    [
        new(0,"Helipad", "Grow a broad flat top, then glue or freeze it for landing.",1,"LANDING / rotor wash",
            new UnionVolume(new BoxVolume(new(0,.7f,0),new(.68f,.18f,.5f)),new CylinderVolume(new(0,.35f,0),.45f,.3f))),
        new(1,"Bird Nest", "Grow a rim; clear the middle to hold three eggs during a spin.",1,"EGGS / chair spin", new UnionVolume(new CylinderVolume(new(0,.55f,0),.62f,.43f,.22f),new CylinderVolume(new(0,.23f,0),.45f,.11f))),
        new(2,"Rocket Silo", "Grow a tall ring. Clear the central launch tunnel.",1,"LAUNCH / tunnel clearance", new CylinderVolume(new(0,.85f,0),.58f,.75f,.2f)),
        new(3,"Cat Tree", "Grow a tall trunk and a broad top; stiffen it for the cat.",1,"CAT / climb and stay",
            new UnionVolume(new CylinderVolume(new(0,.8f,0),.28f,.7f),new BoxVolume(new(0,1.6f,0),new(.65f,.18f,.45f)))),
        new(4,"Birthday Cake", "Trim three tiers. Char the outer rim and place five candles upright.",1,"CANDLES / five flames",
            new UnionVolume(new CylinderVolume(new(0,.35f,0),.65f,.25f),new CylinderVolume(new(0,.75f,0),.44f,.2f),new CylinderVolume(new(0,1.1f,0),.24f,.18f))),
        new(5,"Hair Bridge", "Grow and bend a connected span, then glue or anchor it.",1,"CART / cross the span",
            new UnionVolume(new BoxVolume(new(0,1,0),new(.95f,.18f,.3f)),new BoxVolume(new(-.5f,.5f,0),new(.22f,.5f,.3f)),new BoxVolume(new(.5f,.5f,0),new(.22f,.5f,.3f)))),
        new(6,"Underwear Line", "Make two tall posts and a stiff crossbar for three pants.",1,"FAN / laundry test",
            new UnionVolume(new BoxVolume(new(-.46f,.7f,0),new(.18f,.6f,.22f)),new BoxVolume(new(.46f,.7f,0),new(.18f,.6f,.22f)),new BoxVolume(new(0,1.3f,0),new(.64f,.12f,.22f)))),
        new(7,"Toilet Hair", "Make a deep bowl with an open center. Hold and flush water.",1,"WATER / fill and flush", new CylinderVolume(new(0,.55f,0),.62f,.42f,.25f))
    ];
    public static int[] Choices(int seed, int round, int slot)
    {
        var rng = new Random(seed + round * 97 + slot * 619);
        return All.OrderBy(_ => rng.Next()).Take(3).Select(g => g.Id).ToArray();
    }
}

public sealed class Score
{
    public int Goal=-1;
    public float Shape, Function, Prop, Penalty, State;
    public bool FunctionOnly;
    public float Final {get{if(FunctionOnly)return Math.Clamp(Function-Penalty,0,100); if(Goal<0)return Math.Clamp(Shape*.7f+Function*.2f+Prop*.1f-Penalty,0,100);var weight=GoalMaterials.Weights(Goal);float result=Shape*weight.Shape+State*weight.State+Function*weight.Function-Penalty;return Math.Clamp(Math.Min(result,Function<50?69:weight.State>0&&State<40?79:100),0,100);}}
}
public static class Scoring
{
    public static float Shape(IEnumerable<Vector3> hair, GoalDefinition goal)
    {
        var samples = hair.ToArray();
        if (samples.Length == 0 || goal.TargetSamples.Length == 0) return 0;
        float precision = (float)samples.Count(p => goal.Target.Contains(p, .15f)) / samples.Length;
        // Same exact radius query as before, indexed for repeated live observations.
        var cells=new System.Collections.Generic.Dictionary<(int,int,int),System.Collections.Generic.List<Vector3>>();
        (int,int,int) Cell(Vector3 v)=>((int)MathF.Floor(v.X/.24f),(int)MathF.Floor(v.Y/.24f),(int)MathF.Floor(v.Z/.24f));
        foreach(var p in samples){var key=Cell(p);if(!cells.TryGetValue(key,out var list))cells[key]=list=new();list.Add(p);}
        bool Near(Vector3 t){var (a,b,c)=Cell(t);for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)if(cells.TryGetValue((a+x,b+y,c+z),out var points)&&points.Any(p=>Vector3.DistanceSquared(p,t)<.24f*.24f))return true;return false;}
        float coverage=(float)goal.TargetSamples.Count(Near)/goal.TargetSamples.Length;
        return precision + coverage < .0001f ? 0 : 200 * precision * coverage / (precision + coverage);
    }
    public static float Support(Head h, Vector3 local, float radius = .32f)
    {
        float strength=0;
        for(int z=-1;z<=1;z++)for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
        {
            var point=local+new Vector3(x,y,z)*radius*.65f;if(h.Volume.Sample(point)<=0)continue;
            var p=HairSystem.MaterialAt(h,point);strength=Math.Max(strength,Math.Clamp(.25f+.75f*p.Resistance-(p.Burning?.5f:0),0,1));
        }
        return strength;
    }
}

