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
    /// Adds a mergeable RocksDB store to the builder for the specified column family.
    /// This method enforces that the store inherits from <see cref="MergeableRocksDbStore{TKey,TValue,TOperand}"/>
    /// and requires a merge operator, providing compile-time safety for merge operations.
    /// </summary>
    /// <param name="columnFamily">The name of the column family to associate with the store.</param>
    /// <param name="mergeOperator">The merge operator to use for atomic read-modify-write operations.</param>
    /// <typeparam name="TKey">The type of the store's key.</typeparam>
    /// <typeparam name="TValue">The type of the store's values (the stored state).</typeparam>
    /// <typeparam name="TOperand">The type of the merge operand (the delta/change to apply).</typeparam>
    /// <typeparam name="TStore">The type of the store to add. Must inherit from <see cref="MergeableRocksDbStore{TKey,TValue,TOperand}"/>.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified column family is already registered.</exception>
    /// <remarks>
    /// <para>
    /// Use this method when your store needs merge operations. The <typeparamref name="TStore"/> constraint
    /// ensures that only stores designed for merge operations can be registered with this method.
    /// </para>
    /// <para>
    /// The separation of <typeparamref name="TValue"/> and <typeparamref name="TOperand"/> allows for flexible patterns:
    /// <list type="bullet">
    /// <item><description>Counters: TValue=long, TOperand=long (same type)</description></item>
    /// <item><description>List append: TValue=IList&lt;T&gt;, TOperand=IList&lt;T&gt; (same type)</description></item>
    /// <item><description>List with add/remove: TValue=IList&lt;T&gt;, TOperand=CollectionOperation&lt;T&gt; (different types)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// For stores that don't need merge operations, use <see cref="AddStore{TKey,TValue,TStore}(string)"/> instead.
    /// </para>
    /// </remarks>
    IRocksDbBuilder AddMergeableStore<TKey, TValue, TStore, TOperand>(string columnFamily, IMergeOperator<TValue, TOperand> mergeOperator) 
        where TStore : MergeableRocksDbStore<TKey, TValue, TOperand>;
}