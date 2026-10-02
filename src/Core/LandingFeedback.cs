using System.Collections.Generic;
namespace Hairball.Core;

// Text describes host-measured blockers, never guesses a cause from appearance.
public static class LandingFeedback
{
    public static string Describe(LandingIssue issues,bool zh,bool advice=false)
    {
        if(issues==LandingIssue.None)return zh?"接地稳定 · 正在计时":"Stable contact · counting";
        var lines=new List<string>();
        void Add(LandingIssue flag,string cn,string en,string cnHelp,string enHelp)
        {
            if(!issues.HasFlag(flag))return;
            lines.Add((zh?cn:en)+(advice?(zh?" — "+cnHelp:" — "+enHelp):""));
        }
        Add(LandingIssue.CustomerFled,"顾客受不了跑了","Customer fled: tolerance exhausted","尽早喷水灭火","Extinguish earlier");
        Add(LandingIssue.MissingHelicopter,"直升机缺失","Helicopter missing","请重开本局","Restart this round");
        Add(LandingIssue.NoContact,"起落架未保持接地","Landing gear is not in contact","落脚面须接住起落架","Keep support beneath the landing gear");
        Add(LandingIssue.Coverage,"落脚区域支撑不足","Not enough support under the landing gear","补足落脚区域的头发","Add or transfer hair beneath the landing gear");
        Add(LandingIssue.Uneven,"落脚面不平或倾斜","Landing surface is uneven or tilted","修平落脚面，头动时可扶稳","Level the surface; brace if the head moves");
        Add(LandingIssue.Soft,"头发太软，承重不足","Hair is too soft to support the helicopter","用胶水或冷冻加固落脚区域","Reinforce the landing area with glue or freezing");
        Add(LandingIssue.Burning,"落脚区域正在燃烧","Landing area is burning","先灭火","Extinguish the fire");
        Add(LandingIssue.CustomerDamage,"顾客伤害达到失败上限","Customer damage reached the failure limit","本局已无法完成降落","This round can no longer succeed");
        Add(LandingIssue.CustomerRecovery,"顾客仍在受伤恢复中","Customer is still recovering from injury","停止伤害，等待恢复","Stop causing damage and allow recovery");
        Add(LandingIssue.DwellIncomplete,"截止时连续稳定不足 3 秒","Less than 3 continuous stable seconds before the deadline","更早完成加固并保持接地","Finish reinforcement earlier and maintain contact");
        return string.Join(advice?"\n":"；",lines);
    }
}
