using Godot;
using System;
using System.Collections.Generic;
namespace Hairball;

// Render-only R7.1 data. COLOR.a remains the existing surface classification;
// it is not distance, occupancy or a modification to HairVolume.
public static class PuppetShellFur
{
    public const float NormalPile=.012f,TrimPile=.0038f,CarvePile=.0026f;
    public const float MaximumLocalOffset=NormalPile*1.04f;
    public const float PlushPile=.012f,PlushTrim=.003f,PlushCarve=.0025f;
    public const float TuftLength=.040f;
    static ImageTexture? roots;
    public static ImageTexture RootPattern=>roots??=MakeRoots();
    static ImageTexture? volumeRoots;
    public static ImageTexture VolumeRootPattern=>volumeRoots??=MakeRoots(true);
    public static ShaderMaterial Material(int layer,int count,bool plush)
    {
        roots??=MakeRoots();
        var m=new ShaderMaterial{Shader=GD.Load<Shader>("res://shaders/puppet_shell_fur.gdshader")};
        m.SetShaderParameter("root_pattern",roots);m.SetShaderParameter("layer_t",layer/(float)count);
        m.SetShaderParameter("normal_pile",plush?PlushPile:NormalPile);m.SetShaderParameter("trim_pile",plush?PlushTrim:TrimPile);m.SetShaderParameter("carve_pile",plush?PlushCarve:CarvePile);
        return m;
    }
    static ImageTexture MakeRoots(bool directions=false)
    {
        // Periodic Poisson roots remove the visible rows of one-root-per-cell
        // jitter. Cells accelerate lookup only; they do not place the roots.
        const int cells=32,cell=16,size=cells*cell;
        var random=new Random(74219);var seeds=new List<Vector3>[cells,cells];
        for(int y=0;y<cells;y++)for(int x=0;x<cells;x++)seeds[x,y]=new();
        int accepted=0;
        for(int attempt=0;accepted<cells*cells&&attempt<100000;attempt++){
            float px=(float)random.NextDouble()*cells,py=(float)random.NextDouble()*cells;int ix=(int)px,iy=(int)py;bool clear=true;
            for(int dy=-1;dy<=1&&clear;dy++)for(int dx=-1;dx<=1&&clear;dx++)foreach(var s in seeds[(ix+dx+cells)%cells,(iy+dy+cells)%cells]){
                float u=px-(ix+dx+s.X),v=py-(iy+dy+s.Y);if(u*u+v*v<.49f){clear=false;break;}
            }
            if(clear){seeds[ix,iy].Add(new(px-ix,py-iy,(float)random.NextDouble()));accepted++;}
        }
        if(accepted!=cells*cells)throw new InvalidOperationException("Incomplete shell follicle texture");
        var bytes=new byte[size*size*4];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++){
            float px=x/(float)cell,py=y/(float)cell,best=float.PositiveInfinity,height=0,offsetX=0,offsetY=0;
            int ix=x/cell,iy=y/cell;
            for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)foreach(var s in seeds[(ix+dx+cells)%cells,(iy+dy+cells)%cells]){
                float u=px-(ix+dx+s.X),v=py-(iy+dy+s.Y),d=u*u+v*v;
                if(d<best){best=d;height=s.Z;offsetX=u;offsetY=v;}
            }
            int at=(y*size+x)*4;
            bytes[at]=(byte)Math.Clamp((int)(MathF.Exp(-best*3.2f)*255),0,255);
            bytes[at+1]=(byte)(height*255);
            bytes[at+2]=directions?(byte)Math.Clamp((offsetX*.5f+.5f)*255,0,255):(byte)0;
            bytes[at+3]=directions?(byte)Math.Clamp((offsetY*.5f+.5f)*255,0,255):(byte)255;
        }
        using var image=Image.CreateFromData(size,size,false,Image.Format.Rgba8,bytes);image.GenerateMipmaps();
        return ImageTexture.CreateFromImage(image);
    }
}
