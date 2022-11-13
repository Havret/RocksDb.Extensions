using System;
using Google.Protobuf;

namespace RocksDb.Extensions.Protobuf;

public class ProtobufSerializerFactory : ISerializerFactory
{
    public bool CanCreateSerializer<T>()
    {
        var type = typeof(T);
        return typeof(IMessage).IsAssignableFrom(type);
    }

    public ISerializer<T> CreateSerializer<T>()
    {
        var type = typeof(ProtobufSerializer<>).MakeGenericType(typeof(T));
        return (ISerializer<T>) Activator.CreateInstance(type);
    }
}
