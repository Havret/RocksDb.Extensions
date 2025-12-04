namespace RocksDb.Extensions;

/// <summary>
/// Defines a merge operator for RocksDB that enables atomic read-modify-write operations.
/// Merge operators allow efficient updates without requiring a separate read before write,
/// which is particularly useful for counters, list appends, set unions, and other accumulative operations.
/// </summary>
/// <typeparam name="TValue">The type of the value being merged.</typeparam>
public interface IMergeOperator<TValue>
{
    /// <summary>
    /// Gets the name of the merge operator. This name is stored in the database
    /// and must remain consistent across database opens.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Performs a full merge of the existing value with one or more operands.
    /// Called when a Get operation encounters merge operands and needs to produce the final value.
    /// </summary>
    /// <param name="key">The key being merged.</param>
    /// <param name="existingValue">The existing value in the database. For value types, this will be default if no value exists (check hasExistingValue).</param>
    /// <param name="operands">The list of merge operands to apply, in order.</param>
    /// <returns>The merged value.</returns>
    TValue FullMerge(ReadOnlySpan<byte> key, TValue existingValue, IReadOnlyList<TValue> operands);

    /// <summary>
    /// Performs a partial merge of multiple operands without the existing value.
    /// Called during compaction to combine multiple merge operands into a single operand.
    /// This is an optimization that reduces the number of operands that need to be stored.
    /// </summary>
    /// <param name="key">The key being merged.</param>
    /// <param name="operands">The list of merge operands to combine, in order.</param>
    /// <returns>
    /// The combined operand if partial merge is possible; otherwise, default to indicate
    /// that partial merge is not supported and operands should be kept separate.
    /// </returns>
    TValue PartialMerge(ReadOnlySpan<byte> key, IReadOnlyList<TValue> operands);
}
