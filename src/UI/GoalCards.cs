using Godot;
using Hairball.Core;
using System.Collections.Generic;
using V=System.Numerics.Vector3;
namespace Hairball;
public partial class Hud
{
    readonly Dictionary<int,ImageTexture> goalCards=new();
    ImageTexture GoalCardImage(int id)
    {
        if(goalCards.TryGetValue(id,out var texture))return texture;
        using var image=Image.CreateEmpty(72,64,false,Image.Format.Rgba8);
        var direction=V.Normalize(new V(-.3f,-.4f,-1));var right=V.Normalize(V.Cross(direction,V.UnitY));var up=V.Cross(right,direction);
        for(int y=0;y<64;y++)for(int x=0;x<72;x++)
        {
            var origin=new V(0,.75f,0)-direction*4+right*((x-36)/27f)+up*((32-y)/27f);
            image.SetPixel(x,y,TargetProjection.Hit(Goals.All[id].Target,origin,direction)?new Color("84dfd2"):new Color("203840"));
        }
        texture=ImageTexture.CreateFromImage(image);goalCards[id]=texture;return texture;
    }
}
