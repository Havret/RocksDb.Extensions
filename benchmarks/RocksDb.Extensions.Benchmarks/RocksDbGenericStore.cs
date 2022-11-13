namespace RocksDb.Extensions.Benchmarks;

public class RocksDbGenericStore<TKey, TValue> : RocksDbStore<TKey, TValue>
{
    public RocksDbGenericStore(IRocksDbAccessor<TKey, TValue> rocksDbAccessor) : base(rocksDbAccessor)
    {
    }
}
