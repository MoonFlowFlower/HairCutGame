using Godot;
using System;
using System.Collections.Generic;
namespace Hairball;

public sealed class PuppetLabSurface
{
    public static bool TransportEnabled {get;set;}
    readonly Dictionary<MeshInstance3D,Material> originals=new();
    readonly Dictionary<Material,ShaderMaterial> fleece=new();
    readonly Dictionary<MeshInstance3D,(StandardMaterial3D Original,StandardMaterial3D Finish)> plastics=new();
    public void Collect(Node n,bool stage=false)
    {
        if(!stage&&n is MeshInstance3D eye&&eye.MaterialOverride is StandardMaterial3D plastic&&(plastic==PuppetLabModels.White||plastic==PuppetLabModels.Dark))
        {
            var finish=(StandardMaterial3D)plastic.Duplicate();finish.Roughness=plastic==PuppetLabModels.White?.26f:.21f;
            if(plastic==PuppetLabModels.White)finish.AlbedoColor=new Color("eee9df");
            plastics[eye]=(plastic,finish);
        }
        if(n is MeshInstance3D m&&m.MaterialOverride is StandardMaterial3D s&&(stage&&!s.EmissionEnabled||!stage&&s.Roughness>=.92f))
        {
            originals[m]=s;
            if(!fleece.ContainsKey(s))
            {
                var mat=new ShaderMaterial{Shader=GD.Load<Shader>(stage?"res://shaders/puppet_set_material.gdshader":"res://shaders/puppet_microfleece.gdshader")};mat.SetShaderParameter("base_color",s.AlbedoColor);
                if(!stage)mat.SetShaderParameter("clean_fabric",PuppetHairSettings.Current.Round>=3&&PuppetHairSettings.Current.CleanFabric);
                if(!stage)mat.SetShaderParameter("soft_fabric",PuppetHairSettings.Current.SoftFabric);
                if(stage){mat.SetShaderParameter("roughness",s.Roughness);mat.SetShaderParameter("metal",s.Metallic);mat.SetShaderParameter("kind",s.ResourceName=="oak"||m.Position.Y<.05f?1:s.ResourceName=="plaster"?2:3);}
                else{var c=PuppetLabModels.Cream.AlbedoColor;bool cloth=(Math.Abs(s.AlbedoColor.R-c.R)+Math.Abs(s.AlbedoColor.G-c.G)+Math.Abs(s.AlbedoColor.B-c.B))<.025f||s.AlbedoColor.B>s.AlbedoColor.R*1.1f;mat.SetShaderParameter("woven",cloth);string asset=cloth?"wool_boucle":"caban";mat.SetShaderParameter("scan_height",GD.Load<Texture2D>($"res://assets/b2_puppet_textures/{asset}_disp_1k.jpg"));mat.SetShaderParameter("scan_normal",GD.Load<Texture2D>($"res://assets/b2_puppet_textures/{asset}_nor_gl_1k.jpg"));}
                fleece[s]=mat;
            }
        }
        foreach(var c in n.GetChildren())Collect(c,stage);
    }
    public void Enable(bool on){foreach(var kv in originals)if(GodotObject.IsInstanceValid(kv.Key))kv.Key.MaterialOverride=on?fleece[kv.Value]:kv.Value;SetPlastics(on&&TransportEnabled);}
    void SetPlastics(bool on){foreach(var kv in plastics)if(GodotObject.IsInstanceValid(kv.Key))kv.Key.MaterialOverride=on?kv.Value.Finish:kv.Value.Original;}
    public void SetTransport(bool on){foreach(var mat in fleece.Values)mat.SetShaderParameter("transport_material",on);SetPlastics(on);}
    public void Clear(){originals.Clear();fleece.Clear();plastics.Clear();}
    public static ShaderMaterial Hair(bool strands,int state,bool states,PuppetHairSettings? settings=null)
    {
        settings??=PuppetHairSettings.Current;
        var mat=new ShaderMaterial{Shader=GD.Load<Shader>(strands?(TransportEnabled?"res://shaders/puppet_fiber_scatter.gdshader":"res://shaders/puppet_fiber.gdshader"):"res://shaders/puppet_hair.gdshader")};
        mat.SetShaderParameter("hair_optimization",settings.Enabled);mat.SetShaderParameter("microfiber_core",settings.Microfiber);
        mat.SetShaderParameter("short_trim",settings.Trim);mat.SetShaderParameter("short_carve",settings.Carve);
        mat.SetShaderParameter("optimization_round",settings.Round);
        mat.SetShaderParameter("stable_sampling",settings.Round>=3&&settings.StableSampling);
        mat.SetShaderParameter("soft_hair_lighting",settings.Round>=3&&settings.SoftHairLighting);
        mat.SetShaderParameter("edge_only",strands&&settings.ShellFur&&!settings.PlushTufts);
        mat.SetShaderParameter("plush_geometry",strands&&settings.PlushTufts);
        mat.SetShaderParameter("transport_material",TransportEnabled);mat.SetShaderParameter("strands",strands);mat.SetShaderParameter("state",state);mat.SetShaderParameter("states_enabled",states);
        mat.SetShaderParameter("nap_height",GD.Load<Texture2D>("res://assets/b2_puppet_textures/wool_boucle_disp_1k.jpg"));
        mat.SetShaderParameter("nap_normal",GD.Load<Texture2D>("res://assets/b2_puppet_textures/wool_boucle_nor_gl_1k.jpg"));
        return mat;
    }
}
