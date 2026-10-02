using Godot;
using Hairball.Core;
using System;
using System.Linq;
namespace Hairball;
public partial class SalonView
{
    void SyncMoldView(Node3D view,PropState prop)
    {
        if(prop.Mold==MoldKind.None)return;
        view.Scale=Vector3.One*prop.MaterialScale;
        string stamp=prop.Holes.Count+":"+prop.Pinned;
        if(view.GetMeta("mold_stamp","").AsString()==stamp)return;view.SetMeta("mold_stamp",stamp);
        foreach(var child in view.GetChildren()){view.RemoveChild(child);child.QueueFree();}
        var color=prop.Pinned?new Color("efac55"):new Color(.4f,.8f,.84f,.42f);
        bool Hole(Vector3 pos)=>prop.Holes.Any(h=>Art.V(h).DistanceTo(pos)<MoldSystem.HoleRadius);
        void Tile(Vector3 p,Vector3 size){if(!Hole(p))Art.Box(view,p,size,color);}
        if(prop.Mold==MoldKind.Pad){
            for(int x=-2;x<=2;x++)for(int z=-2;z<=2;z++){Tile(new(x*.13f,.12f,z*.115f),new(.125f,.022f,.11f));Tile(new(x*.13f,-.12f,z*.115f),new(.125f,.022f,.11f));}
            for(int i=-2;i<=2;i++)foreach(int side in new[]{-1,1}){Tile(new(side*.33f,0,i*.115f),new(.025f,.24f,.11f));Tile(new(i*.13f,0,side*.29f),new(.125f,.24f,.025f));}
        }else{
            float radius=prop.Mold==MoldKind.Ring?.39f:.36f;
            for(int i=0;i<24;i++){float a=i*Mathf.Tau/24;var pos=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);if(!Hole(pos)){var segment=Art.Box(view,pos,new(.09f,.26f,.028f),color);segment.Rotation=new(0,-a+Mathf.Pi/2,0);}if(prop.Mold==MoldKind.Ring){pos*=.15f/radius;Tile(pos,new(.07f,.24f,.025f));}}
            for(int x=-2;x<=2;x++)for(int z=-2;z<=2;z++){var pos=new Vector3(x*.13f,-.14f,z*.13f);if(MoldSystem.Interior(prop.Mold,Art.N(pos+Vector3.Up*.03f))>0)Tile(pos,new(.125f,.025f,.125f));}
        }
        Art.Label(view,prop.Pinned?"MOLD · PINNED":"MOLD · HOLD OR PIN",new(0,.43f,0),16);
    }
    int discoveryCount=-1;
    void SyncDiscoveries(WorldState w)
    {
        if(!w.Experiment.IsB)return;
        if(w.Experiment.Discoveries.Count==discoveryCount)return;discoveryCount=w.Experiment.Discoveries.Count;
        try{
            System.IO.Directory.CreateDirectory(galleryPath);string file=System.IO.Path.Combine(galleryPath,"combos.json");
            Combo[] existing=[];if(System.IO.File.Exists(file))try{existing=System.Text.Json.JsonSerializer.Deserialize<Combo[]>(System.IO.File.ReadAllText(file))??[];}catch(System.Text.Json.JsonException){}
            var all=existing.Where(Enum.IsDefined).Concat(w.Experiment.Discoveries).Distinct().Take(4).ToArray();System.IO.File.WriteAllText(file,System.Text.Json.JsonSerializer.Serialize(all));
            if(GetNodeOrNull<Label3D>("ComboWall") is {} old){RemoveChild(old);old.QueueFree();}
            var wall=Art.Label(this,"",new(-5.80f,2.9f,-3.4f),16);wall.Name="ComboWall";((LocalizedLabel3D)wall).DynamicText=()=>L.T("COMBO WALL")+"\n"+string.Join("\n",all.Select(ComboText.Text));wall.Text=((LocalizedLabel3D)wall).DynamicText();
        }catch(System.IO.IOException e){GD.Print("COMBO_WALL_UNAVAILABLE "+e.Message);}catch(UnauthorizedAccessException e){GD.Print("COMBO_WALL_UNAVAILABLE "+e.Message);}
    }
}
