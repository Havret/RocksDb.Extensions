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
    /// Indicates whether the existing RocksDB database should be automatically deleted on start-up.
    /// </summary>
    /// <remarks>
    /// If true, the library will automatically delete the existing database on start-up.
    /// This can be useful when testing and debugging, as it eliminates
    /// the need to manually delete the database before running the code again.
    /// The default value is false.
    /// </remarks>
    public bool DeleteExistingDatabaseOnStartup { get; set; }

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

    /// <summary>
    /// Internal dictionary of merge operators per column family.
    /// </summary>
    internal Dictionary<string, MergeOperatorConfig> MergeOperators { get; } = new(StringComparer.InvariantCultureIgnoreCase);

    /// <summary>
    /// Enables direct I/O mode for reads, which bypasses the OS page cache.
    /// </summary>
    /// <remarks>
    /// When enabled, RocksDB will open files in “direct I/O” mode, meaning data read from disk will not be cached or buffered
    /// by the operating system. This can reduce double-buffering and improve performance in specific scenarios,
    /// though actual impact depends on the hardware and access patterns.
    /// <para>
    /// Note that memory-mapped files are not affected by this setting. Hardware buffers may still be used.
    /// </para>
    /// The default value is <c>false</c>.
    /// </remarks>
    public bool UseDirectReads { get; set; } = false;

    
    /// <summary>
    /// Enables direct I/O mode for flush and compaction operations.
    /// </summary>
    /// <remarks>
    /// When enabled, RocksDB will open files in “direct I/O” mode during flush and compaction.
    /// This means that data written to disk will bypass the OS page cache, avoiding extra buffering by the operating system.
    /// This can potentially improve performance in some scenarios, especially when double-buffering is a concern.
    /// <para>
    /// Memory-mapped files are not impacted by this setting. Hardware-level buffering may still apply.
    /// </para>
    /// The default value is <c>false</c>.
    /// </remarks>
    public bool UseDirectIoForFlushAndCompaction { get; set; } = false;

    /// <summary>
    /// Indicates whether the flush operation should be completed before continuing.
    /// </summary>
    /// <remarks>
    /// When set to <c>true</c>, the database will wait for the flush operation to finish before continuing.
    /// This helps ensure data durability and consistency, but may slightly impact performance.
    /// <para>
    /// The default value is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool WaitForFlush { get; set; } = true;
}