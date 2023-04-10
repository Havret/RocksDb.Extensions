namespace RocksDb.Extensions;

/// <summary>
/// Defines an interface for a serializer factory that can create serializers for a given type.
/// </summary>
public interface ISerializerFactory
{
    /// <summary>
    /// Determines whether this factory can create a serializer for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to create a serializer for.</typeparam>
    /// <returns><c>true</c> if this factory can create a serializer for the specified type; otherwise, <c>false</c>.</returns>
    bool CanCreateSerializer<T>();

    /// <summary>
    /// Creates a serializer for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to create a serializer for.</typeparam>
    /// <returns>A serializer for the specified type.</returns>
    ISerializer<T> CreateSerializer<T>();
}
