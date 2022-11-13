using System.Diagnostics.CodeAnalysis;

namespace RocksDb.Extensions;

public interface IRocksDbAccessor<TKey, TValue>
{
    void Remove(TKey key);
    void Put(TKey key, TValue value);
    bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value);
    void PutRange(ReadOnlySpan<TKey> keys, ReadOnlySpan<TValue> values);
    IEnumerable<TValue> GetAll();
}
