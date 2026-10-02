using Godot;
using Hairball.Core;
using System;
using System.Linq;
using V=System.Numerics.Vector3;
namespace Hairball;
public static class SculptChecks
{
    static void Check(bool b,string text){if(!b)throw new Exception(text);GD.Print("SCULPT_PASS "+text);}
    public static void Run(Main root,SalonView view,Session session)
    {
        var w=session.State;var p=w.Player(1)!;
        var floor=new DebrisState();DebrisSystem.Emit(floor,new(0,2,-2),V.Zero,2,new(){Color=new(.4f,.2f,.1f)});
        for(int i=0;i<120;i++)DebrisSystem.Tick(floor,1f/60,session.DebrisCast);
        Check(floor.Flights.Count==0&&floor.Piles.Count==1&&floor.Piles[0].Position.Y<.1f,"production floor catches clippings");
        var table=new DebrisState();DebrisSystem.Emit(table,new(-5.25f,2,0),V.Zero,2,new(){Color=new(.4f,.2f,.1f)});
        for(int i=0;i<120;i++)DebrisSystem.Tick(table,1f/60,session.DebrisCast);
        Check(table.Piles.Single().Position.Y>.95f&&table.Piles.Single().Position.Y<1.08f,"production workbench supports clippings");
        var seat=new DebrisState();DebrisSystem.Emit(seat,Session.WorkCenter+new V(0,2,.1f),V.Zero,1,new());
        for(int i=0;i<120;i++)DebrisSystem.Tick(seat,1f/60,session.DebrisCast);
        Check(seat.Piles.Single().Position.Y>.7f&&seat.Piles.Single().Position.Y<.8f,"chair debris rests on visible seat, not floating on player collider");
        view.Debris.Sync(floor,1,.016f,false);Check(view.Debris.RestingCount>0&&view.Debris.FlyingCount==0,"landed material has persistent visible instances");
        DebrisSystem.Emit(floor,new(0,2,-2),V.Zero,1,new(){Color=new(.4f,.2f,.1f)});view.Debris.Sync(floor,2,.016f,false);
        Check(view.Debris.FlyingCount>0&&view.Debris.FlyingCount<=256,"flying chips use bounded MultiMesh instances");
        var replay=new ReplayFrame{Debris=table.Clone(),Heads=w.Heads.Select(h=>h.Clone()).ToList(),Customers=w.Customers,Players=[p.Position],Ladders=w.Ladders};
        w.Debris=floor;float live=w.Debris.Mass;view.Sync(w,1,.016f,replay);Check(w.Debris.Mass==live,"replaying historical debris never deposits into live scene");
        p.Held=0;w.Tools[0].Holder=1;w.Phase=Phase.Build;
        view.Camera.Position=Art.V(w.SharedHead.Position)+new Vector3(0,.5f,2);view.Camera.Rotation=Vector3.Zero;
        view.Brush.Sync(w,p,view.Camera,false,false,null,session.ObstructionDistance);
        Check(view.Brush.Visible&&view.Brush.Mesh.GetSurfaceCount()>0,"production brush preview draws a real surface footprint");
        w.Debris=new();var ground=new V(1.8f,.015f,-2);DebrisSystem.Deposit(w.Debris,ground,.8f,new(){Color=new(.4f,.2f,.1f)});
        p.Position=new(1.8f,0,0);var aim=V.Normalize(ground-Session.Eye(p));p.Yaw=0;p.Pitch=MathF.Asin(aim.Y);p.Held=2;w.Tools[2].Holder=1;p.Cooldown=0;p.Reservoir=0;
        session.UseTool(p,false);
        Check(w.Debris.Mass<.0001f&&Math.Abs(p.Reservoir-.8f)<.0001f,"production vacuum clears floor through real scene obstruction query and recovers all material");
        DebrisSystem.Deposit(w.Debris,ground,1,new());p.Reservoir=20;p.Cooldown=0;session.UseTool(p,false);
        string locale=L.Locale;L.SetLocale("zh");bool translated=L.T(w.Notice).Contains("储量已满");L.SetLocale(locale);
        Check(w.Debris.Mass==1&&w.Notice.Contains("Reservoir full")&&translated,"full vacuum leaves clippings intact and reports capacity in both languages");
        p.Held=3;w.Tools[3].Holder=1;p.Cooldown=0;session.UseTool(p,false);
        Check(w.Debris.Piles.Count==0&&w.Debris.Flights.Count>0&&w.Debris.Flights[0].Velocity.Y>1&&Math.Abs(w.Debris.Mass-1)<.0001f,"production downward blower lifts existing floor material without duplication");
        var settling=new Session{Lab=true,DebrisCast=session.DebrisCast};settling.AddPlayer(1,"Settling");settling.StartMatch();
        var chunk=Head.Create(9000,1);chunk.Loose=chunk.Fragment=true;chunk.Position=new(0,1,-2);chunk.Rotation=new(.3f,.2f,.1f);chunk.Volume.Fill(v=>.22f-v.Length());settling.State.Heads.Add(chunk);float chunkMass=chunk.Mass;
        for(int i=0;i<1100;i++)settling.Tick(1f/60);
        Check(!settling.State.Heads.Any(h=>h.Fragment)&&Math.Abs(settling.State.Debris.Mass-chunkMass)<.01f,"real scene contact remains at rest long enough to merge a rotated fragment after 15 seconds");
        GD.Print("SCULPT_CHECKS_OK");root.QuitGracefully();
    }
}
