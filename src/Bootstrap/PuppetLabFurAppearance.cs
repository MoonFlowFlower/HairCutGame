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
    // Diagnostic isolation uses the same FPS camera and original material.
    // It never edits density or substitutes an opaque beauty core.
    async void RunFurAppearanceAudit()
    {
        try{
            SetShellReviewShot(args.GetValueOrDefault("shot","r7-trimmed-rear"));
            var pose=camera.GlobalTransform;string density=hair.DensityHash;
            var fur=hair.VolumeFur??throw new InvalidOperationException("Volume fur required");
            var lights=room.GetChildren().OfType<Light3D>().ToArray();
            var shadowStates=lights.ToDictionary(l=>l,l=>l.ShadowEnabled);
            var handMeshes=Descendants(held).OfType<GeometryInstance3D>().ToArray();
            var handShadows=handMeshes.ToDictionary(m=>m,m=>m.CastShadow);
            var env=room.GetChildren().OfType<WorldEnvironment>().Single().Environment;
            bool ao=env.SsaoEnabled,gi=env.SdfgiEnabled;
            var records=new List<object>();
            async Task Shot(string name){
                double start=clock.Elapsed.TotalSeconds;
                while(clock.Elapsed.TotalSeconds-start<1)await DrawFrames(1);
                await SaveShellPng(name);
                if(camera.GlobalTransform!=pose||hair.DensityHash!=density)throw new InvalidOperationException("Isolation pose/material changed");
                records.Add(new{name,camera=V(camera.GlobalPosition),forward=V(-camera.GlobalBasis.Z),fov=camera.Fov,densityHash=density,
                    coreVisible=hair.Core.Visible,shellVisible=fur.Node.Visible,finsVisible=fur.Fins?.Visible,
                    shellCasts=fur.Node.CastShadow.ToString(),finsCast=fur.Fins?.CastShadow.ToString(),
                    ssao=env.SsaoEnabled,gi=env.SdfgiEnabled,lights=lights.Select(l=>new{name=l.Name.ToString(),at=V(l.GlobalPosition),shadow=l.ShadowEnabled}).ToArray()});
            }
            await DrawFrames(100);await Shot("01-baseline");
            foreach(var l in lights)l.ShadowEnabled=false;await Shot("02-no-direct-shadows");
            foreach(var pair in shadowStates)pair.Key.ShadowEnabled=pair.Value;
            if(fur.Fins!=null)fur.Fins.Visible=false;await Shot("03-shell-only");
            fur.Node.Visible=false;if(fur.Fins!=null)fur.Fins.Visible=true;await Shot("04-fins-only");
            if(fur.Fins!=null)fur.Fins.Visible=false;await Shot("05-no-hair");
            fur.Node.Visible=true;if(fur.Fins!=null)fur.Fins.Visible=true;
            foreach(var l in lights.OfType<OmniLight3D>())l.ShadowEnabled=false;await Shot("06-no-pendant-shadows");
            foreach(var pair in shadowStates)pair.Key.ShadowEnabled=pair.Value;
            foreach(var l in lights.OfType<SpotLight3D>())l.ShadowEnabled=false;await Shot("07-no-softbox-shadows");
            foreach(var pair in shadowStates)pair.Key.ShadowEnabled=pair.Value;
            foreach(var m in handMeshes)m.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;await Shot("08-no-held-shadows");
            foreach(var pair in handShadows)pair.Key.CastShadow=pair.Value;
            env.SsaoEnabled=false;await Shot("09-no-ao");env.SsaoEnabled=ao;
            env.SdfgiEnabled=false;await Shot("10-no-gi");env.SdfgiEnabled=gi;
            foreach(var m in fur.MotionMaterials)m.SetShaderParameter("mask_pass",true);await Shot("11-flat-white-fur");
            foreach(var m in fur.MotionMaterials)m.SetShaderParameter("mask_pass",false);
            int lightIndex=0;
            foreach(var spot in lights.OfType<SpotLight3D>()){
                foreach(var l in lights)l.ShadowEnabled=l==spot;
                await Shot("12-softbox-"+(lightIndex++));
            }
            foreach(var pair in shadowStates)pair.Key.ShadowEnabled=pair.Value;
            var lampMeshes=Descendants(room).OfType<GeometryInstance3D>().Where(m=>m.Position.Y>3&&m.Position.Y<4&&Math.Abs(m.Position.Z+.5f)<.01f).ToArray();
            foreach(var m in lampMeshes)m.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
            await Shot("13-no-lamp-casters");
            File.WriteAllText(Path.Combine(output,"appearance-audit.json"),JsonSerializer.Serialize(new{actualFirstPerson=true,eyeHeight=camera.Position.Y,diagnosticOnly=true,performanceValid=false,records},new JsonSerializerOptions{WriteIndented=true}));
            GD.Print("FUR_APPEARANCE_AUDIT_COMPLETE");QuitCleanly();
        }catch(Exception e){GD.PushError("FUR_APPEARANCE_AUDIT_FAIL "+e);GetTree().Quit(1);}
    }
}
