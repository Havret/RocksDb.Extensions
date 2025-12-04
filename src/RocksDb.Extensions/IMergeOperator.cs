namespace RocksDb.Extensions;

/// <summary>
/// Defines a merge operator for RocksDB that enables atomic read-modify-write operations.
/// Merge operators allow efficient updates without requiring a separate read before write,
/// which is particularly useful for counters, list appends, set unions, and other accumulative operations.
/// </summary>
/// <typeparam name="TValue">The type of the value stored in the database.</typeparam>
/// <typeparam name="TOperand">The type of the merge operand (the delta/change to apply).</typeparam>
/// <remarks>
/// The separation of <typeparamref name="TValue"/> and <typeparamref name="TOperand"/> allows for flexible merge patterns:
/// <list type="bullet">
/// <item><description>For counters: TValue=long, TOperand=long (same type)</description></item>
/// <item><description>For list append: TValue=IList&lt;T&gt;, TOperand=IList&lt;T&gt; (same type)</description></item>
/// <item><description>For list with add/remove: TValue=IList&lt;T&gt;, TOperand=CollectionOperation&lt;T&gt; (different types)</description></item>
/// </list>
/// </remarks>
public interface IMergeOperator<TValue, TOperand>
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
    /// <param name="existingValue">The existing value in the database, or null/default if no value exists.</param>
    /// <param name="operands">The span of merge operands to apply, in order.</param>
    /// <returns>A tuple containing:
    /// <list type="bullet">
    /// <item><description><c>Success</c>: true if the merge operation succeeded; false if it failed.</description></item>
    /// <item><description><c>Value</c>: The merged result when Success is true; otherwise a default value.</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// This method must handle the case where existingValue is null (for reference types) or default (for value types).
    /// The Success flag allows graceful error handling - if false is returned, the Get operation will return as if the key doesn't exist.
    /// </remarks>
    (bool Success, TValue Value) FullMerge(TValue? existingValue, ReadOnlySpan<TOperand> operands);

    /// <summary>
    /// Performs a partial merge of multiple operands without the existing value.
    /// Called during compaction to combine multiple merge operands into a single operand.
    /// This is an optimization that reduces the number of operands that need to be stored.
    /// </summary>
    /// <param name="operands">The span of merge operands to combine, in order.</param>
    /// <returns>A tuple containing:
    /// <list type="bullet">
    /// <item><description><c>Success</c>: true if operands were successfully combined; false if it's unsafe to combine without knowing the existing value.</description></item>
    /// <item><description><c>Operand</c>: The combined operand when Success is true; otherwise null.</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Return (false, null) when it's not safe to combine operands without knowing the existing value.
    /// When false is returned, RocksDB will keep the operands separate and call FullMerge later.
    /// This allows for more efficient storage when operations are commutative and associative.
    /// </remarks>
    (bool Success, TOperand Operand) PartialMerge(ReadOnlySpan<TOperand> operands);
}
