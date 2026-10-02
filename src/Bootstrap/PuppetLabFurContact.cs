using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
namespace Hairball;

public partial class PuppetLab
{
    async Task CaptureFurContactPixels()
    {
        if(furContact==null||furContactVisual==null)throw new InvalidOperationException("No actual contact sphere");
        var shape=(SphereShape3D)furContact.GetChildren().OfType<CollisionShape3D>().Single().Shape;
        float radius=shape.Radius*furContact.GlobalBasis.Scale.X;
        var center=furContactVisual.GlobalPosition;var pose=camera.GlobalTransform;
        if(center.DistanceTo(furContact.GlobalPosition)>.00001f)throw new InvalidOperationException("Contact body and visible pose differ");
        var heads=ShellReviewHeads();
        var meshes=heads.SelectMany(h=>new GeometryInstance3D?[]{h.Core,h.Fuzz}.Concat(h.ShellLayers)).Where(m=>m!=null).Cast<GeometryInstance3D>().ToHashSet();
        var other=Descendants(this).OfType<MeshInstance3D>().Where(m=>!meshes.Contains(m)).ToDictionary(m=>m,m=>m.MaterialOverride);
        var materials=heads.SelectMany(h=>h.VolumeFur!.MotionMaterials).ToArray();
        var worldEnv=room.GetChildren().OfType<WorldEnvironment>().Single();var previous=worldEnv.Environment;
        bool taa=GetViewport().UseTaa,visible=furContactVisual.Visible;
        int red=0,solidRed=0;
        var partRows=new List<object>();var originalVisibility=meshes.ToDictionary(m=>m,m=>m.Visible);
        try{
            var black=new ShaderMaterial{Shader=GD.Load<Shader>("res://shaders/puppet_mask_occluder.gdshader")};
            foreach(var m in other.Keys)m.MaterialOverride=black;
            furContactVisual.Visible=false;
            worldEnv.Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=Colors.Black,TonemapMode=Godot.Environment.ToneMapper.Linear};
            GetViewport().UseTaa=false;
            foreach(var m in materials){m.SetShaderParameter("contact_pixels",true);m.SetShaderParameter("contact_diagnostic_center",center);m.SetShaderParameter("contact_diagnostic_radius",radius);}
            foreach(string part in new[]{"all","shell","fins","push-limit","root-fade","displacement","tip-displacement","encoding-calibration"}){
                bool displacement=part is "displacement" or "tip-displacement" or "encoding-calibration";
                foreach(var m in materials){
                    m.SetShaderParameter("contact_pixels",!displacement);
                    m.SetShaderParameter("contact_limit_pixels",part=="push-limit"?1:part=="root-fade"?2:0);
                    m.SetShaderParameter("displacement_pixels",part=="encoding-calibration"?3:part=="tip-displacement"?2:displacement?1:0);
                }
                foreach(var h in heads){h.VolumeFur!.Node.Visible=originalVisibility[h.VolumeFur.Node]&&part!="fins";if(h.VolumeFur.Fins!=null)h.VolumeFur.Fins.Visible=originalVisibility[h.VolumeFur.Fins]&&part!="shell";}
                await DrawFrames(3);
                using var image=GetViewport().GetTexture().GetImage();image.Convert(Image.Format.Rgb8);
                string file=part=="all"?"contact-inside-pixels.png":"contact-inside-"+part+".png";
                if(image.SavePng(Path.Combine(output,file))!=Error.Ok)throw new IOException("Contact mask capture");
                var data=image.GetData();int partRed=0,partSolid=0;
                for(int i=0;i<data.Length;i+=3)if(data[i+1]<16&&data[i+2]<16){if(data[i]>32)partRed++;if(data[i]>=188)partSolid++;}
                if(displacement){
                    static float Linear(byte b){float c=b/255f;return c<=.04045f?c/12.92f:MathF.Pow((c+.055f)/1.055f,2.4f);}
                    var values=new List<float>();
                    for(int i=0;i<data.Length;i+=3)if(data[i+1]>=128&&data[i+2]<16)values.Add(.16f*Linear(data[i])/Linear(data[i+1]));
                    values.Sort();
                    if(values.Count==0)throw new InvalidOperationException("No actual fur pixels in displacement diagnostic");
                    if(part=="encoding-calibration"){
                        var errors=values.Select(v=>Math.Abs(v-.050f)).Order().ToArray();
                        float p99Error=errors[(int)((errors.Length-1)*.99)];
                        if(p99Error>.002f)throw new InvalidOperationException("Displacement color encoding calibration exceeds2mm");
                        partRows.Add(new{part,visiblePixels=values.Count,expectedWorld=.050f,p99AbsoluteErrorWorld=p99Error,maxAbsoluteErrorWorld=errors[^1],calibrationOnly=true});
                    }else partRows.Add(new{part,visiblePixels=values.Count,maxRasterDisplacementWorld=values[^1],p99RasterDisplacementWorld=values[(int)((values.Count-1)*.99)],meanRasterDisplacementWorld=values.Average(),encoding="sRGB decoded red/green coverage ratio times0.16m. Post-WPO raster/MSAA measurement, not a complete vertex maximum; tip pass includes surviving original curve t>=.85 only"});
                }else partRows.Add(new{part,redPixels=partRed,solidRedPixels=partSolid});
                if(part=="all"){red=partRed;solidRed=partSolid;}
            }
        }finally{
            foreach(var kv in other)kv.Key.MaterialOverride=kv.Value;
            foreach(var kv in originalVisibility)kv.Key.Visible=kv.Value;
            foreach(var m in materials){m.SetShaderParameter("contact_pixels",false);m.SetShaderParameter("contact_limit_pixels",0);m.SetShaderParameter("displacement_pixels",0);}
            furContactVisual.Visible=visible;worldEnv.Environment=previous;GetViewport().UseTaa=taa;
        }
        if(camera.GlobalTransform!=pose)throw new InvalidOperationException("Contact diagnostic camera moved");
        File.WriteAllText(Path.Combine(output,"contact-inside-pixels.json"),JsonSerializer.Serialize(new{
            actualViewportPixels=true,afterControlledPerf=true,firstPerson=true,camera=V(camera.GlobalPosition),fov=camera.Fov,eyeHeight=camera.Position.Y,
            bodyId=furContact.GetInstanceId(),sphereCenter=V(center),sphereRadiusWorld=radius,insideToleranceWorld=.0005f,
            redPixels=red,solidRedPixels=solidRed,response=furMotion[0].ResponseEnabled,cutExposureDepthWorld=furCutExposureDepth,parts=partRows,
            definition="Red rendered hair fragments lie more than0.5mm inside the actual diagnostic sphere, using post-WPO fragment VERTEX in view space and INV_VIEW_MATRIX. Sphere visual hidden; outside-sphere hair discarded; other scene depth occlusion retained. TAA disabled only for this mask. This is not an exhaustive whole-mesh penetration distance."
        },new JsonSerializerOptions{WriteIndented=true}));
        GD.Print($"FUR_CONTACT_PIXELS inside={red} solid={solidRed} response={furMotion[0].ResponseEnabled}; diagnostic, not guide metric");
    }
}
