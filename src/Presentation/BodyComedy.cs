using Godot;
using Hairball.Core;
using System;
using System.Linq;
using System.Collections.Generic;
namespace Hairball;
public partial class SalonView
{
    readonly Dictionary<int,Label3D> downLabels=new();
    MeshInstance3D? hose,scissorLink;
    void SyncBodyComedy(WorldState w,int localId)
    {
        foreach(var p in w.Players.Where(p=>p.Active))if(Bodies.TryGetValue(p.Id,out var body)){
            body.Model.Visible=!p.Customer;bool down=w.Time<p.DownUntil;body.Model.Position=down?new(0,.15f,.6f):Vector3.Zero;body.Model.Rotation=new(down?Mathf.Pi/2:0,p.Yaw+Mathf.Pi,0);
            if(!downLabels.TryGetValue(p.Id,out var label)){label=Art.Label(this,"",Vector3.Zero,22,new("ffd279"));downLabels[p.Id]=label;}
            label.Visible=down||w.Time<p.HitUntil;label.Position=body.Position+Vector3.Up*2.4f;label.Text=down?(L.Locale=="zh"?"E · 拍醒":"E · SLAP AWAKE"):"✦";
        }
        hose??=Art.Cylinder(this,Vector3.Zero,.018f,1,new("e5c165"));scissorLink??=Art.Box(this,Vector3.Zero,new(.05f,1,.10f),new("d8e7eb"));
        var n=w.Props.FirstOrDefault(p=>p.Coop==CoopKind.Nozzle);var tank=w.Props.FirstOrDefault(p=>p.Coop==CoopKind.Tank);
        void Link(MeshInstance3D view,PropState? a,PropState? b){view.Visible=a!=null&&b!=null&&a.Holder!=0&&b.Holder!=0;if(!view.Visible)return;var from=Art.V(a!.Position);var to=Art.V(b!.Position);var d=to-from;view.Position=(from+to)*.5f;view.Quaternion=d.LengthSquared()>.001f?new Quaternion(Vector3.Up,d.Normalized()):Quaternion.Identity;view.Scale=new(1,Math.Max(.01f,d.Length()),1);}
        Link(hose,n,tank);Link(scissorLink,w.Props.FirstOrDefault(p=>p.Coop==CoopKind.ScissorA),w.Props.FirstOrDefault(p=>p.Coop==CoopKind.ScissorB));
    }
    void SyncCoopView(Node3D view,PropState prop,WorldState w)
    {
        if(prop.Coop==CoopKind.None||prop.Coop==CoopKind.LargePad)return;
        if(!view.HasMeta("coop")){view.SetMeta("coop",true);if(prop.Coop==CoopKind.Tank){Art.Cylinder(view,Vector3.Zero,.22f,.6f,new("cd954c"));Art.Cylinder(view,new(0,.38f,0),.08f,.12f,new("3c4b51"));}
            else if(prop.Coop==CoopKind.Nozzle){Art.Box(view,Vector3.Zero,new(.16f,.20f,.50f),new("4d8e9e"));Art.Cylinder(view,new(0,0,-.36f),.07f,.25f,new("d7e8eb")).RotationDegrees=new(90,0,0);}
            else {Art.Cylinder(view,Vector3.Zero,.20f,.08f,new("f08e65"));Art.Box(view,new(0,0,-.42f),new(.09f,.05f,.7f),new("d7e8eb"));}
            Art.Label(view,"",new(0,.55f,0),17,new("ffe29d")).Name="CoopHint";
        }
        string name=prop.Coop switch{CoopKind.Nozzle=>L.Locale=="zh"?"管线喷头 · 队友泵液":"HOSE NOZZLE · TEAMMATE PUMPS",CoopKind.Tank=>L.Locale=="zh"?"泵罐 · 左键出液 / 右键切换":"PUMP TANK · LMB PUMP / RMB CHANGE",_=>L.Locale=="zh"?"大剪刀 · 两人同时左键":"GIANT SCISSORS · BOTH PRESS LMB"};
        if(prop.Coop==CoopKind.Tank)name+="\n"+(L.Locale=="zh"?new[]{"胶水","液氮","清水"}:new[]{"GLUE","NITROGEN","WATER"})[prop.CoopMode];view.GetNode<Label3D>("CoopHint").Text=name;
    }
}
