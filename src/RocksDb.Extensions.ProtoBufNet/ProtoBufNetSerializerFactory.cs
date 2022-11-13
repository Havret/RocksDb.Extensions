using System;
using ProtoBuf.Meta;

namespace RocksDb.Extensions.ProtoBufNet;

public class ProtoBufNetSerializerFactory : ISerializerFactory
{
    public bool CanCreateSerializer<T>()
    {
        return RuntimeTypeModel.Default.CanSerialize(typeof(T));
    }

    public ISerializer<T> CreateSerializer<T>()
    {
        var type = typeof(ProtoBufNetSerializer<>).MakeGenericType(typeof(T));
        return (ISerializer<T>) Activator.CreateInstance(type);
    }
}
