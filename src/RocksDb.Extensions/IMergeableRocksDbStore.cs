namespace RocksDb.Extensions;

/// <summary>
/// Interface for a RocksDB store that supports merge operations.
/// </summary>
/// <typeparam name="TKey">The type of the store's keys.</typeparam>
/// <typeparam name="TValue">The type of the store's values.</typeparam>
public interface IMergeableRocksDbStore<in TKey, TValue>
{
    /// <summary>
    /// Performs an atomic merge operation on the value associated with the specified key.
    /// This operation uses RocksDB's merge operator to combine the operand with the existing value
    /// without requiring a separate read operation, which is more efficient than Get+Put for
    /// accumulative operations like counters, list appends, or set unions.
    /// </summary>
    /// <param name="key">The key to merge the operand with.</param>
    /// <param name="operand">The operand to merge with the existing value.</param>
    void Merge(TKey key, TValue operand);
}
