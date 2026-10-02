using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
namespace Hairball.Core;
public sealed record PhotoAsset(string Key,string Hash);
public sealed record PhotoMetadata(int Round,bool Success,int Goal,CurtainReaction Reaction,ActionFact[] Actions,string CapturedUtc,string ImageHash="",PhotoAsset[]? Assets=null,HairTrace[]? Hair=null,string Match="",TargetCard? Target=null,TargetCard? Family=null);
public sealed record GalleryEntry(string Image,PhotoMetadata Metadata);
public static class GalleryStore
{
    public const int Limit=24;
    public static GalleryEntry[] Load(string folder)
    {
        var entries=new List<GalleryEntry>();
        try{if(!Directory.Exists(folder))return [];
            foreach(var file in Directory.EnumerateFiles(folder,"photo-*.json").OrderByDescending(File.GetLastWriteTimeUtc).Take(96))
            {try{if(new FileInfo(file).Length>128000)continue;var m=JsonSerializer.Deserialize<PhotoMetadata>(File.ReadAllText(file));var png=Path.ChangeExtension(file,".png");if(m!=null&&m.Actions!=null&&Valid(png,m.ImageHash)&&(m.Assets==null||m.Assets.Length<=6&&m.Assets.All(a=>a!=null)&&m.Assets.Select(a=>a.Key).Distinct().Count()==m.Assets.Length&&m.Assets.All(a=>SafeKey(a.Key)&&Valid(AssetPath(png,a.Key),a.Hash))))entries.Add(new(png,m));}catch(IOException){}catch(JsonException){}catch(UnauthorizedAccessException){}}
        }catch(IOException){}catch(UnauthorizedAccessException){}
        return entries.Take(Limit).ToArray();
    }
    static bool SafeKey(string? key)=>key is "before" or "group"||key!=null&&key.Length==8&&key.StartsWith("avatar-")&&key[7] is >= '0' and <= '3';
    static bool Valid(string path,string hash)=>File.Exists(path)&&new FileInfo(path).Length<=8000000&&hash==Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    public static string AssetPath(string primary,string key){if(!SafeKey(key))throw new ArgumentException("Invalid photo asset");return Path.ChangeExtension(primary,null)+"--"+key+".png";}
    public static void Save(string folder,string id,byte[] png,PhotoMetadata data,IReadOnlyDictionary<string,byte[]>? assets=null)
    {
        if(id.Length>80||id.Any(c=>!char.IsAsciiLetterOrDigit(c)&&c!='-'))throw new ArgumentException("Invalid gallery id");
        if(assets!=null&&(assets.Count>6||assets.Keys.Any(k=>!SafeKey(k))))throw new ArgumentException("Invalid gallery assets");
        Directory.CreateDirectory(folder);string file=Path.Combine(folder,"photo-"+id);
        File.WriteAllBytes(file+".png",png);
        if(assets!=null)foreach(var asset in assets)File.WriteAllBytes(AssetPath(file+".png",asset.Key),asset.Value);
        File.WriteAllText(file+".json",JsonSerializer.Serialize(data with{ImageHash=Convert.ToHexString(SHA256.HashData(png)),Assets=assets?.Select(a=>new PhotoAsset(a.Key,Convert.ToHexString(SHA256.HashData(a.Value)))).ToArray()}));
        foreach(var extra in Directory.EnumerateFiles(folder,"photo-*.json").OrderByDescending(File.GetLastWriteTimeUtc).Skip(Limit))
        {string primary=Path.ChangeExtension(extra,".png");foreach(var key in new[]{"before","group","avatar-0","avatar-1","avatar-2","avatar-3"})File.Delete(AssetPath(primary,key));File.Delete(primary);File.Delete(extra);}
    }
}

