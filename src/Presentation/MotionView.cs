using Godot;
using Hairball.Core;
using System;
using System.Linq;
namespace Hairball;
public partial class SalonView
{
    public void SyncMotionColliders(MotionFrame frame,int localId)
    {
        foreach(var p in frame.Players)if(p.Id!=localId&&Bodies.TryGetValue(p.Id,out var body)){body.Position=Art.V(p.Position);body.Model.Position=frame.Time<p.DownUntil?new(0,.15f,.6f):Vector3.Zero;}
        var headCollider=GetNode<AnimatableBody3D>("SharedHeadCollider");headCollider.CollisionLayer=frame.Players.Any(p=>p.Customer&&p.Standing)||frame.Leave is not (LeaveStage.Seated or LeaveStage.Rising)?0u:2u;headCollider.Position=Art.V(frame.Head.Position);headCollider.Rotation=Art.V(frame.Head.Rotation);
    }
    public void RenderCosmetics(WorldState w,int localId,float time)
    {
        foreach(var hair in heads.Values)hair.RenderClock(time);
        mirrorHair.RenderClock(time);customerMirrorHair?.RenderClock(time);
        foreach(var hair in mirrorFaces.Values)hair.RenderClock(time);
        foreach(var hair in mirrorWigs.Values)hair.RenderClock(time);
        foreach(var prop in w.Props.Where(p=>p.Goal==0))if(props.TryGetValue($"{w.Round}:{prop.Id}",out var node))SpinRotor(node,time,prop.Failed?2:24);
        var held=w.Tools.FirstOrDefault(p=>p.Holder==localId);bool working=held!=null&&w.Time-held.LastUse<.2f;
        heldRoot.Position=new(.40f,-.30f+MathF.Sin(time*(working?23:2))*(working?.007f:.004f),-.9f);
    }
    static Vector3 SmoothRotation(System.Numerics.Vector3 a,System.Numerics.Vector3 b,float t)=>Quaternion.FromEuler(Art.V(a)).Slerp(Quaternion.FromEuler(Art.V(b)),t).GetEuler();
    public void RenderMotion(WorldState w,int localId,MotionFrame a,MotionFrame b,float t)
    {
        if(a.Round!=w.Round||b.Round!=w.Round)return;
        RenderCosmetics(w,localId,Mathf.Lerp(a.Time,b.Time,t));
        foreach(var p in b.Players)
        {
            if(p.Id==localId||!Bodies.TryGetValue(p.Id,out var body))continue;
            var previous=a.Players.FirstOrDefault(x=>x.Id==p.Id);if(previous.Id!=p.Id)previous=p;
            var shownPosition=Art.V(previous.Position).Lerp(Art.V(p.Position),t);float yaw=Mathf.LerpAngle(previous.Yaw,p.Yaw,t);
            // Visual delay must not rewind the collision bodies used by local prediction.
            bool down=Mathf.Lerp(a.Time,b.Time,t)<p.DownUntil;body.Model.Position=shownPosition-body.Position+(down?new Vector3(0,.15f,.6f):Vector3.Zero);body.Model.Rotation=new(down?Mathf.Pi/2:0,yaw+Mathf.Pi,0);
            foreach(var h in w.Heads.Where(x=>x.Barber&&x.Owner==p.Id))if(heads.TryGetValue(h.Id,out var hair))
                {var barberRotation=new Vector3(down?Mathf.Pi/2:0,yaw+Mathf.Pi,0);hair.Position=shownPosition+(down?new Vector3(0,.15f,.6f)+Basis.FromEuler(barberRotation)*Vector3.Up*1.7f:Vector3.Up*1.7f)+(h.Facial?Basis.FromEuler(barberRotation)*Art.V(Head.FaceOffset(h.Region)):Vector3.Zero);hair.Rotation=barberRotation;}
            foreach(var tool in w.Tools.Where(x=>x.Holder==p.Id))if(tools.TryGetValue(tool.Id,out var view))
            {view.Position=shownPosition+new Vector3(.35f,1.1f,-.4f).Rotated(Vector3.Up,yaw);view.Rotation=new(Mathf.Lerp(previous.Pitch,p.Pitch,t),yaw,0);}
        }
        var headPosition=Art.V(a.Head.Position).Lerp(Art.V(b.Head.Position),t);var rotation=SmoothRotation(a.Head.Rotation,b.Head.Rotation,t)+Art.V(TwistCards.DisplayDelta(w,localId,Mathf.Lerp(a.Time,b.Time,t),(t>=1?b:a).ExpressionKind,(t>=1?b:a).ExpressionAt,(t>=1?b:a).Bracing));
        if(customers.TryGetValue(0,out var model))
        {
            model.Rotation=rotation;model.Position=headPosition-model.Basis*new Vector3(0,1.64f,0);
            foreach(var h in w.Heads.Where(x=>!x.Barber&&!x.Loose&&(x.Id==0||x.Facial)))if(heads.TryGetValue(h.Id,out var hair))
            {hair.Position=headPosition+(h.Facial?model.Basis*Art.V(Head.FaceOffset(h.Region)):Vector3.Zero);hair.Rotation=rotation;}
            if(braceHands!=null){braceHands.Position=headPosition;braceHands.Rotation=rotation;}
        }
        var cue=t>=1?b:a;
        ApplyTwistVisibility(w,localId);
        if(catView!=null&&b.Props.FirstOrDefault(p=>p.Id==w.Cat.PropId) is var catPose&&catPose.Id==w.Cat.PropId){var oldCat=a.Props.FirstOrDefault(p=>p.Id==catPose.Id);if(oldCat.Id!=catPose.Id)oldCat=catPose;catView.Position=Art.V(oldCat.Position).Lerp(Art.V(catPose.Position),t);catView.Rotation=SmoothRotation(oldCat.Rotation,catPose.Rotation,t);}
        if(w.Experiment.IsB)
        {
            if(flightCue!=null){flightCue.Visible=w.Props.FirstOrDefault(p=>p.Id==w.Experiment.HelicopterId)?.Attached==true;flightCue.Text=L.T(cue.Leave.ToString())+(PresentationSettings.Numbers?$" · {cue.StableSeconds:0.0}/3s":"");}
            if(reactionCue!=null){reactionCue.Position=headPosition+new Vector3(.72f,.8f,.12f);reactionCue.Visible=cue.Attention!=AttentionStage.Unaware;reactionCue.Text=cue.Attention==AttentionStage.Notice?"?":cue.Attention==AttentionStage.Commit?"!":L.Locale=="zh"?"往那边看":"LOOKING OVER";reactionCue.FontSize=cue.Attention==AttentionStage.React?22:64;}
            if(braceHands!=null)braceHands.Visible=cue.Bracing;
            if(attentionArrow!=null){attentionArrow.Visible=cue.Attention!=AttentionStage.Unaware;var direction=Art.V(cue.AttentionSource)-headPosition;if(direction.Length()<.01f)direction=Vector3.Right;direction=direction.Normalized();attentionArrow.Position=headPosition+Vector3.Up*.9f+direction*.45f;attentionArrow.Quaternion=new Quaternion(Vector3.Up,direction);}
            if(mirrorCue!=null)mirrorCue.Text=L.Locale=="zh"?(cue.MirrorBlocked?"顾客镜子 · 已遮挡":"顾客镜子 · 视线畅通"):(cue.MirrorBlocked?"CUSTOMER MIRROR · BLOCKED":"CUSTOMER MIRROR · CLEAR");
        }
        foreach(var prop in b.Props)
        {
            if(!props.TryGetValue($"{w.Round}:{prop.Id}",out var view))continue;
            var previous=a.Props.FirstOrDefault(x=>x.Id==prop.Id);if(previous.Id!=prop.Id)previous=prop;
            view.Position=Art.V(previous.Position).Lerp(Art.V(prop.Position),t);
            if(w.Props.FirstOrDefault(p=>p.Id==prop.Id) is {} actual)view.Position-=Vector3.Up*MaterialFeel.Sink(w,actual);
            bool failed=w.Props.FirstOrDefault(x=>x.Id==prop.Id)?.Failed??false;
            view.Rotation=SmoothRotation(previous.Rotation,prop.Rotation,t)+(failed?new Vector3(.45f,0,.7f):Vector3.Zero);
            if(prop.Id==w.Experiment.HelicopterId&&flightCue!=null)flightCue.Position=view.Position+Vector3.Up*.55f;
        }
    }
}
