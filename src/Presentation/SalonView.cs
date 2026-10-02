using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hairball;
public partial class SalonView : Node3D
{
    public readonly Dictionary<int,Body> Bodies=new();
    readonly Dictionary<int,HeadView> heads=new();
    readonly Dictionary<int,Node3D> customers=new(),tools=new();
    readonly Dictionary<string,Node3D> props=new();
    readonly GoalSection goalSection=new();
    readonly TargetReference reference=new();
    readonly System.Collections.Generic.Dictionary<int,PracticeModelView> practiceModels=new();
    public readonly DebrisView Debris=new();
    public readonly BrushPreview Brush=new();
    public readonly ToolFeedback Feedback=new();
    readonly ContactFeedback contactFeedback=new();
    public readonly Dictionary<int,LaundryView> Laundry=new();
    public readonly Dictionary<int,LadderView> Ladders=new();
    readonly Dictionary<int,LadderView> replayLadders=new();
    readonly Dictionary<int,HeadView> mirrorFaces=new();
    readonly Dictionary<int,Label3D> customerLabels=new();
    readonly Dictionary<int,RigidBody3D> ragdolls=new();
    readonly HashSet<int> fallen=new();
    readonly List<Node3D> replayModels=new();
    public Camera3D Camera=null!,ReplayCamera=null!;
    Node3D heldRoot=null!,heldTool=null!;
    int lastHeld=-2,lastGoal=-1,lastRound=-1;
    HeadView mirrorHair=null!;int bodyRound=-1;string bodyMatch="";
    Node3D portraitRoot=null!;
    readonly Dictionary<int,HeadView> mirrorWigs=new();
    public SubViewport Mirror=null!;
    public override void _Ready()
    {
        Art.Shop(this);Art.Box(this,new(0,.75f,4.05f),new(2.8f,.15f,1.2f),new("8b735a"));Art.Collider(this,new(0,.75f,4.05f),new(2.8f,.15f,1.2f));Art.Label(this,L.T("JOB PROPS • E carry / place"),new(0,1.1f,4.7f),22);CreateOrderStation();AddChild(goalSection);AddChild(Debris);AddChild(Brush);AddChild(Feedback);AddChild(contactFeedback);
        var headBody=new AnimatableBody3D{Name="SharedHeadCollider",CollisionLayer=2,CollisionMask=0,SyncToPhysics=false};AddChild(headBody);headBody.AddChild(new CollisionShape3D{Shape=new SphereShape3D{Radius=.38f}});
        Camera=new(){Fov=73,Near=.045f};AddChild(Camera);Camera.Current=true;
        AddChild(reference);reference.Setup(Camera);
        ReplayCamera=new(){Fov=65};AddChild(ReplayCamera);
        heldRoot=new(){Position=new(.36f,-.3f,-.58f),RotationDegrees=new(-8,-7,0)};Camera.AddChild(heldRoot);
        if(VisualQuality.Puppet){
            PuppetLabModels.Hand(heldRoot,new(.06f,-.14f,.13f),1,1.05f);
            TargetLabGeometry.Limb(heldRoot,new(.08f,-.24f,.22f),new(.18f,-.52f,.55f),.10f,PuppetLabModels.Cream);PuppetActor.Finish(heldRoot);
        }else{Art.Ball(heldRoot,new(.08f,-.2f,.16f),new(.16f,.13f,.2f),new("eed8af"));Art.Box(heldRoot,new(.1f,-.25f,.32f),new(.15f,.16f,.3f),new("4e6773"));}
        // A dedicated portrait camera is the low-cost shop mirror/readback permitted by the control spec.
        Mirror=new(){Size=new(280,220),OwnWorld3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Always,TransparentBg=false};AddChild(Mirror);
        if(VisualQuality.Puppet)Mirror.Msaa3D=Viewport.Msaa.Msaa4X;
        var portrait=new Node3D();portraitRoot=portrait;Mirror.AddChild(portrait);Art.Person(portrait,new("577c8d"));
        mirrorHair=new(){Position=new(0,1.7f,0)};portrait.AddChild(mirrorHair);
        var cam=new Camera3D{Position=new(0,2.05f,3.3f),Fov=47,Current=true};portrait.AddChild(cam);cam.LookAt(new(0,1.9f,0));
        portrait.AddChild(new DirectionalLight3D{RotationDegrees=new(-35,-25,0),LightEnergy=1.4f});
        portrait.AddChild(new WorldEnvironment{Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new("243c49"),AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=Colors.White,AmbientLightEnergy=.65f}});
        for(int i=0;i<1;i++)
        {
            var screen=Art.Box(this,Art.V(Session.WorkCenter)+new Vector3(0,2.2f,-4.5f),new(1.5f,1.1f,.05f),Colors.White);
            screen.MaterialOverride=new StandardMaterial3D{AlbedoTexture=Mirror.GetTexture(),ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded};
            Art.Label(this,"YOUR HAIR • LIVE PORTRAIT",screen.Position+new Vector3(0,.7f,0),22).Visible=false;
        }
    }
    public void Sync(WorldState w,int localId,float dt,ReplayFrame? replay=null)
    {
        GetNode<SceneLook>("SceneLook").Sync(w,localId);
        SyncOrders(w,replay);contactFeedback.Sync(w,replay?.Time??w.Time,replay?.Tools??w.Tools.Concat(ToolFeedback.CoopFeedback(w)));
        Debris.Sync(replay?.Debris??w.Debris,replay?.Time??w.Time,dt,replay!=null);
        var local=w.Player(localId);
        if(w.Customers.Count>0){GetNode<AnimatableBody3D>("SharedHeadCollider").CollisionLayer=w.Experiment.IsB&&(w.Player(w.Experiment.CustomerActor)?.Standing==true||w.Experiment.Leave is not (LeaveStage.Seated or LeaveStage.Rising))?0u:2u;GetNode<Node3D>("SharedHeadCollider").Position=Art.V(w.SharedHead.Position);GetNode<Node3D>("SharedHeadCollider").Rotation=Art.V(w.SharedHead.Rotation);}
        // Historical presentation has separate nodes; live collision never follows replay transforms.
        foreach(var ladder in w.Ladders){if(!Ladders.TryGetValue(ladder.Id,out var ladderView)){ladderView=new();AddChild(ladderView);Ladders[ladder.Id]=ladderView;}ladderView.Sync(ladder,true);ladderView.Visible=replay==null;}
        foreach(var historical in replayLadders.Values)historical.Visible=false;
        if(replay!=null)foreach(var ladder in replay.Ladders){if(!replayLadders.TryGetValue(ladder.Id,out var ladderView)){ladderView=new();AddChild(ladderView);replayLadders[ladder.Id]=ladderView;}ladderView.Sync(ladder,false);ladderView.Visible=true;}
        foreach(var c in w.Customers)GetNode<Node3D>($"ChairCollider_{c.Slot}").Position=Art.V(Session.WorkCenter)+Vector3.Up*(.85f+c.ChairHeight);
        foreach(var c in replay?.Customers??w.Customers)GetNode<Node3D>($"ChairLift_{c.Slot}").Position=Art.V(Session.WorkCenter)+Vector3.Up*c.ChairHeight;
        foreach(var p in w.Players.Where(p=>p.Active))
        {
            if(!Bodies.TryGetValue(p.Id,out var body))
            {body=new(){Name=$"Barber_{p.Id}",Position=Art.V(p.Position)};AddChild(body);body.Setup(p.Id,p.Slot);Bodies[p.Id]=body;}
        }
        if(bodyRound!=w.Round||bodyMatch!=w.MatchTag){bodyRound=w.Round;bodyMatch=w.MatchTag;foreach(var view in heads.Values)view.ResetPuppet();mirrorHair.ResetPuppet();customerMirrorHair?.ResetPuppet();foreach(var p in w.Players.Where(p=>p.Active))if(Bodies.TryGetValue(p.Id,out var body)){body.Position=Art.V(p.Position);body.Velocity=Vector3.Zero;}}
        if(w.Customers.Count>0)
        {
            if(!customers.ContainsKey(0)){var created=Art.Person(this,Art.Team[0],true);Art.Layers(created,1u<<19);created.Name="SharedCustomer";customers[0]=created;}
            var cs=(replay?.Customers??w.Customers)[0];
            var shownHead=replay?.Heads.FirstOrDefault(h=>h.Id==0)??w.SharedHead;
            var model=customers[0];model.Rotation=Art.V(shownHead.Rotation+TwistCards.DisplayDelta(w,localId));
            CustomerAppearance(model,cs.Profile,CustomerMotion.Anticipating(cs,replay?.Time??w.Time));
            model.Position=Art.V(shownHead.Position)-model.Basis*new Vector3(0,1.64f,0);
            LaundryView.Expression(model,replay?.Time??w.Time,cs.Panic,true);
            bool anticipating=CustomerMotion.Anticipating(cs,replay?.Time??w.Time);
            foreach(int side in new[]{-1,1}){model.GetNode<Node3D>($"Lid_{side}").Visible=!anticipating;model.GetNode<Node3D>($"Eye_{side}").Scale=anticipating?new(.25f,.29f,.13f):new(.22f,.23f,.11f);}
            if(w.Experiment.IsB)SyncPartyBody(w,model);
            if(w.Job.Goal==6&&!Laundry.ContainsKey(0)){var rig=new LaundryView();AddChild(rig);Laundry[0]=rig;}
            if(Laundry.TryGetValue(0,out var laundry))laundry.Sync(shownHead,replay?.Time??w.Time,LaundryRig.Wind(w,shownHead),false&&w.Job.Goal==6&&w.Phase is Phase.Choice or Phase.Preview or Phase.Build or Phase.Highlight);
        }
        foreach(var player in w.Players.Where(p=>p.Active))if(Bodies.TryGetValue(player.Id,out var networkBody)){networkBody.Visible=!player.NetworkAway;networkBody.CollisionLayer=player.NetworkAway?0u:4u;}
        foreach(int id in Bodies.Keys.Where(id=>w.Player(id)==null).ToArray()){Bodies[id].QueueFree();Bodies.Remove(id);}
        var displayHeads=replay?.Heads??(w.Phase==Phase.Validation?w.Heads.Where(h=>h.Barber||h.Miniature).Concat(w.ValidationHeads.Where(h=>!h.Miniature)).ToList():w.Heads);
        foreach(var h in displayHeads.Where(h=>h.Miniature))
        {
            if(!practiceModels.TryGetValue(h.Id,out var doll)){doll=new();AddChild(doll);practiceModels[h.Id]=doll;}
            doll.Sync(h,replay==null);
        }
        foreach(int id in practiceModels.Keys.Where(id=>!displayHeads.Any(h=>h.Id==id&&h.Miniature)).ToArray()){var old=practiceModels[id];RemoveChild(old);old.QueueFree();practiceModels.Remove(id);}
        foreach(var h in displayHeads)
        {
            if(!heads.TryGetValue(h.Id,out var view)){view=new(){Name=$"Hair_{h.Id}"};AddChild(view);heads[h.Id]=view;}
            view.Visible=!h.Barber||w.Player(h.Owner) is {Customer:false};
            uint layer=h.Barber?1u<<((w.Player(h.Owner)?.Slot??0)+1):h.Id==0||h.ParentHead==0||h.AttachedTo==0?1u<<19:1u;
            view.Observer=local==null?h.Position:Session.Eye(local);view.Update(h,w.Time,layer,editing:h.Id==Brush.ActiveHeadId||(local!=null&&h.Owner==localId&&local.Held>=0));
            if(replay==null)foreach(var blower in w.Players.Where(p=>p.Active&&p.UsingBlower&&Session.LookingAt(p,h.Position+System.Numerics.Vector3.UnitY*.5f,1.2f,6)))view.VisualImpulse(Art.V(Session.Aim(blower))*Math.Min(dt,.05f)*1.4f);
            if(replay==null){foreach(var hit in w.Tools.Where(t=>t.HitHair&&w.Time-t.LastUse<.12f))if(Math.Abs(h.Volume.Sample(h.ToLocal(hit.Contact)))<.2f)view.Touch(hit.Contact,hit.LastUse);if(h.Id==0&&w.Props.FirstOrDefault(p=>p.Goal==0&&p.Attached) is {} loadProp)view.Touch(loadProp.Position,w.Time,.25f);}
            if(!h.Barber&&!h.Loose&&customers.TryGetValue((h.Facial?h.ParentHead:h.Id)/2,out var customerModel))
            {view.Position=customerModel.ToGlobal(new Vector3(0,1.64f,0)+(h.Facial?Art.V(Head.FaceOffset(h.Region)):Vector3.Zero));view.Rotation=customerModel.Rotation;}
            if(h.Barber && replay==null && Bodies.TryGetValue(h.Owner,out var body))view.Position=body.Position+Art.V(h.Position-w.Player(h.Owner)!.Position);
        }
        foreach(int id in heads.Keys.Where(id=>!displayHeads.Any(h=>h.Id==id)).ToArray()){heads[id].QueueFree();heads.Remove(id);}
        foreach(var t in replay?.Tools??w.Tools)
        {
            if(!tools.TryGetValue(t.Id,out var view))
            {view=Art.Tool(this,t.Definition);tools[t.Id]=view;Art.Label(view,SpecialOrders.ToolName(t),new(0,.32f,0),19,Art.ToolColors[t.Definition]).Name="ToolHint";if(t.Special>0)Art.Box(view,new(0,.16f,0),new(.3f,.08f,.45f),new("ffe9a7"));}
            UpdateBottle(view,t);
            view.GetNode<Label3D>("ToolHint").Visible=local!=null&&t.Holder==0&&Session.LookingAt(local,t.Position,.4f,3);
            view.Visible=replay!=null||t.Holder!=localId;
            if(replay!=null){view.Position=Art.V(t.Position);continue;}
            if(t.Holder==0){view.Position=Art.V(t.Position);view.Rotation=new(0,(t.Position.X<0?Mathf.Pi/2:-Mathf.Pi/2),0);}
            else if(w.Player(t.Holder) is {} holder){view.Position=Art.V(holder.Position)+new Vector3(.35f,1.1f,-.4f).Rotated(Vector3.Up,holder.Yaw);view.Rotation=new(holder.Pitch,holder.Yaw,0);}
        }
        foreach(int id in tools.Keys.Where(id=>!(replay?.Tools??w.Tools).Any(t=>t.Id==id)).ToArray()){tools[id].QueueFree();tools.Remove(id);}
        foreach(var body in Bodies.Values)
        {
            var propBoard=body.Model.GetNodeOrNull<Node3D>("RemoteReference");
            if(propBoard==null){propBoard=Art.Box(body.Model,new(.38f,1.12f,.42f),new(.45f,.3f,.025f),new("d6c6a4"));propBoard.Name="RemoteReference";}
            propBoard.Visible=body.Peer!=localId&&(replay?.ReferencePlayers.Contains(body.Peer)??w.Player(body.Peer)?.ReferenceUp??false);
        }
        foreach(var prop in replay?.Props??w.Props)
        {
            string key=$"{w.Round}:{prop.Id}";
            if(!props.TryGetValue(key,out var view)){view=MakeProp(prop.Goal);props[key]=view;}
            SyncMoldView(view,prop);SyncCoopView(view,prop,w);SyncGestureProp(view,prop);view.Position=Art.V(prop.Position);
            if(replay==null&&w.Experiment.IsB)view.Position-=Vector3.Up*MaterialFeel.Sink(w,prop);
            view.Rotation=Art.V(prop.Rotation)+(prop.Failed?new Vector3(.45f,0,.7f):Vector3.Zero);
            if(prop.Goal==0)SpinRotor(view,w.Time,prop.Failed?2:24);
        }
        foreach(string key in props.Keys.Where(key=>!(replay?.Props??w.Props).Any(p=>$"{w.Round}:{p.Id}"==key)).ToArray()){props[key].QueueFree();props.Remove(key);}
        if(local==null)return;
        SyncSharedPresentation(w,replay);
        GetNode<Label3D>("ChairHint_0").Visible=Session.LookingAt(local,Session.ChairControl(0),.6f,3);
        mirrorHair.Update(local.Customer?w.SharedHead:w.Barber(local.Slot),w.Time,1,true);
        foreach(var face in w.Heads.Where(h=>h.ParentHead==(local.Customer?0:local.Slot*2+1)))
        {if(!mirrorFaces.TryGetValue(face.Id,out var faceView)){faceView=new();portraitRoot.AddChild(faceView);mirrorFaces[face.Id]=faceView;}faceView.Position=new Vector3(0,1.7f,0)+Art.V(Head.FaceOffset(face.Region));faceView.Update(face,w.Time,1,true);}
        var ownWigs=w.Heads.Where(h=>h.AttachedTo==(local.Customer?0:local.Slot*2+1)).ToArray();
        foreach(var wig in ownWigs)
        {if(!mirrorWigs.TryGetValue(wig.Id,out var mirrorWig)){mirrorWig=new(){Position=new(0,2.1f,0)};portraitRoot.AddChild(mirrorWig);mirrorWigs[wig.Id]=mirrorWig;}mirrorWig.Update(wig,w.Time,1,true);}
        foreach(int id in mirrorWigs.Keys.Where(id=>!ownWigs.Any(h=>h.Id==id)).ToArray()){mirrorWigs[id].QueueFree();mirrorWigs.Remove(id);}
        Camera.CullMask=uint.MaxValue & ~(1u<<(local.Slot+1)) & (local.Customer?~(1u<<19):uint.MaxValue);
        int held=w.Tools.FirstOrDefault(t=>t.Id==local.Held)?.Definition??-1;
        if(held!=lastHeld)
        {if(heldTool!=null && IsInstanceValid(heldTool))heldTool.QueueFree();heldTool=held<0?null!:Art.Tool(heldRoot,held);lastHeld=held;}
        var heldState=w.Tools.FirstOrDefault(t=>t.Id==local.Held);float working=heldState!=null&&w.Time-heldState.LastUse<.2f?1:0;
        if(heldTool!=null&&heldState!=null)UpdateBottle(heldTool,heldState);
        heldRoot.Position=new(.40f,-.30f+MathF.Sin(w.Time*(working>0?23:2))*(working>0?.007f:.004f),-.9f);heldRoot.Scale=Vector3.One*.65f;
        heldRoot.Visible=held>=0 && w.Phase!=Phase.Highlight;
        var nozzle=heldTool?.GetNodeOrNull<Node3D>("Muzzle");Feedback.Sync(w,localId,nozzle?.GlobalPosition??Camera.GlobalPosition,replay?.Time??w.Time,replay?.Tools);
        if(lastGoal!=w.Job.Goal || lastRound!=w.Round){RebuildGhost(local,w);lastGoal=w.Job.Goal;lastRound=w.Round;}
        reference.Sync(w,local,replay!=null);
        goalSection.Visible=!w.Experiment.IsB&&Input.IsKeyPressed(Key.H)&&w.Phase is Phase.Choice or Phase.Preview or Phase.Build;
        SyncExperiment(w,localId,replay);SyncGallery(w);SyncDiscoveries(w);SyncBodyComedy(w,localId);SyncCustomerPresentation(w);SyncTwistPresentation(w,localId);SyncWorldPresentation(w,localId);
        if(goalSection.Visible){var targetHead=w.SharedHead;goalSection.UpdateView(Camera,new(Basis.FromEuler(Art.V(targetHead.Rotation)),Art.V(targetHead.Position)));}
        bool highlighting=w.Phase==Phase.Highlight && replay!=null;
        bool validating=w.Phase==Phase.Validation;
        ReplayCamera.Current=highlighting||validating;Camera.Current=!highlighting&&!validating;
        foreach(var body in Bodies.Values)body.Model.Visible=!highlighting;
        foreach(var model in replayModels)model.Visible=highlighting;
        if(highlighting)
        {
            while(replayModels.Count<replay!.Players.Count)replayModels.Add(Art.Person(this,Art.Team[replayModels.Count]));
            for(int i=0;i<replay.Players.Count;i++)replayModels[i].Position=Art.V(replay.Players[i]);
            var focus=Art.V(w.HighlightFocus);focus.Y=Mathf.Clamp(focus.Y,1,2.5f);
            ReplayCamera.Position=new(Mathf.Clamp(focus.X+3,-5.5f,5.5f),3.8f,Mathf.Clamp(focus.Z+4,-5.5f,5.5f));
            ReplayCamera.LookAt(focus);
        }
        else if(validating)
        {
            // Tools are locked: briefly spectate the customer's physical test, then return to FPS.
            var focus=Art.V(w.SharedHead.Position)+new Vector3(0,.65f,0);
            if(w.Job.Goal==0&&w.Props.FirstOrDefault(p=>p.Failed) is {} fallenProp)focus=focus.Lerp(Art.V(fallenProp.Position)+Vector3.Up*.4f,.3f);
            ReplayCamera.Position=focus+new Vector3(2.6f,1.6f,3.2f);
            ReplayCamera.LookAt(focus);
        }
        GetNode<SceneLook>("SceneLook").FinishHints(w,localId,dt);
    }
    static void UpdateBottle(Node3D view,ToolState t){if(t.Definition!=1)return;var fluid=view.GetNodeOrNull<Node3D>("BottleFluid");if(fluid==null)return;float fill=Math.Clamp(t.GrowthRemaining/GrowthSpray.BottleCapacity,0,1);fluid.Visible=fill>0;fluid.Scale=new(1,Math.Max(.001f,fill),1);fluid.Position=new(.278f,-.105f+.14f*fill,.12f);}
    public void PhysicsCustomers(WorldState w)
    {
        if(w.Customers.Count==0)return;
        CustomerMotion.Apply(w);
        foreach(var face in w.Heads.Where(h=>h.Facial&&!h.Barber)){face.Position=w.SharedHead.ToWorld(Head.FaceOffset(face.Region));face.Rotation=w.SharedHead.Rotation;}
        GetNode<Node3D>("ChairCollider_0").Position=new Vector3(0,.85f+w.Customers[0].ChairHeight,0);
    }
    public bool CanPlaceLadder(System.Numerics.Vector3 position,float yaw,int carrier)
    {
        var query=new PhysicsShapeQueryParameters3D{Shape=new BoxShape3D{Size=new(1.08f,1.3f,1.9f)},Transform=new Transform3D(new Basis(Vector3.Up,yaw),Art.V(position)+Vector3.Up*.76f),CollisionMask=23};
        if(Bodies.TryGetValue(carrier,out var body))query.Exclude=new Godot.Collections.Array<Rid>{body.GetRid()};
        return GetWorld3D().DirectSpaceState.IntersectShape(query,1).Count==0;
    }
    void RebuildGhost(PlayerState p,WorldState w)=>goalSection.SetGoal(w.Job.Goal);
    Node3D MakeProp(int goal)=>CreateProp(this,goal);
    public static Node3D CreateProp(Node3D parent,int goal)
    {
        var n=new Node3D();parent.AddChild(n);
        switch(goal)
        {
            case -1:
                Art.Box(n,Vector3.Zero,new(1.24f,.94f,.06f),new("f9dd9b"));
                Art.Ball(n,new(0,0,.04f),new(.45f,.45f,.02f),new("e7b37a"));
                Art.Label(n,"GOOD HAIR DAY",new(0,.28f,.045f),17,new("173a50"));break;
            case 0:
                Art.Ball(n,Vector3.Zero,new(.7f,.35f,.4f),new("e96551"));Art.Box(n,new(0,.02f,.16f),new(.4f,.2f,.06f),new("a7e7ed"));
                Art.Box(n,new(0,.3f,0),new(1.25f,.045f,.09f),new("344153")).Name="Rotor";Art.Box(n,new(0,0,-.45f),new(.09f,.1f,.5f),new("e96551"));
                for(int i=-1;i<=1;i+=2)Art.Box(n,new(i*.23f,-.24f,0),new(.04f,.04f,.65f),new("455561"));break;
            case 1:Art.Ball(n,Vector3.Zero,new(.2f,.28f,.2f),new("fff1c8"));break;
            case 2:Art.Cylinder(n,Vector3.Zero,.12f,.65f,new("f0e5c8"),.02f);Art.Cylinder(n,new(0,-.43f,0),.04f,.25f,new("f39840"),.13f);break;
            case 3:
                Art.Box(n,Vector3.Zero,new(.38f,.22f,.2f),new("e9b05f"));Art.Ball(n,new(.22f,.14f,0),Vector3.One*.24f,new("e9b05f"));
                for(int i=-1;i<=1;i+=2)Art.Cylinder(n,new(.2f,.28f,i*.07f),.065f,.15f,new("c98451"),0);break;
            case 4:Art.Cylinder(n,Vector3.Zero,.045f,.32f,new("f2deb4"));Art.Ball(n,new(0,.2f,0),new(.08f,.14f,.08f),new("ffbd55"));break;
            case 5:
                Art.Box(n,Vector3.Zero,new(.42f,.22f,.28f),new("68aebe"));
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Art.Ball(n,new(x*.15f,-.12f,z*.17f),Vector3.One*.13f,new("304554"));break;
            case 6:n.AddChild(LaundryView.Underwear(1));break;
            case 7:Art.Ball(n,Vector3.Zero,Vector3.One*.11f,new("70cbe8"));break;
        }
        return n;
    }
    public void Beam(Vector3 from,Vector3 to,Color color)
    {
        var d=to-from;if(d.Length()<.01f)return;
        var mesh=Art.Cylinder(this,(from+to)/2,.018f,d.Length(),color);
        mesh.Quaternion=new Quaternion(Vector3.Up,d.Normalized());
        var tween=CreateTween();tween.TweenProperty(mesh,"scale",new Vector3(.05f,1,.05f),.12);tween.TweenCallback(Callable.From(mesh.QueueFree));
    }
}
