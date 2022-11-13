using System.Runtime.Serialization;
using ProtoBuf;

namespace RocksDb.Extensions.Tests.Protos;

[DataContract]
public class ProtoNetCacheKey
{
    [ProtoMember(1)]
    public int Id { get; set; }
}
