using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
namespace Hairball.Core;

public sealed class SharedJob
{
    public int Goal,Wallet=200,Penalty,Delta;
    public int[] Choices=[0,1,5];
    public Score Result=new();
    public bool Settled;
    public System.Collections.Generic.List<SpotlightAward> Awards=new();
    public void Begin(int seed,int round)
    {
        Choices=round==1?[0,1,5]:new[]{0,1,2,3,4,5,6,7}.OrderBy(g=>new Random(seed+round*173+g*719).Next()).Take(3).ToArray();
        Goal=Choices[0];Penalty=Delta=0;Result=new();Settled=false;Awards.Clear();
    }
    public void Resolve(IEnumerable<PlayerState> players,int seed)
    {
        var counts=Choices.Select(g=>(Goal:g,Count:players.Count(p=>p.Active&&p.Vote==g))).ToArray();int max=counts.Max(c=>c.Count);
        var ties=counts.Where(c=>c.Count==max).Select(c=>c.Goal).ToArray();Goal=max==0?Choices[0]:ties[new Random(seed).Next(ties.Length)];
    }
    public void Settle()
    {if(Settled)return;Settled=true;Result.Penalty=Penalty;Delta=(int)MathF.Round(Result.Goal<0?20+Result.Shape+Result.Function*.5f-Penalty:20+Result.Final*1.5f);Wallet+=Delta;}
}

public enum CustomerStimulus { Noise,Heat,Wind,Impact,Validator }
public enum CustomerReaction { None,Duck,Turn,Recoil,Spit }
public enum HeadMaterialKind { Hair,Wool }
public sealed record CustomerProfile(string Id,HeadMaterialKind Material,float Sensitivity)
{
    public static readonly CustomerProfile Human=new("human",HeadMaterialKind.Hair,1);
    public static readonly CustomerProfile Llama=new("llama",HeadMaterialKind.Wool,1.1f);
    public static CustomerProfile Get(string id)=>id=="llama"?Llama:Human;
}

public static class CustomerMotion
{
    public const float Anticipation=.65f,Duration=1.25f;
    public static (Vector3 Offset,Vector3 Rotation) Pose(CustomerState c,float time)
    {
        float t=time-c.ReactionStart-Anticipation;
        if(c.Action==CustomerReaction.None||t<0||t>Duration)return(default,default);
        float v=MathF.Sin(MathF.PI*t/Duration);
        return c.Action switch {
            CustomerReaction.Duck=>(new(0,-.26f*v,0),new(.12f*v,0,0)),
            CustomerReaction.Turn=>(new(c.Direction*.035f*v,0,0),new(0,c.Direction*.32f*v,0)),
            _=>(new(0,.06f*v,-.17f*v),new(-.18f*v,c.Direction*.12f*v,0))};
    }
    public static bool Anticipating(CustomerState c,float time)=>c.Action!=CustomerReaction.None&&time-c.ReactionStart is >=0 and <Anticipation;
    public static void Apply(WorldState w)
    {
        if(w.Experiment.IsB&&w.Experiment.Resolved&&w.Experiment.CustomerActor==0)return;
        var c=w.Customers[0];var (offset,rotation)=w.Experiment.IsB?Session.AttentionPose(w):Pose(c,w.Time);
        var party=w.Experiment.IsB?PartyLoop.Pose(w):(Offset:Vector3.Zero,Rotation:Vector3.Zero);
        if(w.Experiment.IsB&&w.Experiment.Remark is Remark.StyleHot or Remark.StyleCold){float age=w.Time-w.Experiment.RemarkAt;float duration=w.Experiment.Remark==Remark.StyleHot?.6f:.8f;float pulse=age>=0&&age<duration?MathF.Sin(age/duration*MathF.Tau):0;float strength=w.Players.Any(p=>p.Active&&p.Bracing)?.22f:1;rotation+=w.Experiment.Remark==Remark.StyleHot?new Vector3(.12f*pulse*strength,0,0):new Vector3(0,.18f*pulse*strength,0);}
        var human=w.Players.FirstOrDefault(p=>p.Active&&p.Customer);
        if(human!=null){party=(Vector3.Zero,Vector3.Zero);rotation+=HumanCustomer.Expression(w);}
        w.SharedHead.Position=(human?.Standing==true?human.Position+new Vector3(0,1.5f+Math.Clamp((w.Time-human.StandAt)/.4f,0,1)*.2f,0):Session.WorkCenter+new Vector3(0,1.5f+c.ChairHeight,0))+offset+party.Offset;
        if(human==null&&w.Experiment.IsB)rotation.X+=w.Experiment.Drowsiness*MathF.PI/18;
        w.SharedHead.Rotation=rotation+party.Rotation+new Vector3(0,human?.Standing==true?human.Yaw-MathF.PI:c.ChairYaw,w.Experiment.IsB?w.Experiment.Tilt*(w.Players.Any(p=>p.Active&&p.Bracing)?.22f:1):0);
    }
}

public sealed partial class Session
{
    public void Stimulate(float amount,int source,CustomerStimulus stimulus)
    {
        if(State.Phase!=Phase.Build||!float.IsFinite(amount))return;
        var c=State.Customers[0];c.Panic=Math.Clamp(c.Panic+amount*CustomerProfile.Get(c.Profile).Sensitivity,0,1);c.LastAttacker=source;c.LastStimulus=stimulus;
    }
    void TickCustomer(float dt)
    {
        var c=State.Customers[0];c.Panic=Math.Max(0,c.Panic-dt*.028f);c.ReactionCooldown=Math.Max(0,c.ReactionCooldown-dt);
        if(State.SharedHead.Patches.Any(p=>p.Burning))Stimulate(dt*.16f,State.SharedHead.Patches.First(p=>p.Burning).BurnSource,CustomerStimulus.Heat);
        if(c.Action!=CustomerReaction.None&&State.Time-c.ReactionStart>CustomerMotion.Anticipation+CustomerMotion.Duration)c.Action=CustomerReaction.None;
        if(c.Action==CustomerReaction.None&&c.ReactionCooldown<=0&&c.Panic>=.35f)
        {
            c.ReactionCount++;c.Action=c.Panic>.8f?CustomerReaction.Recoil:c.ReactionCount%2==1?CustomerReaction.Duck:CustomerReaction.Turn;
            if(c.Profile=="llama"&&c.ReactionCount%2==1)c.Action=CustomerReaction.Spit;
            c.SignatureUsed=false;
            var target=State.Player(c.LastAttacker);
            var spitAim=target==null?Vector3.UnitZ:Eye(target)-State.SharedHead.Position;
            c.SignatureDirection=spitAim.LengthSquared()>.001f?Vector3.Normalize(spitAim):Vector3.UnitZ;
            c.Direction=State.Player(c.LastAttacker)?.Position.X<0?-1:1;c.ReactionStart=State.Time;c.ReactionCooldown=c.Panic>.8f?9:6;
            Event(c.LastAttacker,1,State.SharedHead.Position,"Customer braces before moving",12);
        }
        if(c.Action==CustomerReaction.Spit&&!c.SignatureUsed&&State.Time-c.ReactionStart>=CustomerMotion.Anticipation)
        {
            c.SignatureUsed=true;
            foreach(var p in State.Players.Where(p=>p.Active))
            {
                var offset=Eye(p)-State.SharedHead.Position;
                if(offset.Length()<3.3f&&Vector3.Dot(Vector3.Normalize(offset),c.SignatureDirection)>.91f)p.Obscured=1.25f;
            }
            Event(0,1,State.SharedHead.Position,"Llama spit — mind the warning!",24);
        }
        CustomerMotion.Apply(State);
        if(State.Job.Goal==0&&State.Remaining<15&&!Lab)
        {
            if(!c.ValidatorWarned){c.ValidatorWarned=true;Stimulate(.08f,0,CustomerStimulus.Validator);Event(0,1,State.SharedHead.Position,"Helicopter approaching — rotor wash",18);}
            var helicopter=State.Props.FirstOrDefault(p=>p.Goal==0);
            if(helicopter!=null&&helicopter.Holder==0){helicopter.Attached=false;helicopter.Released=false;helicopter.Velocity=default;helicopter.Position=Vector3.Lerp(helicopter.Position,State.SharedHead.Position+new Vector3(1.9f,2.2f,.9f),1-MathF.Exp(-dt*.6f));}
            if(helicopter!=null&&State.Time-c.WashTime>.25f){c.WashTime=State.Time;ApplyDownwash(.25f);}
        }
    }
    void ApplyDownwash(float dt)
    {
        var h=State.SharedHead;float resistance=h.Patches.Average(p=>p.Resistance);
        h.Volume.Brush(new(EffectKind.ApplyForce,dt*.014f*(1-resistance),new(.2f,-1,.1f)),new(0,.8f,0),1.4f);
        DebrisSystem.Blow(State.Debris,h.Position+Vector3.UnitY*2,-Vector3.UnitY,5,1.5f);
        foreach(var loose in State.Heads.Where(h=>h.Loose&&h.AttachedTo<0&&Vector3.Distance(h.Position,WorkCenter)<3))loose.Velocity+=new Vector3(.3f,0,.15f);
        foreach(var t in State.Tools.Where(t=>t.Holder==0&&Vector3.Distance(t.Position,WorkCenter)<2.6f))t.Velocity+=new Vector3(.15f,0,.08f);
    }
    public void ResolveWorldEffects(PlayerState source,ToolState tool)
    {
        var origin=ToolEye(State,source);var dir=Aim(source);int id=tool.Definition;
        float range=ObstructionDistance?.Invoke(origin,dir,Tools.Get(id).Range)??Tools.Get(id).Range;
        var h=State.SharedHead;float distance=Vector3.Distance(origin,h.Position);
        bool near=HairSystem.DistanceToSegment(h.Position,origin,origin+dir*range)<1;
        if(id==7&&distance<9)Stimulate(.48f,source.Id,CustomerStimulus.Noise);
        else if(near&&id is 3 or 8 or 9)Stimulate(id==8?.1f:id==9?.04f:.025f,source.Id,id==8?CustomerStimulus.Heat:id==3?CustomerStimulus.Wind:CustomerStimulus.Noise);
        foreach(var target in State.Players.Where(p=>p.Active&&p.Id!=source.Id))
        {
            var center=target.Position+Vector3.UnitY*1.25f;float along=Vector3.Dot(center-origin,dir);
            float radius=id==3?.82f:id==2?.5f:id==7?.28f:.55f;
            if(along<=0||along>Math.Min(range,id==7?22:4)||HairSystem.DistanceToSegment(center,origin,origin+dir*range)>radius)continue;
            if(id==3)PushPlayer(target,dir*1.4f,source.Id,false);
            if(id==2&&State.Barber(target.Slot).Mass>.3f)PushPlayer(target,-dir*1.1f,source.Id,false);
            if(id is 7 or 8 or 9)PushPlayer(target,dir*(id==7?4:1.5f)+Vector3.UnitY*.3f,source.Id,true);
        }
        AffectModels(source,tool,range);
        if(id is 2 or 3)foreach(var prop in State.Props.Where(t=>t.Holder==0&&!(source.Customer&&t.Attached)&&HairSystem.DistanceToSegment(t.Position,origin,origin+dir*range)<.8f))
        {if(id==3&&prop.Attached)RecordAction(PartyAction.BlowProp,source.Id,prop.Id);prop.Pinned=false;prop.Attached=false;prop.Released=true;prop.Velocity+=dir*(id==3?2:-1.5f)+Vector3.UnitY*.35f;}
        if(id==3)foreach(var dropped in State.Tools.Where(t=>t.Holder==0&&HairSystem.DistanceToSegment(t.Position,origin,origin+dir*range)<.9f))dropped.Velocity+=dir*1.8f+Vector3.UnitY*.3f;
    }
    void PushPlayer(PlayerState target,Vector3 impulse,int source,bool accident)
    {
        if(State.Experiment.IsB)return; // G-4: teammate tools never displace the FPS camera.
        if(target.ImpactCooldown>0)return;
        target.Impulse+=impulse;target.ImpactCooldown=accident?1.2f:.15f;
        if(accident){int affected=target.Held>=0?2:1;Drop(target);State.Job.Penalty+=8;if(State.Player(source) is {} p)p.Incidents++;Stimulate(.15f,source,CustomerStimulus.Impact);RecordIncident(source,8,target.Position,affected);Event(source,affected,target.Position,$"Tool impact: {target.Name}, shop -8",22);}
        else Event(source,1,target.Position,$"Tool force moved {target.Name}",6);
    }
    void TickWorldTools(float dt)
    {
        foreach(var source in State.Players.Where(p=>State.Phase==Phase.Build&&p.Active&&!p.Bracing&&State.Tools.Any(t=>t.Id==p.Held&&t.Definition==9)))
        {
            var from=Eye(source)-Vector3.UnitY*.2f;var tip=from+Aim(source)*1.05f;
            foreach(var target in State.Players.Where(p=>p.Active&&p.Id!=source.Id))
                if(HairSystem.DistanceToSegment(target.Position+Vector3.UnitY*1.35f,from,tip)<.45f)PushPlayer(target,Aim(source)*1.7f,source.Id,true);
        }
        foreach(var t in State.Tools.Where(t=>t.Holder==0&&t.Velocity.LengthSquared()>.001f))
        {
            t.Velocity-=Vector3.UnitY*dt*7*TwistCards.Gravity(State);var end=t.Position+t.Velocity*dt;
            if(FlightContact(t.Position,end,t.Thrower,t.ThrownAt,false,p=>{t.Holder=p.Id;t.Velocity=default;p.Held=t.Id;},()=>{t.Velocity=default;}))continue;
            var hit=DebrisCast?.Invoke(t.Position-Vector3.UnitY*.2f,end-Vector3.UnitY*.22f);
            if(hit.HasValue){end=hit.Value+Vector3.UnitY*.22f;t.Velocity*=.35f;t.Velocity.Y=0;}
            if(end.Y<.22f){end.Y=.22f;t.Velocity*=.65f;t.Velocity.Y=0;}
            t.Position=new(Math.Clamp(end.X,-5.5f,5.5f),end.Y,Math.Clamp(end.Z,-5.5f,5.5f));
        }
    }
}

