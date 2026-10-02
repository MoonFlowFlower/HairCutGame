using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Hairball;
public partial class SalonView
{
    Node3D? catView;
    Label3D? catCue;
    double returningReplayAt=-1;
    public static Node3D CreateCat(Node3D parent)
    {
        var n=new Node3D();parent.AddChild(n);Art.Ball(n,new(0,.08f,0),new(.62f,.38f,.55f),new("f1c57e"));Art.Ball(n,new(0,.15f,.24f),new(.34f,.31f,.28f),new("ffe3b0"));
        foreach(int side in new[]{-1,1}){Art.Cylinder(n,new(side*.12f,.37f,.22f),.065f,.15f,new("d19a5f"),.001f);Art.Ball(n,new(side*.08f,.19f,.377f),new(.025f,.035f,.012f),new("263d39"));Art.Ball(n,new(side*.22f,-.07f,.15f),new(.08f,.10f,.08f),new("e2b17a"));}
        var tail=Art.Cylinder(n,new(0,.24f,-.36f),.045f,.42f,new("d19a5f"));tail.Rotation=new(.8f,0,0);return n;
    }
    void SyncWorldPresentation(WorldState w,int localId)
    {
        catView??=CreateCat(this);catCue??=Art.Label(this,"",Vector3.Zero,20,new("ffe99f"));var cat=ShopCat.Prop(w);catView.Visible=cat!=null&&w.Cat.Mode!=CatMode.Absent;
        if(cat!=null){catView.Position=Art.V(cat.Position);catView.Rotation=Art.V(cat.Rotation);catView.Scale=w.Cat.Mode==CatMode.Telegraph?new(1,.6f,1):Vector3.One;catCue.Position=catView.Position+Vector3.Up*.65f;catCue.Visible=w.Cat.Mode==CatMode.Telegraph;catCue.Text=L.Locale=="zh"?"盯……":"WATCHING…";}else catCue.Visible=false;
        if(props.TryGetValue($"{w.Round}:{w.Cat.PropId}",out var generic))generic.Visible=false;
        if(w.Phase!=Phase.Complete)returningReplayAt=-1;
        if(w.Phase==Phase.Complete&&w.ReturningHeads&&w.RoundHistory.Count>0){double now=Time.GetTicksMsec()/1000d;if(returningReplayAt<0)returningReplayAt=now;int index=(int)((now-returningReplayAt)/3)%w.RoundHistory.Count;var memory=w.RoundHistory[index];RevealPhoto=roundPhotos.GetValueOrDefault(memory.Round);ReturnPhotoRound=memory.Round;}
    }
    public async System.Threading.Tasks.Task ReturnPhotoForQA(WorldState w,string folder){var png=await RenderPhoto(w,"after");using var image=new Image();image.LoadPngFromBuffer(png);var texture=ImageTexture.CreateFromImage(image);roundPhotos[w.Round]=texture;RevealPhoto=texture;GalleryStore.Save(folder,"returning-"+w.Round,png,new(w.Round,w.Experiment.Success,w.Job.Goal,w.Experiment.Curtain,[],DateTime.UtcNow.ToString("O"),Match:w.MatchTag,Target:w.Experiment.ResultTarget));}
    public int ReturnPhotoRound;
    readonly Dictionary<int,Texture2D> roundPhotos=new();
}
