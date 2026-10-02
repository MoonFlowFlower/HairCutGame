using Godot;
using Hairball.Core;
using System;
using System.Linq;
namespace Hairball;
public partial class Main
{
    void StageMaterialLab()
    {
        if(!args.ContainsKey("material-lab"))return;
        world.Heads.RemoveAll(h=>h.Id is >=900 and <906);
        for(int i=0;i<6;i++)
        {
            var h=Head.Create(900+i,0);h.Position=new(-3+i*1.2f,1.3f,-2.1f);h.GeometryScale=.62f;
            foreach(var p in h.Patches)HairMaterials.Set(p,(HairState)i);
            world.Heads.Add(h);
            if(salon!.GetNodeOrNull<Node3D>($"Sample_{i}")==null)
            {
                var stand=new Node3D{Name=$"Sample_{i}"};salon.AddChild(stand);
                Art.Box(stand,Art.V(h.Position)-Vector3.Up*.7f,new(.65f,1.1f,.65f),new("526574"));
                Art.Ball(stand,Art.V(h.Position),new(.45f,.44f,.4f),new("ecc69f"));
                Art.Label(stand,L.T(((HairState)i).ToString()),Art.V(h.Position)+Vector3.Up*.82f,20);
            }
        }
        world.Phase=Phase.Build;world.Remaining=99999;
    }
}
