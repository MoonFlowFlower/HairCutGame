using Godot;
using Hairball.Core;
using System.Linq;
namespace Hairball;
public partial class SalonView
{
    Label3D? orderSign;
    MeshInstance3D? specialFlash;
    void CreateOrderStation()
    {
        Art.Box(this,new(3,.8f,-4.25f),new(1.85f,.18f,1.2f),new("667f85"));
        Art.Collider(this,new(3,.445f,-4.25f),new(1.85f,.89f,1.2f));
        Art.Box(this,new(3,1.45f,-4.99f),new(1.9f,.95f,.12f),new("293c50"));
        Art.Label(this,"SHARED ORDERS",new(3,2.04f,-4.85f),24,new("f5da86")).Name="OrderTitle";
        for(int i=0;i<3;i++)
        {
            var pos=Art.V(SpecialOrders.Button(i));Art.Box(this,pos,new(.26f,.19f,.14f),i==0?new("87d985"):i==1?new("8ed8ee"):new("ea8c75")).Name="OrderButton_"+i;
            Art.Label(this,new[]{"BURST $35","CRYO $25","CANCEL"}[i],pos+Vector3.Up*.3f,15).Name="OrderLabel_"+i;
        }
        orderSign=Art.Label(this,"",new(3,1.86f,-4.83f),17,new("fff1c1"));
        specialFlash=Art.Ball(this,Vector3.Zero,Vector3.One,new Color(.65f,.95f,.8f,.22f));specialFlash.Visible=false;
    }
    void SyncOrders(WorldState w,ReplayFrame? replay)
    {
        if(orderSign==null)CreateOrderStation();
        GetNode<Label3D>("OrderTitle").Text=L.T(w.Experiment.IsB?"TOOLS ARE SUPPLIED":"SHARED ORDERS");
        for(int i=0;i<3;i++){GetNode<MeshInstance3D>("OrderButton_"+i).Visible=!w.Experiment.IsB;GetNode<Label3D>("OrderLabel_"+i).Visible=!w.Experiment.IsB;}
        GetNode<Label3D>("OrderLabel_0").Text=w.Experiment.IsB?(L.Locale=="zh"?"生长剂已配给":"GROWTH SUPPLIED"):L.T("BURST $35");
        GetNode<MeshInstance3D>("OrderButton_0").MaterialOverride=Art.Material(w.Experiment.IsB?new("5e6264"):new("87d985"));
        orderSign!.Text=w.Order.Pending?L.T("PENDING {0:0.0}s · shared ${1}",System.Math.Max(0,w.Order.CommitAt-w.Time),SpecialOrders.Prices[w.Order.Option]):L.T("SHOP WALLET ${0}",w.Job.Wallet);
        var recent=(replay?.Tools??w.Tools).Where(t=>t.Special>0&&(replay?.Time??w.Time)-t.LastUse is >=0 and <.4f).OrderByDescending(t=>t.LastUse).FirstOrDefault();
        specialFlash!.Visible=recent!=null;
        if(recent!=null){float age=(replay?.Time??w.Time)-recent.LastUse;specialFlash.Position=Art.V(recent.Contact);specialFlash.Scale=Vector3.One*(.4f+age*5);}
    }
}
