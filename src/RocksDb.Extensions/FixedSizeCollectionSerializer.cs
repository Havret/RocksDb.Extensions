using System.Buffers;

namespace RocksDb.Extensions;

/// <summary>
/// Base class for serializing collections of fixed-size elements like primitive types (int, long, etc.)
/// where each element occupies the same number of bytes in memory.
/// </summary>
/// <remarks>
/// The serialized format consists of:
/// - 4 bytes: Collection length (number of elements)
/// - Remaining bytes: Contiguous array of serialized elements
/// </remarks>
/// <typeparam name="TCollection">The collection type (e.g., IList{T}, ISet{T})</typeparam>
/// <typeparam name="TElement">The element type</typeparam>
internal abstract class FixedSizeCollectionSerializer<TCollection, TElement> : ISerializer<TCollection>
    where TCollection : ICollection<TElement>
{
    private readonly ISerializer<TElement> _scalarSerializer;

    protected FixedSizeCollectionSerializer(ISerializer<TElement> scalarSerializer)
    {
        _scalarSerializer = scalarSerializer;
    }

    /// <summary>
    /// Creates a new collection instance with the specified capacity.
    /// </summary>
    protected abstract TCollection CreateCollection(int capacity);

    /// <summary>
    /// Adds an element to the collection.
    /// </summary>
    protected abstract void AddElement(TCollection collection, TElement element);

    public bool TryCalculateSize(ref TCollection value, out int size)
    {
        size = sizeof(int); // size of the collection
        if (value.Count == 0)
        {
            return true;
        }

        var referentialElement = value.First();
        if (_scalarSerializer.TryCalculateSize(ref referentialElement, out var elementSize))
        {
            size += value.Count * elementSize;
            return true;
        }

        return false;
    }

    public void WriteTo(ref TCollection value, ref Span<byte> span)
    {
        // Write the size of the collection
        var slice = span.Slice(0, sizeof(int));
        BitConverter.TryWriteBytes(slice, value.Count);

        if (value.Count == 0)
        {
            return;
        }

        // Write the elements of the collection
        int offset = sizeof(int);
        var elementSize = (span.Length - offset) / value.Count;
        foreach (var item in value)
        {
            var element = item;
            slice = span.Slice(offset, elementSize);
            _scalarSerializer.WriteTo(ref element, ref slice);
            offset += elementSize;
        }
    }

    public void WriteTo(ref TCollection value, IBufferWriter<byte> buffer)
    {
        throw new NotImplementedException();
    }

    public TCollection Deserialize(ReadOnlySpan<byte> buffer)
    {
        // Read the size of the collection
        var slice = buffer.Slice(0, sizeof(int));
        var size = BitConverter.ToInt32(slice);

        var collection = CreateCollection(size);

        if (size == 0)
        {
            return collection;
        }

        // Read the elements of the collection
        int offset = sizeof(int);
        var elementSize = (buffer.Length - offset) / size;
        for (int i = 0; i < size; i++)
        {
            slice = buffer.Slice(offset, elementSize);
            var element = _scalarSerializer.Deserialize(slice);
            AddElement(collection, element);
            offset += elementSize;
        }

        return collection;
    }
}

