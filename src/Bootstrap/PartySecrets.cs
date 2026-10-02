using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Hairball;
public partial class Main
{
    void PartyBodyMove(Body body,PlayerState p,Vector2 movement,float facing,double dt,bool jump,Vector3 impulse,float time)
    {
        body.CollisionMask=p.Customer?21u:23u;
        if(p.Customer&&!p.Standing){body.Position=Art.V(Session.WorkCenter);body.Move(Vector2.Zero,facing,dt,false,false,Vector3.Zero,true);return;}
        bool b=world.Experiment.IsB;var move=b?PartyAccidents.Movement(p,new(movement.X,movement.Y),time):new System.Numerics.Vector2(movement.X,movement.Y);
        var drag=GlueDrag.Velocity(world,p,time,!authority&&pendingMotion!=null?pendingMotion.Head.Position:null);
        if(drag.LengthSquared()>0){var relative=Session.Rotate(drag,-facing)/3.4f;move=new(relative.X,relative.Z);}
        body.Move(new(move.X,move.Y),facing,dt,world.Barber(p.Slot).Patches.Any(q=>q.Anchored),jump&&(!b||PartyAccidents.Jump(p,time)),impulse,b&&(time<p.FreezeUntil||time<p.DownUntil));
    }
    float secretClock;int privateRound=-1;
    readonly HashSet<int> privateRecipients=new();
    bool secretBuildLeak,targetBuildLeak;
    void UpdateSecrets(float dt)
    {
        if(!world.Experiment.IsB)return;
        if(privateRound!=world.Round){privateRound=world.Round;privateRecipients.Clear();targetRecipients.Clear();hud.OwnSecret=null;}
        if(world.Phase==Phase.Build&&(world.Experiment.ResultTarget!=null||world.Experiment.ResultStyle.Count>0||world.Twist.ResultFamily!=null||world.Twist.FamilyChecks.Count>0))targetBuildLeak=true;
        ObserveAccidents();
        if(world.Phase==Phase.Build&&world.Experiment.ResultTasks.Count>0)secretBuildLeak=true;
        if(authority){hud.OwnSecret=simulation.OwnTask(localId);secretClock+=dt;
            if(secretClock>=1){secretClock=0;foreach(var link in links.Values.Where(l=>l.Identity.Peer>0&&!l.Lost))if(simulation.OwnTask(link.Identity.Actor) is {} task)NetRpc(link.Identity.Peer,MethodName.PrivateTaskData,task.Actor,task.Round,(int)task.Kind,task.Complete);}
        }
        if(hud.OwnSecret?.Round!=world.Round||!world.Experiment.SecretsEnabled)hud.OwnSecret=null;
    }
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel=1)]
    public void PrivateTaskData(int actor,int round,int kind,bool complete)
    {
        if(authority||connectionEpoch==0||!Enum.IsDefined(typeof(SecretKind),kind))return;
        privateRecipients.Add(actor);
        if(actor!=localId){secretBuildLeak=true;GD.PushError("PRIVATE_TASK_WRONG_RECIPIENT");return;}
        if(round>=world.Round)hud.OwnSecret=new(actor,round,(SecretKind)kind,complete);
    }
    bool SecretSmokeGood()=>!targetBuildLeak&&(!world.Experiment.IsB||!world.Experiment.SecretsEnabled||!secretBuildLeak&&world.Experiment.ResultTasks.Count==world.Players.Count(p=>p.Active&&!p.Customer)&&(authority||(world.Player(localId)?.Customer==true?privateRecipients.Count==0:privateRecipients.Count==1&&privateRecipients.Contains(localId))));
    object SecretReport()=>new{actor=localId,receivedActors=privateRecipients.Order().ToArray(),buildLeak=secretBuildLeak,targetBuildLeak,targetRevealed=world.Experiment.ResultTarget!=null,targetReceived=targetRecipients.Order().ToArray(),customer=world.Experiment.CustomerActor,twist=world.Twist.Kind.ToString(),family=world.Twist.FamilyActor,familyEdited=world.Player(world.Twist.FamilyActor) is {} f&&(f.Added>0||f.Removed>0),catMode=world.Cat.Mode.ToString(),catActions=world.Cat.Actions,round=world.Round,history=world.RoundHistory.Count,returning=world.ReturningHeads,revealed=world.Experiment.ResultTasks.Count,enabled=world.Experiment.SecretsEnabled,accidentObserved=accidentObserved.Order().ToArray(),accidentStage};
}

