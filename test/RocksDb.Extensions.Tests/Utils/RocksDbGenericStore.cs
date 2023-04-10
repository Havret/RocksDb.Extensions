namespace RocksDb.Extensions.Tests.Utils;

public class RocksDbGenericStore<TKey, TValue> : RocksDbStore<TKey, TValue>
{
    public RocksDbGenericStore(IRocksDbAccessor<TKey, TValue> rocksDbAccessor) : base(rocksDbAccessor)
    {
    }
}
