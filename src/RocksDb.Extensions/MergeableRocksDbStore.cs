namespace RocksDb.Extensions;

/// <summary>
/// Base class for a RocksDB store that supports merge operations.
/// Inherit from this class when you need to use RocksDB's merge operator functionality
/// for efficient atomic read-modify-write operations.
/// </summary>
/// <typeparam name="TKey">The type of the store's keys.</typeparam>
/// <typeparam name="TValue">The type of the store's values.</typeparam>
/// <remarks>
/// <para>
/// Merge operations are useful for:
/// - Counters: Increment/decrement without reading current value
/// - Lists: Append items without reading the entire list
/// - Sets: Add/remove items atomically
/// </para>
/// <para>
/// When using this base class, you must register the store with a merge operator using
/// <see cref="IRocksDbBuilder.AddMergeableStore{TKey,TValue,TStore}"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CounterStore : MergeableRocksDbStore&lt;string, long&gt;
/// {
///     public CounterStore(IRocksDbAccessor&lt;string, long&gt; accessor) : base(accessor) { }
///     
///     public void Increment(string key, long delta = 1) => Merge(key, delta);
/// }
/// 
/// // Registration:
/// builder.AddMergeableStore&lt;string, long, CounterStore&gt;("counters", new Int64AddMergeOperator());
/// </code>
/// </example>
public abstract class MergeableRocksDbStore<TKey, TValue> : RocksDbStore<TKey, TValue>, IMergeableRocksDbStore<TKey, TValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MergeableRocksDbStore{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="rocksDbAccessor">The RocksDB accessor to use for database operations.</param>
    protected MergeableRocksDbStore(IRocksDbAccessor<TKey, TValue> rocksDbAccessor) : base(rocksDbAccessor)
    {
    }

    /// <summary>
    /// Performs an atomic merge operation on the value associated with the specified key.
    /// This operation uses RocksDB's merge operator to combine the operand with the existing value
    /// without requiring a separate read operation.
    /// </summary>
    /// <param name="key">The key to merge the operand with.</param>
    /// <param name="operand">The operand to merge with the existing value.</param>
    public new void Merge(TKey key, TValue operand) => base.Merge(key, operand);
}
