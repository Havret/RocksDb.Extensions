using System.Diagnostics.CodeAnalysis;

namespace RocksDb.Extensions;

/// <summary>
/// Base class for a RocksDB store that supports merge operations.
/// Inherit from this class when you need to use RocksDB's merge operator functionality
/// for efficient atomic read-modify-write operations.
/// </summary>
/// <typeparam name="TKey">The type of the store's keys.</typeparam>
/// <typeparam name="TValue">The type of the store's values.</typeparam>
/// <typeparam name="TOperand">The type of the merge operand.</typeparam>
/// <remarks>
/// <para>
/// Merge operations are useful for:
/// - Counters: Increment/decrement without reading current value (TValue=long, TOperand=long)
/// - Lists: Append items without reading the entire list (TValue=IList&lt;T&gt;, TOperand=IList&lt;T&gt;)
/// - Lists with add/remove: Modify lists atomically (TValue=IList&lt;T&gt;, TOperand=ListOperation&lt;T&gt;)
/// </para>
/// <para>
/// When using this base class, you must register the store with a merge operator using
/// <see cref="IRocksDbBuilder.AddMergeableStore{TKey,TValue,TOperand,TStore}"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Counter store where value and operand are the same type
/// public class CounterStore : MergeableRocksDbStore&lt;string, long, long&gt;
/// {
///     public CounterStore(IMergeAccessor&lt;string, long, long&gt; mergeAccessor) 
///         : base(mergeAccessor) { }
///     
///     public void Increment(string key, long delta = 1) => Merge(key, delta);
/// }
/// 
/// // Tags store where value is IList&lt;string&gt; but operand is ListOperation&lt;string&gt;
/// public class TagsStore : MergeableRocksDbStore&lt;string, IList&lt;string&gt;, ListOperation&lt;string&gt;&gt;
/// {
///     public TagsStore(IMergeAccessor&lt;string, IList&lt;string&gt;, ListOperation&lt;string&gt;&gt; mergeAccessor) 
///         : base(mergeAccessor) { }
///     
///     public void AddTag(string key, string tag) => Merge(key, ListOperation&lt;string&gt;.Add(tag));
///     public void RemoveTag(string key, string tag) => Merge(key, ListOperation&lt;string&gt;.Remove(tag));
/// }
/// </code>
/// </example>
public abstract class MergeableRocksDbStore<TKey, TValue, TOperand>
{
    private readonly IMergeAccessor<TKey, TValue, TOperand> _rocksDbAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MergeableRocksDbStore{TKey, TValue, TOperand}"/> class.
    /// </summary>
    /// <param name="rocksDbAccessor">The RocksDB accessor to use for database operations.</param>
    protected MergeableRocksDbStore(IMergeAccessor<TKey, TValue, TOperand> rocksDbAccessor) 
    {
        _rocksDbAccessor = rocksDbAccessor;
    }

    /// <summary>
    /// Performs an atomic merge operation on the value associated with the specified key.
    /// This operation uses RocksDB's merge operator to combine the operand with the existing value
    /// without requiring a separate read operation.
    /// </summary>
    /// <param name="key">The key to merge the operand with.</param>
    /// <param name="operand">The operand to merge with the existing value.</param>
    public void Merge(TKey key, TOperand operand) => _rocksDbAccessor.Merge(key, operand);
    
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
    /// Adds or updates a collection of key-value pairs in the store.
    /// </summary>
    /// <param name="items">The collection of key-value pairs to add or update.</param>
    public void PutRange(IReadOnlyList<(TKey key, TValue value)> items) => _rocksDbAccessor.PutRange(items);

    /// <summary>
    /// Gets all the values in the store.
    /// </summary>
    /// <returns>An enumerable collection of all the values in the store.</returns>
    public IEnumerable<TValue> GetAllValues() => _rocksDbAccessor.GetAllValues();

    /// <summary>
    /// Determines whether the store contains a value for a specific key.
    /// </summary>
    /// <param name="key">The key to check in the store for an associated value.</param>
    /// <returns><c>true</c> if the store contains an element with the specified key; otherwise, <c>false</c>.</returns>
    public bool HasKey(TKey key) => _rocksDbAccessor.HasKey(key);

    /// <summary>
    /// Resets the column family associated with the store.
    /// This operation destroys the current column family and creates a new one,
    /// effectively removing all stored key-value pairs.
    ///
    /// Note: This method is intended for scenarios where a complete reset of the column family
    /// is required. The operation may involve internal reallocation and metadata changes, which
    /// can impact performance during execution. Use with caution in high-frequency workflows.
    /// </summary>
    public void Clear() => _rocksDbAccessor.Clear();
    
    /// <summary>
    /// Gets the number of key-value pairs currently stored.
    /// </summary>
    /// <remarks>
    /// This method is <b>not</b> a constant-time operation. Internally, it iterates over all entries in the store
    /// to compute the count. While the keys and values are not deserialized during iteration, this process may still
    /// be expensive for large datasets.
    ///
    /// Use this method with caution in performance-critical paths, especially if the store contains a high number of entries.
    /// </remarks>
    /// <returns>The total count of items in the store.</returns>
    public int Count() => _rocksDbAccessor.Count();

    /// <summary>
    /// Gets all the keys in the store.
    /// </summary>
    /// <returns>An enumerable collection of all the keys in the store.</returns>
    public IEnumerable<TKey> GetAllKeys() => _rocksDbAccessor.GetAllKeys();
}
