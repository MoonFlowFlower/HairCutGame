using Godot;
using Hairball.Core;
using System;
namespace Hairball;
public partial class SalonView
{
    AudioStreamPlayer? placementSound,warningSound;
    MeshInstance3D? placementHalo;
    AudioStreamWav? putSound,settledSound,unstableSound;
    int momentRound=-1;
    float seenPlacement=-100,warningAt=-100;
    bool momentFinished;
    static AudioStreamWav Cue(float frequency,float seconds,bool rattle=false)
    {
        int n=(int)(seconds*16000);var data=new byte[n*2];var random=new Random(23);
        for(int i=0;i<n;i++){double t=i/16000.0;double f=frequency*(1+(rattle?.09*Math.Sin(t*50):.15*t));double v=(Math.Sin(t*2*Math.PI*f)*.8+(rattle?(random.NextDouble()*2-1)*.3:0))*Math.Exp(-t*8);short value=(short)(v*12000);data[i*2]=(byte)value;data[i*2+1]=(byte)(value>>8);}
        return new(){Data=data,MixRate=16000,Format=AudioStreamWav.FormatEnum.Format16Bits};
    }
    void SyncPlacementMoment(WorldState w,PropState prop)
    {
        if(placementSound==null){placementSound=new(){VolumeDb=-16};warningSound=new(){VolumeDb=-12};AddChild(placementSound);AddChild(warningSound);putSound=Cue(650,.5f);settledSound=Cue(1050,.25f);unstableSound=Cue(180,.4f,true);placementHalo=new(){Mesh=new TorusMesh{InnerRadius=.62f,OuterRadius=.70f,Rings=24,RingSegments=8},MaterialOverride=Art.Material(new("ffe7a0"),true)};AddChild(placementHalo);}
        bool fresh=prop.Attached&&(momentRound!=w.Round||seenPlacement!=prop.PlacedAt);
        if(fresh){momentRound=w.Round;seenPlacement=prop.PlacedAt;momentFinished=false;if(w.Time-prop.PlacedAt<.5f){placementSound.Stream=putSound;placementSound.Play();}}
        float age=w.Time-seenPlacement;
        placementHalo!.Visible=momentRound==w.Round&&age>=0&&age<=1;placementHalo.Position=Art.V(prop.Position)-Vector3.Up*.25f;placementHalo.Scale=Vector3.One*(1+MathF.Sin(age*15)*.08f);
        if(momentRound==w.Round&&!momentFinished&&age>=1){momentFinished=true;bool stable=prop.Attached&&(w.Experiment.LandingIssues&~LandingIssue.DwellIncomplete)==LandingIssue.None;placementSound.Stream=stable?settledSound:unstableSound;placementSound.Play();}
        if(w.Phase==Phase.Build&&w.Experiment.Tolerance>=70&&(w.Time-warningAt>5||w.Time<warningAt)){warningAt=w.Time;warningSound!.Stream=unstableSound;warningSound.Play();}
        if(momentRound==w.Round&&age<1.5f&&(w.Experiment.LandingIssues&~LandingIssue.DwellIncomplete)!=LandingIssue.None&&props.TryGetValue($"{w.Round}:{prop.Id}",out var view))view.Rotation+=new Vector3(MathF.Sin(age*32)*.06f,0,MathF.Cos(age*32)*.06f);
    }
}
