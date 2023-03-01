namespace RocksDb.Extensions;

/// <summary>
/// A builder that can be used to create a RocksDB instance with one or more stores. 
/// </summary>
public interface IRocksDbBuilder
{
    /// <summary>
    /// Adds a RocksDB store to the builder for the specified column family.
    /// </summary>
    /// <param name="columnFamily"></param>
    /// <typeparam name="TKey">The type of the store's key.</typeparam>
    /// <typeparam name="TValue">The type of the store's value.</typeparam>
    /// <typeparam name="TStore">The type of the store to add.</typeparam>
    /// <returns>The builder instance for method chaining</returns>
    /// <remarks>
    /// The <typeparamref name="TStore"/> type must be a concrete implementation of the abstract class 
    /// <see cref="RocksDbStore{TKey,TValue}"/>.
    /// </remarks>
    IRocksDbBuilder AddStore<TKey, TValue, TStore>(string columnFamily) where TStore : RocksDbStore<TKey, TValue>;
}