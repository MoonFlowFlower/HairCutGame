using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Hairball.Core;

public sealed class DebrisMaterial
{
    public Vector3 Color;
    public float Heat,Glue,Char,Young,Wet;
    public DebrisMaterial Clone()=>(DebrisMaterial)MemberwiseClone();
    public static DebrisMaterial From(Head head,Vector3 point)
    {
        var p=HairSystem.MaterialAt(head,point);var color=p.Wig?new Vector3(.68f,.44f,.73f):head.Barber?new(.35f,.24f,.41f):new Vector3(.47f,.32f,.23f);
        if(head.Material==HeadMaterialKind.Wool&&!head.Barber)color=new(.875f,.804f,.694f);
        if(head.Fragment)color=head.FragmentColor;
        color=HairMaterials.Color(p,color);
        return new(){Color=color,Heat=p.Temperature,Glue=p.Glue,Char=p.Char,Young=p.Young,Wet=p.Wet};
    }
    public void Mix(DebrisMaterial other,float ratio){Color=Vector3.Lerp(Color,other.Color,ratio);Heat+=(other.Heat-Heat)*ratio;Glue+=(other.Glue-Glue)*ratio;Char+=(other.Char-Char)*ratio;Young+=(other.Young-Young)*ratio;Wet+=(other.Wet-Wet)*ratio;}
}
public sealed class DebrisFlight
{
    public int Id;public Vector3 Position,Velocity;public float Mass,Age;public DebrisMaterial Material=new();
    public DebrisFlight Clone()=>new(){Id=Id,Position=Position,Velocity=Velocity,Mass=Mass,Age=Age,Material=Material.Clone()};
}
public sealed class DebrisPile
{
    public int Id;public Vector3 Position;public float Mass;public DebrisMaterial Material=new();
    public DebrisPile Clone()=>new(){Id=Id,Position=Position,Mass=Mass,Material=Material.Clone()};
}
public sealed class DebrisState
{
    public const int MaxFlights=64,MaxPiles=2048;
    public int NextId,Revision;
    public List<DebrisFlight> Flights=new();public List<DebrisPile> Piles=new();
    public float Mass=>Flights.Sum(x=>x.Mass)+Piles.Sum(x=>x.Mass);
    public DebrisState Clone()=>new(){NextId=NextId,Revision=Revision,Flights=Flights.Select(x=>x.Clone()).ToList(),Piles=Piles.Select(x=>x.Clone()).ToList()};
}
public static class DebrisSystem
{
    public static void ReleaseSupport(DebrisState state,Vector3 position,float radius)
    {
        foreach(var pile in state.Piles.Where(p=>p.Position.Y>.15f&&Vector2.Distance(new(p.Position.X,p.Position.Z),new(position.X,position.Z))<radius).ToArray())
        {state.Piles.Remove(pile);Emit(state,pile.Position+Vector3.UnitY*.08f,Vector3.UnitY*.4f,pile.Mass,pile.Material);}
    }
    public static void Emit(DebrisState state,Vector3 position,Vector3 velocity,float mass,DebrisMaterial material)
    {
        if(mass<=.000001f||!float.IsFinite(mass))return;
        var near=state.Flights.FirstOrDefault(f=>Vector3.DistanceSquared(f.Position,position)<.04f&&f.Age<.15f);
        if(near==null&&state.Flights.Count>=DebrisState.MaxFlights)near=state.Flights.MinBy(f=>Vector3.DistanceSquared(f.Position,position));
        if(near!=null){near.Material.Mix(material,mass/(near.Mass+mass));near.Mass+=mass;}
        else state.Flights.Add(new(){Id=state.NextId++,Position=position,Velocity=velocity,Mass=mass,Material=material.Clone()});
        state.Revision++;
    }
    public static void Deposit(DebrisState state,Vector3 position,float mass,DebrisMaterial material)
    {
        if(mass<=.000001f)return;
        int X(Vector3 p)=>(int)MathF.Floor(p.X/.4f); int Z(Vector3 p)=>(int)MathF.Floor(p.Z/.4f);
        var pile=state.Piles.FirstOrDefault(p=>X(p.Position)==X(position)&&Z(p.Position)==Z(position)&&Math.Abs(p.Position.Y-position.Y)<.08f);
        if(pile==null&&state.Piles.Count>=DebrisState.MaxPiles)pile=state.Piles.MinBy(p=>Vector3.DistanceSquared(p.Position,position));
        if(pile==null)state.Piles.Add(new(){Id=state.NextId++,Position=position,Mass=mass,Material=material.Clone()});
        else {pile.Material.Mix(material,mass/(pile.Mass+mass));pile.Mass+=mass;}
        state.Revision++;
    }
    public static void Tick(DebrisState state,float dt,Func<Vector3,Vector3,Vector3?>? cast,float gravity=1)
    {
        if(state.Flights.Count==0)return;
        foreach(var f in state.Flights.ToArray())
        {
            f.Age+=dt;f.Velocity-=Vector3.UnitY*(9.8f*dt*gravity);var next=f.Position+f.Velocity*dt;
            var hit=cast?.Invoke(f.Position,next);
            if(!hit.HasValue&&next.Y<=.015f)hit=new Vector3(next.X,.015f,next.Z);
            if(hit.HasValue||f.Age>5)
            {
                var rest=hit??cast?.Invoke(f.Position,f.Position-Vector3.UnitY*20)??new Vector3(f.Position.X,.015f,f.Position.Z);
                Deposit(state,rest,f.Mass,f.Material);state.Flights.Remove(f);
            }
            else f.Position=new(Math.Clamp(next.X,-5.75f,5.75f),next.Y,Math.Clamp(next.Z,-5.75f,5.75f));
        }
        state.Revision++;
    }
    public static float Vacuum(DebrisState state,Vector3 origin,Vector3 direction,float range,float radius,float budget,DebrisMaterial absorbed,float efficiency=1)
    {
        float total=0;
        bool InCone(Vector3 pos){float along=Vector3.Dot(pos-origin,direction);return along>0&&along<range+.15f&&HairSystem.DistanceToSegment(pos,origin,origin+direction*range)<radius;}
        foreach(var pile in state.Piles.Where(p=>InCone(p.Position)).OrderBy(p=>Vector3.DistanceSquared(origin,p.Position)).ToArray())
        {float yield=Math.Clamp(efficiency,.01f,1)*(efficiency<1&&pile.Material.Char>=.5f?.5f:1);float take=Math.Min(pile.Mass,(budget-total)/yield),gain=take*yield;if(take<=0)break;absorbed.Mix(pile.Material,gain/(total+gain));total+=gain;pile.Mass-=take;if(pile.Mass<.000001f)state.Piles.Remove(pile);}
        foreach(var f in state.Flights.Where(f=>InCone(f.Position)).ToArray())
        {float yield=Math.Clamp(efficiency,.01f,1)*(efficiency<1&&f.Material.Char>=.5f?.5f:1);float take=Math.Min(f.Mass,(budget-total)/yield),gain=take*yield;if(take<=0)break;absorbed.Mix(f.Material,gain/(total+gain));total+=gain;f.Mass-=take;if(f.Mass<.000001f)state.Flights.Remove(f);}
        if(total>0)state.Revision++;return total;
    }
    public static void Blow(DebrisState state,Vector3 origin,Vector3 direction,float range,float radius)
    {
        var velocity=direction*2+Vector3.UnitY*(1.2f+Math.Max(0,-direction.Y*2));
        foreach(var p in state.Piles.Where(p=>Vector3.Dot(p.Position-origin,direction)>0&&HairSystem.DistanceToSegment(p.Position,origin,origin+direction*range)<radius).ToArray())
        {state.Piles.Remove(p);Emit(state,p.Position+Vector3.UnitY*.08f,velocity,p.Mass,p.Material);}
        foreach(var f in state.Flights)if(HairSystem.DistanceToSegment(f.Position,origin,origin+direction*range)<radius)f.Velocity=velocity;
    }
}
