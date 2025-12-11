namespace RocksDb.Extensions;

/// <summary>
/// Internal configuration for a merge operator associated with a column family.
/// </summary>
internal class MergeOperatorConfig
{
    /// <summary>
    /// Gets the name of the merge operator.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets the full merge callback delegate.
    /// </summary>
    public global::RocksDbSharp.MergeOperators.FullMergeFunc FullMerge { get; set; } = null!;

    /// <summary>
    /// Gets the partial merge callback delegate.
    /// </summary>
    public global::RocksDbSharp.MergeOperators.PartialMergeFunc PartialMerge { get; set; } = null!;

    /// <summary>
    /// Gets the value serializer for deserializing and serializing values.
    /// </summary>
    public object ValueSerializer { get; set; } = null!;
}
