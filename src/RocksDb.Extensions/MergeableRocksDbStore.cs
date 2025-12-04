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
///     public CounterStore(IRocksDbAccessor&lt;string, long&gt; accessor, IMergeAccessor&lt;string, long&gt; mergeAccessor) 
///         : base(accessor, mergeAccessor) { }
///     
///     public void Increment(string key, long delta = 1) => Merge(key, delta);
/// }
/// 
/// // Tags store where value is IList&lt;string&gt; but operand is ListOperation&lt;string&gt;
/// public class TagsStore : MergeableRocksDbStore&lt;string, IList&lt;string&gt;, ListOperation&lt;string&gt;&gt;
/// {
///     public TagsStore(IRocksDbAccessor&lt;string, IList&lt;string&gt;&gt; accessor, IMergeAccessor&lt;string, ListOperation&lt;string&gt;&gt; mergeAccessor) 
///         : base(accessor, mergeAccessor) { }
///     
///     public void AddTag(string key, string tag) => Merge(key, ListOperation&lt;string&gt;.Add(tag));
///     public void RemoveTag(string key, string tag) => Merge(key, ListOperation&lt;string&gt;.Remove(tag));
/// }
/// </code>
/// </example>
public abstract class MergeableRocksDbStore<TKey, TValue, TOperand> : RocksDbStore<TKey, TValue>
{
    private readonly IMergeAccessor<TKey, TOperand> _mergeAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MergeableRocksDbStore{TKey, TValue, TOperand}"/> class.
    /// </summary>
    /// <param name="rocksDbAccessor">The RocksDB accessor to use for database operations.</param>
    /// <param name="mergeAccessor">The merge accessor to use for merge operations.</param>
    protected MergeableRocksDbStore(IRocksDbAccessor<TKey, TValue> rocksDbAccessor, IMergeAccessor<TKey, TOperand> mergeAccessor) 
        : base(rocksDbAccessor)
    {
        _mergeAccessor = mergeAccessor;
    }

    /// <summary>
    /// Performs an atomic merge operation on the value associated with the specified key.
    /// This operation uses RocksDB's merge operator to combine the operand with the existing value
    /// without requiring a separate read operation.
    /// </summary>
    /// <param name="key">The key to merge the operand with.</param>
    /// <param name="operand">The operand to merge with the existing value.</param>
    public void Merge(TKey key, TOperand operand) => _mergeAccessor.Merge(key, operand);
}
