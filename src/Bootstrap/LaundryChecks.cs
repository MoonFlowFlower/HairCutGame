using Godot;
using Hairball.Core;
using System;
using System.Linq;
using V=System.Numerics.Vector3;
namespace Hairball;
public partial class Main
{
    void LaundryReviewInput(ref Vector2 movement,ref Buttons buttons)
    {
        if(!args.TryGetValue("laundry-action",out var action)||world.Player(localId) is not {} p)return;
        int definition=action switch{"growth"=>1,"wind"=>3,_=>4};
        var tool=world.Tools.First(t=>t.Definition==definition);foreach(var old in world.Tools.Where(t=>t.Holder==p.Id))old.Holder=0;tool.Holder=p.Id;p.Held=tool.Id;
        var target=world.SharedHead.ToWorld(definition==1?new(-.45f,.85f,.16f):definition==3?new(0,.65f,.1f):new(.46f,.65f,.18f));
        var dir=V.Normalize(target-Session.Eye(p));yaw=MathF.Atan2(-dir.X,-dir.Z);pitch=MathF.Asin(dir.Y);movement=Vector2.Zero;
        // Capture a brief risky wind adjustment; a sustained blast can genuinely destroy the beam.
        buttons=elapsed>(definition==3?2.8f:.7f)?Buttons.Primary:Buttons.None;
    }
    void LaundryChecks()
    {
        void Check(bool result,string message){if(!result)throw new Exception(message);GD.Print("LAUNDRY_PASS "+message);}
        var head=world.SharedHead;var player=world.Player(1)!;
        Check(world.Phase==Phase.Build&&world.Job.Goal==6&&world.Props.Count(p=>p.Goal==6)==3,"three garments appear during live build");
        var before=Wire.Encode(world);salon!.Laundry[0].Sync(head,world.Time+1,.1f,true);float calm=salon.Laundry[0].MotionAmount;salon.Laundry[0].Sync(head,world.Time+1,1,true);
        Check(salon.Laundry[0].MotionAmount>calm,"wind visibly amplifies garment motion");
        Check(before.SequenceEqual(Wire.Encode(world)),"cloth and repair presentation cannot change authoritative hair or score");
        salon.PhysicsCustomers(world);salon.Sync(world,1,.016f);
        var actual=salon.GetNode<HeadView>("Hair_0");Check(actual.Position.DistanceTo(Art.V(head.Position))<.001f&&actual.Rotation.DistanceTo(Art.V(head.Rotation))<.001f,"load pose is shared by rendered scalp and authoritative surface queries");
        player.Cooldown=0;simulation.UseTool(player,false);salon.Sync(world,1,.016f);
        Check(salon.Feedback.VisibleParticles>0,"real held glue use produces visible fixed-pool feedback");
        Check(head.Patches.Any(p=>p.Glue>.55f),"stabilizing with glue still changes actual gameplay material");
        var contact=LaundryRig.Contact(head,1);Check(contact.OnHair,"garment attaches to actual current shell");
        head.Volume=new();var missing=LaundryRig.Contact(head,1);Check(!missing.OnHair&&missing.Strength==0,"missing beam cannot create imaginary scoring support");
        salon.Bodies[1].Position+=Vector3.Right*2;StartNewMatch();
        Check(salon.Bodies[1].Position.DistanceTo(Art.V(world.Player(1)!.Position))<.001f&&LaundryRig.Contact(world.SharedHead,1).OnHair,"F5 reset restores the playable fixture and physical player position");
        GD.Print("LAUNDRY_CHECKS_OK");QuitGracefully();
    }
}
