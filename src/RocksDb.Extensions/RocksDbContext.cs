using Microsoft.Extensions.Options;
using RocksDbSharp;

namespace RocksDb.Extensions;

internal class RocksDbContext : IDisposable
{
    private readonly RocksDbSharp.RocksDb _rocksDb;
    private readonly Cache _cache;
    private readonly ColumnFamilyOptions _userSpecifiedOptions;

    private const long BlockCacheSize = 50 * 1024 * 1024L;
    private const long BlockSize = 4096L;
    private const long WriteBufferSize = 16 * 1024 * 1024L;
    private const int MaxWriteBuffers = 3;

    public RocksDbContext(IOptions<RocksDbOptions> options)
    {
        var dbOptions = new DbOptions();
        _userSpecifiedOptions = new ColumnFamilyOptions();
        var tableConfig = new BlockBasedTableOptions();
        _cache = Cache.CreateLru(BlockCacheSize);
        tableConfig.SetBlockCache(_cache);
        tableConfig.SetBlockSize(BlockSize);

        var filter = BloomFilterPolicy.Create();
        tableConfig.SetFilterPolicy(filter);
        _userSpecifiedOptions.SetBlockBasedTableFactory(tableConfig);
        _userSpecifiedOptions.SetWriteBufferSize(WriteBufferSize);
        _userSpecifiedOptions.SetCompression(Compression.No);
        _userSpecifiedOptions.SetCompactionStyle(Compaction.Universal);
        _userSpecifiedOptions.SetMaxWriteBufferNumber(MaxWriteBuffers);
        _userSpecifiedOptions.SetCreateIfMissing();
        _userSpecifiedOptions.SetCreateMissingColumnFamilies();
        _userSpecifiedOptions.SetErrorIfExists(false);
        _userSpecifiedOptions.SetInfoLogLevel(InfoLogLevel.Error);

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
        dbOptions.SetUseDirectReads(options.Value.UseDirectReads);
        dbOptions.SetUseDirectIoForFlushAndCompaction(options.Value.UseDirectIoForFlushAndCompaction);

        var fOptions = new FlushOptions();
        fOptions.SetWaitForFlush(options.Value.WaitForFlush);

        var writeOptions = new WriteOptions();
        writeOptions.DisableWal(1);

        _userSpecifiedOptions.EnableStatistics();

        var columnFamilies = CreateColumnFamilies(options.Value.ColumnFamilies, options.Value.MergeOperators, _userSpecifiedOptions);

        if (options.Value.DeleteExistingDatabaseOnStartup)
        {
            DestroyDatabase(options.Value.Path);
        }

        _rocksDb = RocksDbSharp.RocksDb.Open(dbOptions, options.Value.Path, columnFamilies);
    }

    private static void DestroyDatabase(string path)
    {
        var dbOptions = new DbOptions();
        Native.Instance.rocksdb_destroy_db(dbOptions.Handle, path);
    }

    public RocksDbSharp.RocksDb Db => _rocksDb;

    public ColumnFamilyOptions ColumnFamilyOptions => _userSpecifiedOptions;

    private ColumnFamilies CreateColumnFamilies(
        IReadOnlyList<string> columnFamilyNames,
        IReadOnlyDictionary<string, MergeOperatorConfig> mergeOperators,
        ColumnFamilyOptions defaultColumnFamilyOptions)
    {
        var columnFamilies = new ColumnFamilies(defaultColumnFamilyOptions);
        foreach (var columnFamilyName in columnFamilyNames)
        {
            if (mergeOperators.TryGetValue(columnFamilyName, out var mergeOperatorConfig))
            {
                // Create a copy of the default options for this column family
                var cfOptions = new ColumnFamilyOptions();
                
                // Apply the same settings as the default options
                var tableConfig = new BlockBasedTableOptions();
                tableConfig.SetBlockCache(_cache);
                tableConfig.SetBlockSize(BlockSize);
                var filter = BloomFilterPolicy.Create();
                tableConfig.SetFilterPolicy(filter);
                cfOptions.SetBlockBasedTableFactory(tableConfig);
                cfOptions.SetWriteBufferSize(WriteBufferSize);
                cfOptions.SetCompression(Compression.No);
                cfOptions.SetCompactionStyle(Compaction.Universal);
                cfOptions.SetMaxWriteBufferNumber(MaxWriteBuffers);
                cfOptions.SetCreateIfMissing();
                cfOptions.SetCreateMissingColumnFamilies();
                cfOptions.SetErrorIfExists(false);
                cfOptions.SetInfoLogLevel(InfoLogLevel.Error);
                cfOptions.EnableStatistics();

                // Create and set the merge operator
                var mergeOp = global::RocksDbSharp.MergeOperators.Create(
                    mergeOperatorConfig.Name,
                    mergeOperatorConfig.PartialMerge,
                    mergeOperatorConfig.FullMerge);
                
                cfOptions.SetMergeOperator(mergeOp);
                columnFamilies.Add(columnFamilyName, cfOptions);
            }
            else
            {
                columnFamilies.Add(columnFamilyName, defaultColumnFamilyOptions);
            }
        }

        return columnFamilies;
    }

    public void Dispose()
    {
        Db.Dispose();
    }
}
