namespace RocksDb.Extensions.MergeOperators;

/// <summary>
/// Specifies the type of operation to perform on a list.
/// </summary>
public enum OperationType
{
    /// <summary>
    /// Add items to the list.
    /// </summary>
    Add,

    /// <summary>
    /// Remove items from the list (first occurrence of each item).
    /// </summary>
    Remove
}