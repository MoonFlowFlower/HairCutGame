using Hairball.Core;
using System.Numerics;

internal static class GoalFixtures
{
    public static void Place(WorldState w)
    {
        PhysicalProps.Stage(w);
        foreach(var p in w.Props){float angle=p.Index*2.399f;Vector3 point=w.Job.Goal switch{1=>new(MathF.Sin(angle)*.15f,.55f,MathF.Cos(angle)*.15f),2=>new(0,.1f,0),3=>new(0,.25f,0),4=>new(MathF.Cos(angle)*.22f,1.25f,MathF.Sin(angle)*.22f),5=>new(-.85f,1.18f,0),6=>new((p.Index-1)*.4f,1.32f,0),7=>new(0,.4f,0),_=>new(2,2,0)};p.Position=w.SharedHead.ToWorld(point);}
    }
    public static void Build(Head head,int goal)
    {
        float Field(ITargetVolume target,Vector3 p)=>target switch{
            BoxVolume b=>Math.Min(b.Half.X-Math.Abs(p.X-b.Center.X),Math.Min(b.Half.Y-Math.Abs(p.Y-b.Center.Y),b.Half.Z-Math.Abs(p.Z-b.Center.Z))),
            CylinderVolume c=>Math.Min(c.HalfHeight-Math.Abs(p.Y-c.Center.Y),Math.Min(c.Radius-new Vector2(p.X-c.Center.X,p.Z-c.Center.Z).Length(),c.Hole>0?new Vector2(p.X-c.Center.X,p.Z-c.Center.Z).Length()-c.Hole:999)),
            UnionVolume u=>u.Parts.Max(t=>Field(t,p)),_=>-1};
        head.Volume.Fill(p=>Field(Goals.All[goal].Target,p));
        foreach(var p in head.Patches)
        {
            float radius=new Vector2(p.Root.X,p.Root.Z).Length();
            float height=goal switch {0=>.9f,1 or 7=>.88f,2=>1.5f,3=>1.8f,4=>radius<.3f?1.3f:radius<.47f?.9f:.55f,5=>1.13f,6=>1.42f,_=>.8f};
            Vector3 target=new(p.Root.X*(goal==5?1.8f:1),height,p.Root.Z);
            var d=target-p.Root;p.Length=d.Length();p.Direction=Vector3.Normalize(d);p.Glue=1;
            if(goal is 1 or 2 or 7 && radius<.27f)p.Length=0;
        }
    }
}
