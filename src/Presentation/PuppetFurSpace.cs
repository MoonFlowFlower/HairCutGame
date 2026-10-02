using Godot;
namespace Hairball;

// Material coordinates are independent of the authoritative lattice. The lab
// uses a 3.5x lattice; B keeps its original one-metre gameplay coordinates.
public readonly record struct PuppetFurSpace(float DensityScale,Vector3 DensityOffset,float RootPacking=1,bool Cartesian=false)
{
    public static readonly PuppetFurSpace Lab=new(PuppetLabHair.Scale,Vector3.Zero);
    public static readonly PuppetFurSpace Mainline=new(1,-PuppetVolumeFur.RootCenter,4);
    public Vector3 ToDensity(Vector3 p)=>p*DensityScale+DensityOffset;
    public Vector3 ToMaterial(Vector3 p)=>(p-DensityOffset)/DensityScale;
}
