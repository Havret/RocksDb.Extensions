using RocksDbSharp;

namespace RocksDb.Extensions;

/// <summary>
/// Internal configuration for a merge operator associated with a column family.
/// </summary>
internal class MergeOperatorConfig
{
    /// <summary>
    /// Gets the name of the merge operator.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the full merge callback delegate.
    /// </summary>
    public required MergeOperator.FullMergeImpl FullMerge { get; init; }

    /// <summary>
    /// Gets the partial merge callback delegate.
    /// </summary>
    public required MergeOperator.PartialMergeImpl PartialMerge { get; init; }

    /// <summary>
    /// Gets the value serializer for deserializing and serializing values.
    /// </summary>
    public required object ValueSerializer { get; init; }
}
