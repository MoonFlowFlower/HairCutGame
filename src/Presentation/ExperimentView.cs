using Godot;
using Hairball.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hairball;
public partial class SalonView
{
    Node3D? experimentArt,braceHands;
    Label3D? mirrorCue,flightCue,callCue,remarkCue;
    MeshInstance3D? toleranceRing;
    MeshInstance3D? attentionArrow,callButton;
    StaticBody3D? callRadioBody;
    readonly Label3D[] landingFaultLabels=new Label3D[4];
    readonly MeshInstance3D[] landingFaultMarkers=new MeshInstance3D[4];
    float landingProbeAt=-100;
    int landingProbeRound=-1;
    LandingIssue landingProbeIssues;
    bool landingWasShown;
    SubViewport? customerMirror;
    HeadView? customerMirrorHair;
    void SyncPartyBody(WorldState w,Node3D model)
    {
        var e=w.Experiment;float age=PartyLoop.Age(w);bool moving=age>=0&&age<4||age>=6&&age<10;
        bool seated=w.Player(e.CustomerActor) is {} human?!human.Standing:e.Leave==LeaveStage.Seated;
        if(w.Player(e.CustomerActor) is {} customer) moving=customer.Standing;
        bool watch=e.Leave==LeaveStage.Seated&&w.Remaining<=20&&w.Remaining>10;
        if(model.GetNodeOrNull<Node3D>("SeatedApron") is {} apron)apron.Visible=seated;
        bool coat=e.Leave==LeaveStage.Seated&&w.Remaining<=10||e.Leave==LeaveStage.Rising;
        for(int side=-1;side<=1;side+=2)
        {
            var arm=model.GetNode<Node3D>($"Arm_{side}");arm.Position=new(side*.46f,1,.03f);
            arm.Rotation=watch?new(-1.1f,0,-side*.55f):coat?new(-.65f,0,-side*.4f):new(moving?MathF.Sin(age*7+side)*.2f:0,0,side*.21f);
            var leg=model.GetNode<Node3D>($"Leg_{side}");leg.Position=new(side*.2f,.38f,seated?.18f:0);leg.Scale=new(1,1,seated?1:.47f);leg.Rotation=new(moving?MathF.Sin(age*7+side)*.12f:0,0,0);
        }
        if(model.GetNodeOrNull<Node3D>("WatchFace")==null)Art.Ball(model,new(-.25f,1.35f,.32f),new(.12f,.1f,.04f),new("ffe29e")).Name="WatchFace";
        model.GetNode<Node3D>("WatchFace").Visible=watch;
        LaundryView.Expression(model,w.Time,Math.Max(w.Customers[0].Panic,e.Tolerance/100),e.Tolerance>=40);
    }
    void SyncExperiment(WorldState w,int localId,ReplayFrame? replay)
    {
        bool b=w.Experiment.IsB;
        if(!b){if(experimentArt!=null)experimentArt.Visible=false;if(callRadioBody!=null)callRadioBody.CollisionLayer=0;return;}
        if(experimentArt==null)
        {
            experimentArt=new(){Name="ExperimentB"};AddChild(experimentArt);
            Art.Label(experimentArt,"E: pick up / place; hold E: brace; G: drop",new(-1.8f,2.45f,-3.4f),22);
            customerMirror=new(){Size=new(256,256),OwnWorld3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Always};AddChild(customerMirror);
            if(VisualQuality.Puppet)customerMirror.Msaa3D=Viewport.Msaa.Msaa4X;
            var scene=new Node3D();customerMirror.AddChild(scene);Art.Person(scene,new("577c8d"));
            customerMirrorHair=new(){Position=new(0,1.64f,0)};scene.AddChild(customerMirrorHair);
            var camera=new Camera3D{Position=new(0,2.25f,3.3f),Current=true};scene.AddChild(camera);camera.LookAt(new Vector3(0,1.8f,0));
            scene.AddChild(new DirectionalLight3D{RotationDegrees=new(-35,-25,0),LightEnergy=1.5f});
            scene.AddChild(new WorldEnvironment{Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new("4e737f"),AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=Colors.White,AmbientLightEnergy=.7f}});
            var mirror=new MeshInstance3D{Position=Art.V(Session.MirrorPoint),Rotation=new(0,Mathf.Pi,0),Mesh=new QuadMesh{Size=new(1.1f,.85f)},MaterialOverride=new StandardMaterial3D{AlbedoTexture=customerMirror.GetTexture(),ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,CullMode=BaseMaterial3D.CullModeEnum.Disabled}};experimentArt.AddChild(mirror);
            Art.Cylinder(experimentArt,Art.V(Session.MirrorPoint)-Vector3.Up*.95f,.035f,1.05f,new("f4d58d"));
            mirrorCue=Art.Label(experimentArt,"",Art.V(Session.MirrorPoint)+Vector3.Up*.59f,19,new("f4d58d"));
            flightCue=Art.Label(experimentArt,"",Vector3.Zero,19,new("f4d58d"));
            var radio=new Node3D{Name="CompletionBellRadio",Position=Art.V(PartyLoop.Bell)};experimentArt.AddChild(radio);
            Art.Cylinder(radio,new(0,-.47f,0),.035f,.86f,new("516c76"));
            Art.Box(radio,new(0,-.86f,0),new(.44f,.07f,.36f),new("516c76"));
            Art.Box(radio,new(0,-.10f,0),new(.39f,.18f,.30f),new("263f49"));
            Art.Cylinder(radio,new(.14f,.14f,-.08f),.012f,.32f,new("e2dbbd"));
            callButton=Art.Ball(radio,Vector3.Zero,new(.16f,.10f,.16f),new("93ee80"));callButton.Name="CompletionBellButton";
            callCue=Art.Label(radio,"",new(0,.66f,0),18,new("d1f1a8"));callCue.Name="CompletionBellCue";
            // The low stand is solid; the raised button itself remains unobstructed for E's aim query.
            callRadioBody=new(){Name="CompletionBellStand",Position=Art.V(PartyLoop.Bell)+new Vector3(0,-.50f,0),CollisionLayer=1,CollisionMask=0};experimentArt.AddChild(callRadioBody);
            callRadioBody.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=new(.28f,.76f,.25f)}});
            for(int i=0;i<4;i++)
            {
                landingFaultMarkers[i]=Art.Ball(experimentArt,Vector3.Zero,new(.12f,.07f,.12f),new("ff9475"));landingFaultMarkers[i].Name=$"LandingZoneMarker_{i}";landingFaultMarkers[i].Visible=false;
                landingFaultLabels[i]=Art.Label(experimentArt,"",Vector3.Zero,18,new("ffe0af"));landingFaultLabels[i].Name=$"LandingZone_{i}";landingFaultLabels[i].Visible=false;
            }
            attentionArrow=Art.Cylinder(experimentArt,Vector3.Zero,.035f,.45f,new("ffd66d"),.005f);
            braceHands=new();experimentArt.AddChild(braceHands);
            for(int i=-1;i<=1;i+=2)Art.Ball(braceHands,new(i*.43f,.0f,.12f),new(.22f,.27f,.2f),new("edc19d"));
        }
        experimentArt.Visible=true;
        if(experimentArt.GetNodeOrNull<Node3D>("WigStands")==null){var stands=new Node3D{Name="WigStands"};experimentArt.AddChild(stands);foreach(int side in new[]{-1,1}){var pos=new Vector3(side*1.4f,.65f,-1.3f);Art.Cylinder(stands,pos-Vector3.Up*.35f,.035f,.6f,new("68787e"));Art.Ball(stands,pos,new(.34f,.22f,.30f),new("f1d6b4"));Art.Collider(stands,pos-Vector3.Up*.12f,new(.60f,.15f,.5f),32);Art.Label(stands,"WIG PATCH · GLUE / FREEZE / NAIL",pos+Vector3.Up*.95f,17);}}
        if(experimentArt.GetNodeOrNull<Node3D>("CommissionTable")==null){var table=new Node3D{Name="CommissionTable",Position=Art.V(PartyLoop.Table)};experimentArt.AddChild(table);Art.Box(table,Vector3.Zero,new(1.2f,.12f,.8f),new("ba9170"));for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Art.Box(table,new(x*.48f,-.41f,z*.28f),new(.07f,.76f,.07f),new("ba9170"));var tableBody=new StaticBody3D{Position=new(0,-.06f,0),CollisionLayer=1,CollisionMask=0};table.AddChild(tableBody);tableBody.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=new(1.2f,.12f,.8f)}});Art.Label(table,"RC HELICOPTER",new(0,.55f,0),18);}
        if(remarkCue==null){remarkCue=Art.Label(experimentArt,"",Vector3.Zero,22);remarkCue.Name="CustomerRemark";toleranceRing=new(){Name="ToleranceRing",Mesh=new TorusMesh{InnerRadius=.59f,OuterRadius=.64f,Rings=24,RingSegments=8}};experimentArt.AddChild(toleranceRing);}
        remarkCue.Position=Art.V(w.SharedHead.Position)+Vector3.Up*1.55f;remarkCue.Text=(w.Experiment.Leave==LeaveStage.Mirror&&w.Experiment.Tolerance<40?PartyResults.Reaction(w.Experiment.Curtain):w.Time<w.Experiment.RemarkUntil?CustomerRemarks.Text(w.Experiment.Remark):"")+"\n"+L.T("Customer tolerance")+(PresentationSettings.Numbers?$" {w.Experiment.Tolerance:0}/100":"");
        toleranceRing!.Position=Art.V(w.SharedHead.Position)+Vector3.Up*1.12f;toleranceRing.MaterialOverride=Art.Material(w.Experiment.Tolerance>=70?new("ff6157"):w.Experiment.Tolerance>=40?new("ffcb58"):new("82d89b"),true);
        toleranceRing.CastShadow=VisualQuality.Scene?GeometryInstance3D.ShadowCastingSetting.Off:GeometryInstance3D.ShadowCastingSetting.On;
        callRadioBody!.CollisionLayer=1;
        var e=replay?.Experiment??w.Experiment;
        var head=replay?.Heads.FirstOrDefault(h=>h.Id==0)??w.SharedHead;
        customerMirrorHair!.Update(head,replay?.Time??w.Time,1,true);
        mirrorCue!.Text=L.Locale=="zh"?(e.Attention.Blocked?"顾客镜子 · 已遮挡":"顾客镜子 · 视线畅通"):(e.Attention.Blocked?"CUSTOMER MIRROR · BLOCKED":"CUSTOMER MIRROR · CLEAR");
        var heli=(replay?.Props??w.Props).FirstOrDefault(p=>p.Goal==0);
        flightCue!.Visible=heli!=null&&e.Flight!=FlightStage.Staging;
        if(heli!=null){flightCue.Position=Art.V(heli.Position)+Vector3.Up*.55f;flightCue.Text=L.T(e.Leave.ToString())+(PresentationSettings.Numbers?$" · {e.StableSeconds:0.0}/3s":"");SyncPlacementMoment(w,heli);}
        bool ready=w.Phase==Phase.Build&&e.Leave==LeaveStage.Seated&&heli?.Attached==true;
        callButton!.MaterialOverride=Art.Material(ready?new("93ee80"):new("a49b6c"),ready);
        callCue!.Visible=replay==null&&w.Player(localId) is {} caller&&Session.LookingAt(caller,PartyLoop.Bell,.25f,2.5f);
        callCue.Text=L.T("Completion bell")+"\n"+(ready?L.T("E: leave early"):L.T("Place the commission first"));
        bool showLanding=replay==null&&w.Phase==Phase.Build&&heli?.Attached==true;
        if(!showLanding)foreach(var label in landingFaultLabels)label.Visible=false;
        if(!showLanding)foreach(var marker in landingFaultMarkers)marker.Visible=false;
        if(showLanding&&(!landingWasShown||w.Round!=landingProbeRound||w.Time<landingProbeAt||w.Time-landingProbeAt>=.2f||e.LandingIssues!=landingProbeIssues))
        {
            landingProbeRound=w.Round;landingProbeAt=w.Time;landingProbeIssues=e.LandingIssues;
            var zones=ExperimentText.LandingZones(w,heli!,e.LandingIssues);
            var shownZones=zones.Where(z=>z.Issues!=LandingIssue.None).Take(2).Select(z=>z.Index).ToArray();
            for(int i=0;i<4;i++)
            {
                var zone=zones.FirstOrDefault(z=>z.Index==i);bool visible=shownZones.Contains(i);
                landingFaultLabels[i].Visible=landingFaultMarkers[i].Visible=visible;
                if(!visible)continue;
                var pos=Art.V(zone.Position);landingFaultMarkers[i].Position=pos+Vector3.Up*.03f;
                landingFaultLabels[i].Position=pos+new Vector3(zone.Column*.16f,.24f,zone.Row*.12f);
                landingFaultLabels[i].Text=ExperimentText.ZoneName(zone.Column,zone.Row,L.Locale=="zh")+"\n"+ExperimentText.ZoneAdvice(zone.Issues,L.Locale=="zh");
            }
        }
        landingWasShown=showLanding;
        if(showLanding&&localId!=0&&w.Player(localId) is {} observer)for(int i=0;i<4;i++){if(!landingFaultMarkers[i].Visible)continue;float facing=MaterialFeel.Facing(Session.Eye(observer),head.Position,Art.N(landingFaultMarkers[i].Position));landingFaultMarkers[i].Transparency=1-facing;landingFaultLabels[i].Modulate=new Color(1,1,1,facing);}
        braceHands!.Visible=replay?.BracingPlayers.Count>0||replay==null&&w.Players.Any(p=>p.Active&&p.Bracing);
        braceHands.Position=Art.V(head.Position);braceHands.Rotation=Art.V(head.Rotation);
        attentionArrow!.Visible=e.Attention.Stage!=AttentionStage.Unaware;
        var direction=Art.V(e.Attention.Source-head.Position);if(direction.Length()<.01f)direction=Vector3.Right;direction=direction.Normalized();
        attentionArrow.Position=Art.V(head.Position)+Vector3.Up*.9f+direction*.45f;attentionArrow.Quaternion=new Quaternion(Vector3.Up,direction);
        if(w.Player(localId)?.Bracing==true)heldRoot.Visible=false;
    }
}
public static class ExperimentText
{
    public static IReadOnlyList<LandingZoneFeedback> LandingZones(WorldState w,PropState heli,LandingIssue blockers)
    {
        var probes=Session.LandingProbes(w,heli);var zones=new List<LandingZoneFeedback>();int index=0;
        var shownProbes=new HashSet<(int Column,int Row)>();
        foreach(int row in new[]{1,-1})foreach(int column in new[]{-1,1})
        {
            int regionIndex=index++;
            var region=probes.Where(p=>(p.Column==column||p.Column==0)&&(p.Row==row||p.Row==0)).ToArray();
            var issues=region.Aggregate(LandingIssue.None,(flags,p)=>flags|p.Issues)&blockers;
            issues&=LandingIssue.Coverage|LandingIssue.Soft|LandingIssue.Uneven|LandingIssue.Burning;
            if(issues==LandingIssue.None)continue;
            var primary=issues.HasFlag(LandingIssue.Burning)?LandingIssue.Burning:issues.HasFlag(LandingIssue.Coverage)?LandingIssue.Coverage:
                issues.HasFlag(LandingIssue.Soft)?LandingIssue.Soft:LandingIssue.Uneven;
            // Shared centre/axis queries can make several regions fail. Point at the actual
            // failed query and show it once, rather than falsely labelling healthy corners.
            var fault=region.Where(p=>p.Issues.HasFlag(primary)).OrderBy(p=>(p.Column-column)*(p.Column-column)+(p.Row-row)*(p.Row-row)).First();
            if(!shownProbes.Add((fault.Column,fault.Row)))continue;
            zones.Add(new(regionIndex,fault.Column,fault.Row,fault.Surface,fault.Issues&issues));
        }
        return zones;
    }
    public static string ZoneName(int column,int row,bool zh)
    {
        if(column==0&&row==0)return zh?"中央":"Centre";
        if(column==0)return zh?(row>0?"前侧中央":"后侧中央"):(row>0?"Front centre":"Back centre");
        if(row==0)return zh?(column<0?"左侧中央":"右侧中央"):(column<0?"Left centre":"Right centre");
        return zh?(column<0?"左":"右")+(row>0?"前":"后"):(row>0?"Front":"Back")+" "+(column<0?"left":"right");
    }
    public static string ZoneAdvice(LandingIssue issues,bool zh)
    {
        if(issues.HasFlag(LandingIssue.Burning))return zh?"燃烧 · 先灭火":"Burning · extinguish";
        if(issues.HasFlag(LandingIssue.Coverage))return zh?"缺支撑 · 补头发":"Missing support · add hair";
        if(issues.HasFlag(LandingIssue.Soft))return zh?"太软 · 胶水/冷冻":"Too soft · glue/freeze";
        if(issues.HasFlag(LandingIssue.Uneven))return zh?"不平 · 修平/扶稳":"Uneven · level/brace";
        return "";
    }
}
public readonly record struct LandingZoneFeedback(int Index,int Column,int Row,System.Numerics.Vector3 Position,LandingIssue Issues);
