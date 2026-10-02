using Godot;
using Hairball.Core;
using System;
using System.Linq;
using System.Collections.Generic;
namespace Hairball;
public partial class ContactFeedback : Node3D
{
    readonly AudioStreamPlayer3D[] voices=new AudioStreamPlayer3D[4];
    readonly float[] last=new float[4];
    readonly Dictionary<HairState,AudioStreamWav> sounds=new();
    AudioStreamWav hard=null!,slap=null!;
    int seenEvent;
    public override void _Ready()
    {
        for(int i=0;i<4;i++){voices[i]=new(){MaxDistance=10,VolumeDb=-20};AddChild(voices[i]);last[i]=-100;}
        foreach(var state in Enum.GetValues<HairState>())
        {
            var data=new byte[1600];var random=new Random(47+(int)state);
            for(int i=0;i<800;i++){double t=i/8000.0;double frequency=state==HairState.Frozen?1750:state==HairState.Wet?150:state==HairState.Glued?310:state==HairState.Charred?750:460;
                double noise=random.NextDouble()*2-1;double v=(noise*.45+Math.Sin(t*Math.PI*2*frequency)*.55)*Math.Exp(-t*(state==HairState.Frozen?55:25));short pcm=(short)(v*11000);data[i*2]=(byte)pcm;data[i*2+1]=(byte)(pcm>>8);}
            sounds[state]=new(){Data=data,MixRate=8000,Format=AudioStreamWav.FormatEnum.Format16Bits};
        }
        var ring=new byte[2400];for(int i=0;i<1200;i++){double t=i/8000.0;short pcm=(short)(Math.Sin(t*2*Math.PI*2200)*Math.Exp(-t*32)*14000);ring[i*2]=(byte)pcm;ring[i*2+1]=(byte)(pcm>>8);}hard=new(){Data=ring,MixRate=8000,Format=AudioStreamWav.FormatEnum.Format16Bits};
        var smack=new byte[2400];var rng=new Random(83);for(int i=0;i<1200;i++){double t=i/16000.0;short pcm=(short)(((rng.NextDouble()*2-1)*.75+Math.Sin(t*Math.PI*2*140)*.25)*Math.Exp(-t*65)*15000);smack[i*2]=(byte)pcm;smack[i*2+1]=(byte)(pcm>>8);}slap=new(){Data=smack,MixRate=16000,Format=AudioStreamWav.FormatEnum.Format16Bits};
    }
    public void Sync(WorldState world,float time,IEnumerable<ToolState> tools)
    {
        foreach(var e in world.Events.Where(e=>e.Id>seenEvent)){
            seenEvent=e.Id;
            if(e.Text!="Customer slapped awake"||time-e.Time>.4f||world.Player(e.Source) is not {} actor)continue;
            var voice=voices[actor.Slot];voice.Position=Art.V(e.Position);voice.Stream=slap;voice.VolumeDb=-14;voice.PitchScale=1;voice.Play();
        }
        foreach(var tool in tools)
        {
            var player=world.Player(tool.Holder);if(player==null||!tool.HitHair||time-tool.LastUse>.15f||tool.LastUse-last[player.Slot]<.13f)continue;
            // Match the growth plume: contact without accepted material has no growth effect sound.
            if(world.Experiment.IsB&&tool.Definition==1&&tool.EffectMass<=.00001f)continue;
            var head=world.Heads.Where(h=>!h.Miniature).MinBy(h=>Math.Abs(h.Volume.Sample(h.ToLocal(tool.Contact))));
            float resistance=head==null?0:HairSystem.MaterialAt(head,head.ToLocal(tool.Contact)).Resistance;
            int slot=player.Slot;last[slot]=tool.LastUse;var voice=voices[slot];voice.Position=Art.V(tool.Contact);voice.Stream=resistance>=.65f&&tool.ContactState!=HairState.Frozen?hard:sounds[tool.ContactState];
            voice.VolumeDb=tool.EffectMass>.5f?-14:tool.EffectMass>.005f?-20:-29;voice.PitchScale=tool.EffectMass>.5f?.75f:1.1f;voice.Play();
        }
    }
}
