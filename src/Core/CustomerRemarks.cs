using System;
using System.Linq;

namespace Hairball.Core;

public enum Remark { None, Hot, Fire, Warning, Flee, Cold, Wet, Object, Mirror, Neck, Watch, Coat,StyleHot,StyleCold }

public static class CustomerRemarks
{
    public static int Priority(Remark r)=>r switch { Remark.Flee=>5,Remark.Warning=>4,Remark.Fire or Remark.Hot=>3,Remark.Cold=>2,_=>1 };
    // The cap guarantees eight seconds from zero even with every patch burning.
    public static float Tolerance(float value,int burning,float dt)=>Math.Clamp(value+dt*(burning>0?Math.Min(12.5f,4+Math.Max(0,burning-4)*.5f):-10),0,100);
    public static bool Say(ExperimentState e,Remark r,float time)
    {
        if(r==Remark.None||time<e.RemarkUntil&&Priority(r)<=Priority(e.Remark))return false;
        e.Remark=r;e.RemarkUntil=time+4;e.RemarkAt=time;return true;
    }
    public static Remark FromHead(WorldState w)
    {
        var patches=w.Heads.Where(h=>h.Id==0||h.AttachedTo==0).SelectMany(h=>h.Patches).ToArray();
        if(w.Experiment.Tolerance>=70)return Remark.Warning;
        if(patches.Any(p=>p.Burning))return Remark.Fire;
        if(patches.Any(p=>p.Temperature>80))return Remark.Hot;
        if(patches.Count(p=>p.Frozen)>4)return Remark.Cold;
        if(patches.Length>0&&patches.Average(p=>p.Wet)>.65f)return Remark.Wet;
        return Remark.None;
    }
    public static string Text(Remark r)=>L.T(r switch
    {
        Remark.StyleHot=>L.Locale=="zh"?"哦！越来越像那个意思了！":"Oh! You're getting warmer!",Remark.StyleCold=>L.Locale=="zh"?"嗯……感觉还远着呢……":"Hmm... still getting colder...",
        Remark.Hot or Remark.Fire=>"Ow, hot! Water, please!",Remark.Warning=>"Put it out! I am about to leave!",
        Remark.Flee=>"I cannot take it! Customer fled.",Remark.Cold=>"Brr! My hair is frozen!",Remark.Wet=>"I am soaking wet!",
        Remark.Object=>"Something on my head? Keep it steady!",Remark.Mirror=>"Let me see that in the mirror!",
        Remark.Neck=>"My neck is getting sore!",Remark.Watch=>"Twenty seconds. I need to leave soon.",Remark.Coat=>"Where is my coat? Ten seconds!",_=>""
    });
}

public sealed partial class Session
{
    void TickTolerance(float dt)
    {
        var e=State.Experiment;if(!e.IsB||State.Phase!=Phase.Build||e.Resolved)return;
        int burning=State.Heads.Where(h=>h.Id==0||h.AttachedTo==0).Sum(h=>h.Patches.Count(p=>p.Burning));
        e.Tolerance=CustomerRemarks.Tolerance(e.Tolerance,burning,dt);
        CustomerRemarks.Say(e,CustomerRemarks.FromHead(State),State.Time);
        if(e.Tolerance>=100)
        {e.LandingIssues=LandingIssue.CustomerFled;CustomerRemarks.Say(e,Remark.Flee,State.Time);ResolveExperiment(false,"customer fled");}
    }
}
