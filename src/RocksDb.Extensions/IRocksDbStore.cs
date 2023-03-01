using System.Diagnostics.CodeAnalysis;

namespace RocksDb.Extensions;

/// <summary>
/// Base class for a RocksDB store that provides basic operations such as add, update, remove, get and get all.
/// </summary>
/// <typeparam name="TKey">The type of the store's keys.</typeparam>
/// <typeparam name="TValue">The type of the store's values.</typeparam>
public abstract class RocksDbStore<TKey, TValue>
{
    private readonly IRocksDbAccessor<TKey, TValue> _rocksDbAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="RocksDbStore{TKey, TValue}"/> class with the specified RocksDB accessor.
    /// </summary>
    /// <param name="rocksDbAccessor">The RocksDB accessor to use for database operations.</param>
    protected RocksDbStore(IRocksDbAccessor<TKey, TValue> rocksDbAccessor) => _rocksDbAccessor = rocksDbAccessor;

    /// <summary>
    /// Removes the specified key and its associated value from the store.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    public void Remove(TKey key) => _rocksDbAccessor.Remove(key);

    /// <summary>
    /// Adds or updates the specified key-value pair in the store.
    /// </summary>
    /// <param name="key">The key to add or update.</param>
    /// <param name="value">The value to add or update.</param>
    public void Put(TKey key, TValue value) => _rocksDbAccessor.Put(key, value);

    /// <summary>
    /// Tries to get the value associated with the specified key in the store.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="value">The value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
    /// <returns><c>true</c> if the key is found; otherwise, <c>false</c>.</returns>
    public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value) => _rocksDbAccessor.TryGet(key, out value);

    /// <summary>
    /// Puts the specified keys and values in the store.
    /// </summary>
    /// <param name="keys">The keys to put in the store.</param>
    /// <param name="values">The values to put in the store.</param>
    public void PutRange(ReadOnlySpan<TKey> keys, ReadOnlySpan<TValue> values) => _rocksDbAccessor.PutRange(keys, values);
    
    /// <summary>
    /// Puts the specified values in the store using the specified key selector function to generate keys.
    /// </summary>
    /// <param name="values">The values to put in the store.</param>
    /// <param name="keySelector">The function to use to generate keys for the values.</param>
    public void PutRange(ReadOnlySpan<TValue> values, Func<TValue, TKey> keySelector) => _rocksDbAccessor.PutRange(values, keySelector);

    /// <summary>
    /// Gets all the values in the store.
    /// </summary>
    /// <returns>An enumerable collection of all the values in the store.</returns>
    public IEnumerable<TValue> GetAll() => _rocksDbAccessor.GetAll();
}
