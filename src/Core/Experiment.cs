using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Hairball.Core;

public enum ExperimentVariant { Off, A, B }
public enum AttentionStage { Unaware, Notice, Commit, React }
public enum FlightStage { Staging, Flyby, Circling, Approach, Descent, Contact, Resolved, Returning }
[Flags]
public enum LandingIssue { None=0, NoContact=1, Coverage=2, Uneven=4, Soft=8, Burning=16, CustomerDamage=32, CustomerRecovery=64, MissingHelicopter=128, DwellIncomplete=256, CustomerFled=512 }
public readonly record struct LandingMeasurement(int Contacts,float Height,float Variance,float Quality,float Resistance,LandingIssue Issues)
{
    public bool Stable=>Issues==LandingIssue.None;
}
public readonly record struct LandingProbe(int Column,int Row,Vector3 Target,Vector3 Surface,bool HasContact,float Resistance,bool Burning,LandingIssue Issues);
public sealed class AttentionState
{
    public AttentionStage Stage;
    public string Cause="";
    public Vector3 Source;
    public int Actor;
    public float Started, QuietUntil;
    public bool Mirror, Blocked;
    public Dictionary<string,float> Sounds=new();
    public AttentionState Clone()=>new(){Stage=Stage,Cause=Cause,Source=Source,Actor=Actor,Started=Started,QuietUntil=QuietUntil,Mirror=Mirror,Blocked=Blocked,Sounds=new(Sounds)};
}
public sealed class ExperimentState
{
    public ExperimentVariant Variant;
    public AttentionState Attention=new();
    public FlightStage Flight;
    public float Tolerance, RemarkUntil, RemarkAt;
    public LeaveStage Leave;
    public float LeaveStarted=-1, DelayUsed, EarlySeconds, BadSupportSeconds, SlippedAt=-1;
    public Vector3 SlipPosition;
    public LandingIssue SlipIssues;
    public int PlacementCount, PickupCount, SlipCount, BellActor;
    public bool WatchShown, CoatShown;
    public float Drowsiness;
    public float Tilt,ComboUntil;
    public Combo Combo;
    public List<Combo> Discoveries=new();
    public CurtainReaction Curtain;
    public int BasePay,Tip;
    public List<ActionFact> ResultActions=new();
    public List<HairTrace> ResultHair=new();
    public bool PassedMirror;public int CustomerActor;public bool StandActive,ReturnCut=true;public float StandProgress,StandStarted,StandCooldown,ResultStarted;
    public TargetCard? ResultTarget;
    public List<StyleCheck> ResultStyle=new();
    public bool SecretsEnabled;
    public List<TaskReveal> ResultTasks=new();
    public Remark Remark;
    public int HelicopterId=-1;
    public float StableSeconds, TouchdownAt=-1;
    public bool Resolved, Success;
    public LandingIssue LandingIssues;
    public int SupportContacts;
    public float SupportVariance, SupportResistance;
    public const float Dwell=3f, BuildDuration=150f;
    public bool IsB=>Variant==ExperimentVariant.B;
    public ExperimentState Clone()=>new(){Drowsiness=Drowsiness,PassedMirror=PassedMirror,CustomerActor=CustomerActor,StandActive=StandActive,StandProgress=StandProgress,StandStarted=StandStarted,StandCooldown=StandCooldown,ResultStarted=ResultStarted,ReturnCut=ReturnCut,Variant=Variant,Tilt=Tilt,Combo=Combo,ComboUntil=ComboUntil,Discoveries=new(Discoveries),Curtain=Curtain,BasePay=BasePay,Tip=Tip,ResultActions=new(ResultActions),ResultHair=new(ResultHair),ResultTarget=ResultTarget,ResultStyle=new(ResultStyle),ResultTasks=new(ResultTasks),SecretsEnabled=SecretsEnabled,Attention=Attention.Clone(),Tolerance=Tolerance,Remark=Remark,RemarkUntil=RemarkUntil,RemarkAt=RemarkAt,Leave=Leave,LeaveStarted=LeaveStarted,DelayUsed=DelayUsed,EarlySeconds=EarlySeconds,BadSupportSeconds=BadSupportSeconds,SlippedAt=SlippedAt,SlipPosition=SlipPosition,SlipIssues=SlipIssues,PlacementCount=PlacementCount,PickupCount=PickupCount,SlipCount=SlipCount,BellActor=BellActor,WatchShown=WatchShown,CoatShown=CoatShown,Flight=Flight,HelicopterId=HelicopterId,StableSeconds=StableSeconds,TouchdownAt=TouchdownAt,Resolved=Resolved,Success=Success,LandingIssues=LandingIssues,SupportContacts=SupportContacts,SupportVariance=SupportVariance,SupportResistance=SupportResistance};
}
public sealed record ExperimentEvent(float Time,int Round,string Variant,string Kind,int Actor,Vector3 Position,string Detail,int IncidentId);

// Host-only events are streamed to disk, not accumulated in every network snapshot.
public sealed partial class Session
{
    public Action<ExperimentEvent>? ExperimentLog;
    float mirrorClock, sampleClock;
    Phase loggedPhase=(Phase)(-1);
    readonly Dictionary<int,float> toolOnsets=new();
    public static readonly Vector3 MirrorPoint=new(0,1.9f,3.15f);
    public void ExperimentEvent(string kind,int actor,Vector3 point,string detail)
    {
        if(State.Experiment.Variant==ExperimentVariant.Off)return;
        ActionFromEvent(kind,actor);
        ExperimentLog?.Invoke(new(State.Time,State.Round,State.Experiment.Variant.ToString(),kind,actor,point,detail,State.NextEvent-1));
    }
    void StageExperiment()
    {
        var variant=State.Experiment.Variant;
        if(variant==ExperimentVariant.Off)return;
        // Pure simulation fixtures get the B default too; explicit short QA durations survive.
        if(variant==ExperimentVariant.B&&BuildSeconds==75)BuildSeconds=ExperimentState.BuildDuration;
        State.Experiment=new(){Variant=variant,Discoveries=KnownCombos.ToList()};balanceAt=0;balanceOffset=0;ResetActions();mirrorClock=sampleClock=0;toolOnsets.Clear();
        State.Job.Goal=0;State.Job.Choices=[0,0,0];
        // Keep the A first-round customer and materials exactly as the baseline.
        PhysicalProps.Stage(State);
        foreach(var p in State.Players)p.Bracing=false;
        ExperimentEvent("variant",0,default,"v0.6 single human helipad; seed="+State.Seed);
    }
    public static void StageExperimentProps(WorldState w)
    {
        if(!w.Experiment.IsB)return;
        if(w.Twist.Kind!=TwistKind.MiniDemo)w.Heads.RemoveAll(h=>h.Miniature);
        // NextRound stages the new shared job before B selects its commission.
        // Rebuild that staging when the ordinary job has a different goal.
        if(!w.Props.Any(p=>p.Goal==0)){w.Props.Clear();w.Job.Goal=0;PhysicalProps.Stage(w);return;}
        var heli=w.Props.Single(p=>p.Goal==0);
        heli.Position=PartyLoop.Table+Vector3.UnitY*PhysicalProps.Clearance(0);heli.StartPosition=heli.Position;
        w.Experiment.HelicopterId=heli.Id;
        w.Props.Add(new(){Id=w.Round*100+99,Goal=-1,Position=new(1.3f,.6f,.1f)});
    }
    public bool CanBrace(PlayerState p)=>State.Experiment.IsB&&State.Phase==Phase.Build&&p.Active&&!p.Customer&&p.CarriedProp<0&&p.CarriedModel<0&&p.CarryLadder<0&&
        State.Time>=p.DownUntil&&p.CarriedHair<0&&p.SupportLadder<0&&Vector3.Distance(Eye(p),State.SharedHead.Position)<1.9f&&LookingAt(p,State.SharedHead.Position,.75f,2.1f)&&
        (ObstructionDistance?.Invoke(Eye(p),Vector3.Normalize(State.SharedHead.Position-Eye(p)),2.1f)??2.1f)>=Vector3.Distance(Eye(p),State.SharedHead.Position)-.4f;
    void SetBrace(PlayerState p,bool value)
    {
        if(p.Bracing==value)return;p.Bracing=value;EndStroke(p.Id);
        ExperimentEvent(value?"brace_start":"brace_stop",p.Id,p.Position,"E held; primary suppressed");
    }
    void UpdateBraces()
    {
        foreach(var p in State.Players)
        {
            if(p.Customer){SetBrace(p,!p.Standing&&(p.CarriedHair>=0||State.Props.Any(x=>x.Id==p.CarriedProp&&x.Mold!=MoldKind.None)));continue;}
            bool held=Inputs.TryGetValue(p.Id,out var input)&&input.Buttons.HasFlag(Buttons.Interact);
            // Prefer pickup when E initially targets a physical prop/tool, never steal a carried item.
            bool pickup=!p.Bracing&&(State.Props.Any(t=>t.Holder==0&&LookingAt(p,t.Position,.35f,2.5f))||State.Tools.Any(t=>t.Holder==0&&LookingAt(p,t.Position,.45f,2.5f)));
            bool callTarget=State.Experiment.IsB&&LookingAt(p,PartyLoop.Bell,.25f,2.5f);
            SetBrace(p,held&&!pickup&&!callTarget&&CanBrace(p));
        }
    }
    public static bool MirrorBlocked(WorldState w)
    {
        var eye=w.SharedHead.ToWorld(new(0,.1f,.34f));var ray=MirrorPoint-eye;float length=ray.Length();var dir=ray/length;
        foreach(var p in w.Players.Where(p=>p.Active))
        {
            float t=Vector3.Dot(p.Position+Vector3.UnitY*1.05f-eye,dir);
            if(t<=.15f||t>=length)continue;
            var point=eye+dir*t;
            if(new Vector2(point.X-p.Position.X,point.Z-p.Position.Z).Length()<.34f&&point.Y>p.Position.Y+.15f&&point.Y<p.Position.Y+1.85f)return true;
        }
        foreach(var prop in w.Props.Where(p=>p.Goal==-1))
        {
            var normal=Rotate(Vector3.UnitZ,prop.Rotation.Y);float dot=Vector3.Dot(dir,normal);
            if(Math.Abs(dot)<.01f)continue;
            float t=Vector3.Dot(prop.Position-eye,normal)/dot;if(t<=0||t>=length)continue;
            var local=Rotate(eye+dir*t-prop.Position,-prop.Rotation.Y);
            if(Math.Abs(local.X)<.62f&&Math.Abs(local.Y)<.47f)return true;
        }
        return false;
    }
    public bool NoticeDanger(Vector3 position,int actor,string cause)
    {
        if(!State.Experiment.IsB||State.Phase!=Phase.Build)return false;
        // Mirror sees the chair work zone behind the forward-facing customer's eyes.
        var local=State.SharedHead.ToLocal(position);if(local.Z>.4f||Math.Abs(local.X)>2.5f||local.Y<-.8f||local.Y>2.5f)return false;
        bool blocked=MirrorBlocked(State);State.Experiment.Attention.Blocked=blocked;
        ExperimentEvent("mirror_attempt",actor,position,(blocked?"blocked:":"visible:")+cause);
        return !blocked&&BeginAttention(position,actor,cause,true);
    }
    public bool Hear(Vector3 position,int actor,string category)
    {
        if(!State.Experiment.IsB||State.Phase!=Phase.Build||Vector3.Distance(position,State.SharedHead.Position)>12)return false;
        AlertActivity();
        var a=State.Experiment.Attention;var d=position-State.SharedHead.Position;
        int sector=(int)MathF.Floor((MathF.Atan2(d.X,d.Z)+MathF.PI)/(MathF.PI/2));
        string key=category+":"+sector;
        bool novel=!a.Sounds.TryGetValue(key,out float last)||State.Time-last>8;
        a.Sounds[key]=State.Time;
        // A first physical gear impact is a new salient event, even just after rotor habituation.
        if(category=="landing_gear"&&novel)a.QuietUntil=State.Time;
        foreach(var stale in a.Sounds.Where(x=>State.Time-x.Value>20).Select(x=>x.Key).ToArray())a.Sounds.Remove(stale);
        ExperimentEvent("sound",actor,position,category+(novel?":novel":":habituated"));
        if(novel)NovelSound(position,actor,category);
        return novel&&BeginAttention(position,actor,category,false);
    }
    bool BeginAttention(Vector3 source,int actor,string cause,bool mirror)
    {
        var a=State.Experiment.Attention;
        if(a.Stage!=AttentionStage.Unaware||State.Time<a.QuietUntil)return false;
        a.Source=source;a.Actor=actor;a.Cause=cause;a.Mirror=mirror;a.Started=State.Time;a.Stage=AttentionStage.Notice;
        State.Customers[0].ReactionCount++;
        ExperimentEvent("attention",actor,source,"Notice:"+cause);return true;
    }
    void ExperimentTool(PlayerState p,ToolState t)
    {
        if(State.Experiment.Variant==ExperimentVariant.Off)return;
        bool onset=!toolOnsets.TryGetValue(t.Id,out float last)||State.Time-last>.45f;toolOnsets[t.Id]=State.Time;
        if(t.Definition is 2 or 3 or 7 or 8 or 9&&Vector3.Distance(Eye(p),State.SharedHead.Position)<=12)AlertActivity();
        if(!onset)return;
        RecordAction(PartyAction.ToolUse,p.Id,t.Definition);
        ExperimentEvent(t.Definition is 5 or 6 or 7 or 8 or 9?"dangerous_tool":"tool_start",p.Id,Eye(p),Tools.Get(t.Definition).Id+"; remaining="+State.Remaining);
        if(t.Definition is 2 or 3 or 7 or 8 or 9)Hear(Eye(p),p.Id,t.Definition==7?"shot":t.Definition==8?"fire_burst":"tool_motor");
    }
    void TickAttention(float dt)
    {
        var e=State.Experiment;var a=e.Attention;var c=State.Customers[0];c.Action=CustomerReaction.None;
        c.Panic=Math.Max(0,c.Panic-dt*.028f);
        mirrorClock-=dt;
        if(mirrorClock<=0)
        {
            mirrorClock=.35f;a.Blocked=MirrorBlocked(State);
            if(State.SharedHead.Patches.Any(p=>p.Burning))NoticeDanger(State.SharedHead.Position,0,"fire");
            foreach(var t in State.Tools.Where(t=>t.Holder!=0&&t.Definition is 7 or 8 or 9&&State.Time-t.LastUse<.25f))
                if(State.Player(t.Holder) is {} actor)NoticeDanger(Eye(actor),actor.Id,"dangerous_tool");
        }
        float age=State.Time-a.Started;
        bool visibleCause=State.SharedHead.Patches.Any(p=>p.Burning)||State.Tools.Any(t=>t.Definition is 7 or 8 or 9&&t.Holder!=0&&State.Time-t.LastUse<.4f);
        if(a.Mirror&&a.Stage==AttentionStage.Notice&&(MirrorBlocked(State)||!visibleCause))
        {a.Stage=AttentionStage.Unaware;a.QuietUntil=State.Time+.5f;ExperimentEvent("attention_cancel",a.Actor,a.Source,"mirror occluded / evidence removed");}
        var next=a.Stage switch{AttentionStage.Notice when age>=1.15f=>AttentionStage.Commit,AttentionStage.Commit when age>=1.55f=>AttentionStage.React,AttentionStage.React when age>=3.55f=>AttentionStage.Unaware,_=>a.Stage};
        if(next!=a.Stage){a.Stage=next;if(next==AttentionStage.Unaware)a.QuietUntil=State.Time+2;ExperimentEvent("attention",a.Actor,a.Source,next+":"+a.Cause);}
        CustomerMotion.Apply(State);
        if(a.Stage==AttentionStage.React&&age>=1.75f&&a.Actor>0)RecordAction(PartyAction.Attention,a.Actor);
    }
    public static (Vector3 Offset,Vector3 Rotation) AttentionPose(WorldState w)
    {
        var a=w.Experiment.Attention;if(a.Stage==AttentionStage.Unaware)return(default,default);
        float age=w.Time-a.Started;
        float amount=a.Stage==AttentionStage.React?MathF.Sin(Math.Clamp((age-1.55f)/2,0,1)*MathF.PI):.06f;
        float strength=w.Players.Any(p=>p.Active&&p.Bracing)?.22f:1;
        var dir=a.Source-(PartyLoop.Root(w)+new Vector3(0,1.5f+w.Customers[0].ChairHeight,0));
        float yaw=Math.Clamp(MathF.Atan2(dir.X,dir.Z),-.55f,.55f);
        float pitch=-Math.Clamp(MathF.Atan2(dir.Y,new Vector2(dir.X,dir.Z).Length()),-.25f,.35f);
        return (new(MathF.Sign(dir.X)*.045f*amount*strength,0,-.025f*amount*strength),new Vector3(pitch,yaw,0)*(amount*strength));
    }
    // World-space gear sampling: reacts to the same head transform the tool queries see.
    public static IReadOnlyList<LandingProbe> LandingProbes(WorldState w,PropState heli)
    {
        var probes=new List<LandingProbe>(9);float min=99,max=-99;
        for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
        {
            var gear=Vector3.Transform(new Vector3(x*.27f,-.26f,z*.29f),Quaternion.CreateFromYawPitchRoll(heli.Rotation.Y,heli.Rotation.X,heli.Rotation.Z));
            var offset=gear;
            var origin=heli.Position+offset;origin.Y=Math.Max(5,w.SharedHead.Position.Y+3.5f);
            var target=heli.Position+gear;
            Head? best=null;Vector3 local=default;float nearest=origin.Y;
            foreach(var h in w.Heads.Where(h=>h.Id==0||h.AttachedTo==0))
                if(h.Raycast(origin,-Vector3.UnitY,nearest,out var hit,out var distance)){best=h;local=hit;nearest=distance;}
            var surface=best?.ToWorld(local)??new Vector3(origin.X,w.SharedHead.Position.Y+.2f,origin.Z);
            bool contact=best!=null&&surface.Y>=w.SharedHead.Position.Y+.2f&&(!w.Experiment.IsB||Math.Abs(surface.Y-target.Y)<.18f);
            float resistance=0;bool burning=false;var issues=LandingIssue.None;
            if(contact)
            {
                var patch=HairSystem.MaterialAt(best!,local);resistance=patch.Resistance;burning=patch.Burning;
                min=Math.Min(min,surface.Y);max=Math.Max(max,surface.Y);
                if(resistance<=.4f)issues|=LandingIssue.Soft;if(burning)issues|=LandingIssue.Burning;
            }
            else issues|=LandingIssue.Coverage;
            probes.Add(new(x,z,target,surface,contact,resistance,burning,issues));
        }
        if(max-min>=.18f)
            for(int i=0;i<probes.Count;i++)if(probes[i].HasContact&&Math.Abs(probes[i].Surface.Y-(min+max)*.5f)>=.09f)
                probes[i]=probes[i] with{Issues=probes[i].Issues|LandingIssue.Uneven};
        return probes;
    }
    public static LandingMeasurement LandingSupport(WorldState w,PropState heli)
    {
        var contacts=LandingProbes(w,heli).Where(p=>p.HasContact).ToArray();int count=contacts.Length;
        float variance=count>0?contacts.Max(p=>p.Surface.Y)-contacts.Min(p=>p.Surface.Y):3;
        float firmness=count>0?contacts.Average(p=>p.Resistance):0;
        var issues=LandingIssue.None;
        if(count<7)issues|=LandingIssue.Coverage;
        // No surface is missing support, not evidence of softness or unevenness.
        if(count>0){if(variance>=.18f)issues|=LandingIssue.Uneven;if(firmness<=.4f)issues|=LandingIssue.Soft;}
        if(contacts.Any(p=>p.Burning))issues|=LandingIssue.Burning;
        return new(count,count>0?contacts.Average(p=>p.Surface.Y):w.SharedHead.Position.Y+.2f,variance,count/9f*100,firmness,issues);
    }
    public static LandingIssue LandingBlockers(WorldState w,LandingMeasurement support,bool touching)
    {
        var issues=support.Issues;
        if(!touching)issues|=LandingIssue.NoContact;
        if(!w.Experiment.IsB&&w.Job.Penalty>=50)issues|=LandingIssue.CustomerDamage;
        if(w.Customers[0].Recovery>0)issues|=LandingIssue.CustomerRecovery;
        return issues;
    }
    string LandingTelemetry()
    {
        var e=State.Experiment;
        return FormattableString.Invariant($"issues={e.LandingIssues}; contacts={e.SupportContacts}/9; heightSpread={e.SupportVariance:0.000}; resistance={e.SupportResistance:0.000}; dwell={e.StableSeconds:0.000}; penalty={State.Job.Penalty}; recovery={State.Customers[0].Recovery:0.000}");
    }
    bool SpendGrowth(Head h,HairVolume before,ToolState tool)
    {
        float added=Math.Max(0,(h.Volume.Mass-before.Mass)*MathF.Pow(h.GeometryScale,3));
        float budget=tool.GrowthRemaining;
        if(added<=budget+.00001f){tool.GrowthRemaining=Math.Max(0,budget-added);return true;}
        float CellMass(byte value)=>Math.Clamp((HairVolume.Decode(value)+HairVolume.Step*.5f)/HairVolume.Step,0,1)*
            HairVolume.Step*HairVolume.Step*HairVolume.Step/HairVolume.UnitVolume*MathF.Pow(h.GeometryScale,3);
        float minimumIncrement=Math.Min(CellMass(96)-CellMass(95),CellMass(160)-CellMass(159));
        // A sub-cell remainder is retained for smaller collateral targets; repeatedly holding
        // on this target cannot spend it, so avoid the expensive clipping search every tick.
        if(budget<minimumIncrement-.000001f){h.Volume=before;return false;}
        // Retain the largest affordable portion of this accelerated dab rather than discarding
        // its whole shape and all remaining supply when only its end exceeds the bottle budget.
        var requested=h.Volume.Data;var affordable=(byte[])before.Data.Clone();float accepted=0,low=0,high=1;
        for(int pass=0;pass<14;pass++)
        {
            float amount=(low+high)*.5f;var candidate=(byte[])before.Data.Clone();
            for(int i=0;i<candidate.Length;i++)candidate[i]=(byte)Math.Clamp(before.Data[i]+(int)MathF.Floor((requested[i]-before.Data[i])*amount),0,255);
            h.Volume.Data=candidate;h.Volume.Revision++;
            float mass=Math.Max(0,(h.Volume.Mass-before.Mass)*MathF.Pow(h.GeometryScale,3));
            if(mass<=budget+.000001f){low=amount;affordable=candidate;accepted=mass;}else high=amount;
        }
        h.Volume.Data=affordable;h.Volume.Revision++;
        tool.GrowthRemaining=Math.Max(0,budget-accepted);return false;
    }
    void ResolveExperiment(bool success,string reason)
    {
        var e=State.Experiment;e.Resolved=true;e.Success=success;e.Flight=FlightStage.Resolved;
        RevealSecrets();RevealTarget();FinishImpacts();foreach(var p in State.Players){ResetAccidents(p);SetBrace(p,false);EndStroke(p.Id);ResetGrowthHold(p);}foreach(var h in State.Heads)h.Locked=true;
        // Shape is hidden telemetry only. Neither outcome nor payment depends on it.
        State.Job.Result=new(){Goal=0,Function=success?100:0,Prop=success?100:0,Shape=Validation.Shape(State),State=GoalMaterials.Evaluate(State.SharedHead,0),FunctionOnly=true};
        State.Job.Awards.Clear();var pay=PartyResults.Evaluate(State,success,targetCard);e.BasePay=pay.Base;e.Tip=pay.Tip;e.Curtain=pay.Reaction;e.ResultActions=actions.TakeLast(6).ToList();
        RevealTwist(success);RememberRound();
        State.Job.Settled=true;State.Job.Result.Penalty=State.Job.Penalty;State.Job.Delta=e.BasePay+e.Tip;State.Job.Wallet+=State.Job.Delta;State.Order.Pending=false;
        e.ResultStarted=State.Time;e.ReturnCut=ReturnCutEnabled;
        if(e.ReturnCut&&State.Player(e.CustomerActor) is {} customer){Drop(customer);var gift=new ToolState{Id=1900,Definition=0,Holder=customer.Id,Charge=-1};State.Tools.Add(gift);customer.Held=gift.Id;}State.Phase=Phase.Results;State.Remaining=ForceAI?ResultsSeconds:Math.Max(ResultsSeconds,11+(e.CustomerActor!=0&&e.ReturnCut?5:0));Replay.Select(State);
        ExperimentEvent("result",0,State.SharedHead.Position,$"success={success}; {reason}; {LandingTelemetry()}; hiddenShape={State.Job.Result.Shape}; growthLeft={TotalGrowth(State)}");
    }
    void SampleExperiment(float dt)
    {
        if(State.Experiment.Variant==ExperimentVariant.Off)return;
        if(State.Phase!=loggedPhase){loggedPhase=State.Phase;ExperimentEvent("phase",0,default,State.Phase.ToString());if(State.Phase==Phase.Results&&!State.Experiment.IsB)ExperimentEvent("result",0,State.SharedHead.Position,$"function={State.Job.Result.Function}; shape={State.Job.Result.Shape}; score={State.Job.Result.Final}");}
        sampleClock+=dt;if(sampleClock<.5f)return;sampleClock=0;
        foreach(var p in State.Players.Where(p=>p.Active))
        {Inputs.TryGetValue(p.Id,out var input);ExperimentEvent("player",p.Id,p.Position,$"buttons={(int)input.Buttons}; yaw={p.Yaw}; pitch={p.Pitch}; tool={p.Held}; brace={p.Bracing}; remaining={State.Remaining}");}
    }
}

