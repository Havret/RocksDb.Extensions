namespace RocksDb.Extensions.MergeOperators;

/// <summary>
/// A merge operator that supports both adding and removing items from a list.
/// Each merge operand is a ListOperation that specifies whether to add or remove items.
/// Operations are applied in order, enabling atomic list modifications without read-before-write.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
/// <example>
/// <code>
/// public class TagsStore : RocksDbStore&lt;string, IList&lt;string&gt;&gt;
/// {
///     public TagsStore(IRocksDbAccessor&lt;string, IList&lt;string&gt;&gt; accessor) : base(accessor) { }
///     
///     public void AddTags(string key, params string[] tags)
///     {
///         Merge(key, new List&lt;ListOperation&lt;string&gt;&gt; { ListOperation&lt;string&gt;.Add(tags) });
///     }
///     
///     public void RemoveTags(string key, params string[] tags)
///     {
///         Merge(key, new List&lt;ListOperation&lt;string&gt;&gt; { ListOperation&lt;string&gt;.Remove(tags) });
///     }
/// }
/// 
/// // Registration:
/// builder.AddStore&lt;string, IList&lt;string&gt;, TagsStore&gt;("tags", new ListMergeOperator&lt;string&gt;());
/// </code>
/// </example>
/// <remarks>
/// <para>
/// The value type stored in RocksDB is <c>IList&lt;T&gt;</c> (the actual list contents),
/// but merge operands are <c>IList&lt;ListOperation&lt;T&gt;&gt;</c> (the operations to apply).
/// </para>
/// <para>
/// Remove operations delete the first occurrence of each item (same as <see cref="List{T}.Remove"/>).
/// If an item to remove doesn't exist in the list, the operation is silently ignored.
/// </para>
/// <para>
/// For append-only use cases where removes are not needed, prefer <see cref="ListAppendMergeOperator{T}"/>
/// which has less serialization overhead.
/// </para>
/// </remarks>
public class ListMergeOperator<T> : IMergeOperator<IList<ListOperation<T>>>
{
    /// <inheritdoc />
    public string Name => $"ListMergeOperator<{typeof(T).Name}>";

    /// <inheritdoc />
    public IList<ListOperation<T>> FullMerge(
        ReadOnlySpan<byte> key,
        IList<ListOperation<T>> existingValue,
        IReadOnlyList<IList<ListOperation<T>>> operands)
    {
        // Start with existing items or empty list
        var result = new List<T>();
        
        // If there's an existing value, it contains the accumulated operations from previous merges
        // We need to apply those operations first
        if (existingValue != null)
        {
            ApplyOperations(result, existingValue);
        }

        // Apply all new operands in order
        foreach (var operandBatch in operands)
        {
            ApplyOperations(result, operandBatch);
        }

        // Return the final list wrapped as a single Add operation
        // This collapses all operations into the final state
        return new List<ListOperation<T>> { ListOperation<T>.Add(result) };
    }

    /// <inheritdoc />
    public IList<ListOperation<T>> PartialMerge(
        ReadOnlySpan<byte> key,
        IReadOnlyList<IList<ListOperation<T>>> operands)
    {
        // Combine all operations into a single list
        // We preserve all operations rather than trying to resolve them
        // because removes can't be safely combined without knowing the base state
        var combined = new List<ListOperation<T>>();
        
        foreach (var operandBatch in operands)
        {
            foreach (var op in operandBatch)
            {
                combined.Add(op);
            }
        }

        return combined;
    }

    private static void ApplyOperations(List<T> result, IList<ListOperation<T>> operations)
    {
        foreach (var operation in operations)
        {
            switch (operation.Type)
            {
                case OperationType.Add:
                    foreach (var item in operation.Items)
                    {
                        result.Add(item);
                    }
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
}
