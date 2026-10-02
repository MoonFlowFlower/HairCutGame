using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Numerics;
namespace Hairball;

// Read-only, conservative articulation test. Deliberately misses wider weak necks.
// A warning requires an actual one-cell cut vertex and a large root-free subtree.
// No mutation, strength score, detach call or networking belongs here.
public sealed class PuppetHairWarning
{
    static readonly (int X,int Y,int Z)[] Neighbours = {
        (1,0,0),(0,1,0),(0,0,1),(1,1,0),(1,0,1),(0,1,1),(1,1,1),
        (-1,0,0),(0,-1,0),(0,0,-1),(-1,-1,0),(-1,0,-1),(0,-1,-1),(-1,-1,-1)
    };
    public readonly bool[] Island=new bool[HairVolume.Count];
    public int Cells {get;private set;} public Vector3 Neck {get;private set;}
    public float Weight(Vector3 p)
    {
        var q=(p-HairVolume.Min)/HairVolume.Step;
        int x=(int)MathF.Round(q.X),y=(int)MathF.Round(q.Y),z=(int)MathF.Round(q.Z);
        return x>=0&&y>=0&&z>=0&&x<HairVolume.NX&&y<HairVolume.NY&&z<HairVolume.NZ&&Island[HairVolume.Index(x,y,z)]?1:0;
    }
    public static PuppetHairWarning Evaluate(HairVolume volume,Func<Vector3,bool> support)
    {
        var result=new PuppetHairWarning();int count=HairVolume.Count,time=0;
        var entered=new int[count];var low=new int[count];var parent=new int[count];Array.Fill(parent,-1);
        var sizes=new int[count];var roots=new int[count];var order=new List<int>();
        var candidates=new List<(int Child,int Neck)>();
        for(int start=0;start<count;start++)
        {
            if(volume.Data[start]<128||entered[start]!=0)continue;
            int componentBegin=candidates.Count;
            void Enter(int cell){entered[cell]=low[cell]=++time;sizes[cell]=1;roots[cell]=support(HairVolume.Position(cell%HairVolume.NX,cell/HairVolume.NX%HairVolume.NY,cell/(HairVolume.NX*HairVolume.NY)))?1:0;order.Add(cell);}
            var stack=new Stack<(int Cell,int Next)>();Enter(start);stack.Push((start,0));
            while(stack.TryPop(out var frame))
            {
                int cell=frame.Cell;
                if(frame.Next<Neighbours.Length)
                {
                    stack.Push((cell,frame.Next+1));var d=Neighbours[frame.Next];
                    int x=cell%HairVolume.NX+d.X,y=cell/HairVolume.NX%HairVolume.NY+d.Y,z=cell/(HairVolume.NX*HairVolume.NY)+d.Z;
                    if(x<0||y<0||z<0||x>=HairVolume.NX||y>=HairVolume.NY||z>=HairVolume.NZ)continue;
                    int other=HairVolume.Index(x,y,z);if(volume.Data[other]<128||other==parent[cell])continue;
                    if(entered[other]==0){parent[other]=cell;Enter(other);stack.Push((other,0));}
                    else low[cell]=Math.Min(low[cell],entered[other]);
                }
                else if(parent[cell]>=0)
                {
                    int above=parent[cell];sizes[above]+=sizes[cell];roots[above]+=roots[cell];low[above]=Math.Min(low[above],low[cell]);
                    if(low[cell]>=entered[above]&&roots[cell]==0&&sizes[cell]>=64)candidates.Add((cell,above));
                }
            }
            if(roots[start]==0)candidates.RemoveRange(componentBegin,candidates.Count-componentBegin);
        }
        int best=-1,neck=-1;
        foreach(var c in candidates)if(best<0||sizes[c.Child]>sizes[best]){best=c.Child;neck=c.Neck;}
        if(best<0)return result;
        result.Cells=sizes[best];result.Neck=HairVolume.Position(neck%HairVolume.NX,neck/HairVolume.NX%HairVolume.NY,neck/(HairVolume.NX*HairVolume.NY));
        foreach(int cell in order)if(entered[cell]>=entered[best]&&entered[cell]<entered[best]+sizes[best])result.Island[cell]=true;
        return result;
    }
}
