using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Hairball.Core;

public enum ConnectionHealth { Normal, Unstable, Waiting, Reconnecting, Synchronizing, Failed, Leaving }

// Separate monotonic wall time from game time: a paused world must still recover.
public sealed class RecoveryWindow
{
    public double Deadline { get; private set; }
    public double HealthySince { get; private set; } = -10;
    public bool Paused { get; private set; }
    public bool Open => Deadline > 0;
    public bool Begin(double now)
    {
        if(Open)return false;
        Deadline=now+30;Paused=now-HealthySince>=10;return true;
    }
    public void Continue()=>Paused=false;
    public void Finish(double now){Deadline=0;Paused=false;HealthySince=now;}
    public bool Expired(double now)=>Open&&now>=Deadline;
}

public sealed class ChunkSend
{
    public const int Payload=900, Window=32, MaxBytes=8*1024*1024;
    public readonly long Id;
    public readonly int Kind, Revision;
    public readonly byte[] Data, Hash;
    readonly HashSet<int> sent=new(),ack=new();
    readonly Dictionary<int,double> lastSent=new();
    int next;
    public int Count=>(Data.Length+Payload-1)/Payload;
    public int Outstanding=>sent.Count-ack.Count;
    public bool Received=>ack.Count==Count;
    public ChunkSend(long id,int kind,int revision,byte[] data)
    {
        if(data.Length==0||data.Length>MaxBytes)throw new ArgumentOutOfRangeException(nameof(data));
        Id=id;Kind=kind;Revision=revision;Data=data;Hash=SHA256.HashData(data);
    }
    public IEnumerable<(int Index,byte[] Data)> Pump(int budget=32,double now=0,double retrySeconds=.6)
    {
        // Independent packets: a missing chunk must not hold later chunks behind an ENet reliable queue.
        foreach(int index in sent.Where(i=>!ack.Contains(i)&&now-lastSent[i]>=retrySeconds).OrderBy(i=>lastSent[i]).ToArray())
        {
            if(budget--<=0)yield break;
            lastSent[index]=now;yield return(index,Data.AsSpan(index*Payload,Math.Min(Payload,Data.Length-index*Payload)).ToArray());
        }
        while(next<Count&&Outstanding<Window&&budget-->0)
        {int index=next++;sent.Add(index);lastSent[index]=now;yield return(index,Data.AsSpan(index*Payload,Math.Min(Payload,Data.Length-index*Payload)).ToArray());}
    }
    public bool Ack(int index)=>sent.Contains(index)&&ack.Add(index);
    public void AckPrefix(int count){for(int i=0;i<Math.Min(count,next);i++)ack.Add(i);}
}

public sealed class ChunkReceive
{
    public readonly long Id;
    public readonly int Kind,Revision,Length;
    public readonly byte[] Hash;
    readonly byte[] data;
    readonly bool[] seen;
    int received;
    public bool Complete=>received==seen.Length;
    public int Received=>received;
    public ChunkReceive(long id,int kind,int revision,int length,byte[] hash)
    {
        if(id<=0||kind is <0 or >1||length<=0||length>ChunkSend.MaxBytes||hash.Length!=32)throw new ArgumentException("Invalid transfer header");
        Id=id;Kind=kind;Revision=revision;Length=length;Hash=hash.ToArray();data=new byte[length];seen=new bool[(length+899)/900];
    }
    public bool Add(int index,byte[] bytes)
    {
        if(index<0||index>=seen.Length||bytes.Length!=Math.Min(900,Length-index*900))throw new ArgumentException("Invalid chunk");
        if(seen[index])return false;
        bytes.CopyTo(data,index*900);seen[index]=true;received++;return true;
    }
    public byte[] Finish()
    {
        if(!Complete||!CryptographicOperations.FixedTimeEquals(Hash,SHA256.HashData(data)))throw new ArgumentException("Incomplete/corrupt transfer");
        return data;
    }
    public int[] AckWindow()
    {
        int prefix=0;while(prefix<seen.Length&&seen[prefix])prefix++;
        return new[]{-prefix-1}.Concat(Enumerable.Range(prefix,Math.Min(32,seen.Length-prefix)).Where(i=>seen[i])).ToArray();
    }
}

// Tokens never appear in the serialized world or diagnostic records.
public sealed class RecoveryIdentity
{
    public int Actor,Peer,Epoch,Attempt;
    public string Token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public bool Matches(string token)=>token.Length==Token.Length&&CryptographicOperations.FixedTimeEquals(System.Text.Encoding.ASCII.GetBytes(token),System.Text.Encoding.ASCII.GetBytes(Token));
    public string ResumeError(string room,string requestedRoom,string token,double now,double deadline,bool livePeer,int attempt=int.MaxValue)
    {
        if(room!=requestedRoom||!Matches(token)||deadline>0&&now>=deadline)return "expired";
        if(attempt<=Attempt)return "stale_attempt";
        return livePeer?"duplicate":"";
    }
}
