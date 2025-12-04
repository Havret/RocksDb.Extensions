using Microsoft.Extensions.Options;
using RocksDbSharp;

namespace RocksDb.Extensions;

internal class RocksDbContext : IDisposable
{
    private readonly WriteOptions _writeOptions;
    private readonly Dictionary<string, MergeOperatorConfig> _mergeOperators;
    private readonly Cache _cache;

    private const long BlockCacheSize = 50 * 1024 * 1024L;
    private const long BlockSize = 4096L;
    private const long WriteBufferSize = 16 * 1024 * 1024L;
    private const int MaxWriteBuffers = 3;

    public RocksDbContext(IOptions<RocksDbOptions> options)
    {
        var dbOptions = new DbOptions();
        _cache = Cache.CreateLru(BlockCacheSize);

        // this is the recommended way to increase parallelism in RocksDb
        // note that the current implementation of setIncreaseParallelism affects the number
        // of compaction threads but not flush threads (the latter remains one). Also,
        // the parallelism value needs to be at least two because of the code in
        // https://github.com/facebook/rocksdb/blob/62ad0a9b19f0be4cefa70b6b32876e764b7f3c11/util/options.cc#L580
        // subtracts one from the value passed to determine the number of compaction threads
        // (this could be a bug in the RocksDB code and their devs have been contacted).
        dbOptions.IncreaseParallelism(Math.Max(Environment.ProcessorCount, 2));
        dbOptions.SetCreateIfMissing();
        dbOptions.SetCreateMissingColumnFamilies();
        dbOptions.SetErrorIfExists(false);
        dbOptions.SetInfoLogLevel(InfoLogLevel.Error);
        dbOptions.SetUseDirectReads(options.Value.UseDirectReads);
        dbOptions.SetUseDirectIoForFlushAndCompaction(options.Value.UseDirectIoForFlushAndCompaction);
        dbOptions.EnableStatistics();
        dbOptions.SetMaxWriteBufferNumber(MaxWriteBuffers);
        dbOptions.SetWriteBufferSize(WriteBufferSize);
        dbOptions.SetCompression(Compression.No);
        dbOptions.SetCompactionStyle(Compaction.Universal);
        
        var tableConfig = new BlockBasedTableOptions();
        tableConfig.SetBlockCache(_cache);
        tableConfig.SetBlockSize(BlockSize);
        
        var filter = BloomFilterPolicy.Create();
        tableConfig.SetFilterPolicy(filter);
        
        dbOptions.SetBlockBasedTableFactory(tableConfig);
        
        _writeOptions = new WriteOptions();
        _writeOptions.DisableWal(1);

        _mergeOperators = options.Value.MergeOperators;
        
        var columnFamilies = CreateColumnFamilies(options.Value.ColumnFamilies);

        if (options.Value.DeleteExistingDatabaseOnStartup)
        {
            DestroyDatabase(options.Value.Path);
        }

        Db = RocksDbSharp.RocksDb.Open(dbOptions, options.Value.Path, columnFamilies);
    }

    private static void DestroyDatabase(string path)
    {
        var dbOptions = new DbOptions();
        Native.Instance.rocksdb_destroy_db(dbOptions.Handle, path);
    }

    public RocksDbSharp.RocksDb Db { get; }

    public WriteOptions WriteOptions => _writeOptions;

    public ColumnFamilyOptions CreateColumnFamilyOptions(string columnFamilyName)
    {
        var cfOptions = new ColumnFamilyOptions();
        if (_mergeOperators.TryGetValue(columnFamilyName, out var mergeOperatorConfig))
        {
            var mergeOp = global::RocksDbSharp.MergeOperators.Create(
                mergeOperatorConfig.Name,
                mergeOperatorConfig.PartialMerge,
                mergeOperatorConfig.FullMerge);

            cfOptions.SetMergeOperator(mergeOp);
        }

        return cfOptions;
    }


    private ColumnFamilies CreateColumnFamilies(IReadOnlyList<string> columnFamilyNames)
    {
        var columnFamilies = new ColumnFamilies();
        foreach (var columnFamilyName in columnFamilyNames)
        {
            var columnFamilyOptions = CreateColumnFamilyOptions(columnFamilyName);
            columnFamilies.Add(columnFamilyName, columnFamilyOptions);
        }

        return columnFamilies;
    }

    public void Dispose()
    {
        Db.Dispose();
    }
}
