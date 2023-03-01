using System.Runtime.Serialization;
using ProtoBuf;

namespace RocksDb.Extensions.Tests.Protos;

[DataContract]
public class ProtoNetCacheValue
{
    [ProtoMember(1)]
    public int Id { get; set; }

    [ProtoMember(2)]
    public string Value { get; set; } = null!;
}
