using Godot;
using Hairball.Core;
using System.Linq;
using System.Collections.Generic;
namespace Hairball;
public partial class SalonView
{
    readonly Dictionary<int,Node3D> revealMasks=new(),likes=new();
    Node3D? floorRoute,projectileViews;
    void SyncCustomerPresentation(WorldState w)
    {
        if(floorRoute==null){floorRoute=new();AddChild(floorRoute);for(int i=0;i<12;i++){float t=i/11f;var pos=Art.V(System.Numerics.Vector3.Lerp(PartyLoop.MirrorRoot,PartyLoop.DoorRoot,t));Art.Box(floorRoute,pos+Vector3.Up*.02f,new(.22f,.03f,.4f),new("f9d56c"));}}
        floorRoute.Visible=w.Experiment.CustomerActor!=0&&w.Experiment.LeaveStarted>=0&&!w.Experiment.Resolved;
        foreach(var p in w.Players.Where(p=>p.Active)){
            if(!revealMasks.TryGetValue(p.Id,out var face)){face=new();AddChild(face);Art.Ball(face,Vector3.Zero,new(.34f,.18f,.13f),new("fff0cf"));revealMasks[p.Id]=face;}
            face.Visible=w.Time<p.CosmeticUntil;face.Position=Art.V(p.Customer?w.SharedHead.Position:Session.Eye(p))+Vector3.Back.Rotated(Vector3.Up,p.Yaw+Mathf.Pi)*.22f;face.Rotation=new(0,p.Yaw+Mathf.Pi,0);
            ((MeshInstance3D)face.GetChild(0)).MaterialOverride=Art.Material(p.CosmeticKind==0?new Color("fff0cf"):new Color("94683f"));
            if(!likes.TryGetValue(p.Id,out var like)){like=new();AddChild(like);Art.Box(like,Vector3.Zero,new(.25f,.28f,.10f),new("ffe077"));Art.Box(like,new(-.18f,.16f,0),new(.10f,.30f,.10f),new("ffe077"));Art.Ball(like,new(0,-.20f,0),new(.13f,.08f,.13f),new("ffe077"));likes[p.Id]=like;}
            like.Visible=w.Time<p.LikedUntil;like.Position=Art.V(p.Position)+Vector3.Up*2.5f;
        }
        if(projectileViews!=null){RemoveChild(projectileViews);projectileViews.QueueFree();}projectileViews=new();AddChild(projectileViews);
        foreach(var shot in w.Projectiles){var node=Art.Ball(projectileViews,Art.V(shot.Position),shot.Kind==0?new(.17f,.07f,.17f):new(.12f,.16f,.12f),shot.Kind==0?new("fff0cf"):new("94683f"));node.Rotation=new(w.Time*5,0,.2f);}
    }
}
