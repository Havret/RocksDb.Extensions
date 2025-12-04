using System.Runtime.Serialization;
using ProtoBuf;

namespace RocksDb.Extensions.Tests.Protos;

[DataContract]
public class ProtoNetTag
{
    [ProtoMember(1)]
    public int Id { get; set; }

    [ProtoMember(2)]
    public string Name { get; set; } = null!;

    public override bool Equals(object? obj)
    {
        return obj is ProtoNetTag tag && Id == tag.Id && Name == tag.Name;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name);
    }
}

