using Godot;
using System;
using System.Collections.Generic;
using static Hairball.TargetLabGeometry;
namespace Hairball;

// Capped visual-only accents. Material-only state captures are taken with this node hidden.
public partial class PuppetLabEffects : Node3D
{
    readonly List<MeshInstance3D> puffs=new(),flames=new(),spray=new();int state;float time;
    public bool Spraying;public Vector3 SprayFrom,SprayTo;
    public int Count=>puffs.Count+flames.Count+(Spraying?spray.Count:0);
    public void SetState(int next)
    {
        foreach(var p in puffs)p.Free();foreach(var p in flames)p.Free();puffs.Clear();flames.Clear();state=next;time=0;
        if(spray.Count==0)for(int i=0;i<18;i++){var m=Ball(this,Vector3.Zero,Vector3.One,Mat("d9e8dc",.97f));m.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;m.Visible=false;m.Layers=2;spray.Add(m);}
        if(next==2)
        {
            var mat=new ShaderMaterial{Shader=GD.Load<Shader>("res://shaders/puppet_smoke.gdshader")};
            for(int i=0;i<8;i++){var m=Ball(this,Vector3.Zero,Vector3.One*.14f,mat);m.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;m.Layers=2;puffs.Add(m);}
            for(int i=0;i<3;i++){var fire=new ShaderMaterial{Shader=GD.Load<Shader>("res://shaders/puppet_flame.gdshader")};fire.SetShaderParameter("phase",i*2.1f);var shape=PuppetLabModels.Surface((t,a)=>{float r=.045f*MathF.Pow(Math.Max(0,MathF.Sin(t*MathF.PI)),.75f)*(1-.6f*t);return new Vector3(r*MathF.Cos(a)+t*t*.016f,t*.22f,r*MathF.Sin(a)*.65f);},20,12);var m=Mesh(this,shape,new(.33f+i*.035f,.31f,.43f),fire);m.Layers=2;m.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;flames.Add(m);}
        }
        if(next==4)
        {
            var mat=Mat("abcbe0",.31f);mat.MetallicSpecular=.35f;
            for(int i=0;i<20;i++)
            {
                float x=.13f+(i%5)*.075f,y=.27f+(i/5)*.060f,z=.38f+(i%4)*.024f;
                var m=Mesh(this,new CylinderMesh{TopRadius=0,BottomRadius=.009f,Height=.037f+(i%3)*.008f,RadialSegments=5},new(x,y,z),mat);m.RotationDegrees=new(25,i*137,-25);m.Layers=2;m.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;puffs.Add(m);
            }
        }
        foreach(var mesh in puffs)mesh.GIMode=GeometryInstance3D.GIModeEnum.Disabled;
        foreach(var mesh in flames)mesh.GIMode=GeometryInstance3D.GIModeEnum.Disabled;
        foreach(var mesh in spray)mesh.GIMode=GeometryInstance3D.GIModeEnum.Disabled;
    }
    public override void _Process(double delta)
    {
        time+=(float)delta;if(!Visible)return;
        if(state==2)for(int i=0;i<puffs.Count;i++){float t=(time*.50f+i/8f)%1;puffs[i].Position=new(.35f+.075f*MathF.Sin(t*6+i),.43f+t*.60f,.39f+.045f*MathF.Cos(t*7+i));puffs[i].Scale=Vector3.One*(.08f+t*.18f);puffs[i].Transparency=Math.Clamp(t*t,0,1);}
        for(int i=0;i<flames.Count;i++){flames[i].Scale=new(1,.82f+.23f*MathF.Sin(time*17+i*2),1);flames[i].Rotation=new(0,0,.15f*MathF.Sin(time*12+i));}
        for(int i=0;i<spray.Count;i++){spray[i].Visible=Spraying;if(!Spraying)continue;float t=(time*1.6f+i/18f)%1;spray[i].GlobalPosition=SprayFrom.Lerp(SprayTo,t)+new Vector3(.012f*MathF.Sin(i*3.7f+time*8)*t,.10f*MathF.Sin(t*MathF.PI)+.015f*MathF.Sin(i*2.1f+time*5)*t,.01f*MathF.Cos(i*4.2f)*t);spray[i].Scale=Vector3.One*(.056f+.065f*t)*(.87f+.16f*MathF.Sin(i*1.7f+time*.8f));}
    }
}
