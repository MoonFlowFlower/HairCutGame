using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
namespace Hairball.Core;

public enum PartyAction { Ignite, Extinguish, PickCommission, PlaceCommission, Wig, Glue, Freeze, Bell, SeatDelay, EscortBrace, BottleEmpty, MoldHole, FreezeFriend, GlueFriend, FireFriend, ThawFriend, ReleaseFriend, WaterFriend, ClipFriend, Recycle, BlowProp, Attention, ToolUse,Down,Revive,Throw,Catch,CoopSpray,CoopCut }
public sealed record ActionFact(float Time,PartyAction Kind,int Actor,string Name,int Target);
public enum CurtainReaction { Satisfied, Funny, Amazed, Complaint }
public static class PartyResults
{
    public sealed record TipRule(string Id,int Value,Func<WorldState,bool> Match);
    public static readonly TipRule[] Tips=[
        new("Wig",10,w=>w.Heads.Any(h=>h.AttachedTo==0&&h.Patches.Any(p=>p.Wig))),
        new("Ice",10,w=>w.Heads.Where(h=>h.Id==0||h.AttachedTo==0).Any(h=>h.Patches.Any(p=>p.Frozen))),
        new("Spike",10,w=>w.SharedHead.Volume.Samples().Any(v=>v.Y>1.65f))];
    public static (int Base,int Tip,CurtainReaction Reaction) Evaluate(WorldState w,bool success,TargetCard? card=null)
    {
        if(card!=null){int matches=TargetCards.Judge(w,card).Count(c=>c.Match);var cardReaction=matches==card.Rules.Length?CurtainReaction.Amazed:!success?CurtainReaction.Complaint:matches>0?CurtainReaction.Funny:CurtainReaction.Satisfied;return(success?40:0,Math.Max(0,matches*10+(success?(int)(Math.Max(0,w.Experiment.EarlySeconds)/10)*5:0)-Math.Max(0,w.Job.Penalty)),cardReaction);}
        int funny=Tips.Where(t=>t.Match(w)).Sum(t=>t.Value);
        var reaction=!success?CurtainReaction.Complaint:funny>=20?CurtainReaction.Amazed:funny>0?CurtainReaction.Funny:CurtainReaction.Satisfied;
        return(success?40:0,success?Math.Max(0,funny+(int)(Math.Max(0,w.Experiment.EarlySeconds)/10)*5-Math.Max(0,w.Job.Penalty)):0,reaction);
    }
    public static string Reaction(CurtainReaction r)=>L.T(r switch{
        CurtainReaction.Funny=>"That's ridiculous. I love it!",CurtainReaction.Amazed=>"How does that even stay up?",CurtainReaction.Complaint=>"Well, at least we got a photo.",_=>"Thanks! I'll take it from here."});
    public static string Text(ActionFact a)=>$"{a.Time:0.0}s · {a.Name} · "+(a.Kind==PartyAction.ToolUse?L.T("used tool")+" · "+L.T(Tools.Get(a.Target).Name):L.T(a.Kind switch{
        PartyAction.Down=>L.Locale=="zh"?"撞倒了队友":"knocked down a teammate",PartyAction.Revive=>L.Locale=="zh"?"拍醒了队友":"slapped a teammate awake",PartyAction.Throw=>L.Locale=="zh"?"扔出物件":"threw an object",PartyAction.Catch=>L.Locale=="zh"?"接住物件":"caught an object",PartyAction.CoopSpray=>L.Locale=="zh"?"为管线喷枪泵液":"pumped the hose sprayer",PartyAction.CoopCut=>L.Locale=="zh"?"合剪了一刀":"made a shared cut",PartyAction.Ignite=>"lit hair",PartyAction.Extinguish=>"extinguished hair",PartyAction.PickCommission=>"picked up the commission",PartyAction.PlaceCommission=>"placed the commission",PartyAction.Wig=>"attached a wig",PartyAction.Glue=>"glued hair",PartyAction.Freeze=>"froze hair",PartyAction.Bell=>"rang the bell",PartyAction.SeatDelay=>"seated the customer again",PartyAction.EscortBrace=>"braced during departure",PartyAction.BottleEmpty=>"emptied a bottle",PartyAction.MoldHole=>"punctured a mold",PartyAction.FreezeFriend=>"froze a teammate",PartyAction.GlueFriend=>"glued a teammate's hands",PartyAction.FireFriend=>"lit a teammate's hair",PartyAction.ThawFriend=>"thawed a teammate",PartyAction.ReleaseFriend=>"freed sticky hands",PartyAction.WaterFriend=>"extinguished a teammate",PartyAction.ClipFriend=>"clipped a teammate's hair",PartyAction.Recycle=>"recycled loose hair",PartyAction.BlowProp=>"blew a prop loose",_=>"caught the customer's attention"}));
}
public sealed partial class Session
{
    public const int ActionLimit=128;
    readonly List<ActionFact> actions=new();
    readonly Dictionary<(PartyAction,int,int),float> actionTimes=new();
    public IReadOnlyList<ActionFact> ActionLog=>actions;
    public void RecordAction(PartyAction kind,int actor,int target=0)
    {
        if(!State.Experiment.IsB||State.Phase!=Phase.Build)return;
        var key=(kind,actor,target);if(actionTimes.TryGetValue(key,out float at)&&State.Time-at<2)return;
        actionTimes[key]=State.Time;actions.Add(new(State.Time,kind,actor,State.Player(actor)?.Name??L.T("Customer / world"),target));
        SecretAction(kind,actor,target);
        if(actions.Count>ActionLimit)actions.RemoveAt(0);
        ExperimentEvent("action",actor,State.Player(actor)?.Position??State.SharedHead.Position,$"{kind}; target={target}");
    }
    void ResetActions(){actions.Clear();actionTimes.Clear();}
    void ActionFromEvent(string kind,int actor)
    {
        PartyAction? action=kind switch{"commission_pickup"=>PartyAction.PickCommission,"commission_place"=>PartyAction.PlaceCommission,"bell"=>PartyAction.Bell,"seat_delay"=>PartyAction.SeatDelay,"brace_start" when State.Experiment.Leave!=LeaveStage.Seated=>PartyAction.EscortBrace,_=>null};
        if(action.HasValue)RecordAction(action.Value,actor);
    }
}

