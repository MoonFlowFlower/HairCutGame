using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
namespace Hairball.Core;
public enum StyleAttribute {Height,Side,Front,Horns,Wet,Ice,Char,Glue,Young,Wig,Object,Volume}
public sealed record StyleRule(StyleAttribute Attribute,float Min,float Max)
{
    public bool Match(float value)=>float.IsFinite(value)&&value>=Min&&value<=Max;
}
public sealed record StyleCheck(StyleRule Rule,float Actual,bool Match);
public static class StyleAttributes
{
    static readonly float NormalMass=HairVolume.Create().Mass;
    // All coordinates are in the shared head's frame; chair yaw must not change style.
    public static Dictionary<StyleAttribute,float> Evaluate(WorldState w)
    {
        var head=w.SharedHead;var points=new List<(Vector3 Point,Patch Patch)>();
        foreach(var h in w.Heads.Where(h=>h.Id==0||h.AttachedTo==0&&!h.Facial))
            foreach(var v in h.Volume.Samples(1))points.Add((head.ToLocal(h.ToWorld(v)),HairSystem.MaterialAt(h,v)));
        float Fraction(Func<Patch,bool> test)=>points.Count==0?0:(float)points.Count(p=>test(p.Patch))/points.Count;
        var center=points.Count==0?Vector3.Zero:points.Aggregate(Vector3.Zero,(v,p)=>v+p.Point)/points.Count;
        var cells=points.Select(p=>((int)MathF.Round(p.Point.X/HairVolume.Step),(int)MathF.Round(p.Point.Y/HairVolume.Step),(int)MathF.Round(p.Point.Z/HairVolume.Step))).ToHashSet();
        float symmetry=cells.Count==0?1:(float)cells.Count(p=>cells.Contains((-p.Item1,p.Item2,p.Item3)))/cells.Count;
        int side=Math.Abs(center.X)<.12f&&symmetry>=.72f?0:center.X<0?-1:1;
        float height=points.Count==0?0:Math.Max(0,points.Max(p=>p.Point.Y));
        var prop=w.Props.FirstOrDefault(p=>p.Id==w.Experiment.HelicopterId&&p.Attached);
        int place=0;if(prop!=null){var v=head.ToLocal(prop.Position);place=Math.Abs(v.X)>Math.Abs(v.Z)?(v.X<-.25f?3:v.X>.25f?4:0):(v.Z>.25f?1:v.Z<-.25f?2:0);}
        return new(){[StyleAttribute.Height]=height,[StyleAttribute.Side]=side,[StyleAttribute.Front]=Math.Abs(center.Z)<.12f?0:Math.Sign(center.Z),[StyleAttribute.Horns]=Peaks(points.Select(p=>p.Point)),[StyleAttribute.Wet]=Fraction(p=>p.Wet>=.5f),[StyleAttribute.Ice]=Fraction(p=>p.Frozen),[StyleAttribute.Char]=Fraction(p=>p.Char>=.5f),[StyleAttribute.Glue]=Fraction(p=>p.Glue>=.5f),[StyleAttribute.Young]=Fraction(p=>p.Young>=.5f),[StyleAttribute.Wig]=w.Heads.Any(h=>h.AttachedTo==0&&h.Patches.Any(p=>p.Wig))?1:0,[StyleAttribute.Object]=place,[StyleAttribute.Volume]=w.Heads.Where(h=>h.Id==0||h.AttachedTo==0&&!h.Facial).Sum(h=>h.Mass)/NormalMass};
    }
    // Height-map peaks with a .28 m prominence and .42 m merge radius. A flat ridge is one peak.
    public static int Peaks(IEnumerable<Vector3> points)
    {
        var map=new Dictionary<(int X,int Z),float>();foreach(var p in points){var k=((int)MathF.Round(p.X/HairVolume.Step),(int)MathF.Round(p.Z/HairVolume.Step));map[k]=Math.Max(map.GetValueOrDefault(k,-1),p.Y);}
        var candidates=new HashSet<(int X,int Z)>();
        foreach(var cell in map.Where(p=>p.Value>=1.0f)){
            float low=cell.Value;bool highest=true;
            for(int x=-4;x<=4;x++)for(int z=-4;z<=4;z++){int d=x*x+z*z;if(d==0||d>16)continue;float h=map.GetValueOrDefault((cell.Key.X+x,cell.Key.Z+z),0);if(d<=4&&h>cell.Value+.01f)highest=false;if(d>=9)low=Math.Min(low,h);}
            if(highest&&cell.Value-low>=.28f)candidates.Add(cell.Key);
        }
        int count=0;while(candidates.Count>0){count++;var queue=new Queue<(int X,int Z)>();var first=candidates.First();candidates.Remove(first);queue.Enqueue(first);while(queue.TryDequeue(out var p)){foreach(var q in candidates.Where(q=>(q.X-p.X)*(q.X-p.X)+(q.Z-p.Z)*(q.Z-p.Z)<=9).ToArray()){candidates.Remove(q);queue.Enqueue(q);}}}return Math.Min(3,count);
    }
    public static string Name(StyleAttribute a)=>L.Locale=="zh"?new[]{"高度","左右偏向","前后重心","尖角数量","湿润占比","冰冻占比","焦发占比","上胶占比","新生占比","假发","委托物位置","总体积"}[(int)a]:new[]{"Height","Left/right balance","Front/back balance","Peaks","Wet share","Frozen share","Charred share","Glued share","New growth share","Wig","Commission position","Volume"}[(int)a];
    public static string Text(StyleRule rule)
    {
        bool zh=L.Locale=="zh";string value=rule.Attribute switch{
            StyleAttribute.Side=>rule.Min==0?(zh?"对称":"symmetric"):rule.Min<0?(zh?"偏左":"left"):zh?"偏右":"right",
            StyleAttribute.Front=>rule.Min<0?(zh?"偏后":"back"):rule.Min>0?(zh?"偏前":"front"):zh?"居中":"center",
            StyleAttribute.Wig=>rule.Min>0?(zh?"要假发":"with wig"):zh?"不戴假发":"no wig",
            StyleAttribute.Object=>(zh?new[]{"顶","前","后","左","右"}:new[]{"top","front","back","left","right"})[(int)rule.Min],
            StyleAttribute.Height=>rule.Min<=.001f&&rule.Max<=.8f?(zh?"低":"low"):rule.Min>=1.35f?(zh?"冲天":"sky high"):$"{rule.Min:0.0}–{rule.Max:0.0} m",
            StyleAttribute.Volume=>rule.Max<=.55f?(zh?"稀":"sparse"):rule.Min>=1.8f?(zh?"爆炸":"explosive"):zh?"正常":"normal",
            StyleAttribute.Horns=>rule.Min>=3?(zh?"3 个以上":"3+"):rule.Min==rule.Max?rule.Min.ToString("0"):(zh?"3 个以上":"3+"),
            _=>rule.Min>=.65f?(zh?"大部分":"mostly"):rule.Max<=.35f?(zh?"少量或没有":"little / none"):$"{rule.Min:P0}–{rule.Max:P0}"};return Name(rule.Attribute)+" · "+value;
    }
}

