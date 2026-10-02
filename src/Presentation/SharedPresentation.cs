using Godot;
using Hairball.Core;
using System;
using System.Linq;
namespace Hairball;
public partial class SalonView
{
    Label3D? reactionCue;
    AudioStreamPlayer3D? rotorSound,inhaleSound;
    float heardReaction=-100;
    readonly System.Collections.Generic.List<MeshInstance3D> spitDrops=new();
    void CustomerAppearance(Node3D model,string profile,bool anticipating)
    {
        var llama=model.GetNodeOrNull<Node3D>("LlamaFeatures");
        if(llama==null)
        {
            llama=new(){Name="LlamaFeatures"};model.AddChild(llama);
            Art.Ball(llama,new(0,1.56f,.48f),new(.46f,.34f,.40f),new("d4bfa3")).Name="Muzzle";
            Art.Box(llama,new(0,1.58f,.68f),new(.16f,.08f,.03f),new("51493e"));
            for(int side=-1;side<=1;side+=2){var ear=Art.Ball(llama,new(side*.55f,2.48f,-.08f),new(.19f,.8f,.2f),new("d4bfa3"));ear.RotationDegrees=new(0,0,-side*15);}
        }
        llama.Visible=profile=="llama";
        llama.GetNode<Node3D>("Muzzle").Scale=anticipating?new(.59f,.40f,.43f):new(.46f,.34f,.40f);
    }
    static AudioStreamWav Sound(bool rotor)
    {
        const int rate=8000;int count=rotor?8000:4000;var data=new byte[count*2];var rng=new Random(44);
        for(int i=0;i<count;i++)
        {
            double t=(double)i/rate,noise=rng.NextDouble()*2-1;
            double value=rotor?(.17*Math.Sin(t*Math.PI*2*96)+noise*.16)*(.55+.45*Math.Cos(t*Math.PI*2*18)):noise*.16*Math.Sin(Math.PI*i/count);
            short pcm=(short)(value*16000);data[i*2]=(byte)pcm;data[i*2+1]=(byte)(pcm>>8);
        }
        return new(){Data=data,Format=AudioStreamWav.FormatEnum.Format16Bits,MixRate=rate,LoopMode=rotor?AudioStreamWav.LoopModeEnum.Forward:AudioStreamWav.LoopModeEnum.Disabled,LoopEnd=count};
    }
    public static void SpinRotor(Node3D heli,float time,float speed)
    {if(heli.GetNodeOrNull<Node3D>("Rotor") is {} rotor)rotor.Rotation=new(0,time*speed,0);}
    void SyncSharedPresentation(WorldState w,ReplayFrame? replay)
    {
        if(w.Customers.Count==0)return;
        if(rotorSound==null){rotorSound=new(){Stream=Sound(true),VolumeDb=-15,MaxDistance=20};AddChild(rotorSound);inhaleSound=new(){Stream=Sound(false),VolumeDb=-9,MaxDistance=12};AddChild(inhaleSound);}
        var head=w.SharedHead;
        if(reactionCue==null){reactionCue=Art.Label(this,"",Vector3.Zero,25,new("ffce81"));reactionCue.Name="CustomerAnticipation";}
        var c=(replay?.Customers??w.Customers)[0];float time=replay?.Time??w.Time;
        bool rotorActive=replay==null&&w.Job.Goal==0&&w.Props.Any(p=>p.Goal==0)&&(w.Phase==Phase.Validation||w.Phase==Phase.Build&&(!w.Experiment.IsB&&w.Remaining<15));
        if(w.Experiment.IsB)rotorSound.VolumeDb=w.Experiment.Flight==FlightStage.Staging?-25:w.Experiment.Flight==FlightStage.Circling?-21:-12;
        rotorSound.Position=Art.V(w.Props.FirstOrDefault(p=>p.Goal==0)?.Position??head.Position);
        if(rotorActive&&!rotorSound.Playing)rotorSound.Play();else if(!rotorActive&&rotorSound.Playing)rotorSound.Stop();
        if(replay==null&&CustomerMotion.Anticipating(c,time)&&heardReaction!=c.ReactionStart){heardReaction=c.ReactionStart;inhaleSound!.Position=Art.V(head.Position);inhaleSound.Play();}
        reactionCue.Visible=CustomerMotion.Anticipating(c,time);
        reactionCue.Text=L.T(c.Action==CustomerReaction.Spit?"* cheeks puff *":c.Action==CustomerReaction.Duck?"* inhale *":c.Action==CustomerReaction.Turn?"Huh?!":"Whoa!");
        reactionCue.Position=Art.V(head.Position)+new Vector3(.62f,.15f,.2f);
        if(w.Experiment.IsB)
        {
            var attention=(replay?.Experiment??w.Experiment).Attention;
            reactionCue.Visible=attention.Stage!=AttentionStage.Unaware;
            reactionCue.Text=attention.Stage==AttentionStage.Notice?"?":attention.Stage==AttentionStage.Commit?"!":L.Locale=="zh"?"往那边看":"LOOKING OVER";
            reactionCue.FontSize=attention.Stage==AttentionStage.React?22:64;
            reactionCue.Position=Art.V(head.Position)+new Vector3(.72f,.8f,.12f);
            if(replay==null&&attention.Stage==AttentionStage.Notice&&heardReaction!=attention.Started){heardReaction=attention.Started;inhaleSound!.Position=Art.V(head.Position);inhaleSound.Play();}
        }
        if(spitDrops.Count==0)for(int i=0;i<6;i++)spitDrops.Add(Art.Ball(this,Vector3.Zero,new(.07f,.045f,.09f),new("b9d99b")));
        float age=time-c.ReactionStart-CustomerMotion.Anticipation;
        for(int i=0;i<spitDrops.Count;i++)
        {
            var drop=spitDrops[i];float t=age-i*.025f;
            drop.Visible=c.Action==CustomerReaction.Spit&&t is >=0 and <.48f;
            drop.Position=Art.V(head.Position+c.SignatureDirection*(.5f+t*5))+new Vector3((i-2.5f)*.025f,-t*t*2,0);
        }
    }
}
