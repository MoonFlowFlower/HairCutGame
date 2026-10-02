using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
namespace Hairball;

public partial class PuppetLab
{
    // Actual rendered frames. Readbacks and disk writes are excluded from the
    // steady-state GPU sample saved before this optional diagnostic sequence.
    async Task CaptureHairSamplingMotion()
    {
        var origin=camera.Transform;string density=hair.DensityHash;
        var images=new List<(Image Image,double Time,Vector3 Position)>();
        double begin=clock.Elapsed.TotalSeconds,next=0;
        try {
            while(clock.Elapsed.TotalSeconds-begin<5.8){
                // Never reconnect FramePostDraw from inside its own emission.
                // Advance a real process frame before changing the camera pose.
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                float time=(float)(clock.Elapsed.TotalSeconds-begin);
                float t=Math.Clamp((time-.8f)/5f,0,1);
                var pose=origin;
                // Hold .8 s for same-pixel stability, then a slow sideways and
                // depth move. FPS eye height and FOV are never changed.
                pose.Origin+=new Vector3(.055f*MathF.Sin(t*MathF.Tau),0,-.16f*MathF.Sin(t*MathF.PI));
                camera.Transform=pose;
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                double elapsed=clock.Elapsed.TotalSeconds-begin;
                if(elapsed>=next){
                    images.Add((GetViewport().GetTexture().GetImage(),elapsed,camera.Position));
                    next=elapsed+1.0/30;
                }
            }
            GD.Print($"PUPPET_SAMPLING_CAPTURED frames={images.Count} seconds={clock.Elapsed.TotalSeconds-begin:F3} densityUnchanged={hair.DensityHash==density}");
            if(images.Count<45||hair.DensityHash!=density)throw new InvalidOperationException("Incomplete or mutated sampling sequence");
            var folder=Path.Combine(output,"motion");Directory.CreateDirectory(folder);
            var concat=new StringBuilder("ffconcat version 1.0\n");var records=new List<object>();
            for(int i=0;i<images.Count;i++){
                var frame=images[i];string file=$"motion/frame{i:D4}.png";frame.Image.SavePng(Path.Combine(output,file));
                double duration=i+1<images.Count?images[i+1].Time-frame.Time:1.0/30;
                concat.Append($"file '{file}'\nduration {duration.ToString("F6",CultureInfo.InvariantCulture)}\n");
                records.Add(new{time=frame.Time,x=frame.Position.X,y=frame.Position.Y,z=frame.Position.Z});
            }
            concat.Append($"file 'motion/frame{images.Count-1:D4}.png'\n");
            File.WriteAllText(Path.Combine(output,"motion.ffconcat"),concat.ToString());
            File.WriteAllText(Path.Combine(output,"motion.json"),JsonSerializer.Serialize(new{actualRealtime=true,shot,width=1280,height=800,frames=images.Count,duration=images[^1].Time-images[0].Time,fov=camera.Fov,eyeHeight=origin.Origin.Y,densityHash=density,times=records}));
            GD.Print($"PUPPET_SAMPLING_MOTION_COMPLETE frames={images.Count}");
        }finally{camera.Transform=origin;foreach(var frame in images)frame.Image.Dispose();}
    }
}
