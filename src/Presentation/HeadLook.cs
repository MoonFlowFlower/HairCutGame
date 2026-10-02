using Godot;
using Hairball.Core;
using System;
using System.Linq;
using System.Collections.Generic;
namespace Hairball;
public partial class HeadView
{
    static ShaderMaterial? humanMaterial;
    readonly ShaderMaterial legacyMaterial;
    int visualRevision=-1;
    bool look;
    Vector4 stateAverage;
    Vector4 sentAverage=new(-1,-1,-1,-1);
    Color sentColor;
    int sentSpecies=-1;
    static int Species(Head h)=>h.Barber?0:VisualQuality.Phase>=4&&VisualQuality.Profile==HairProfile.Alien?2:VisualQuality.Phase>=3&&(VisualQuality.Profile==HairProfile.Animal||h.Material==HeadMaterialKind.Wool)?1:0;
    public static Color BaseColor(Head h)
    {
        Color color=h.Patches.Any(p=>p.Wig)?new("af70b9"):h.Barber?new("593e68"):new("79513b");
        if(h.Material==HeadMaterialKind.Wool&&!h.Barber)color=new("dfcdb1");
        if(Species(h)==1)color=new("dfcdb1");
        if(Species(h)==2)color=new("9258bc");
        if(h.Fragment)color=new(h.FragmentColor.X,h.FragmentColor.Y,h.FragmentColor.Z);
        if(!h.Barber&&!h.Loose)color=VisualQuality.Palette switch{"black"=>new("29242a"),"blond"=>new("bc914c"),"red"=>new("914a31"),"dyed"=>new("8353ac"),_=>color};
        return color;
    }
    void UpdateLook(Head h)
    {
        look=referenceGoal<0&&VisualQuality.Phase>=2&&VisualQuality.Profile!=HairProfile.Current;
        bool settingsChanged=visualRevision!=VisualQuality.Revision;
        if(settingsChanged)
        {
            visualRevision=VisualQuality.Revision;revision=-1;
            humanMaterial??=new(){Shader=GD.Load<Shader>("res://shaders/hair_lookdev.gdshader")};
            shell.MaterialOverride=look?humanMaterial:legacyMaterial;
        }
        if(!look)return;
        int species=Species(h);var color=BaseColor(h);stateAverage=Vector4.Zero;
        foreach(var p in h.Patches)stateAverage+=new Vector4(p.Wet,p.Frozen?1:0,p.Char,p.Glue);stateAverage/=h.Patches.Count;
        if(settingsChanged||species!=sentSpecies){shell.SetInstanceShaderParameter("species",species);sentSpecies=species;}
        if(settingsChanged||color!=sentColor){shell.SetInstanceShaderParameter("base_color",color);sentColor=color;}
        if(settingsChanged||stateAverage!=sentAverage){shell.SetInstanceShaderParameter("average_state",stateAverage);sentAverage=stateAverage;}
        if(settingsChanged){shell.SetInstanceShaderParameter("local_states",VisualQuality.LocalStates);shell.SetInstanceShaderParameter("visual_offset",Vector3.Zero);shell.SetInstanceShaderParameter("features",new Vector4(VisualQuality.Flocking?1:0,VisualQuality.Rim?1:0,VisualQuality.Micro?1:0,1));}
    }
    // Presentation interpolation between existing nearest-patch samples; does not change hit/scoring states.
    static Color LocalState(Head h,System.Numerics.Vector3 point,Dictionary<(int,int,int),Color> cache)
    {
        var p=point/HairVolume.Step;int x=(int)MathF.Floor(p.X),y=(int)MathF.Floor(p.Y),z=(int)MathF.Floor(p.Z);
        Color At(int a,int b,int c){if(cache.TryGetValue((a,b,c),out var value))return value;var patch=HairSystem.MaterialAt(h,new System.Numerics.Vector3(a,b,c)*HairVolume.Step);value=new(Math.Clamp(patch.Wet,0,1),patch.Frozen?1:0,Math.Clamp(patch.Char,0,1),Math.Clamp(patch.Glue,0,1));cache[(a,b,c)]=value;return value;}
        Color Row(int yy,int zz)=>At(x,yy,zz).Lerp(At(x+1,yy,zz),p.X-x);
        Color Plane(int zz)=>Row(y,zz).Lerp(Row(y+1,zz),p.Y-y);
        return Plane(z).Lerp(Plane(z+1),p.Z-z);
    }
    public Vector4 StateMin {get;private set;}
    public Vector4 StateMax {get;private set;}
}
