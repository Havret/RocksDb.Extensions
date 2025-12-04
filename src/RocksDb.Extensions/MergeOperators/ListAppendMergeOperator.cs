namespace RocksDb.Extensions.MergeOperators;

/// <summary>
/// A merge operator that appends items to a list.
/// Useful for implementing atomic list append operations without requiring a read before write.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
/// <example>
/// <code>
/// public class EventLogStore : MergeableRocksDbStore&lt;string, IList&lt;string&gt;, IList&lt;string&gt;&gt;
/// {
///     public EventLogStore(IRocksDbAccessor&lt;string, IList&lt;string&gt;&gt; accessor, IMergeAccessor&lt;string, IList&lt;string&gt;&gt; mergeAccessor) 
///         : base(accessor, mergeAccessor) { }
///     
///     public void AppendEvent(string key, string eventData)
///     {
///         Merge(key, new List&lt;string&gt; { eventData });
///     }
/// }
/// 
/// // Registration:
/// builder.AddMergeableStore&lt;string, IList&lt;string&gt;, IList&lt;string&gt;, EventLogStore&gt;("events", new ListAppendMergeOperator&lt;string&gt;());
/// </code>
/// </example>
public class ListAppendMergeOperator<T> : IMergeOperator<IList<T>, IList<T>>
{
    /// <inheritdoc />
    public string Name => $"ListAppendMergeOperator<{typeof(T).Name}>";

    /// <inheritdoc />
    public IList<T> FullMerge(ReadOnlySpan<byte> key, IList<T> existingValue, IReadOnlyList<IList<T>> operands)
    {
        var result = existingValue != null ? new List<T>(existingValue) : new List<T>();
        
        foreach (var operand in operands)
        {
            foreach (var item in operand)
            {
                result.Add(item);
            }
        }
        
        return result;
    }

    /// <inheritdoc />
    public IList<T> PartialMerge(ReadOnlySpan<byte> key, IReadOnlyList<IList<T>> operands)
    {
        var result = new List<T>();
        
        foreach (var operand in operands)
        {
            foreach (var item in operand)
            {
                result.Add(item);
            }
        }
        
        return result;
    }
}
