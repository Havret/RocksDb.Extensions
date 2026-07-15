using RocksDbSharp;

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
    /// Column family names are case-sensitive, matching RocksDB's behavior.
    /// </summary>
    internal Dictionary<string, MergeOperatorConfig> MergeOperators { get; } = new();

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

    /// <summary>
    /// The size, in bytes, of the data blocks used by the block-based table format.
    /// </summary>
    /// <remarks>
    /// RocksDB groups key/value pairs into blocks before writing them to SST files, and each block is
    /// compressed and read from disk as a single unit. Smaller blocks reduce the amount of data that
    /// must be read and decompressed to serve a single point lookup, which lowers read amplification
    /// and can improve point-lookup latency, but they increase the size of the index (since there are
    /// more blocks to index) and add per-block overhead, which increases memory usage and space
    /// amplification. Larger blocks do the opposite: they improve compression ratios and reduce index
    /// size, but each read pulls in more unrelated data.
    /// <para>
    /// The default value is <c>4096</c> (4 KiB).
    /// </para>
    /// </remarks>
    public long BlockSize { get; set; } = 4096L;

    /// <summary>
    /// Determines whether index and filter blocks are placed in the same block cache as data blocks.
    /// </summary>
    /// <remarks>
    /// By default, RocksDB keeps index and filter blocks outside of the block cache, in memory that is
    /// not accounted for or bounded by the cache's configured capacity. This is fine for small databases,
    /// but as the dataset grows, the memory used by index and filter blocks grows with it and can lead to
    /// unbounded memory usage. Enabling this setting moves index and filter blocks into the block cache
    /// so that all of RocksDB's block-based memory usage is tracked and limited by a single cache budget,
    /// at the cost of index and filter blocks competing with data blocks for cache space.
    /// <para>
    /// The default value is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool CacheIndexAndFilterBlocks { get; set; } = true;

    /// <summary>
    /// The size, in bytes, of the LRU block cache shared by all column families.
    /// </summary>
    /// <remarks>
    /// RocksDB uses the block cache to hold uncompressed data blocks (and, when
    /// <see cref="CacheIndexAndFilterBlocks"/> is enabled, index and filter blocks too) in memory so that
    /// repeated reads don't have to hit disk. A larger cache increases the hit rate and improves read
    /// performance at the cost of higher memory usage; a smaller cache reduces memory usage but leads to
    /// more disk reads and higher read latency.
    /// <para>
    /// The default value is <c>67108864</c> (64 MiB).
    /// </para>
    /// </remarks>
    public ulong BlockCacheSize { get; set; } = 64 * 1024 * 1024L;

    /// <summary>
    /// A delegate that is invoked with the <see cref="DbOptions"/> instance right after it has been
    /// configured with the library defaults, allowing callers to override or extend them directly.
    /// </summary>
    /// <remarks>
    /// This is an advanced, low-level escape hatch intended for scenarios not covered by the other
    /// options on this class. Settings applied by RocksDb.Extensions before this delegate runs may be
    /// overwritten. The API surface of <see cref="DbOptions"/> is not guaranteed to be stable across
    /// RocksDbSharp versions.
    /// </remarks>
    public Action<DbOptions>? ConfigureDbOptions { get; set; }

    /// <summary>
    /// A delegate that is invoked with the <see cref="BlockBasedTableOptions"/> instance right after it
    /// has been configured with the library defaults, allowing callers to override or extend them directly.
    /// </summary>
    /// <remarks>
    /// This is an advanced, low-level escape hatch intended for scenarios not covered by the other
    /// options on this class. Settings applied by RocksDb.Extensions before this delegate runs may be
    /// overwritten. The API surface of <see cref="BlockBasedTableOptions"/> is not guaranteed to be
    /// stable across RocksDbSharp versions.
    /// </remarks>
    public Action<BlockBasedTableOptions>? ConfigureBlockBasedTableOptions { get; set; }

    /// <summary>
    /// A delegate that is invoked with the <see cref="ColumnFamilyOptions"/> instance for every column
    /// family right after it has been configured with the library defaults, allowing callers to override
    /// or extend them directly.
    /// </summary>
    /// <remarks>
    /// This is an advanced, low-level escape hatch intended for scenarios not covered by the other
    /// options on this class. Settings applied by RocksDb.Extensions before this delegate runs may be
    /// overwritten, and calling <c>SetMergeOperator</c> here will replace any merge operator registered
    /// via <c>AddMergeableStore</c>. The API surface of <see cref="ColumnFamilyOptions"/> is not
    /// guaranteed to be stable across RocksDbSharp versions.
    /// </remarks>
    public Action<ColumnFamilyOptions>? ConfigureColumnFamilyOptions { get; set; }
}