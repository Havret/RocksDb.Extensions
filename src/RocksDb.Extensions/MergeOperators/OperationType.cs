namespace RocksDb.Extensions.MergeOperators;

/// <summary>
/// Specifies the type of operation to perform on a collection.
/// </summary>
public enum OperationType
{
    /// <summary>
    /// Add items to the collection.
    /// </summary>
    Add,

    /// <summary>
    /// Remove items from the collection.
    /// </summary>
    Remove
}