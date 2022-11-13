namespace RocksDb.Extensions;

public interface IRocksDbBuilder
{
    IRocksDbBuilder AddStore<TKey, TValue, TStore>(string columnFamily) where TStore : RocksDbStore<TKey, TValue>;
}
