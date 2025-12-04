namespace RocksDb.Extensions.MergeOperators;

/// <summary>
/// A merge operator that supports both adding and removing items from a list.
/// Each merge operand is a ListOperation that specifies whether to add or remove items.
/// Operations are applied in order, enabling atomic list modifications without read-before-write.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
/// <example>
/// <code>
/// public class TagsStore : MergeableRocksDbStore&lt;string, IList&lt;string&gt;, ListOperation&lt;string&gt;&gt;
/// {
///     public TagsStore(IRocksDbAccessor&lt;string, IList&lt;string&gt;&gt; accessor, IMergeAccessor&lt;string, ListOperation&lt;string&gt;&gt; mergeAccessor) 
///         : base(accessor, mergeAccessor) { }
///     
///     public void AddTags(string key, params string[] tags) => Merge(key, ListOperation&lt;string&gt;.Add(tags));
///     public void RemoveTags(string key, params string[] tags) => Merge(key, ListOperation&lt;string&gt;.Remove(tags));
/// }
/// 
/// // Registration:
/// builder.AddMergeableStore&lt;string, IList&lt;string&gt;, ListOperation&lt;string&gt;, TagsStore&gt;("tags", new ListMergeOperator&lt;string&gt;());
/// </code>
/// </example>
/// <remarks>
/// <para>
/// The value type stored in RocksDB is <c>IList&lt;T&gt;</c> (the actual list contents),
/// while merge operands are <c>ListOperation&lt;T&gt;</c> (the operations to apply).
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
public class ListMergeOperator<T> : IMergeOperator<IList<T>, ListOperation<T>>
{
    /// <inheritdoc />
    public string Name => $"ListMergeOperator<{typeof(T).Name}>";

    /// <inheritdoc />
    public IList<T> FullMerge(
        IList<T>? existingValue,
        IReadOnlyList<ListOperation<T>> operands)
    {
        // Start with existing items or empty list
        var result = existingValue != null ? new List<T>(existingValue) : new List<T>();

        // Apply all operands in order
        foreach (var operand in operands)
        {
            ApplyOperation(result, operand);
        }

        return result;
    }

    /// <inheritdoc />
    public ListOperation<T>? PartialMerge(IReadOnlyList<ListOperation<T>> operands)
    {
        // Check if any operands contain removes
        bool hasRemoves = false;
        var allAdds = new List<T>();
        
        foreach (var operand in operands)
        {
            if (operand.Type == OperationType.Remove)
            {
                hasRemoves = true;
                break;
            }
            
            if (operand.Type == OperationType.Add)
            {
                foreach (var item in operand.Items)
                {
                    allAdds.Add(item);
                }
            }
        }

        // If there are any removes, we can't safely combine without knowing the existing state
        // Return null to signal that RocksDB should keep operands separate
        if (hasRemoves)
        {
            return null;
        }
        
        // Only adds present - safe to combine
        return ListOperation<T>.Add(allAdds);
    }

    private static void ApplyOperation(List<T> result, ListOperation<T> operation)
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
