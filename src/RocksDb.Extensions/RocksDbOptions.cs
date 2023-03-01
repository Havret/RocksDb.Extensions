namespace RocksDb.Extensions;

/// <summary>
///  Represents the configuration options for a RocksDB instance.
/// </summary>
public class RocksDbOptions
{
    /// <summary>
    /// A string that represents the path to the RocksDB instance on disk.
    /// </summary>
    public string Path { get; set; } = null!;

    /// <summary>
    /// A list of <see cref="ISerializerFactory"/> instances that are used to serialize and deserialize data in the RocksDB instance.
    /// </summary>
    /// <remarks>
    /// At least one serializer factory must be registered before using the RocksDB instance.
    /// If no factory can create a serializer for any type defined by any of the registered stores,
    /// an <see cref="InvalidOperationException"/> will be thrown.
    /// </remarks>
    public List<ISerializerFactory> SerializerFactories { get; } = new();

    internal List<string> ColumnFamilies { get; } = new();
}