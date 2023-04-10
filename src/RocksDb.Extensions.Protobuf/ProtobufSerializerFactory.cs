using Google.Protobuf;

namespace RocksDb.Extensions.Protobuf;

/// <summary>
/// A factory class for creating Google.Protobuf serializers. 
/// </summary>
public class ProtobufSerializerFactory : ISerializerFactory
{
    /// <inheritdoc/>
    public bool CanCreateSerializer<T>()
    {
        var type = typeof(T);
        return typeof(IMessage).IsAssignableFrom(type);
    }

    /// <inheritdoc/>
    public ISerializer<T> CreateSerializer<T>()
    {
        var type = typeof(ProtobufSerializer<>).MakeGenericType(typeof(T));
        return (ISerializer<T>)Activator.CreateInstance(type);
    }
}
