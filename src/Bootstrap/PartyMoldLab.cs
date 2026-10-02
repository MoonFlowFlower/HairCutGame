using Godot;
using Hairball.Core;
using System;
using System.Linq;
using V=System.Numerics.Vector3;
namespace Hairball;
public partial class Main
{
    void MoldLabStart()
    {
        var h=world.SharedHead;h.Volume.Fill(v=>HairVolume.Ellipsoid(v,new(0,0,0),new(.42f,.38f,.37f)));
        var mold=world.Props.First(p=>p.Mold==MoldKind.Pad);mold.Attached=mold.Pinned=true;mold.Local=new(0,.48f,0);mold.Position=h.ToWorld(mold.Local);mold.Rotation=default;
        for(int i=0;i<20;i++)MoldSystem.Grow(h,mold,.05f);
        if(args.ContainsKey("mold-hole")){MoldSystem.Puncture(mold,mold.Position+V.UnitY*.12f);for(int i=0;i<18;i++)MoldSystem.Grow(h,mold,.035f);}
        world.Player(localId)!.Position=new(1.2f,0,1.8f);simulation.Inputs.Clear();
        GD.Print("MOLD_LAB_GEOMETRY injected setup and material for visualization; hole="+mold.Holes.Count+" full="+MoldSystem.Full(h,mold));
    }
}
