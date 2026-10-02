using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hairball;
public partial class LadderView : Node3D
{
    public readonly List<StaticBody3D> Colliders=new();
    public LadderView()
    {
        // Broad treads form a real walkable staircase to a work platform.
        for(int i=0;i<5;i++)
        {
            float height=(i+1)*.25f,z=.72f-i*.36f;
            Art.Box(this,new(0,height-.05f,z),new(.94f,.1f,.38f),new("e9b34d"));
            var body=new StaticBody3D{Position=new(0,height*.5f,z),CollisionLayer=16,CollisionMask=0};AddChild(body);
            body.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=new(.94f,height,.36f)}});Colliders.Add(body);
        }
        foreach(float side in new[]{-.51f,.51f})
        {var rail=Art.Box(this,new(side,.65f,0),new(.065f,1.6f,.065f),new("416b76"));rail.RotationDegrees=new(-48,0,0);}
        Art.Label(this,"E • MOVE LADDER",new(0,1.55f,-.65f),19);
    }
    public void Sync(LadderState state,bool colliding)
    {Position=Art.V(state.Position);Rotation=new(0,state.Yaw,0);foreach(var body in Colliders)body.CollisionLayer=colliding&&state.Holder==0?16u:0;}
}

public partial class GoalSection : MeshInstance3D
{
    public int Goal=-1;
    bool dirty=true,hasView;
    Transform3D lastCamera,lastHead;
    float lastFov;
    Vector2 lastViewport;
    ImageTexture? texture;
    public Image? ProjectionPixels {get;private set;}
    public void SetGoal(int id){if(Goal==id)return;Goal=id;dirty=true;}
    public override void _ExitTree(){ProjectionPixels?.Dispose();ProjectionPixels=null;}

    public void UpdateView(Camera3D camera,Transform3D head)
    {
        if(Goal<0)return;
        var viewport=camera.GetViewport().GetVisibleRect().Size;
        if(!dirty&&hasView&&lastCamera==camera.GlobalTransform&&lastHead==head&&lastFov==camera.Fov&&lastViewport==viewport)return;
        dirty=false;hasView=true;lastCamera=camera.GlobalTransform;lastHead=head;lastFov=camera.Fov;lastViewport=viewport;
        var target=Goals.All[Goal].Target;var bounds=TargetProjection.Bounds(target);
        var cameraInverse=camera.GlobalTransform.AffineInverse();
        // Project onto a camera-facing plane one metre in front of the eye. Matching
        // perspective rays keep the outline registered to the world at every angle.
        float tangent=Mathf.Tan(Mathf.DegToRad(camera.Fov)*.5f),aspect=viewport.X/viewport.Y;
        var limit=camera.KeepAspect==Camera3D.KeepAspectEnum.Height?new Vector2(tangent*aspect,tangent):new Vector2(tangent,tangent/aspect);
        var min=new Vector2(float.PositiveInfinity,float.PositiveInfinity);var max=-min;
        bool crossesNear=false;float furthest=float.NegativeInfinity;
        for(int i=0;i<8;i++)
        {
            var local=new Vector3((i&1)==0?bounds.Min.X:bounds.Max.X,(i&2)==0?bounds.Min.Y:bounds.Max.Y,(i&4)==0?bounds.Min.Z:bounds.Max.Z);
            var p=cameraInverse*(head*local);furthest=Math.Max(furthest,-p.Z);
            if(-p.Z<camera.Near){crossesNear=true;continue;}
            var projected=new Vector2(p.X,p.Y)/-p.Z;min=min.Min(projected);max=max.Max(projected);
        }
        if(furthest<camera.Near){Mesh=null;return;}
        if(crossesNear){min=-limit;max=limit;}
        min=(min-Vector2.One*.012f).Max(-limit);max=(max+Vector2.One*.012f).Min(limit);
        var size=max-min;
        if(size.X<=0||size.Y<=0){Mesh=null;return;}
        // This is a translucent guide, not surface geometry. Bound CPU work while
        // preserving perspective and use filtering for the enlarged outline.
        int width=Math.Clamp((int)(size.X/limit.X*viewport.X*.2f),32,144);
        int height=Math.Clamp((int)(size.Y/limit.Y*viewport.Y*.2f),32,144);
        var mask=new bool[width*height];var bytes=new byte[width*height*4];
        var headInverse=head.AffineInverse();var eye=Art.N(headInverse*camera.GlobalPosition);
        var rays=headInverse.Basis*camera.GlobalBasis;
        var right=Art.N(rays.X);var up=Art.N(rays.Y);var forward=Art.N(-rays.Z);
        var rayStep=right*(size.X/width);
        for(int y=0;y<height;y++)
        {
            float py=max.Y-(y+.5f)*size.Y/height;
            var ray=forward+right*(min.X+.5f*size.X/width)+up*py;
            for(int x=0;x<width;x++,ray+=rayStep)mask[y*width+x]=TargetProjection.Hit(target,eye,ray,camera.Near);
        }
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            int index=y*width+x;if(!mask[index])continue;
            bool edge=x<2||y<2||x>=width-2||y>=height-2||!mask[index-2]||!mask[index+2]||!mask[index-width*2]||!mask[index+width*2];
            int offset=index*4;bytes[offset]=56;bytes[offset+1]=255;bytes[offset+2]=217;bytes[offset+3]=edge?(byte)204:(byte)26;
        }
        var image=Image.CreateFromData(width,height,false,Image.Format.Rgba8,bytes);
        ProjectionPixels?.Dispose();ProjectionPixels=image;
        if(texture==null)
        {
            texture=ImageTexture.CreateFromImage(image);
            MaterialOverride=new StandardMaterial3D{AlbedoTexture=texture,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,NoDepthTest=true,CullMode=BaseMaterial3D.CullModeEnum.Disabled,TextureFilter=BaseMaterial3D.TextureFilterEnum.Linear,RenderPriority=1};
        }
        else if(texture.GetWidth()==width&&texture.GetHeight()==height)texture.Update(image);else texture.SetImage(image);
        if(Mesh is not QuadMesh)Mesh=new QuadMesh();((QuadMesh)Mesh).Size=size;
        var center=(min+max)*.5f;
        GlobalTransform=new(camera.GlobalBasis,camera.ToGlobal(new(center.X,center.Y,-1)));
        CastShadow=ShadowCastingSetting.Off;
    }
}
