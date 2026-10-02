using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
namespace Hairball.Core;

public enum SecretKind { ClipFriend,FreezeFriend,Attention,WigAtDoor,BlowProp,Bell,IceAtDoor,Recycle,Escort,Rescue }
public sealed record PrivateTask(int Actor,int Round,SecretKind Kind,bool Complete);
public sealed record TaskReveal(int Actor,string Name,SecretKind Kind,bool Complete);
public static class SecretTasks
{
    public static string Text(SecretKind kind)=>L.T(kind switch{
        SecretKind.ClipFriend=>"Clip a teammate's hair once",SecretKind.FreezeFriend=>"Freeze a teammate once",SecretKind.Attention=>"Make the customer look at you",SecretKind.WigAtDoor=>"Attach a wig that stays until the door",SecretKind.BlowProp=>"Blow a prop loose",SecretKind.Bell=>"Ring the completion bell",SecretKind.IceAtDoor=>"Freeze some customer hair that stays frozen at the door",SecretKind.Recycle=>"Recycle loose hair",SecretKind.Escort=>"Brace the head during departure",_=>"Rescue a teammate from an accident"});
    public static string Reveal(TaskReveal t)=>t.Name+" · "+Text(t.Kind)+" · "+L.T(t.Complete?"Done":"Not done");
}
public sealed partial class Session
{
    public bool SecretTasksEnabled=true;
    public Func<int,SecretKind>? TaskPicker; // Deterministic QA only; default independent private draws.
    readonly Dictionary<int,PrivateTask> secrets=new();
    readonly Dictionary<int,HashSet<int>> wigEvidence=new(),iceEvidence=new();
    public PrivateTask? OwnTask(int actor)=>secrets.GetValueOrDefault(actor);
    void ResetSecrets(){secrets.Clear();wigEvidence.Clear();iceEvidence.Clear();}
    void AssignSecrets()
    {
        if(!State.Experiment.IsB||State.Phase!=Phase.Build||!SecretTasksEnabled)return;
        State.Experiment.SecretsEnabled=true;
        var pool=Enum.GetValues<SecretKind>().Where(k=>State.Players.Count(p=>p.Active&&!p.Customer)>1||k is not (SecretKind.ClipFriend or SecretKind.FreezeFriend or SecretKind.Rescue)).ToArray();
        foreach(var p in State.Players.Where(p=>p.Active&&!p.NetworkAway&&!p.Customer))if(!secrets.ContainsKey(p.Id))secrets[p.Id]=new(p.Id,State.Round,TaskPicker?.Invoke(p.Id)??pool[RandomNumberGenerator.GetInt32(pool.Length)],false);
    }
    void SecretAction(PartyAction action,int actor,int target)
    {
        if(action==PartyAction.Wig){if(!wigEvidence.TryGetValue(actor,out var ids))wigEvidence[actor]=ids=new();ids.Add(target);}
        if(action==PartyAction.Freeze){if(!iceEvidence.TryGetValue(actor,out var ids))iceEvidence[actor]=ids=new();ids.Add(target);}
        if(!secrets.TryGetValue(actor,out var task)||task.Complete)return;
        bool done=task.Kind switch{SecretKind.ClipFriend=>action==PartyAction.ClipFriend,SecretKind.FreezeFriend=>action==PartyAction.FreezeFriend,SecretKind.Attention=>action==PartyAction.Attention,SecretKind.BlowProp=>action==PartyAction.BlowProp,SecretKind.Bell=>action==PartyAction.Bell,SecretKind.Recycle=>action==PartyAction.Recycle,SecretKind.Escort=>action==PartyAction.EscortBrace,SecretKind.Rescue=>action is PartyAction.ThawFriend or PartyAction.ReleaseFriend or PartyAction.WaterFriend,_=>false};
        if(done)secrets[actor]=task with {Complete=true};
    }
    void RevealSecrets()
    {
        if(State.Experiment.Leave==LeaveStage.Done)foreach(var task in secrets.Values.ToArray()){
            bool done=task.Kind switch{SecretKind.WigAtDoor=>wigEvidence.GetValueOrDefault(task.Actor)?.Any(id=>State.Heads.Any(h=>h.Id==id&&h.AttachedTo==0))==true,SecretKind.IceAtDoor=>iceEvidence.GetValueOrDefault(task.Actor)?.Any(id=>State.Heads.Any(h=>h.Id==id&&(h.Id==0||h.AttachedTo==0)&&h.Patches.Any(p=>p.Frozen)))==true,_=>task.Complete};
            secrets[task.Actor]=task with {Complete=done};
        }
        State.Experiment.ResultTasks=secrets.Values.OrderBy(t=>State.Player(t.Actor)?.Slot).Select(t=>new TaskReveal(t.Actor,State.Player(t.Actor)?.Name??"",t.Kind,t.Complete)).ToList();
    }
}
