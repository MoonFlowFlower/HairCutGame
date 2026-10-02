using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Hairball;

// The board is immutable reference artwork; the world model is an authoritative editable Head.
public partial class TargetReference : Node3D
{
    readonly List<SubViewport> views=new();
    readonly List<Node3D> scenes=new();
    readonly List<Label3D> labels=new();
    Node3D board=null!;
    Label3D title=null!, cue=null!, station=null!;
    int goal=-1;
    string locale="";
    public void Setup(Camera3D camera)
    {
        board=new(){Position=new(-.15f,-.23f,-.85f),Name="OffhandReference"};camera.AddChild(board);
        Art.Box(board,Vector3.Zero,new(.98f,.47f,.024f),new("d6c6a4"));
        Art.Ball(board,new(-.43f,-.19f,.02f),new(.12f,.08f,.08f),new("edc19d"));
        title=Art.Label(board,"",new(0,.195f,.019f),18,new("253644"));title.PixelSize=.0015f;title.OutlineSize=0;
        cue=Art.Label(board,"",new(0,-.19f,.019f),14,new("253644"));cue.PixelSize=.0014f;cue.OutlineSize=0;
        for(int i=0;i<3;i++)
        {
            var viewport=new SubViewport{Size=new(256,256),OwnWorld3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Once};AddChild(viewport);views.Add(viewport);
            var root=new Node3D();viewport.AddChild(root);scenes.Add(root);
            root.AddChild(new WorldEnvironment{Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new("cad9da"),AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=Colors.White,AmbientLightEnergy=.65f}});
            root.AddChild(new DirectionalLight3D{RotationDegrees=new(-40,-35,0),LightEnergy=1.1f});
            var cameraView=new Camera3D{Projection=Camera3D.ProjectionType.Orthogonal,Size=2.65f,Current=true};root.AddChild(cameraView);
            var center=new Vector3(0,.65f,0);cameraView.Position=center+(i==0?new Vector3(0,0,5):i==1?new Vector3(5,0,0):new Vector3(0,5,0));cameraView.LookAt(center,i==2?Vector3.Forward:Vector3.Up);
            var panel=new MeshInstance3D{Position=new((i-1)*.31f,0,.018f),Mesh=new QuadMesh{Size=new(.29f,.29f)},MaterialOverride=new StandardMaterial3D{AlbedoTexture=viewport.GetTexture(),ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded}};board.AddChild(panel);
            var label=Art.Label(board,"",new((i-1)*.31f,-.153f,.019f),14,new("253644"));label.PixelSize=.0014f;label.OutlineSize=0;labels.Add(label);
        }
        Art.Box(this,new(-3, .65f,-3.6f),new(1.15f,1.3f,1.15f),new("526574"));
        Art.Collider(this,new(-3,.65f,-3.6f),new(1.15f,1.3f,1.15f));
        station=Art.Label(this,"",new(-3,2.9f,-3.6f),22,new("f6d58b"));
    }
    static void Model(Node3D parent,int id)
    {
        var model=new Node3D{Name="TargetModel"};parent.AddChild(model);
        var person=Art.Person(model,new("577c8d"));person.Position=new(0,-1.64f,0);
        var head=GoalMaterials.Reference(id);var view=new HeadView{referenceGoal=id};model.AddChild(view);view.Update(head,0,1,true);
        for(int i=0;i<PhysicalProps.Count(id);i++){var prop=SalonView.CreateProp(model,id);prop.Position=Art.V(PhysicalProps.ReferencePosition(id,i));}
    }
    public void Sync(WorldState world,PlayerState player,bool replay)
    {
        Visible=!world.Experiment.IsB;
        if(world.Experiment.IsB){board.Visible=false;return;}
        board.Visible=player.ReferenceUp&&!replay&&world.Phase is Phase.Choice or Phase.Preview or Phase.Build;
        if(goal!=world.Job.Goal)
        {
            goal=world.Job.Goal;
            var bounds=TargetProjection.Bounds(Goals.All[goal].Target);float top=Math.Max(bounds.Max.Y,Enumerable.Range(0,PhysicalProps.Count(goal)).Max(i=>PhysicalProps.ReferencePosition(goal,i).Y+.4f));
            for(int i=0;i<scenes.Count;i++){var cam=scenes[i].GetChildren().OfType<Camera3D>().Single();var center=new Vector3(0,(top-.65f)/2,0);cam.Size=Math.Max(2.65f,top+.85f);cam.Position=center+(i==0?new Vector3(0,0,5):i==1?new Vector3(5,0,0):new Vector3(0,5,0));cam.LookAt(center,i==2?Vector3.Forward:Vector3.Up);}
            foreach(var root in scenes){root.GetNodeOrNull<Node3D>("TargetModel")?.Free();Model(root,goal);}
            foreach(var view in views)view.RenderTargetUpdateMode=SubViewport.UpdateMode.Once;
            locale="";
        }
        if(locale!=L.Locale)
        {
            locale=L.Locale;title.Text=L.T(Goals.All[goal].Name);station.Text=L.T("PRACTICE MODEL • E carry • tools affect hair");
            cue.Text=L.T(goal==4?"Char the outer rim • place the candles":goal==1?"Soft nest • place three eggs":goal==0?"Flat + firm • helicopter landing":"Shape + material + real prop test");
            for(int i=0;i<3;i++)labels[i].Text=L.T(new[]{"FRONT","SIDE","TOP"}[i]);
        }
    }
}
