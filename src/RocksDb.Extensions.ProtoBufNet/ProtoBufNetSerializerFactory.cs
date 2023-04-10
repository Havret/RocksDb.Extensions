using ProtoBuf.Meta;

namespace RocksDb.Extensions.ProtoBufNet;

/// <summary>
/// A factory class for creating ProtoBuf-Net serializers. 
/// </summary>
public class ProtoBufNetSerializerFactory : ISerializerFactory
{
    /// <inheritdoc/>
    public bool CanCreateSerializer<T>()
    {
        // Checks if ProtoBuf-Net can serialize the specified type.
        return RuntimeTypeModel.Default.CanSerialize(typeof(T));
    }

    /// <inheritdoc/>
    public ISerializer<T> CreateSerializer<T>()
    {
        // Creates an instance of the ProtoBufNetSerializer with the specified type.
        var type = typeof(ProtoBufNetSerializer<>).MakeGenericType(typeof(T));
        return (ISerializer<T>)Activator.CreateInstance(type);
    }
}