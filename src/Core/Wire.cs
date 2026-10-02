using System.IO;
using System.IO.Compression;
using System.Text.Json;
namespace Hairball.Core;
public static class Wire
{
    public static bool LegacyCompression;
    static readonly JsonSerializerOptions Options=new(){IncludeFields=true,IgnoreReadOnlyProperties=true};
    public static byte[] Encode<T>(T state,CompressionLevel level=CompressionLevel.Fastest)
    {
        using var output=new MemoryStream();
        if(!LegacyCompression)output.Write(new byte[]{72,66,82,1});
        using(Stream zip=LegacyCompression?new DeflateStream(output,CompressionLevel.Fastest,true):new BrotliStream(output,level,true))JsonSerializer.Serialize(zip,state,Options);
        return output.ToArray();
    }
    public static T Decode<T>(byte[] bytes)
    {
        using var input=new MemoryStream(bytes);
        bool brotli=bytes.Length>=4&&bytes[0]==72&&bytes[1]==66&&bytes[2]==82&&bytes[3]==1;if(brotli)input.Position=4;
        using Stream zip=brotli?new BrotliStream(input,CompressionMode.Decompress):new DeflateStream(input,CompressionMode.Decompress);
        using var bounded=new BoundedRead(zip,128*1024*1024);
        var value=JsonSerializer.Deserialize<T>(bounded,Options);
        if(value is null)throw new InvalidDataException("Null network state");
        return value;
    }
    sealed class BoundedRead(Stream source,long limit):Stream
    {
        long count;
        public override int Read(byte[] buffer,int offset,int size){int n=source.Read(buffer,offset,size);count+=n;if(count>limit)throw new InvalidDataException("Decoded payload exceeds limit");return n;}
        public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;
        public override long Length=>throw new System.NotSupportedException();public override long Position{get=>count;set=>throw new System.NotSupportedException();}
        public override void Flush(){}public override long Seek(long offset,SeekOrigin origin)=>throw new System.NotSupportedException();
        public override void SetLength(long value)=>throw new System.NotSupportedException();public override void Write(byte[] buffer,int offset,int count)=>throw new System.NotSupportedException();
    }
}
