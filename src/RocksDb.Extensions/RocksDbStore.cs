namespace RocksDb.Extensions;

/// <summary>
/// Base class for a RocksDB store that provides basic operations such as add, update, remove, get and get all.
/// </summary>
/// <typeparam name="TKey">The type of the store's keys.</typeparam>
/// <typeparam name="TValue">The type of the store's values.</typeparam>
public abstract class RocksDbStore<TKey, TValue> : RocksDbStoreBase<TKey, TValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RocksDbStore{TKey, TValue}"/> class with the specified RocksDB accessor.
    /// </summary>
    /// <param name="rocksDbAccessor">The RocksDB accessor to use for database operations.</param>
    protected RocksDbStore(IRocksDbAccessor<TKey, TValue> rocksDbAccessor) : base(rocksDbAccessor)
    {
    }

    /// <summary>
    /// Gets all the values in the store. (Obsolete, use GetAllValues instead)
    /// </summary>
    /// <returns>An enumerable collection of all the values in the store.</returns>
    [Obsolete("Use GetAllValues() instead.")]
    public IEnumerable<TValue> GetAll() => GetAllValues();
}
