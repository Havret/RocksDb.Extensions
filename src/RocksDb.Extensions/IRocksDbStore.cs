using System.Diagnostics.CodeAnalysis;

namespace RocksDb.Extensions;

public abstract class RocksDbStore<TKey, TValue>
{
    private readonly IRocksDbAccessor<TKey, TValue> _rocksDbAccessor;

    protected RocksDbStore(IRocksDbAccessor<TKey, TValue> rocksDbAccessor) => _rocksDbAccessor = rocksDbAccessor;

    public void Remove(TKey key) => _rocksDbAccessor.Remove(key);

    public void Put(TKey key, TValue value) => _rocksDbAccessor.Put(key, value);

    public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value) => _rocksDbAccessor.TryGet(key, out value);

    public void PutRange(ReadOnlySpan<TKey> keys, ReadOnlySpan<TValue> values) => _rocksDbAccessor.PutRange(keys, values);

    public IEnumerable<TValue> GetAll() => _rocksDbAccessor.GetAll();
}
