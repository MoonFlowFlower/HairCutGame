using Godot;
using Hairball.Core;
using System;
namespace Hairball;
public partial class Main
{
    double nextBodySound,bodySoundAt=-10,nextBodyChunk;
    void TickBodySounds()
    {
        if(!running||!voicePanel.Enabled||world.Experiment.CustomerActor!=0||world.Phase!=Phase.Build)return;
        bool asleep=world.Experiment.Drowsiness>.25f;
        if(NetNow>=nextBodySound){bodySoundAt=NetNow;nextBodyChunk=NetNow;nextBodySound=NetNow+(asleep?2.8:2);}
        if(NetNow-bodySoundAt>.55)return;
        if(NetNow-nextBodyChunk>.12)nextBodyChunk=NetNow;
        if(NetNow<nextBodyChunk)return;float offset=(float)(nextBodyChunk-bodySoundAt);nextBodyChunk+=.02;
        var pcm=new float[960];for(int i=0;i<pcm.Length;i++){float t=offset+i/48000f,envelope=Math.Max(0,MathF.Sin(MathF.PI*t/.55f));float heart=(MathF.Exp(-t*65)+MathF.Exp(-Math.Abs(t-.18f)*65))*MathF.Sin(MathF.Tau*65*t);float breath=MathF.Sin(MathF.Tau*(asleep?85:140)*t)*.6f+MathF.Sin(MathF.Tau*2197*t)*.12f;pcm[i]=envelope*(heart*.015f+breath*(asleep?.03f:.006f));}
        var speaker=Speaker(0);PushVoice(speaker,pcm);speaker.Until=NetNow+.03;
    }
}
