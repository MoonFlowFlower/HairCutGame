using Hairball.Core;
using System.Numerics;

int passed=0;
void Test(string name,Action test) { try { test(); Console.WriteLine("PASS "+name); passed++; } catch(Exception e) { Console.Error.WriteLine("FAIL "+name+": "+e); Environment.Exit(1); } }
void Check(bool value,string message="assertion failed") { if(!value) throw new Exception(message); }
void Near(float a,float b,float eps=.001f) => Check(Math.Abs(a-b)<eps,$"{a} != {b}");
Session SessionWithPlayer() { var s=new Session(); s.AddPlayer(1,"Tester"); s.StartMatch();s.State.Phase=Phase.Build;s.State.Remaining=75; return s; }
Test("32 scalp patches",()=>Check(Head.Create(0,1).Patches.Count==32));
Test("growth/removal clamp finite",()=>{var h=Head.Create(0,1);var p=h.Patches[0]; HairSystem.Apply(h,p,new(EffectKind.AddHair,999));Near(p.Length,3);HairSystem.Apply(h,p,new(EffectKind.RemoveHair,999));Near(p.Length,0);HairSystem.Apply(h,p,new(EffectKind.AddHair,float.NaN));Near(p.Length,0);});
Test("freeze/glue compose and resist wind",()=>{var h=Head.Create(0,1);var p=h.Patches[0];HairSystem.Apply(h,p,new(EffectKind.Glue,.8f));HairSystem.Apply(h,p,new(EffectKind.ChangeTemperature,-100));Check(p.Frozen && p.Glue>.7f && p.Resistance>.9f);var before=p.Direction;HairSystem.Apply(h,p,new(EffectKind.ApplyForce,.2f,Vector3.UnitX));Check(Vector3.Distance(before,p.Direction)<.02f);});
Test("frozen impact fractures; heat thaws",()=>{var h=Head.Create(0,1);var p=h.Patches[0];HairSystem.Apply(h,p,new(EffectKind.ChangeTemperature,-100));float before=p.Length;HairSystem.Apply(h,p,new(EffectKind.ApplyForce,2,Vector3.UnitX));Check(p.Length<before*.5f);HairSystem.Apply(h,p,new(EffectKind.ChangeTemperature,200));Check(!p.Frozen && p.Burning);});
Test("burn removes hair and spreads; cold extinguishes",()=>{var h=Head.Create(0,1);var p=h.Patches[0];float before=h.Mass;HairSystem.Apply(h,p,new(EffectKind.Ignite,1,Source:7));for(int i=0;i<300;i++)HairSystem.Tick(h,.02f);Check(h.Mass<before);Check(h.Patches.Count(p=>p.Char>0)>1);HairSystem.Apply(h,p,new(EffectKind.ChangeTemperature,-250));Check(!p.Burning);});
Test("helpful trim does not steal arson attribution",()=>{var h=Head.Create(0,1);var p=h.Patches[0];HairSystem.Apply(h,p,new(EffectKind.Ignite,1,Source:7));HairSystem.Apply(h,p,new(EffectKind.RemoveHair,.1f,Source:2));Check(p.Source==2&&p.BurnSource==7);});
Test("anchor follows world point and releases strain",()=>{var h=Head.Create(0,1);var p=h.Patches[0];HairSystem.Apply(h,p,new(EffectKind.Anchor,1,Point:new(0,1,0)));HairSystem.Tick(h,.1f);Near((p.Root+p.Direction*p.Length).Y,1);h.Position=new(8,0,0);HairSystem.Tick(h,.1f);Check(!p.Anchored);});
Test("locked head rejects state and burn",()=>{var h=Head.Create(0,1);h.Locked=true;var p=h.Patches[0];p.Burning=true;float before=p.Length;HairSystem.Apply(h,p,new(EffectKind.RemoveHair,1));HairSystem.Tick(h,5);Near(before,p.Length);});
Test("plane cut gives level surface",()=>{var h=Head.Create(0,1);var p=h.Patches[0];p.Direction=Vector3.UnitY;p.Length=2;HairSystem.Apply(h,p,new(EffectKind.CutPlane,1,Vector3.UnitY,Point:new(0,.65f,0)));Near((p.Root+p.Direction*p.Length).Y,.65f);});
Test("detachment releases anchors",()=>{var h=Head.Create(0,1);var p=h.Patches[0];p.Anchored=true;HairSystem.Apply(h,p,new(EffectKind.Detach,1));Check(p.Length==0 && !p.Anchored);});
Test("goal choices unique deterministic same band",()=>{var a=Goals.Choices(42,2,1);Check(a.Length==3 && a.Distinct().Count()==3);Check(a.SequenceEqual(Goals.Choices(42,2,1)));Check(a.Select(i=>Goals.All[i].Band).Distinct().Count()==1);});
Test("perfect, empty, tiny, excess scoring",()=>{foreach(var g in Goals.All){Check(Scoring.Shape(g.TargetSamples,g)>99);Near(Scoring.Shape([],g),0);Check(Scoring.Shape(g.TargetSamples.Take(1),g)<30);Check(Scoring.Shape(g.TargetSamples.Concat(Enumerable.Repeat(new Vector3(9,9,9),g.TargetSamples.Length*2)),g)<60);}});
Test("final clamp and separate penalty",()=>{Near(new Score{Shape=100,Function=100,Prop=100,Penalty=25}.Final,75);Near(new Score{Penalty=500}.Final,0);});
Test("incident charged to shared shop with source attribution, recovery automatic",()=>{var s=SessionWithPlayer();s.AddPlayer(2,"Victim");s.Tick(8.1f);s.Incident(1,1,25);Check(s.State.Job.Penalty==25&&s.State.Player(1)!.Incidents==1&&s.State.Player(2)!.Incidents==0);s.Tick(4.1f);Near(s.State.Customers[0].Recovery,0);});
Test("physical pickup distance and one-tool swap",()=>{var s=SessionWithPlayer();var p=s.State.Player(1)!;Check(!s.Pickup(p,0));p.Position=s.State.Tools[0].Position-new Vector3(0,1,0);Check(s.Pickup(p,0));p.Position=s.State.Tools[1].Position-new Vector3(0,1,0);Check(s.Pickup(p,1));Check(s.State.Tools[0].Holder==0 && s.State.Tools[1].Holder==1);});
Test("authority tool query growth, reservoir conservation",()=>{var s=SessionWithPlayer();s.Tick(8.1f);var p=s.State.Player(1)!;var t=s.State.Tools[1];t.Holder=1;p.Held=t.Id;float Material()=>s.State.Heads.Where(h=>!h.Barber&&!h.Loose).Sum(h=>h.Mass);float before=Material();s.UseTool(p,false);Check(Material()>before);Check(s.State.Events.Last().Targets==1,"facial regions should count as one customer in notices/highlights");t.Holder=0;t=s.State.Tools[2];t.Holder=1;p.Held=2;p.Cooldown=0;before=Material();s.UseTool(p,false);Near(Material()+p.Reservoir,before);Check(p.Reservoir>0);p.Cooldown=0;s.UseTool(p,true);Near(Material()+p.Reservoir,before);});
Test("all tool definitions route effects without nonfinite values",()=>{foreach(var tool in Tools.All){var h=Head.Create(0,1);foreach(var effect in tool.Effects)foreach(var p in h.Patches)HairSystem.Apply(h,p,effect with{Direction=Vector3.UnitY,Point=new(0,.4f,0)});Check(h.Patches.All(p=>float.IsFinite(p.Length)&&HairSystem.Finite(p.Direction)));}});
Test("eight validation proxy types run and reveal",()=>{for(int goal=0;goal<8;goal++){var s=SessionWithPlayer();s.State.Job.Goal=goal;PhysicalProps.Stage(s.State);Validation.Begin(s.State);for(int i=0;i<90;i++)Validation.Tick(s.State,.1f,i*.1f);Validation.Finish(s.State);Check(s.State.Props.Count>0);Check(s.State.Job.Result.Final>=0);Check(s.State.Props.All(p=>HairSystem.Finite(p.Position)));}});
Test("all eight validations have achievable successes and empty-head failures",()=>{
    for(int goal=0;goal<8;goal++){
        foreach(bool good in new[]{true,false}){
            var s=SessionWithPlayer();var p=s.State.Player(1)!;s.State.Job.Goal=goal;var h=s.State.SharedHead;
            if(good)GoalFixtures.Build(h,goal);else {h.Volume.Fill(_=>-1);foreach(var patch in h.Patches)patch.Length=0;}
            GoalFixtures.Place(s.State);Validation.Begin(s.State);for(int i=0;i<90;i++)Validation.Tick(s.State,.1f,i*.1f);Validation.Finish(s.State);
            Check(good?s.State.Job.Result.Function>=99:s.State.Job.Result.Function==0,$"goal {goal} good={good} function={s.State.Job.Result.Function}");
        }
    }
});
Test("validation deforms only a working copy",()=>{var s=SessionWithPlayer();s.State.Job.Goal=0;var h=s.State.SharedHead;h.Locked=true;var before=(byte[])h.Volume.Data.Clone();Validation.Begin(s.State);Validation.Tick(s.State,.5f,4);Check(h.Volume.Data.SequenceEqual(before));Check(!s.State.ValidationHeads[0].Volume.Data.SequenceEqual(before));});
Test("attached wig timeout protection includes simulation tick",()=>{var s=SessionWithPlayer();s.Tick(9);var wig=s.SpawnWig(new(0,2,0),0);wig.AttachedTo=0;wig.Patches[0].Burning=true;float before=wig.Mass;s.State.Remaining=0;s.Tick(.00001f);HairSystem.Tick(wig,3);Near(wig.Mass,before);Check(wig.Locked);});
Test("active attacker tool query cannot alter locked shared customer or barber",()=>{
    var s=SessionWithPlayer();var attacker=s.AddPlayer(2,"Attacker")!;s.Tick(9);s.State.Remaining=0;s.Tick(.00001f);
    attacker.Position=Session.WorkCenter+new Vector3(0,0,2);attacker.Yaw=0;attacker.Pitch=.1f;
    attacker.Held=1;s.State.Tools[1].Holder=2;float customer=s.State.SharedHead.Mass,barber=s.State.Barber(0).Mass;
    for(int i=0;i<20;i++){attacker.Cooldown=0;s.UseTool(attacker,false);}
    Near(customer,s.State.SharedHead.Mass);Near(barber,s.State.Barber(0).Mass);
    Check(s.State.Tools[1].Holder==2);
});
Test("unlocked barbers are material sources, wigs carry heat and glue",()=>{
    var s=SessionWithPlayer();var source=s.AddPlayer(2,"Hair donor")!;s.Tick(9);var attacker=s.State.Player(1)!;
    source.Position=Session.WorkCenter;s.State.Barber(1).Position=source.Position+new Vector3(0,1.7f,0);
    attacker.Position=source.Position+new Vector3(0,0,2);attacker.Pitch=.16f;attacker.Held=2;s.State.Tools[2].Holder=1;
    foreach(var head in s.State.Heads.Where(h=>h.Barber&&h.Owner==2))foreach(var patch in head.Patches){patch.Glue=.8f;patch.Temperature=150;}
    // Remove the customer from the ray so the intended barber is nearest.
    s.State.SharedHead.Position=new(20,20,20);s.SyncFaces();float Material()=>s.State.Heads.Where(h=>h.Barber&&h.Owner==2).Sum(h=>h.Mass);float before=Material();s.UseTool(attacker,false);
    Check(Material()<before && attacker.Reservoir>0);Check(attacker.StoredHeat>100 && attacker.StoredGlue>.7f);
});
Test("disconnect clears authority and does not stall",()=>{var s=SessionWithPlayer();s.AddPlayer(2,"Bye");s.RemovePlayer(2);Check(s.State.Player(2)==null);s.Tick(9);s.Tick(76);Check(s.State.Phase==Phase.Validation);});
Test("snapshot roundtrip all facts",()=>{var s=SessionWithPlayer();s.State.SharedHead.Patches[0].Glue=.8f;var copy=Wire.Decode<WorldState>(Wire.Encode(s.State));Near(copy.SharedHead.Patches[0].Glue,.8f);Check(copy.Heads.Count==s.State.Heads.Count && copy.Player(1)!.Position==s.State.Player(1)!.Position);});
Test("highlight ranks catastrophe over tiny trim",()=>Check(ReplayRecorder.Rank(new(){Value=40,Targets=3})>ReplayRecorder.Rank(new(){Value=2,Targets=1})));
Test("bilingual content covers every tool and goal, retains English source",()=>{
    var strings=Tools.All.SelectMany(t=>new[]{t.Name,t.Hint}).Concat(Goals.All.SelectMany(g=>new[]{g.Name,g.Instruction,g.Validation}));
    foreach(var value in strings){L.SetLocale("zh-CN");Check(L.T(value)!=value,value);L.SetLocale("en");Check(L.T(value)==value);}
    foreach(var pair in L.Chinese)Check(System.Text.CompositeFormat.Parse(pair.Key).MinimumArgumentCount==System.Text.CompositeFormat.Parse(pair.Value).MinimumArgumentCount,pair.Key);
});
Test("localized remote events preserve custom names and authoritative state",()=>{
    var s=SessionWithPlayer();var before=Wire.Encode(s.State);L.SetLocale("zh");
    Check(L.Text("Mira: Super Growth Spray → 2 head(s)")=="Mira：超级生发喷雾 → 影响 2 个头部");
    Check(L.Text("Station 2 KO! Source pays -25. Recovery in 4s.").Contains("扣 25 分"));
    Check(L.Text("Barber 1 cashed out")=="理发师 1 已提交作品");Check(L.PlayerName("Mira")=="Mira");
    Check(before.SequenceEqual(Wire.Encode(s.State)));L.SetLocale("en");
});
Test("merged shell is connected and watertight with no exposed cell faces",()=>{
    var v=HairVolume.Create();var mesh=HairShell.Build(v);Check(mesh.Indices.Count>0);
    var edges=new Dictionary<(int,int),int>();var neighbors=new Dictionary<int,List<int>>();
    for(int i=0;i<mesh.Indices.Count;i+=3)for(int j=0;j<3;j++){
        int a=mesh.Indices[i+j],b=mesh.Indices[i+(j+1)%3];var edge=(Math.Min(a,b),Math.Max(a,b));edges[edge]=edges.GetValueOrDefault(edge)+1;
        if(!neighbors.ContainsKey(a))neighbors[a]=new();neighbors[a].Add(b);
    }
    Check(edges.Values.All(n=>n==2),"open seam or nonmanifold edge: "+string.Join(",",edges.Values.GroupBy(n=>n).Select(g=>$"{g.Key}={g.Count()}")));
    var seen=new HashSet<int>();var queue=new Queue<int>();queue.Enqueue(0);while(queue.Count>0){int a=queue.Dequeue();if(!seen.Add(a))continue;foreach(int b in neighbors[a])queue.Enqueue(b);}
    Check(seen.Count==mesh.Vertices.Count,"initial hairstyle has separate pieces");
});
Test("volume cut creates a level surface instead of shortening columns",()=>{
    var v=HairVolume.Create();float before=v.Mass;v.Brush(new(EffectKind.CutPlane,1,Vector3.UnitY),new(0,.55f,0),1.5f);
    Check(v.Mass<before);Check(v.Sample(new(0,.7f,0))<0);Check(v.Sample(new(0,.48f,0))>0);
    var mesh=HairShell.Build(v);Check(mesh.Vertices.Max(p=>p.Y)<.57f);Check(mesh.Vertices.Count(p=>Math.Abs(p.Y-.55f)<.025f)>8);
});
Test("puncture opens an actual ray tunnel without deleting the surrounding mass",()=>{
    var v=HairVolume.Create();var origin=new Vector3(.17f,.62f,1);var dir=-Vector3.UnitZ;
    Check(v.Raycast(origin,dir,3,out var hit,out _));float before=v.Mass;
    v.Brush(new(EffectKind.Puncture,1,dir),hit,.18f,origin,3);
    Check(!v.Raycast(origin,dir,3,out _,out _));Check(v.Mass>before*.7f&&v.Mass<before);
    Check(v.Raycast(origin+new Vector3(-.37f,0,0),dir,3,out _,out _));
    var mesh=HairShell.Build(v);var counts=new Dictionary<(int,int),int>();for(int i=0;i<mesh.Indices.Count;i+=3)for(int j=0;j<3;j++){int a=mesh.Indices[i+j],b=mesh.Indices[i+(j+1)%3];var e=(Math.Min(a,b),Math.Max(a,b));counts[e]=counts.GetValueOrDefault(e)+1;}Check(counts.Values.All(n=>n==2),"hole rim is not watertight");
});
Test("side shaving is local and growth bulges the visible hit surface",()=>{
    var v=HairVolume.Create();var origin=new Vector3(-2,.48f,0);var dir=Vector3.UnitX;
    Check(v.Raycast(origin,dir,4,out var hit,out float distance));float opposite=v.Sample(new(.45f,.45f,0));float before=v.Mass;
    v.Brush(new(EffectKind.RemoveHair,.12f),hit,.23f);Check(v.Sample(hit)<0);Near(v.Sample(new(.45f,.45f,0)),opposite);Check(v.Mass<before);
    v=HairVolume.Create();v.Raycast(origin,dir,4,out hit,out distance);v.Brush(new(EffectKind.AddHair,.16f),hit,.3f);Check(v.Raycast(origin,dir,4,out _,out float grown)&&grown<distance-.04f);
});
Test("frozen shell geometry unchanged, burn erodes the real material",()=>{
    var h=Head.Create(0,1);var before=(byte[])h.Volume.Data.Clone();foreach(var p in h.Patches)HairSystem.Apply(h,p,new(EffectKind.ChangeTemperature,-90));Check(before.SequenceEqual(h.Volume.Data));
    foreach(var p in h.Patches)HairSystem.Apply(h,p,new(EffectKind.Ignite,1));float mass=h.Mass;for(int i=0;i<10;i++)HairSystem.Tick(h,.12f);Check(h.Mass<mass);Check(!before.SequenceEqual(h.Volume.Data));
});
Test("density facts survive snapshots and replay copies without aliasing",()=>{
    var h=Head.Create(0,1);h.Volume.Brush(new(EffectKind.Puncture,1,-Vector3.UnitZ),new(0,.6f,.4f),.18f,new(0,.6f,1),3);
    var clone=h.Clone();var copy=Wire.Decode<Head>(Wire.Encode(h));Check(copy.Volume.Data.SequenceEqual(h.Volume.Data));Near(copy.Mass,h.Mass);
    clone.Volume.Brush(new(EffectKind.CutPlane,1),new(0,.5f,0),2);Check(copy.Volume.Data.SequenceEqual(h.Volume.Data));Check(!clone.Volume.Data.SequenceEqual(h.Volume.Data));
});
Test("authoritative sniper tunnel persists in remote decoded geometry",()=>{
    var s=SessionWithPlayer();s.Tick(9);var p=s.State.Player(1)!;var h=s.State.SharedHead;p.Position=Session.WorkCenter+new Vector3(0,0,2);p.Pitch=MathF.Atan2(.62f-.2f,2);p.Yaw=0;p.Held=7;s.State.Tools[7].Holder=1;
    Check(h.Volume.Raycast(Session.Eye(p)-h.Position,Session.Aim(p),8,out _,out _));s.UseTool(p,false);
    var copy=Wire.Decode<WorldState>(Wire.Encode(s.State)).SharedHead;Check(!copy.Volume.Raycast(Session.Eye(p)-copy.Position,Session.Aim(p),8,out _,out _));Check(copy.Mass>0);
});
Test("moving an anchored head stretches real density and submission blocks brushes",()=>{
    var h=Head.Create(0,1);var patch=h.Patches[0];HairSystem.Apply(h,patch,new(EffectKind.Anchor,1,Point:new(-.3f,.55f,0)));
    var before=(byte[])h.Volume.Data.Clone();h.Position=new(.15f,0,0);HairSystem.Tick(h,.12f);Check(!before.SequenceEqual(h.Volume.Data));
    h.Locked=true;before=(byte[])h.Volume.Data.Clone();HairSystem.Apply(h,patch,new(EffectKind.RemoveHair,1));HairSystem.Tick(h,1);Check(before.SequenceEqual(h.Volume.Data));
});
Test("chair controls clamp and move all customer hair, enforce aim and submission",()=>{
    var s=SessionWithPlayer();var p=s.State.Player(1)!;s.Tick(9);p.Position=Session.ChairControl(0)+new Vector3(0,-.9f,2);
    var d=Session.ChairControl(0)-Session.Eye(p);p.Pitch=MathF.Asin(d.Y/d.Length());p.Yaw=0;
    for(int i=0;i<100;i++)Check(s.AdjustChair(p,0,.05f));Near(s.State.Customers[0].ChairHeight,1);Near(s.State.SharedHead.Position.Y,2.5f);
    foreach(var face in s.State.Heads.Where(h=>h.ParentHead==0))Near(face.Position.Y,2.5f+Head.FaceOffset(face.Region).Y);
    for(int i=0;i<100;i++)s.AdjustChair(p,0,-.05f);Near(s.State.Customers[0].ChairHeight,-.4f);
    s.State.Remaining=0;s.Tick(.00001f);Check(!s.AdjustChair(p,0,.05f));
});
Test("ladder is exclusive physical possession and blocked placement keeps it carried",()=>{
    var s=SessionWithPlayer();s.Tick(9);var p=s.State.Player(1)!;var ladder=s.State.Ladders[0];p.Position=ladder.Position+new Vector3(0,0,2.2f);var d=ladder.Position+Vector3.UnitY*.9f-Session.Eye(p);p.Pitch=MathF.Asin(d.Y/d.Length());p.Yaw=0;p.Held=1;s.State.Tools[1].Holder=1;
    Check(s.PickupLadder(p,0));Check(p.Held==-1&&s.State.Tools[1].Holder==0&&p.CarryLadder==0);Check(!s.Pickup(p,1));
    var other=s.AddPlayer(2,"Other")!;other.Position=p.Position;other.Pitch=p.Pitch;Check(!s.PickupLadder(other,0));
    s.CanPlaceLadder=(_,_,_)=>false;Check(!s.DropLadder(p)&&ladder.Holder==1);s.CanPlaceLadder=(_,_,_)=>true;Check(s.DropLadder(p));Check(ladder.Holder==0&&p.CarryLadder==-1);Near(ladder.Position.Y,0);
});
Test("eyebrows and beard receive real aimed clipping, growth and freezing",()=>{
    foreach(var region in new[]{HairRegion.LeftBrow,HairRegion.RightBrow,HairRegion.Beard})foreach(int tool in new[]{0,1,5}){
        var s=SessionWithPlayer();s.Tick(9);var p=s.State.Player(1)!;var face=s.State.Heads.Single(h=>h.ParentHead==0&&h.Region==region);
        s.State.SharedHead.Volume.Fill(_=>-1); // Expose the facial region; exact surface rays must not pass through the fringe.
        p.Position=face.Position+new Vector3(0,-1.7f,1.5f);p.Yaw=0;p.Pitch=0;p.Held=tool;s.State.Tools[tool].Holder=1;float before=face.Mass;s.UseTool(p,false);
        Check(tool==0?face.Mass<before:tool==1?face.Mass>before:face.Patches.Any(x=>x.Frozen),$"region {region} tool {tool}: before={before}, after={face.Mass}, stroke={s.Stroke(1)?.Plane}/{s.Stroke(1)?.Previous}/{s.Stroke(1)?.Head}, hit={string.Join(",",ToolQuery.Find(s.State,p,tool,false,Session.Aim(p),4).Select(c=>$"{c.HeadId}:{c.Point}:{c.Normal}"))}");
    }
});
Test("facial material follows rotation and persists only on barbers between rounds",()=>{
    var s=SessionWithPlayer();var customer=s.State.Heads.Single(h=>h.ParentHead==0&&h.Region==HairRegion.Beard);var barber=s.State.Heads.Single(h=>h.ParentHead==1&&h.Region==HairRegion.Beard);
    customer.Volume.Fill(_=>-1);barber.Volume.Fill(_=>-1);s.State.Barber(0).Rotation=new(0,MathF.PI,0);s.SyncFaces();Check(barber.Position.Z<s.State.Barber(0).Position.Z);
    s.NextRound();Check(s.State.Heads.Single(h=>h.ParentHead==0&&h.Region==HairRegion.Beard).Mass>0);Near(s.State.Heads.Single(h=>h.ParentHead==1&&h.Region==HairRegion.Beard).Mass,0);
    s.State.Phase=Phase.Build;s.State.Remaining=0;s.Tick(.00001f);Check(s.State.Heads.Where(h=>h.ParentHead is 0 or 1).All(h=>h.Locked));
    var copy=Wire.Decode<WorldState>(Wire.Encode(s.State));Check(copy.Ladders.Count==2&&copy.Heads.Count(h=>h.Facial)==6);
});
Test("target projection respects front side and top of three-dimensional laundry frame",()=>{
    var target=Goals.All[6].Target;
    Check(!TargetProjection.Hit(target,new(0,.7f,4),-Vector3.UnitZ));
    Check(TargetProjection.Hit(target,new(.46f,.7f,4),-Vector3.UnitZ));
    Check(TargetProjection.Hit(target,new(0,1.3f,4),-Vector3.UnitZ));
    Check(TargetProjection.Hit(target,new(4,.7f,0),-Vector3.UnitX));
    Check(!TargetProjection.Hit(target,new(4,.7f,.3f),-Vector3.UnitX));
    Check(TargetProjection.Hit(target,new(0,4,0),-Vector3.UnitY));
    Check(!TargetProjection.Hit(target,new(0,4,.3f),-Vector3.UnitY));
});
Test("target hollow cylinders reveal bore and nest has a real supporting floor",()=>{
    Check(TargetProjection.Hit(Goals.All[1].Target,new(0,4,0),-Vector3.UnitY));
    foreach(int goal in new[]{2,7}){
        var cylinder=(CylinderVolume)Goals.All[goal].Target;
        Check(!TargetProjection.Hit(cylinder,new(0,4,0),-Vector3.UnitY));
        Check(TargetProjection.Hit(cylinder,new(cylinder.Radius-.1f,4,0),-Vector3.UnitY));
        Check(TargetProjection.Hit(cylinder,new(0,cylinder.Center.Y,4),-Vector3.UnitZ));
        Check(!TargetProjection.Hit(cylinder,new(0,4,0),Vector3.UnitY));
    }
});
Test("target ray queries match dense independent membership sampling for all goals",()=>{
    var random=new Random(428);
    foreach(var goal in Goals.All){
        var bounds=TargetProjection.Bounds(goal.Target);
        Check(goal.TargetSamples.All(p=>p.X>=bounds.Min.X-.00001f&&p.Y>=bounds.Min.Y-.00001f&&p.Z>=bounds.Min.Z-.00001f&&p.X<=bounds.Max.X+.00001f&&p.Y<=bounds.Max.Y+.00001f&&p.Z<=bounds.Max.Z+.00001f));
        for(int i=0;i<200;i++){
            var aim=new Vector3((float)random.NextDouble()*2.6f-1.3f,(float)random.NextDouble()*2,(float)random.NextDouble()*2-1);
            var origin=Vector3.Normalize(new Vector3((float)random.NextDouble()-.5f,(float)random.NextDouble()-.5f,(float)random.NextDouble()-.5f))*4+Vector3.UnitY*.8f;
            var direction=Vector3.Normalize(aim-origin);bool sampled=false;
            for(float t=0;t<8;t+=.004f)if(goal.Target.Contains(origin+direction*t)){sampled=true;break;}
            Check(TargetProjection.Hit(goal.Target,origin,direction)==sampled,$"goal {goal.Id}, ray {i}");
        }
    }
});
Test("laundry preset is opt-in and three pegs follow real density without modifying it",()=>{
    var s=SessionWithPlayer();var before=Wire.Encode(s.State);s.StageLaundry();Check(before.SequenceEqual(Wire.Encode(s.State)));
    s.Lab=true;s.StageLaundry();var h=s.State.SharedHead;before=Wire.Encode(s.State);
    for(int i=0;i<3;i++){var contact=LaundryRig.Contact(h,i);Check(contact.OnHair&&contact.Point.Y>1,"peg must find the raised beam");Check(Math.Abs(h.Volume.Sample(contact.Point))<.015f);}
    Check(before.SequenceEqual(Wire.Encode(s.State)));Check(s.State.Player(1)!.Held==4&&s.State.Tools.Single(t=>t.Id==4).Holder==1);
});
Test("damaged laundry has no imaginary beam support and stronger bounded load reaction",()=>{
    var s=SessionWithPlayer();s.Lab=true;s.StageLaundry();var h=s.State.SharedHead;float calm=LaundryRig.Tension(h,0);
    h.Volume=new();Check(!LaundryRig.Contact(h,1).OnHair&&LaundryRig.Contact(h,1).Strength==0);Check(LaundryRig.Tension(h,0)>calm);
    for(float time=0;time<10;time+=.1f){var rotation=LaundryRig.LoadRotation(h,time,1);Check(HairSystem.Finite(rotation)&&rotation.Length()<.03f);}
});
Test("actual glue contact is snapshotted and replay does not read later tool state",()=>{
    var s=SessionWithPlayer();s.Lab=true;s.StageLaundry();s.State.Phase=Phase.Build;var p=s.State.Player(1)!;s.UseTool(p,false);
    var t=s.State.Tools.Single(t=>t.Id==4);Check(t.HitHair&&t.LastUse==s.State.Time);var point=t.Contact;
    var copy=Wire.Decode<WorldState>(Wire.Encode(s.State));Check(copy.Tools.Single(t=>t.Id==4).Contact==point);
    s.Replay.Record(s.State,.11f);var recorded=s.Replay.Rolling.Last().Tools.Single(t=>t.Id==4);t.Contact=new(99);t.LastUse=999;
    Check(recorded.Contact==point&&recorded.LastUse!=999);
});
MiniatureTests.Run(Test,Check);
ExperimentTests.Run(Test,Check);
PartyTests.Run(Test,Check);
GrowthSprayTests.Run(Test,Check);
PartyPlacementTests.Run(Test,Check);
PartyMaterialTests.Run(Test,Check);
PartyResultTests.Run(Test,Check);
PartyMoldTests.Run(Test,Check);
PartyAccidentTests.Run(Test,Check);
Hairball.Tests.VoiceTests.Run(Test,Check);
PresentationTests.Run(Test,Check);
PartyBodyTests.Run(Test,Check);
TargetCardTests.Run(Test,Check);
HumanCustomerTests.Run(Test,Check);
TwistTests.Run(Test,Check);
WorldTests.Run(Test,Check);
MotionTests.Run(Test,Check);
RecoveryTests.Run(Test,Check);
MaterialTests.Run(Test,Check);
SharedTests.Run(Test,Check);
ImpactTests.Run(Test,Check);
SculptTests.Run(Test,Check);
Console.WriteLine($"CORE_TESTS_OK {passed}/{passed}");



