using Godot;
using Hairball.Core;
using System;
using System.Linq;

namespace Hairball;
public partial class BrushPreview : MeshInstance3D
{
    readonly ImmediateMesh lines=new();
    SculptStroke? predicted;
    bool pressed;
    public int ActiveHeadId { get; private set; }=-1;
    public override void _Ready()
    {Mesh=lines;CastShadow=ShadowCastingSetting.Off;MaterialOverride=new StandardMaterial3D{VertexColorUseAsAlbedo=true,ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,NoDepthTest=true};}
    public void Sync(WorldState world,PlayerState player,Camera3D camera,bool active,bool fine,SculptStroke? authoritative,Func<System.Numerics.Vector3,System.Numerics.Vector3,float,float>? obstruction)
    {
        var tool=world.Tools.FirstOrDefault(t=>t.Id==player.Held);Visible=world.Phase==Phase.Build&&tool!=null&&!player.ReferenceUp;
        ActiveHeadId=-1;
        if(!Visible){predicted=null;pressed=false;return;}
        var p=new PlayerState{Id=player.Id,Position=Art.N(camera.GlobalPosition)-System.Numerics.Vector3.UnitY*1.7f,Yaw=camera.Rotation.Y,Pitch=camera.Rotation.X,BrushSize=player.BrushSize};
        var dir=ToolQuery.Direction(world,p,tool!.Definition);float range=obstruction?.Invoke(Session.Eye(p),dir,Tools.Get(tool.Definition).Range)??Tools.Get(tool.Definition).Range;
        var hits=tool.Definition is 0 or 1?ToolQuery.Find(world,p,tool.Definition,fine,dir,range):ToolQuery.EffectHits(world,p,tool.Definition,fine,dir,range).Select(h=>new BrushContext(h.head.Id,h.point,h.head.Volume.Normal(h.point),ToolQuery.EffectRadius(tool.Definition,fine))).ToList();var hit=hits.FirstOrDefault();var head=hits.Count>0?world.Heads.First(x=>x.Id==hit.HeadId):null;
        if(!active)predicted=null;
        else if(head!=null&&(!pressed||predicted!=null&&(predicted.Tool!=tool.Id||predicted.Fine!=fine||predicted.Size!=player.BrushSize)))predicted=new(){Head=head.Id,Tool=tool.Id,Fine=fine,Plane=hit.Point,Normal=hit.Normal,Size=player.BrushSize};
        pressed=active;var stroke=authoritative??predicted;
        var position=head==null?camera.GlobalPosition-camera.GlobalBasis.Z*1.5f:Art.V(head.ToWorld(hit.Point));var normal=head==null?camera.GlobalBasis.Z:Art.V(System.Numerics.Vector3.Transform(hit.Normal,head.Orientation));
        bool valid=head!=null;float radius=head==null?.045f:hit.Radius;
        if(tool.Definition==9&&head!=null)normal=fine?Art.V(-dir):Vector3.Up;
        if(active&&stroke!=null&&tool.Definition==0)
        {
            head=world.Heads.FirstOrDefault(h=>h.Id==stroke.Head);
            if(head!=null&&!head.Locked)
            {
                var o=head.ToLocal(Session.Eye(p));var d=head.LocalDirection(dir);float denom=System.Numerics.Vector3.Dot(d,stroke.Normal);
                float t=Math.Abs(denom)<.12f?-1:System.Numerics.Vector3.Dot(stroke.Plane-o,stroke.Normal)/denom;
                valid=t>0&&t*head.GeometryScale<range&&(hits.Count==0||hits[0].HeadId==head.Id);
                position=Art.V(head.ToWorld(o+d*Math.Max(0,t)-stroke.Normal*(fine?.015f:.05f)));normal=Art.V(System.Numerics.Vector3.Transform(stroke.Normal,head.Orientation));radius=ToolQuery.Radius(0,player.BrushSize)*head.GeometryScale;
            }
        }
        if(!active&&valid&&tool.Definition==0&&head!=null)position-=normal*(fine?.015f:.05f)*head.GeometryScale;
        if(active&&valid&&head!=null)ActiveHeadId=head.Id;
        lines.ClearSurfaces();lines.SurfaceBegin(Mesh.PrimitiveType.Lines);lines.SurfaceSetColor(valid?(fine?new Color("f6df86"):new Color("e6f6ff")):new Color("f07b72"));
        var tangent=normal.Cross(Vector3.Up);if(tangent.LengthSquared()<.01f)tangent=normal.Cross(Vector3.Right);tangent=tangent.Normalized();var up=normal.Cross(tangent);position+=normal*.006f;
        void Line(Vector3 a,Vector3 b){lines.SurfaceAddVertex(a);lines.SurfaceAddVertex(b);}
        if(valid)
        {for(int i=0;i<40;i++){float a=i*Mathf.Tau/40,b=(i+1)*Mathf.Tau/40;Line(position+(tangent*MathF.Cos(a)+up*MathF.Sin(a))*radius,position+(tangent*MathF.Cos(b)+up*MathF.Sin(b))*radius);}Line(position,position+normal*(tool.Definition==1?.18f:.12f));
            if(tool.Definition is 2 or 3){var force=tool.Definition==3&&fine?Vector3.Down*.32f:Art.V(dir)*(tool.Definition==2?-.32f:.32f);for(int j=-1;j<=1;j++){var start=position+tangent*j*radius*.6f;Line(start,start+force);Line(start+force,start+force*.7f+up*.05f);Line(start+force,start+force*.7f-up*.05f);}}
            if(tool.Definition==7)Line(position,position+Art.V(dir)*1.8f);
            if(tool.Definition==1){float height=(fine?.0225f:.09f)*(head?.GeometryScale??1);for(int j=0;j<24;j++){float a=j*Mathf.Tau/24,b=(j+1)*Mathf.Tau/24;var offset=(tangent*MathF.Cos(a)+up*MathF.Sin(a));Line(position+offset*radius*.65f+normal*height,position+(tangent*MathF.Cos(b)+up*MathF.Sin(b))*radius*.65f+normal*height);if(j%6==0)Line(position+offset*radius,position+offset*radius*.65f+normal*height);}}Line(position-tangent*radius,position+tangent*radius);}
        else {Line(position-tangent*radius-up*radius,position+tangent*radius+up*radius);Line(position-tangent*radius+up*radius,position+tangent*radius-up*radius);}
        lines.SurfaceEnd();
    }
}
