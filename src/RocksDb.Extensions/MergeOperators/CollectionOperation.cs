namespace RocksDb.Extensions.MergeOperators;

/// <summary>
/// Represents an operation (add or remove) to apply to a collection via merge.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public class CollectionOperation<T>
{
    /// <summary>
    /// Gets the type of operation to perform.
    /// </summary>
    public OperationType Type { get; }

    /// <summary>
    /// Gets the items to add or remove.
    /// </summary>
    public IList<T> Items { get; }

    /// <summary>
    /// Creates a new collection operation.
    /// </summary>
    /// <param name="type">The type of operation.</param>
    /// <param name="items">The items to add or remove.</param>
    public CollectionOperation(OperationType type, IList<T> items)
    {
        Type = type;
        Items = items ?? throw new ArgumentNullException(nameof(items));
    }

    /// <summary>
    /// Creates an Add operation for the specified items.
    /// </summary>
    public static CollectionOperation<T> Add(params T[] items) => new(OperationType.Add, items);

    /// <summary>
    /// Creates an Add operation for the specified items.
    /// </summary>
    public static CollectionOperation<T> Add(IList<T> items) => new(OperationType.Add, items);

    /// <summary>
    /// Creates a Remove operation for the specified items.
    /// </summary>
    public static CollectionOperation<T> Remove(params T[] items) => new(OperationType.Remove, items);

    /// <summary>
    /// Creates a Remove operation for the specified items.
    /// </summary>
    public static CollectionOperation<T> Remove(IList<T> items) => new(OperationType.Remove, items);
}
