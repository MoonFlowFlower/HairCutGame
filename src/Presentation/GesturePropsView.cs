using Godot;
using Hairball.Core;
using System;
namespace Hairball;
public partial class SalonView
{
    Node3D? gestureTable;
    void SyncGestureProp(Node3D view,PropState prop)
    {
        if(prop.Gesture<0)return;
        if(gestureTable==null){gestureTable=new();AddChild(gestureTable);Art.Box(gestureTable,new(3.1f,.75f,-3.3f),new(3.9f,.15f,1),new("816a4e"));Art.Collider(gestureTable,new(3.1f,.75f,-3.3f),new(3.9f,.15f,1));var label=Art.Label(gestureTable,"",new(3.1f,1.55f,-3.7f),24);((LocalizedLabel3D)label).DynamicText=()=>L.Locale=="zh"?"比划道具台 · 举给顾客看":"GESTURE TABLE · SHOW THE CUSTOMER";label.Text=((LocalizedLabel3D)label).DynamicText();}
        string stamp=prop.Gesture+":"+L.Locale;if(view.GetMeta("gesture","").AsString()==stamp)return;view.SetMeta("gesture",stamp);foreach(var child in view.GetChildren()){view.RemoveChild(child);child.QueueFree();}
        var c=new Color("e9b977");int shape=prop.Gesture;
        if(shape==0&&L.Locale=="zh"){Art.Box(view,Vector3.Zero,new(.13f,.10f,.5f),new("9dbdc7"));Art.Box(view,new(0,0,.06f),new(.6f,.05f,.18f),new("9dbdc7"));}
        else if(shape==1){Art.Cylinder(view,Vector3.Zero,.28f,.10f,new("748591"));Art.Ball(view,new(0,.11f,0),Vector3.One*.10f,c);}
        else if(shape==2){Art.Cylinder(view,Vector3.Zero,.27f,.13f,new("987749"));for(int i=0;i<8;i++)Art.Box(view,new(MathF.Cos(i)*.2f,.10f,MathF.Sin(i)*.2f),new(.3f,.04f,.04f),new("a78250")).Rotation=new(0,i,0);Art.Ball(view,new(0,.14f,0),Vector3.One*.12f,new("fff0c7"));}
        else if(shape==3){Art.Box(view,Vector3.Zero,new(.3f,.16f,.12f),c);Art.Box(view,new(.15f,.12f,0),new(.12f,.25f,.10f),c);for(int i=0;i<4;i++)Art.Box(view,new((i/2)*.20f-.1f,-.13f,(i%2)*.10f-.05f),new(.04f,.22f,.04f),c);Art.Box(view,new(-.22f,.01f,0),new(.22f,.05f,.05f),new("65503f"));}
        else if(shape==4){Art.Ball(view,Vector3.Zero,Vector3.One*.34f,c);}
        else if(shape==5){Art.Cylinder(view,new(0,-.08f,0),.09f,.32f,new("f2e5c8"));Art.Ball(view,new(0,.12f,0),new(.55f,.20f,.45f),new("d46054"));}
        else if(shape==6){Art.Box(view,Vector3.Zero,new(.55f,.07f,.28f),new("d4ba78"));}
        else if(shape==7||shape==17){for(int i=0;i<8;i++){float a=i*.8f;Art.Ball(view,new(MathF.Sin(a)*.09f,i*.055f,MathF.Cos(a)*.09f),Vector3.One*.11f,new("9dc9d2"));}}
        else if(shape==0||shape==8){for(int i=0;i<4;i++)Art.Cylinder(view,new(0,i*.075f,0),.25f-i*.04f,.09f,new("e6c560"));}
        else if(shape==9){Art.Ball(view,Vector3.Zero,new(.4f,.20f,.28f),new("e0c953"));Art.Ball(view,new(.18f,.14f,0),Vector3.One*.18f,new("e0c953"));}
        else if(shape==10){Art.Cylinder(view,new(0,-.12f,0),.16f,.32f,c,0);Art.Ball(view,new(0,.12f,0),Vector3.One*.32f,new("a6ddea"));}
        else if(shape==11){Art.Cylinder(view,Vector3.Zero,.24f,.12f,new("e5bc4e"));for(int i=0;i<3;i++)Art.Cylinder(view,new(MathF.Cos(i*2.1f)*.2f,.13f,MathF.Sin(i*2.1f)*.2f),.08f,.25f,new("e5bc4e"),0);}
        else if(shape==12){Art.Box(view,Vector3.Zero,new(.3f,.44f,.06f),new("b2d8df"));}
        else if(shape==13){Art.Ball(view,Vector3.Zero,new(.12f,.50f,.05f),new("d09abe"));}
        else if(shape==14){Art.Ball(view,Vector3.Zero,Vector3.One*.34f,new("3c4248"));}
        else if(shape==15||shape==16){Art.Cylinder(view,Vector3.Zero,.02f,.4f,c);Art.Cylinder(view,new(0,.22f,0),.3f,.12f,new("85b9be"),.1f);}
        else if(shape==18){for(int i=0;i<3;i++)Art.Box(view,new(0,i*.055f,0),new(.4f,.05f,.30f),i==1?new("e7cc95"):new("67959d"));}
        else {foreach(int side in new[]{-1,1})Art.Cylinder(view,new(side*.12f,0,0),.18f,.20f,new("e7989c"),0).Rotation=new(0,0,side*Mathf.Pi/2);}
        Art.Label(view,TargetCards.Props[prop.Gesture].Name,new(0,.62f,0),20,new("fff0bc"));
    }
}


