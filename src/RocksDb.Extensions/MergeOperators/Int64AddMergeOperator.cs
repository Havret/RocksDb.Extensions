namespace RocksDb.Extensions.MergeOperators;

/// <summary>
/// A merge operator that adds Int64 values together.
/// Useful for implementing atomic counters.
/// </summary>
/// <example>
/// <code>
/// public class CounterStore : MergeableRocksDbStore&lt;string, long, long&gt;
/// {
///     public CounterStore(IRocksDbAccessor&lt;string, long&gt; accessor, IMergeAccessor&lt;string, long&gt; mergeAccessor) 
///         : base(accessor, mergeAccessor) { }
///     
///     public void Increment(string key, long delta = 1) => Merge(key, delta);
/// }
/// 
/// // Registration:
/// builder.AddMergeableStore&lt;string, long, long, CounterStore&gt;("counters", new Int64AddMergeOperator());
/// </code>
/// </example>
public class Int64AddMergeOperator : IMergeOperator<long, long>
{
    /// <inheritdoc />
    public string Name => "Int64AddMergeOperator";

    /// <inheritdoc />
    public long FullMerge(ReadOnlySpan<byte> key, long existingValue, IReadOnlyList<long> operands)
    {
        var result = existingValue;
        foreach (var operand in operands)
        {
            result += operand;
        }
        return result;
    }

    /// <inheritdoc />
    public long PartialMerge(ReadOnlySpan<byte> key, IReadOnlyList<long> operands)
    {
        long result = 0;
        foreach (var operand in operands)
        {
            result += operand;
        }
        return result;
    }
}
