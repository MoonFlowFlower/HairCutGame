using Godot;
using System;
using System.Collections.Generic;
namespace Hairball;

// A fixed, periodic follicle volume, sampled by the existing shells. Each
// filament has one root and one continuous curved path through all heights.
// Cutting never regenerates this field or changes its seed.
public static class PuppetBundleField
{
    public const int Layers=128,Size=256,Cells=16;
    static Texture2DArray? cached;
    public static Texture2DArray Texture=>cached??=Build();
    readonly record struct Strand(Vector2 Root,Vector2 Bend,float Phase,float Turns,float Radius,float Length,float Tip);
    static Texture2DArray Build()
    {
        var random=new Random(284913);float R()=> (float)random.NextDouble();
        var centers=new List<Vector2>();
        for(int attempt=0;centers.Count<Cells*Cells&&attempt<80000;attempt++){
            var p=new Vector2(R()*Cells,R()*Cells);bool okay=true;
            foreach(var c in centers){var d=(p-c).Abs();d=new(Math.Min(d.X,Cells-d.X),Math.Min(d.Y,Cells-d.Y));if(d.LengthSquared()<.50f){okay=false;break;}}
            if(okay)centers.Add(p);
        }
        if(centers.Count!=Cells*Cells)throw new InvalidOperationException("Incomplete bundle roots");
        var strands=new List<Strand>();
        foreach(var c in centers){
            float phase=R()*MathF.Tau,turns=3.5f+R()*3.5f,angle=R()*MathF.Tau,tip=.20f+R()*.80f;
            var bend=new Vector2(MathF.Cos(angle),MathF.Sin(angle))*(.12f+R()*.26f);
            for(int j=0;j<7;j++){
                float a=R()*MathF.Tau,r=MathF.Sqrt(R())*.23f;
                strands.Add(new(c+new Vector2(MathF.Cos(a),MathF.Sin(a))*r,bend,
                    phase+(R()-.5f)*.7f,turns+(R()-.5f)*.6f,.075f+R()*.040f,.92f+R()*.13f,Math.Clamp(tip+(R()-.5f)*.20f,0,1)));
            }
        }
        var images=new Godot.Collections.Array<Image>();float pixels=Size/(float)Cells;
        for(int layer=0;layer<Layers;layer++){
            float h=layer/(float)(Layers-1);var coverage=new float[Size*Size];var bytes=new byte[Size*Size*4];
            for(int i=0;i<Size*Size;i++){bytes[i*4]=128;bytes[i*4+1]=128;}
            foreach(var s in strands){
                float bend=h*h,phase=s.Phase+h*s.Turns*MathF.Tau;
                var orbit=new Vector2(MathF.Cos(phase)-MathF.Cos(s.Phase),MathF.Sin(phase)-MathF.Sin(s.Phase));
                var curl=orbit*(.25f+.40f*h);
                var slope=s.Bend*(2*h)+orbit*.40f+new Vector2(-MathF.Sin(phase),MathF.Cos(phase))*(s.Turns*MathF.Tau*(.25f+.40f*h));
                var p=(s.Root+s.Bend*bend+curl)*pixels;
                float taper=Math.Clamp((s.Length-h)/.075f,0,1),radius=s.Radius*(.20f+.80f*MathF.Sqrt(taper))*pixels;
                if(taper<=0)continue;
                int xmin=(int)MathF.Floor(p.X-radius-.75f),xmax=(int)MathF.Ceiling(p.X+radius+.75f);
                int ymin=(int)MathF.Floor(p.Y-radius-.75f),ymax=(int)MathF.Ceiling(p.Y+radius+.75f);
                for(int y=ymin;y<=ymax;y++)for(int x=xmin;x<=xmax;x++){
                    float dx=x+.5f-p.X,dy=y+.5f-p.Y,d=MathF.Sqrt(dx*dx+dy*dy);
                    float a=Math.Clamp(radius+.65f-d,0,1);a=a*a*(3-2*a);
                    int ix=((x%Size)+Size)%Size,iy=((y%Size)+Size)%Size,i=iy*Size+ix;
                    if(a<=coverage[i])continue;coverage[i]=a;
                    var normal=new Vector3(dx,dy,-(dx*slope.X+dy*slope.Y)*.045f).Normalized();
                    bytes[i*4]=(byte)(normal.X*127+128);
                    bytes[i*4+1]=(byte)(normal.Y*127+128);
                    // Premultiplied persistent endpoint variation survives
                    // mip filtering. This endpoint is relative to the ORIGINAL
                    // volume boundary, never to a newly exposed cut surface.
                    bytes[i*4+2]=(byte)MathF.Round(s.Tip*a*255);
                    bytes[i*4+3]=(byte)MathF.Round(a*255);
                }
            }
            var image=Image.CreateFromData(Size,Size,false,Image.Format.Rgba8,bytes);
            // Coverage is filtered after rasterization, not thresholded after
            // minification. This keeps fine hairs from disappearing in a mip.
            image.GenerateMipmaps();images.Add(image);
        }
        var texture=new Texture2DArray();
        if(texture.CreateFromImages(images)!=Error.Ok)throw new InvalidOperationException("Cannot upload persistent bundle field");
        foreach(var image in images)image.Dispose();
        return texture;
    }
}
