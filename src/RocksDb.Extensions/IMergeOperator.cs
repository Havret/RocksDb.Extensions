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
/// <item><description>For list with add/remove: TValue=IList&lt;T&gt;, TOperand=ListOperation&lt;T&gt; (different types)</description></item>
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
    /// <param name="existingValue">The existing value in the database. For value types, this will be default if no value exists.</param>
    /// <param name="operands">The list of merge operands to apply, in order.</param>
    /// <returns>The merged value to store.</returns>
    TValue FullMerge(TValue? existingValue, IReadOnlyList<TOperand> operands);

    /// <summary>
    /// Performs a partial merge of multiple operands without the existing value.
    /// Called during compaction to combine multiple merge operands into a single operand.
    /// This is an optimization that reduces the number of operands that need to be stored.
    /// </summary>
    /// <param name="operands">The list of merge operands to combine, in order.</param>
    /// <returns>The combined operand, or null if partial merge is not safe for these operands.</returns>
    /// <remarks>
    /// Return null when it's not safe to combine operands without knowing the existing value.
    /// When null is returned, RocksDB will keep the operands separate and call FullMerge later.
    /// </remarks>
    TOperand? PartialMerge(IReadOnlyList<TOperand> operands);
}
