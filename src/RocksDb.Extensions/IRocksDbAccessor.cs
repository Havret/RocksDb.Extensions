using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace RocksDb.Extensions;

#pragma warning disable CS1591

/// <summary>
/// This interface is not intended to be used directly by the clients of the library,
/// as it is used by the <see cref="RocksDbStore{TKey, TValue}"/> to access RocksDB.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IRocksDbAccessor<TKey, TValue>
{
    void Remove(TKey key);
    void Put(TKey key, TValue value);
    bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value);
    void PutRange(ReadOnlySpan<TKey> keys, ReadOnlySpan<TValue> values);
    void PutRange(ReadOnlySpan<TValue> values, Func<TValue, TKey> keySelector);
    IEnumerable<TValue> GetAll();
    bool HasKey(TKey key);
}

#pragma warning restore CS1591