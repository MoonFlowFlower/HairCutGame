using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Hairball.Core;

public enum Phase { Lobby, Arrival, Choice, Preview, Build, Validation, Results, Highlight, Complete }
[Flags] public enum Buttons { None=0, Primary=1, Secondary=2, Interact=4, Drop=8, Jump=16, ChairUp=32, ChairDown=64, Reference=128, ChairLeft=256,ChairRight=512,Stand=1024 }
public sealed class PlayerState
{
    public int Id, Slot, Held = -1, CarryLadder=-1, CarriedProp=-1, CarriedModel=-1, Incidents;
    public int BrushSize=1;
    public int CarriedHair=-1,SupportLadder=-1;
    public bool PieUsed,PoopUsed,LikeUsed,VoiceEnabled=true;public int CosmeticKind;public float CosmeticUntil,LikedUntil;
    public bool AllowDrag,GlueHead;public Vector3 GlueOffset;
    public bool AllowViewChanges,AllowDown,Customer,Standing,HeadLook;
    public int Expression;public float ExpressionAt=-100,DisabledUntil,StandAt,EyeBaseYaw;
    public float DownUntil,DownImmune,HitUntil,ThrowCharge,PrimaryAt=-100,SecondaryAt=-100;
    public string Name = "";
    public bool Active = true;
    public bool NetworkAway;
    public bool UsingBlower, ReferenceUp, Bracing, GrowthFine;
    public int Vote=-1;
    public Vector3 Impulse;
    public float ImpactCooldown, Obscured, Added, Removed;
    public float GrowthHeldSeconds;
    public float FreezeUntil,GlueUntil,FireUntil,FreezeImmune,GlueImmune,FireImmune;
    public Vector3 Position;
    public float Yaw, Pitch, Reservoir, StoredHeat=20, StoredGlue, StoredChar, StoredYoung, StoredWet, Cooldown;
}
public sealed class ToolState
{
    public int ContactHead=-1;
    public int Id, Definition, Holder, Charge, Special;
    public Vector3 Position, Velocity;
    public float GrowthRemaining=GrowthSpray.BottleCapacity;
    public float LastUse=-100;
    public Vector3 Contact;
    public bool HitHair;
    public float EffectMass;
    public HairState ContactState;
    public int Thrower;
    public float ThrownAt;
}
public sealed class LadderState { public int Id,Holder,Supporter; public Vector3 Position; public float Yaw; public LadderState Clone()=>(LadderState)MemberwiseClone(); }
public sealed class CustomerState {
    public int Slot;public float ChairHeight,ChairYaw,Recovery,Reaction;public int LastAttacker;public Vector3 FallPosition,FallRotation;
    public string Profile="human";public float Panic,ReactionStart,ReactionCooldown,WashTime;public int ReactionCount,Direction=1;
    public Vector3 SignatureDirection=Vector3.UnitZ;
    public CustomerReaction Action;public CustomerStimulus LastStimulus;public bool ValidatorWarned,SignatureUsed;
    public CustomerState Clone()=>(CustomerState)MemberwiseClone();
}
public sealed class PropState
{
    public int Slot, Index, Goal, Id, Holder;
    public int Holder2,Thrower,CoopMode;
    public CoopKind Coop;
    public int Gesture=-1;
    public float ThrownAt,LastUse=-100;
    public float MaterialScale=1;
    public Vector3 Contact;
    public bool HitHair;
    public MoldKind Mold;
    public bool Pinned;
    public float PlacedAt;
    public List<Vector3> Holes=new();
    public bool Attached, Released, OnScalp;
    public Vector3 Local, StartPosition, StartLocal, Rotation, AttachedRotation;
    public PropState Clone(){var copy=(PropState)MemberwiseClone();copy.Holes=new(Holes);return copy;}
    public Vector3 Position, Velocity;
    public bool Failed;
    public float HeldTime;
}
public sealed class GameEvent
{
    public int Id, Source, Targets;
    public float Time, Value;
    public Vector3 Position;
    public string Text = "";
}
public sealed class WorldState
{
    public CatState Cat=new();public bool ReturningHeads;public string MatchTag="";public List<RoundMemory> RoundHistory=new();
    public TwistState Twist=new();
    public int MatchNumber,MatchRounds=1;
    public int Seed=729, Round, Revision, NextHead=100, NextEvent;
    public float Time, Remaining;
    public Phase Phase;
    public SharedJob Job=new();
    public ExperimentState Experiment=new();
    public ShopOrder Order=new();
    public List<PlayerState> Players = new();
    public List<Head> Heads = new();
    public List<Head> ValidationHeads = new();
    public List<ToolState> Tools = new();
    public List<CustomerState> Customers = new();
    public List<LadderState> Ladders = new();
    public List<PropState> Props = new();
    public List<PartyProjectile> Projectiles=new();
    public List<GameEvent> Events = new();
    public DebrisState Debris=new();
    public string Notice = "Welcome to Hairball", HighlightTitle = "";
    public float HighlightStart;
    public Vector3 HighlightFocus;
    public PlayerState? Player(int id) => Players.FirstOrDefault(p => p.Id == id && p.Active);
    public Head SharedHead => Heads.First(h => h.Id == 0);
    public Head Barber(int slot) => Heads.First(h => h.Id == slot*2+1);
}

public sealed partial class Session
{
    public WorldState State = new();
    public bool Lab;
    public float ArrivalSeconds=5, PreviewSeconds=3, ChoiceSeconds=6, BuildSeconds=75, ValidationSeconds=9, ResultsSeconds=7, HighlightSeconds=6;
    public readonly Dictionary<int, (Vector2 Move, float Yaw, float Pitch, Buttons Buttons)> Inputs = new();
    public readonly Dictionary<int,float> InputTimes=new();
    readonly Dictionary<int, Buttons> previous = new();
    public readonly ReplayRecorder Replay = new();
    public Func<Vector3,Vector3,float,float>? ObstructionDistance;
    public Func<Vector3,Vector3,Vector3?>? DebrisCast;
    public static Vector3 WorkCenter => Vector3.Zero;
    public static Vector3 Spawn(int slot)=>Rotate(new(0,0,2.6f),-slot*MathF.PI/2);
    public Func<Vector3,float,int,bool>? CanPlaceLadder;
    public static Vector3 ChairControl(int slot)=>WorkCenter+new Vector3(-.76f,.85f,.36f);
    public static Vector3 Rotate(Vector3 v,float yaw)=>Vector3.Transform(v,Quaternion.CreateFromAxisAngle(Vector3.UnitY,yaw));
    void AddFace(Head parent){foreach(var region in new[]{HairRegion.LeftBrow,HairRegion.RightBrow,HairRegion.Beard})State.Heads.Add(Head.CreateFace(parent,region));}
    public void SyncFaces(){foreach(var h in State.Heads.Where(h=>h.Facial)){var parent=State.Heads.FirstOrDefault(p=>p.Id==h.ParentHead);if(parent!=null){h.Position=parent.ToWorld(Head.FaceOffset(h.Region));h.Rotation=parent.Rotation;h.Locked=parent.Locked;}}}
    public bool AdjustChair(PlayerState p,int slot,float delta)
    {
        var customer=State.Customers.FirstOrDefault(c=>c.Slot==slot);
        if(customer==null||State.Experiment.IsB&&State.Experiment.Leave!=LeaveStage.Seated||State.Phase is not (Phase.Choice or Phase.Build)||State.SharedHead.Locked||customer.Recovery>0||!LookingAt(p,ChairControl(slot),.4f,3))return false;
        float previousHeight=customer.ChairHeight;customer.ChairHeight=Math.Clamp(customer.ChairHeight+Math.Clamp(delta,-.05f,.05f),-.4f,1);
        if(customer.ChairHeight!=previousHeight)DebrisSystem.ReleaseSupport(State.Debris,WorkCenter,.85f);
        State.SharedHead.Position=WorkCenter+new Vector3(0,1.5f+customer.ChairHeight,0);SyncFaces();return true;
    }
    public bool PickupLadder(PlayerState p,int id)
    {
        if(Sticky(p))return false;
        var ladder=State.Ladders.FirstOrDefault(l=>l.Id==id);
        if(ladder==null||ladder.Holder!=0||p.CarryLadder>=0||State.Phase is not (Phase.Choice or Phase.Build)||!LookingAt(p,ladder.Position+Vector3.UnitY*.9f,.65f,3))return false;
        if(State.Players.Any(q=>{var offset=Rotate(q.Position-ladder.Position,-ladder.Yaw);return q.Active&&Math.Abs(offset.X)<.8f&&Math.Abs(offset.Z)<1.1f&&offset.Y>.1f;}))return false;
        Drop(p);DebrisSystem.ReleaseSupport(State.Debris,ladder.Position,1.15f);ladder.Holder=p.Id;p.CarryLadder=id;return true;
    }
    public bool DropLadder(PlayerState p)
    {
        var ladder=State.Ladders.FirstOrDefault(l=>l.Id==p.CarryLadder&&l.Holder==p.Id);if(ladder==null){p.CarryLadder=-1;return true;}
        var target=p.Position+Rotate(new(0,0,-1.65f),p.Yaw);target.Y=0;
        bool clear=Math.Abs(target.X)<4.65f&&Math.Abs(target.Z)<4.9f;
        clear&=!State.Customers.Any(c=>Vector2.Distance(new(target.X,target.Z),new(WorkCenter.X,WorkCenter.Z))<1.3f);
        clear&=!State.Ladders.Any(l=>l.Id!=ladder.Id&&l.Holder==0&&Vector3.Distance(l.Position,target)<1.5f);
        clear&=CanPlaceLadder?.Invoke(target,p.Yaw,p.Id)??true;
        if(!clear){State.Notice="Not enough room to place the ladder.";return false;}
        ladder.Position=target;ladder.Yaw=p.Yaw;ladder.Holder=0;p.CarryLadder=-1;return true;
    }
    public static Vector3 Eye(PlayerState p) => p.Position+new Vector3(0,1.7f,0);
    public static Vector3 Aim(PlayerState p) => new(-MathF.Sin(p.Yaw)*MathF.Cos(p.Pitch), MathF.Sin(p.Pitch), -MathF.Cos(p.Yaw)*MathF.Cos(p.Pitch));
    public PlayerState? AddPlayer(int id,string name)
    {
        if(State.Player(id) is {} existing)return existing;
        int slot=Enumerable.Range(0,4).FirstOrDefault(s=>!State.Players.Any(p=>p.Active&&p.Slot==s),-1);if(slot<0)return null;
        State.Heads.RemoveAll(h=>h.Barber&&(h.Id==slot*2+1||h.ParentHead==slot*2+1));
        var p=new PlayerState{Id=id,Slot=slot,Name=name,Position=Spawn(slot),Yaw=-slot*MathF.PI/2};State.Players.Add(p);
        var barber=Head.Create(slot*2+1,id,true);barber.Position=Eye(p);State.Heads.Add(barber);AddFace(barber);
        initialBarberMass[id]=barber.Mass;
        if(State.Customers.Count==0)ResetCustomer();
        if(State.Tools.Count==0)ResetTools();SyncFaces();
        Event(id,0,p.Position,$"{name} joined the shared salon",1);return p;
    }
    void ResetCustomer()
    {
        State.Heads.RemoveAll(h=>!h.Barber&&!h.Loose);
        var profile=State.Round==2?CustomerProfile.Llama:CustomerProfile.Human;
        var head=Head.Create(0,0);head.Material=profile.Material;head.Position=WorkCenter+new Vector3(0,1.5f,0);State.Heads.Add(head);AddFace(head);
        State.Customers.Clear();State.Customers.Add(new(){Slot=0,Profile=profile.Id});
    }
    public void RemovePlayer(int id)
    {
        if(State.Player(id) is not {} p)return;
        CustomerDisconnected(id);ResetAccidents(p);
        Drop(p);if(p.CarryLadder>=0){var ladder=State.Ladders.First(l=>l.Id==p.CarryLadder);ladder.Holder=0;ladder.Position=new(0,0,4);p.CarryLadder=-1;}
        SetBrace(p,false);p.Active=false;Inputs.Remove(id);previous.Remove(id);EndStroke(id);
        State.Heads.RemoveAll(h=>h.Barber&&h.Owner==id);
        foreach(var attached in State.Heads.Where(h=>h.AttachedTo==p.Slot*2+1)){attached.AttachedTo=-1;attached.Locked=false;}
        Event(id,0,p.Position,$"{p.Name} disconnected",1);
    }
    public void StartMatch()
    {
        State.MatchNumber++;State.MatchTag=Guid.NewGuid().ToString("N");State.RoundHistory.Clear();State.ReturningHeads=ReturningEnabled;lastTwist=TwistKind.None;
        State.Debris=new();State.Job=new();State.Round=0;Inputs.Clear();InputTimes.Clear();previous.Clear();
        foreach(var p in State.Players.Where(p=>p.Active))
        {State.Heads.RemoveAll(h=>h.Barber&&h.Owner==p.Id);var barber=Head.Create(p.Slot*2+1,p.Id,true);barber.Position=Eye(p);State.Heads.Add(barber);AddFace(barber);}
        NextRound();
    }
    public void NextRound()
    {
        if(ShopCat.Prop(State) is {} oldCat)CatReturn(oldCat);var retained=RetainedHeads();string retainedProfile=State.Customers.FirstOrDefault()?.Profile??"human";
        foreach(var fragment in State.Heads.Where(h=>h.Fragment).ToArray())DepositFragment(fragment);
        foreach(var ladder in State.Ladders)DebrisSystem.ReleaseSupport(State.Debris,ladder.Position,1.15f);
        DebrisSystem.ReleaseSupport(State.Debris,WorkCenter,.85f);
        strokes.Clear();endedGestures.Clear();topologyRevision.Clear();topologyVolumes.Clear();
        State.Projectiles.Clear();foreach(var p in State.Players){p.PieUsed=p.PoopUsed=p.LikeUsed=false;p.CosmeticUntil=p.LikedUntil=0;}
        State.Round++;State.Phase=Lab?Phase.Build:Phase.Arrival;State.Remaining=Lab?99999:ArrivalSeconds;
        State.Heads.RemoveAll(h=>h.Loose);State.Props.Clear();State.ValidationHeads.Clear();ResetTools();Replay.Clear();ResetCustomer();RestoreReturning(retained,retainedProfile);
        State.Job.Begin(State.Seed,State.Round);PhysicalProps.Stage(State);StageExperiment();SelectCustomer();ResetSecrets();
        State.Order=new();
        Ledger.Begin(State.Round,LiveHealth().Value);initialBarberMass.Clear();
        foreach(var actor in State.Players.Where(p=>p.Active))initialBarberMass[actor.Id]=State.Barber(actor.Slot).Mass;
        foreach(var p in State.Players.Where(p=>p.Active)){ResetAccidents(p);ResetGrowthHold(p);p.Vote=-1;p.Incidents=0;p.Added=p.Removed=0;p.Held=-1;p.CarryLadder=-1;p.CarriedModel=-1;p.Reservoir=0;p.Impulse=default;p.Obscured=0;State.Barber(p.Slot).Locked=false;}
        State.Ladders=[new(){Id=0,Position=new(-2.7f,0,-2.5f)},new(){Id=1,Position=new(2.7f,0,2.5f)}];SyncFaces();
        SpawnWig(State.Experiment.IsB?new(-1.4f,.65f,-1.3f):new(-1,.65f,5),0);SpawnWig(State.Experiment.IsB?new(1.4f,.65f,-1.3f):new(1,.65f,5),1);
        StageMolds();StageCoop();ResetTargetCards();StageTwists();StageCat();ResetAlertness();State.Notice="One customer. One shared job.";
    }
    void ResetTools()
    {
        State.Tools.Clear();
        for(int i=0;i<10;i++) State.Tools.Add(new(){Id=i, Definition=i, Charge=Tools.All[i].Charges, Position=new(i<5?-5.25f:5.25f,1.05f,(i%5-2)*1.4f)});
        // Safe basics at both sides prevent a single shared precision tool bottleneck.
        for(int i=0;i<2;i++) State.Tools.Add(new(){Id=10+i,Definition=i,Charge=-1,Position=new(i==0?5.25f:-5.25f,1.05f,4.2f)});
        State.Tools[0].Position=new(-2.8f,1.05f,.95f);State.Tools[1].Position=new(-2.8f,1.05f,1.65f);
        State.Tools[10].Position=new(2.8f,1.05f,-.95f);State.Tools[11].Position=new(2.8f,1.05f,-1.65f);
        int extra=0;
        for(int definition=0;definition<=4;definition++)
        {
            int count=State.Tools.Count(t=>t.Definition==definition);
            for(int copy=count;copy<4;copy++)
            {int index=extra++;int side=index%2==0?-1:1;State.Tools.Add(new(){Id=12+index,Definition=definition,Charge=-1,Position=new(side*5.55f,1.05f,-3.25f+index/2*1.05f)});}
        }
        State.Tools.Add(new(){Id=90,Definition=10,Charge=-1,Position=new(-4.5f,1.05f,4.2f)});
        foreach(var tool in State.Tools.Where(t=>t.Definition<=4))tool.Charge=-1;
        if(State.Experiment.IsB)State.Tools.RemoveAll(t=>t.Definition==7);
    }
    public void Choose(int id,int choice)
    {if(State.Phase==Phase.Choice&&State.Player(id) is {} p&&State.Job.Choices.Contains(choice))p.Vote=choice;}
    public void Drop(PlayerState p)
    {
        if(Sticky(p))return;
        if(p.CarriedHair>=0){ReleaseHair(p);return;}
        ResetGrowthHold(p);
        EndStroke(p.Id);
        if(p.CarriedModel>=0){ReleaseModel(p);return;}
        if(p.CarriedProp>=0){if(State.Props.FirstOrDefault(x=>x.Id==p.CarriedProp) is {Coop:CoopKind.LargePad} large)ReleaseLarge(p,large);else ReleaseProp(p);return;}
        if(p.CarryLadder>=0){DropLadder(p);return;}
        if(State.Tools.FirstOrDefault(t=>t.Id==p.Held && t.Holder==p.Id) is {} tool)
        { tool.Holder=0;tool.Position=Eye(p)-Vector3.UnitY*.4f+Aim(p)*.5f;tool.Velocity=Aim(p)*1.2f;tool.Thrower=p.Id;tool.ThrownAt=State.Time; }
        p.Held=-1;
    }
    public bool Pickup(PlayerState p,int toolId)
    {
        var t=State.Tools.FirstOrDefault(t=>t.Id==toolId);
        if(!TwistToolAllowed(p,t?.Definition??-1)||Sticky(p)||p.CarryLadder>=0 || State.Phase is not (Phase.Choice or Phase.Build) || t==null || t.Holder!=0 || Vector3.Distance(Eye(p),t.Position)>2.5f) return false;
        Drop(p); p.Held=t.Id; t.Holder=p.Id; return true;
    }
    public void Tick(float dt)
    {
        State.Time+=dt; State.Revision++;
        AssignSecrets();TickAccidents();TickOrder(dt);TickBalance(dt);UpdateBraces();TickTargetCards(dt);TickTwists(dt);TickAlertness(dt);TickHeadGlue();SampleExperiment(dt);
        var environmentBefore=State.Phase==Phase.Build?LiveHealth():default;
        if(State.Phase==Phase.Build){EnsureLedger();if(State.Experiment.IsB)TickAttention(dt);else TickCustomer(dt);}
        TickWorldTools(dt);
        if(State.Phase==Phase.Build)PhysicalProps.Tick(State,dt,DebrisCast,PropFlight);
        if(State.Phase==Phase.Build)RecordEnvironment(environmentBefore);
        SyncFaces();
        foreach(var p in State.Players.Where(p=>p.Active))
        {
            var head=State.Barber(p.Slot); head.Position=PartyBodies.HairPosition(p,State.Time);head.Rotation=PartyBodies.HairRotation(p,State.Time);
            p.Cooldown=Math.Max(0,p.Cooldown-dt);p.Impulse*=MathF.Exp(-dt*4);p.ImpactCooldown=Math.Max(0,p.ImpactCooldown-dt);p.Obscured=Math.Max(0,p.Obscured-dt);
            if(!Inputs.TryGetValue(p.Id,out var input)) { p.UsingBlower=false;ResetGrowthHold(p);EndStroke(p.Id); continue; }
            p.UsingBlower=(!State.Experiment.IsB||PartyAccidents.Primary(p,State.Time))&&!p.Bracing&&input.Buttons.HasFlag(Buttons.Primary)&&State.Tools.Any(t=>t.Id==p.Held&&t.Definition==3);
            if((input.Buttons&(Buttons.Primary|Buttons.Secondary))==0)EndStroke(p.Id);
            p.Yaw=input.Yaw; p.Pitch=input.Pitch;
            head.Position=PartyBodies.HairPosition(p,State.Time);head.Rotation=PartyBodies.HairRotation(p,State.Time);
            if(p.CarriedProp>=0&&State.Props.FirstOrDefault(x=>x.Id==p.CarriedProp) is {} heldProp)heldProp.Rotation.X=Math.Clamp(heldProp.Rotation.X+((input.Buttons.HasFlag(Buttons.ChairUp)?1:0)-(input.Buttons.HasFlag(Buttons.ChairDown)?1:0))*dt,-1.2f,1.2f);
            else if(input.Buttons.HasFlag(Buttons.ChairUp)!=input.Buttons.HasFlag(Buttons.ChairDown))
                foreach(var c in State.Customers)AdjustChair(p,c.Slot,(input.Buttons.HasFlag(Buttons.ChairUp)?1:-1)*dt*.65f);
            p.ReferenceUp=!State.Experiment.IsB&&input.Buttons.HasFlag(Buttons.Reference);if(p.ReferenceUp)EndStroke(p.Id);
            previous.TryGetValue(p.Id,out var old);
            if(State.Experiment.IsB){if(input.Buttons.HasFlag(Buttons.Drop))p.ThrowCharge=Math.Min(1,p.ThrowCharge+dt);else if(old.HasFlag(Buttons.Drop)){if(p.ThrowCharge>=.2f)Throw(p,p.ThrowCharge);else Drop(p);p.ThrowCharge=0;}}
            else if(input.Buttons.HasFlag(Buttons.Drop) && !old.HasFlag(Buttons.Drop)) Drop(p);
            if(input.Buttons.HasFlag(Buttons.Primary)&&!old.HasFlag(Buttons.Primary)){
                p.PrimaryAt=Math.Clamp(InputTimes.GetValueOrDefault(p.Id,State.Time),State.Time-1,State.Time+.1f);
                SlapAI(p);
            }
            if(input.Buttons.HasFlag(Buttons.Secondary)&&!old.HasFlag(Buttons.Secondary))p.SecondaryAt=State.Time;
            if(input.Buttons.HasFlag(Buttons.ChairLeft)!=input.Buttons.HasFlag(Buttons.ChairRight))RotateChair(p,(input.Buttons.HasFlag(Buttons.ChairRight)?1:-1)*dt);
            bool interact=input.Buttons.HasFlag(Buttons.Interact);
            bool customerHandled=CustomerInput(p,input.Buttons,old);
            if(interact&&!old.HasFlag(Buttons.Interact)&&(!p.Bracing||p.Customer)&&!customerHandled)Interact(p);
            UpdateGrowthHold(p,input.Buttons,dt);
            if((State.Phase==Phase.Build||DemoActive(p)) && p.Cooldown<=0 && !p.ReferenceUp)
            {
                if(input.Buttons.HasFlag(Buttons.Primary)) UseTool(p,false);
                else if(input.Buttons.HasFlag(Buttons.Secondary)) UseTool(p,true);
            }
            RevealInput(p,input.Buttons,old,dt);previous[p.Id]=input.Buttons;
        }
        SyncFaces();
        TickCoop(dt);
        foreach(var ladder in State.Ladders.Where(l=>l.Holder!=0))if(State.Player(ladder.Holder) is {} carrier){ladder.Position=carrier.Position+Rotate(new(.55f,.35f,-1.1f),carrier.Yaw);ladder.Yaw=carrier.Yaw;}
        if(State.Phase==Phase.Build)
        {
            var passiveBefore=LiveHealth();
            foreach(var h in State.Heads.ToArray())
            {
                float before=h.Mass;HairSystem.Tick(h,dt);float removed=before-h.Mass;
                if(removed>.000001f)DebrisSystem.Emit(State.Debris,h.Position+Vector3.UnitY*.3f,Vector3.Zero,removed,DebrisMaterial.From(h,Vector3.UnitY*.3f));
                FinalizeHair(h,h.Owner);
            }
            foreach(var c in State.Customers)
            {
                c.Recovery=Math.Max(0,c.Recovery-dt); c.Reaction=Math.Max(0,c.Reaction-dt);
                var head=State.SharedHead;
                if(!State.Experiment.IsB && !head.Locked && head.Patches.Count(p=>p.Burning)>12 && c.Recovery<=0)
                { var source=head.Patches.First(p=>p.Burning).BurnSource; Incident(source,c.Slot,25); }
            }
            RecordEnvironment(passiveBefore);Ledger.Advance(State.Time);
            Replay.Record(State,dt,Ledger);
        }
        var looseBefore=State.Phase==Phase.Build?LiveHealth():default;
        TickModels(dt);
        TickLoose(dt);
        if(State.Phase==Phase.Build)RecordEnvironment(looseBefore);
        TickAccidents();TickTolerance(dt);TickHumanCustomer();TickCat(dt);TickParty(dt);TickReveal(dt);if(State.Phase is Phase.Lobby or Phase.Complete) return;
        State.Remaining-=dt;
        if(State.Phase==Phase.Validation) Validation.Tick(State,dt,ValidationSeconds-State.Remaining);

        if(State.Remaining>0 || Lab || State.Experiment.IsB&&State.Phase==Phase.Build) return;
        switch(State.Phase)
        {
            case Phase.Arrival: State.Phase=Phase.Choice;State.Remaining=ChoiceSeconds;break;
            case Phase.Choice: State.Job.Resolve(State.Players,State.Seed+State.Round);PhysicalProps.Stage(State);State.Phase=Phase.Preview;State.Remaining=PreviewSeconds;break;
            case Phase.Preview: State.Phase=Phase.Build;State.Remaining=BuildSeconds;Ledger.Begin(State.Round,LiveHealth().Value);State.Notice="One customer. One shared job.";break;
            case Phase.Build:
                if(State.Order.Pending){State.Order.Pending=false;State.Order.Notice="Order cancelled. No money spent.";}
                FinishImpacts();
                foreach(var p in State.Players.Where(p=>p.Active))EndStroke(p.Id);
                foreach(var h in State.Heads)h.Locked=true;
                State.Phase=Phase.Validation; State.Remaining=ValidationSeconds; Validation.Begin(State); break;
            case Phase.Validation:
                Validation.Finish(State); State.Phase=Phase.Results; State.Remaining=ResultsSeconds;
                State.Job.Settle();
                Replay.Select(State); break;
            case Phase.Results: State.Phase=Phase.Highlight; State.Remaining=HighlightSeconds; break;
            case Phase.Highlight:
                if(State.Round>=(State.Experiment.Variant==ExperimentVariant.Off?3:State.MatchRounds)||State.Job.Wallet<=0) { State.Phase=Phase.Complete; State.Notice="Match complete"; }
                else NextRound(); break;
        }
    }
    void Interact(PlayerState p)
    {
        if(Sticky(p))return;
        if(State.Time<p.DownUntil)return;
        if(HandleCat(p)||SlapRevive(p)||HandleCoop(p)||SupportLadder(p)||HandleHair(p))return;
        if(RingBell(p))return;
        if(HandleModel(p))return;
        if(HandleProp(p))return;
        if(State.Phase==Phase.Build&&!State.Experiment.IsB)
        {
            if(LookingAt(p,SpecialOrders.Button(2),.22f,2.5f)){CancelOrder(p.Id);return;}
            for(int option=0;option<2;option++)if(LookingAt(p,SpecialOrders.Button(option),.22f,2.5f)){RequestOrder(p.Id,option);return;}
        }
        if(p.CarryLadder>=0){DropLadder(p);return;}
        var ladder=State.Ladders.FirstOrDefault(l=>l.Holder==0&&LookingAt(p,l.Position+Vector3.UnitY*.9f,.65f,3));
        if(ladder!=null&&PickupLadder(p,ladder.Id))return;
        var t=State.Tools.Where(t=>t.Holder==0 && LookingAt(p,t.Position,.45f,2.5f)).OrderBy(t=>Vector3.Distance(Eye(p),t.Position)).FirstOrDefault();
        if(t!=null) { Pickup(p,t.Id); return; }
        if(State.Phase!=Phase.Build||p.Id==State.Twist.FamilyActor) return;
        var wig=State.Heads.Where(h=>h.Loose && !h.Miniature && h.AttachedTo<0 && !h.Locked && LookingAt(p,h.Position+new Vector3(0,.25f,0),.7f,3)).OrderBy(h=>Vector3.Distance(Eye(p),h.Position)).FirstOrDefault();
        if(wig!=null)
        {
            var customer=State.SharedHead;
            var target=Vector3.Distance(p.Position,customer.Position)<2.8f ? customer:State.Barber(p.Slot);
            if(!target.Locked) { using var impact=new ImpactScope(this,p,new ToolState{Definition=-1});wig.AttachedTo=target.Id;wig.AttachedOffset=new(0,.4f,0);wig.AttachedRotation=default; wig.Position=target.ToWorld(wig.AttachedOffset); RecordAction(PartyAction.Wig,p.Id,wig.Id);Event(p.Id,1,wig.Position,"Wig attached — glue it before the wind does its thing",12); }
        }
    }
    public static bool LookingAt(PlayerState p,Vector3 point,float radius,float range)
    {
        var delta=point-Eye(p); float t=Vector3.Dot(delta,Aim(p));
        return t>0 && t<range && (delta-Aim(p)*t).Length()<radius;
    }
    public void UseTool(PlayerState p,bool secondary)
    {
        if(State.Experiment.IsB&&(!PartyAccidents.Primary(p,State.Time)||p.ThrowCharge>0||State.Time<p.DisabledUntil))return;
        if((State.Phase!=Phase.Build&&!DemoActive(p)) || p.Cooldown>0 || p.NetworkAway || p.Bracing || p.ReferenceUp || p.CarriedProp>=0 || p.CarriedModel>=0) return;
        var t=State.Tools.FirstOrDefault(t=>t.Id==p.Held && t.Holder==p.Id);
        if(t==null || t.Charge==0 || !TwistToolAllowed(p,t.Definition)) return;
        TwistTool(p,t);CatTool(p,t);
        if(State.Experiment.IsB&&t.Definition==1&&t.GrowthRemaining<=0)return;
        using var impact=new ImpactScope(this,p,t);
        if(t.Special>0){if(!secondary)ExperimentTool(p,t);UseSpecial(p,t,secondary);return;}
        var def=Tools.Get(t.Definition);
        if(!DemoActive(p)&&!(p.Customer&&State.Twist.Kind==TwistKind.CustomerHelper))AffectPartyPlayers(p,t,secondary);
        if(!DemoActive(p)&&AffectMolds(p,t))return;
        if(t.Definition is 0 or 1){ExperimentTool(p,t);t.LastUse=State.Time;t.Contact=Eye(p)+Aim(p)*3;t.HitHair=false;t.ContactHead=-1;t.EffectMass=0;UseSculpt(p,t,secondary);return;}
        if(secondary && def.Id is not ("vacuum" or "blower" or "trimmer")) return;
        ExperimentTool(p,t);
        p.Cooldown=def.Cooldown; if(t.Charge>0) t.Charge--;
        if(!DemoActive(p))ResolveWorldEffects(p,t);
        var origin=ToolEye(State,p); var dir=Aim(p);
        t.LastUse=State.Time;t.Contact=origin+dir*Math.Min(def.Range,3);t.HitHair=false;t.ContactHead=-1;t.EffectMass=0;
        // Nearby active airflow bends spray/flame cones, irrespective of ownership.
        if(def.Id is "growth" or "flame" or "water")
        foreach(var other in State.Players.Where(q=>q.Active && !q.Bracing && q.Id!=p.Id ))
            if(State.Tools.Any(x=>x.Id==other.Held && x.Definition==3) && Inputs.TryGetValue(other.Id,out var oi) && oi.Buttons.HasFlag(Buttons.Primary) && Vector3.Distance(other.Position,p.Position)<5)
                {dir=Vector3.Normalize(dir+Aim(other)*.8f);Discover(Combo.WindSpray);}
        float range=ObstructionDistance?.Invoke(origin,dir,def.Range) ?? def.Range;
        if(def.Id=="vacuum"&&!secondary)
        {
            if(p.Reservoir>=19.999f){State.Notice="Reservoir full. Transfer hair with RMB before collecting more.";return;}
            var material=new DebrisMaterial();float taken=DebrisSystem.Vacuum(State.Debris,origin,dir,range,def.Radius,Math.Min(20-p.Reservoir,1.2f),material,State.Experiment.IsB?.6f:1);
            if(taken>0){RecordAction(PartyAction.Recycle,p.Id);float ratio=taken/(p.Reservoir+taken);p.StoredHeat+=(material.Heat-p.StoredHeat)*ratio;p.StoredGlue+=(material.Glue-p.StoredGlue)*ratio;p.StoredChar+=(material.Char-p.StoredChar)*ratio;p.StoredYoung+=(material.Young-p.StoredYoung)*ratio;p.StoredWet+=(material.Wet-p.StoredWet)*ratio;p.Reservoir+=taken;}
        }
        if(def.Id=="blower")DebrisSystem.Blow(State.Debris,origin,dir,range,def.Radius);
        var hits=ToolQuery.EffectHits(State,p,t.Definition,secondary,dir,range);
        if(hits.Count>0){var nearest=hits.MinBy(x=>x.distance);t.ContactHead=nearest.head.Id;t.Contact=nearest.head.ToWorld(nearest.point);t.HitHair=true;t.ContactState=HairMaterials.State(nearest.patch);}
        int affected=0;
        foreach(var hit in hits)
        {
            var h=hit.head;var patch=hit.patch;var edit=new VolumeEditResult();
            bool wasBurning=h.Patches.Any(q=>q.Burning),wasFrozen=h.Patches.Any(q=>q.Frozen),wasGlue=h.Patches.Any(q=>q.Glue>.6f);
            h.RestTime=0;
            var region=h.Patches.Where(q=>HairSystem.DistanceToSegment(hit.point,q.Root,q.Root+q.Direction*q.Length)<def.Radius+.25f).ToArray();
            if(region.Length==0)region=[patch];
            bool wetBefore=region.All(q=>q.Wet>.15f)&&region.Any(q=>q.Length>.02f);
            if(State.Experiment.IsB&&def.Id=="nail"&&region.Any(q=>q.Frozen)){float removed=h.Volume.Brush(new(EffectKind.RemoveHair,.22f),hit.point,.3f);if(removed<0){edit.Removed-=removed;Discover(Combo.FrozenImpact);}}
            if(def.Id=="vacuum" && secondary)
            {
                if(p.Reservoir<=0)continue;
                float before=h.Mass;
                float added=h.Volume.Brush(new(EffectKind.Transfer,.16f,dir),hit.point,.34f/h.GeometryScale,budget:p.Reservoir/(h.GeometryScale*h.GeometryScale*h.GeometryScale))*h.GeometryScale*h.GeometryScale*h.GeometryScale;
                p.Reservoir=Math.Max(0,p.Reservoir-added);
                if(added>0)foreach(var q in region){q.Temperature=p.StoredHeat;q.Glue=p.StoredGlue;q.Char=p.StoredChar;q.Young=State.Experiment.IsB?1:p.StoredYoung;q.Wet=p.StoredWet;if(q.Temperature>90&&!q.Burning){q.Burning=true;q.BurnSource=p.Id;}}
                affected++;continue;
            }
            if(def.Id=="vacuum"&&p.Reservoir>=20)continue;
            if(def.Id=="vacuum")h.Volume.Brush(new(EffectKind.ApplyForce,.055f*(1-patch.Resistance),h.LocalDirection(-dir)),hit.point,def.Radius/h.GeometryScale);
            foreach(var effect in def.Effects)
            {
                var vector=h.LocalDirection(effect.Kind==EffectKind.CutPlane?(secondary?-dir:Vector3.UnitY):dir);
                var point=h.ToWorld(hit.point);
                var brushEffect=effect with{Direction=vector,Point=point,Source=p.Id};
                if(effect.Kind==EffectKind.ApplyForce)
                {
                    brushEffect=brushEffect with{Amount=effect.Amount*(1-region.Average(q=>q.Resistance))*Math.Clamp(1-hit.distance/def.Range*.7f,.2f,1)};
                    if(def.Id=="blower"&&secondary)brushEffect=brushEffect with{Direction=h.LocalDirection(-Vector3.UnitY),Amount=brushEffect.Amount*.4f};
                    if((effect.Amount>1.5f&&region.Any(q=>q.Frozen))||(effect.Amount>.25f&&region.Any(q=>q.Char>.6f)))brushEffect=new(EffectKind.RemoveHair,.25f,dir,Source:p.Id);
                    else if(def.Id=="sniper")brushEffect=brushEffect with{Amount=0};
                }
                float delta=h.Volume.Brush(brushEffect with{Amount=brushEffect.Amount/h.GeometryScale},hit.point,Math.Max(.16f,ToolQuery.EffectRadius(t.Definition,secondary))/h.GeometryScale,h.ToLocal(origin),range/h.GeometryScale,def.Id=="vacuum"?(20-p.Reservoir)/(h.GeometryScale*h.GeometryScale*h.GeometryScale):float.PositiveInfinity)*h.GeometryScale*h.GeometryScale*h.GeometryScale;
                foreach(var q in region)HairSystem.Apply(h,q,effect with{Direction=brushEffect.Direction,Point=point,Source=p.Id},false);
                if(State.Experiment.IsB&&effect.Kind==EffectKind.ChangeTemperature&&effect.Amount>0&&region.Any(q=>q.Glue>0&&q.Temperature>65)){foreach(var q in region.Where(q=>q.Temperature>65))q.Glue=Math.Max(0,q.Glue-.15f);Discover(Combo.HeatGlue);}
                t.EffectMass+=Math.Abs(delta);if(delta>0)p.Added+=delta;else p.Removed-=delta;
                if(delta<0)
                {
                    edit.Removed-=delta;
                    if(def.Id=="vacuum")
                    {edit.Absorbed-=delta;float blend=-delta/(p.Reservoir-delta);p.StoredHeat+=(patch.Temperature-p.StoredHeat)*blend;p.StoredGlue+=(patch.Glue-p.StoredGlue)*blend;p.StoredChar+=(patch.Char-p.StoredChar)*blend;p.StoredYoung+=(patch.Young-p.StoredYoung)*blend;p.StoredWet+=(patch.Wet-p.StoredWet)*blend;p.Reservoir=Math.Min(20,p.Reservoir-delta);}
                }
                else edit.Added+=delta;
            }
            if(def.Id=="glue")
            {
                // Bond a touching loose shell in place; glue cannot create a remote support.
                foreach(var piece in State.Heads.Where(x=>x.Loose&&!x.Miniature&&x.AttachedTo<0).ToArray())
                {
                    var target=State.SharedHead;if(target.Locked)continue;
                    var contact=piece.ToLocal(h.ToWorld(hit.point));
                    if(Vector3.Distance(h.ToWorld(hit.point),target.Position)<3&&piece.Volume.Sample(contact)>-.16f&&target.Volume.Sample(target.ToLocal(h.ToWorld(hit.point)))>-.16f)
                    {piece.AttachedTo=target.Id;piece.AttachedOffset=target.ToLocal(piece.Position);piece.AttachedRotation=piece.Rotation-target.Rotation;piece.Bonded=true;foreach(var q in piece.Patches)q.Glue=Math.Max(q.Glue,.65f);}
                }
            }
            if(edit.VisibleRemoved>0)DebrisSystem.Emit(State.Debris,h.ToWorld(hit.point),dir*.4f,edit.VisibleRemoved,DebrisMaterial.From(h,hit.point));
            if(State.Experiment.IsB&&!wasBurning&&h.Barber&&h.Patches.Any(q=>q.Burning)&&State.Player(h.Owner) is {} burningPlayer)StartFriendFire(burningPlayer,p.Id);
            if((h.Id==0||h.AttachedTo==0)&&State.Experiment.IsB){
                if(def.Id=="flame"&&wetBefore&&region.All(q=>!q.Burning))Discover(Combo.WetFire);
                if(!wasBurning&&h.Patches.Any(q=>q.Burning))RecordAction(PartyAction.Ignite,p.Id,h.Id);
                if(wasBurning&&!h.Patches.Any(q=>q.Burning))RecordAction(PartyAction.Extinguish,p.Id,h.Id);
                if(!wasFrozen&&h.Patches.Any(q=>q.Frozen))RecordAction(PartyAction.Freeze,p.Id,h.Id);
                if(!wasGlue&&h.Patches.Any(q=>q.Glue>.6f))RecordAction(PartyAction.Glue,p.Id,h.Id);
            }
            FinalizeHair(h,p.Id,edit);
            affected++;
        }
        if(def.Id=="vacuum" && !secondary)
        foreach(var wig in hits.Select(x=>x.head).Distinct().Where(h=>h.Loose && !h.Miniature && h.Patches.Average(p=>p.Glue)<.5f))
        {wig.AttachedTo=-1;wig.Velocity=-dir*3+Vector3.UnitY;}
        if(def.Id=="blower")
        {
            foreach(var h in hits.Select(x=>x.head).Distinct().Where(h=>h.Loose&&!h.Miniature))
            {
                if(h.Patches.Average(p=>p.Glue)<.5f) { h.AttachedTo=-1; h.Velocity=dir*4+Vector3.UnitY*1.5f; }
            }
            foreach(var hot in hits.Where(x=>x.patch.Burning))
            foreach(var head in State.Heads.Where(h=>!h.Locked && h.Id!=hot.head.Id))
                if(Vector3.Distance(head.Position,hot.head.Position)<2.5f && Vector3.Dot(head.Position-hot.head.Position,dir)>0)
                    HairSystem.Apply(head,head.Patches[0],new(EffectKind.Ignite,1,Source:p.Id));
        }
        foreach(var h in hits.Select(x=>x.head).Distinct().ToArray())FinalizeHair(h,p.Id);
        if(def.Id is "sniper" or "flame" or "trimmer")
        foreach(var customer in State.Customers)
        {
            var head=State.SharedHead; if(head.Locked || customer.Recovery>0) continue;
            if(HairSystem.DistanceToSegment(head.Position-new Vector3(0,.3f,0),origin,origin+dir*range)<(def.Id=="sniper"?.35f:def.Radius*.6f)) Incident(p.Id,customer.Slot,def.Id=="sniper"?25:12);
        }
        int targets=hits.Select(h=>h.head.Facial?h.head.ParentHead:h.head.Id).Distinct().Count();
        if(affected>0) Event(p.Id,targets,origin+dir*Math.Min(3,range),$"{p.Name}: {def.Name} → {targets} head(s)",2+targets*3+(State.Remaining<10?8:0));
    }
    public void Incident(int source,int slot,int penalty)
    {
        var c=State.Customers[0];if(c.Recovery>0||State.Phase!=Phase.Build)return;
        c.Recovery=3;c.LastAttacker=source;State.Job.Penalty+=penalty;
        if(State.Player(source) is {} actor)actor.Incidents++;
        Stimulate(.55f,source,CustomerStimulus.Impact);
        RecordIncident(source,penalty,State.SharedHead.Position);
        Event(source,1,State.SharedHead.Position,$"Customer accident: shop -{penalty}",35);
    }
    public Head SpawnWig(Vector3 position,int style)
    {
        var h=Head.Create(State.NextHead++,0); h.Loose=true; h.Position=position;
        for(int i=0;i<h.Patches.Count;i++)
        {
            var p=h.Patches[i]; p.Wig=true;
            p.Length=style switch {0=>.65f,1=>.9f-p.Root.Y,2=>1.3f,_=>Math.Abs(p.Root.X)>.25f?1.1f:.25f};
            if(style==0) p.Direction=Vector3.Normalize(new(p.Root.X, .6f,p.Root.Z));
        }
        h.Volume.Fill(v=>style switch{
            0=>HairVolume.Ellipsoid(v,new(0,.3f,0),new(.72f,.65f,.65f)),
            1=>Math.Min(.65f-Math.Abs(v.X),Math.Min(.48f-Math.Abs(v.Y-.4f),.5f-Math.Abs(v.Z))),
            2=>HairVolume.Ellipsoid(v,new(0,.5f,0),new(.6f,1,.5f)),
            _=>HairVolume.Ellipsoid(v,new(0,.25f,0),new(.85f,.6f,.45f))});
        State.Heads.Add(h); return h;
    }
    void TickLoose(float dt)
    {
        DebrisSystem.Tick(State.Debris,dt,DebrisCast,TwistCards.Gravity(State));
        foreach(var h in State.Heads.Where(h=>h.Loose&&!h.Miniature).ToArray())
        {
            if(h.Holder!=0&&State.Player(h.Holder) is {} carrier){h.Position=carrier.Customer&&!carrier.Standing?State.SharedHead.Position+Vector3.UnitY*.65f:Eye(carrier)+Aim(carrier)*.8f;h.Velocity=default;continue;}
            if(h.AttachedTo>=0)
            { var target=State.Heads.FirstOrDefault(x=>x.Id==h.AttachedTo); if(target!=null) { if(h.Bonded&&h.Patches.Average(p=>p.Glue)<.2f){h.AttachedTo=-1;h.Bonded=false;continue;}h.Position=target.ToWorld(h.AttachedOffset);h.Rotation=target.Rotation+h.AttachedRotation; h.Locked=target.Locked; } continue; }
            if(h.Patches.Any(p=>p.Anchored))continue;
            // Contact the visible shell, not its invisible negative-density band.
            if(FlightContact(h.Position,h.Position+h.Velocity*dt,h.Thrower,h.ThrownAt,false,p=>{h.Holder=p.Id;h.Velocity=default;p.CarriedHair=h.Id;},()=>h.Velocity=default))continue;
            var bottom=h.Position+h.LowestSurfaceOffset();
            h.Velocity-=Vector3.UnitY*dt*5*TwistCards.Gravity(State);var motion=h.Velocity*dt;var hit=DebrisCast?.Invoke(bottom+Vector3.UnitY*.03f,bottom+motion-Vector3.UnitY*.03f);
            if(!hit.HasValue&&bottom.Y+motion.Y<.015f)hit=new Vector3(bottom.X+motion.X,.015f,bottom.Z+motion.Z);
            if(hit.HasValue){h.Position+=hit.Value-bottom;h.Velocity=new(h.Velocity.X*.82f,0,h.Velocity.Z*.82f);h.RestTime+=dt;}
            else {h.Position+=motion;h.RestTime=0;}
            h.Position=new(Math.Clamp(h.Position.X,-5.7f,5.7f),h.Position.Y,Math.Clamp(h.Position.Z,-5.7f,5.7f));
            if(h.Fragment&&h.RestTime>=15){DepositFragment(h);State.Heads.Remove(h);topologyRevision.Remove(h.Id);topologyVolumes.Remove(h.Id);}
        }
    }
    public void Event(int source,int targets,Vector3 position,string text,float value)
    {
        State.Events.Add(new(){Id=State.NextEvent++,Time=State.Time,Source=source,Targets=targets,Position=position,Text=text,Value=value});
        if(State.Events.Count>80) State.Events.RemoveAt(0);
        State.Notice=text;
    }
}
