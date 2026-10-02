using Godot;
using Hairball.Core;
using System;
using NVector=System.Numerics.Vector3;

namespace Hairball;
// Deterministic visual review uses the production density brushes and production HeadView.
public static class HairShellChecks
{
    public static Head Example(int state)
    {
        var h=Head.Create(state,1);
        switch(state)
        {
            case 1:h.Volume.Brush(new(EffectKind.CutPlane,1,NVector.UnitY),new(0,.56f,0),1.2f);break;
            case 2:h.Volume.Brush(new(EffectKind.Puncture,1,-NVector.UnitZ),new(0,.6f,.4f),.16f,new(0,.6f,1),3);break;
            case 3:
                h.Volume.Erode(.23f,p=>Math.Clamp(1-NVector.Distance(p,new(.4f,.65f,.05f))/.6f,0,1));
                foreach(var p in h.Patches)if(p.Root.X>.1f){p.Char=.9f;p.Length*=.7f;}
                break;
            case 4:foreach(var p in h.Patches)HairSystem.Apply(h,p,new(EffectKind.ChangeTemperature,-90));break;
            case 5:
                // Many broad overlapping passes, as a player can paint over the exterior.
                for(int j=0;j<1;j++)for(int i=0;i<12;i++)
                {float a=i*MathF.PI/6;var origin=new NVector(MathF.Cos(a)*2,1.5f,MathF.Sin(a)*2);var dir=NVector.Normalize(new NVector(0,.35f,0)-origin);if(h.Volume.Raycast(origin,dir,4,out var hit,out _))h.Volume.Brush(new(EffectKind.AddHair,.16f),hit,.36f);}
                break;
        }
        return h;
    }
    public static void Gallery(Node3D root)
    {
        root.AddChild(new WorldEnvironment{Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new("c0cbd1"),AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=Colors.White,AmbientLightEnergy=.55f}});
        root.AddChild(new DirectionalLight3D{RotationDegrees=new(-35,-30,0),LightEnergy=.75f});
        var cam=new Camera3D{Position=new(0,3.45f,12),Projection=Camera3D.ProjectionType.Orthogonal,Size=7.5f,Current=true};root.AddChild(cam);cam.LookAt(new(0,3.45f,0));
        string[] labels=["NORMAL / 整体体积","SHAVED / 削平","HOLE / 贯穿孔","BURNT / 烧蚀","FROZEN / 冻结","OVERGROWN / 膨胀"];
        for(int i=0;i<6;i++)
        {
            var basePosition=new Vector3((i%3-1)*2.4f,i<3?2.5f:0,0);
            var person=Art.Person(root,new("ba7b87"));person.Position=basePosition;
            foreach(var child in person.GetChildren())if(child is Node3D n&&n.Position.Y<1.2f)n.Visible=false;
            Art.Box(person,new(0,1.05f,0),new(.82f,.48f,.35f),new("ba7b87"));
            var h=Example(i);h.Position=Art.N(basePosition+new Vector3(0,1.64f,0));
            var view=new HeadView();root.AddChild(view);view.Update(h,0);
            foreach(var region in new[]{HairRegion.LeftBrow,HairRegion.RightBrow,HairRegion.Beard}){var face=Head.CreateFace(h,region);var facialView=new HeadView();root.AddChild(facialView);facialView.Update(face,0);}
            Art.Label(root,labels[i],basePosition+new Vector3(0,2.8f,0),28,new("24313e"));
            GD.Print($"HAIR_SHELL_STATE {i} triangles={view.SurfaceTriangles} mass={h.Mass:F2}");
        }
    }
}
