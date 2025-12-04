namespace RocksDb.Extensions.MergeOperators;

/// <summary>
/// A merge operator that supports both adding and removing items from a list.
/// Each merge operand is a CollectionOperation that specifies whether to add or remove items.
/// Operations are applied in order, enabling atomic list modifications without read-before-write.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
/// <remarks>
/// <para>
/// The value type stored in RocksDB is <c>IList&lt;T&gt;</c> (the actual list contents),
/// while merge operands are <c>CollectionOperation&lt;T&gt;</c> (the operations to apply).
/// </para>
/// <para>
/// Remove operations delete the first occurrence of each item (same as <see cref="List{T}.Remove"/>).
/// If an item to remove doesn't exist in the list, the operation is silently ignored.
/// </para>
/// <para>
/// The partial merge optimization can combine multiple Add operations into a single operation,
/// but will refuse to combine when Remove operations are present (returns Success = false).
/// This ensures correctness since the order of Add and Remove operations matters.
/// </para>
/// </remarks>
public class ListMergeOperator<T> : IMergeOperator<IList<T>, CollectionOperation<T>>
{
    /// <inheritdoc />
    public string Name => $"ListMergeOperator<{typeof(T).Name}>";

    /// <inheritdoc />
    public (bool Success, IList<T> Value) FullMerge(IList<T>? existingValue, ReadOnlySpan<CollectionOperation<T>> operands)
    {
        // Start with existing items or empty list
        var result = existingValue != null ? new List<T>(existingValue) : new List<T>();

        // Apply all operands in order
        foreach (var operand in operands)
        {
            ApplyOperation(result, operand);
        }

        return (true, result);
    }

    /// <inheritdoc />
    public (bool Success, CollectionOperation<T> Operand) PartialMerge(ReadOnlySpan<CollectionOperation<T>> operands)
    {
        var allAdds = new List<T>();

        foreach (var operand in operands)
        {
            if (operand.Type == OperationType.Remove)
            {
                // If there are any removes, we can't safely combine without knowing the existing state
                // Return false to signal that RocksDB should keep operands separate
                return (false, null!);
            }
        }

        foreach (var operand in operands)
        {
            foreach (var item in operand.Items)
            {
                allAdds.Add(item);
            }
        }

        // Only adds present - safe to combine
        return (true, CollectionOperation<T>.Add(allAdds));
    }

    private static void ApplyOperation(List<T> result, CollectionOperation<T> operation)
    {
        switch (operation.Type)
        {
            case OperationType.Add:
                result.AddRange(operation.Items);
                break;

            case OperationType.Remove:
                foreach (var item in operation.Items)
                {
                    result.Remove(item); // Removes first occurrence
                }

                break;
        }
    }
}