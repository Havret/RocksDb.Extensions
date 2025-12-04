namespace RocksDb.Extensions;

/// <summary>
/// A builder that can be used to create a RocksDB instance with one or more stores. 
/// </summary>
public interface IRocksDbBuilder
{
    /// <summary>
    /// Adds a RocksDB store to the builder for the specified column family.
    /// </summary>
    /// <param name="columnFamily">The name of the column family to associate with the store.</param>
    /// <typeparam name="TKey">The type of the store's key.</typeparam>
    /// <typeparam name="TValue">The type of the store's value.</typeparam>
    /// <typeparam name="TStore">The type of the store to add.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified column family is already registered.</exception>
    /// <remarks>
    /// The <typeparamref name="TStore"/> type must be a concrete implementation of the abstract class 
    /// <see cref="RocksDbStore{TKey,TValue}"/>. Each store is registered uniquely based on its column family name.
    /// 
    /// Stores can also be resolved as keyed services using their associated column family name.
    /// Use <c>GetRequiredKeyedService&lt;TStore&gt;(columnFamily)</c> to retrieve a specific store instance.
    /// </remarks>
    IRocksDbBuilder AddStore<TKey, TValue, TStore>(string columnFamily) where TStore : RocksDbStore<TKey, TValue>;

    /// <summary>
    /// Adds a RocksDB store to the builder for the specified column family with a merge operator.
    /// </summary>
    /// <param name="columnFamily">The name of the column family to associate with the store.</param>
    /// <param name="mergeOperator">The merge operator to use for atomic read-modify-write operations.</param>
    /// <typeparam name="TKey">The type of the store's key.</typeparam>
    /// <typeparam name="TValue">The type of the store's value.</typeparam>
    /// <typeparam name="TStore">The type of the store to add.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified column family is already registered.</exception>
    /// <remarks>
    /// The <typeparamref name="TStore"/> type must be a concrete implementation of the abstract class 
    /// <see cref="RocksDbStore{TKey,TValue}"/>. Each store is registered uniquely based on its column family name.
    /// 
    /// The merge operator enables efficient atomic updates without requiring a separate read operation.
    /// This is useful for counters, list appends, set unions, and other accumulative operations.
    /// 
    /// Stores can also be resolved as keyed services using their associated column family name.
    /// Use <c>GetRequiredKeyedService&lt;TStore&gt;(columnFamily)</c> to retrieve a specific store instance.
    /// </remarks>
    IRocksDbBuilder AddStore<TKey, TValue, TStore>(string columnFamily, IMergeOperator<TValue> mergeOperator) where TStore : RocksDbStore<TKey, TValue>;
}