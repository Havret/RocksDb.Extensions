using System.Buffers;

namespace RocksDb.Extensions;

/// <summary>
/// Base class for serializing collections containing variable-size elements like strings or complex objects
/// where each element may occupy a different number of bytes when serialized.
/// </summary>
/// <remarks>
/// The serialized format consists of:
/// - 4 bytes: Collection length (number of elements)
/// - For each element:
///   - 4 bytes: Size of the serialized element
///   - N bytes: Serialized element data
/// </remarks>
/// <typeparam name="TCollection">The collection type (e.g., IList{T}, ISet{T})</typeparam>
/// <typeparam name="TElement">The element type</typeparam>
internal abstract class VariableSizeCollectionSerializer<TCollection, TElement> : ISerializer<TCollection>
    where TCollection : ICollection<TElement>
{
    private readonly ISerializer<TElement> _scalarSerializer;

    protected VariableSizeCollectionSerializer(ISerializer<TElement> scalarSerializer)
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

        foreach (var item in value)
        {
            var element = item;
            if (_scalarSerializer.TryCalculateSize(ref element, out var elementSize))
            {
                size += sizeof(int);
                size += elementSize;
            }
            else
            {
                // Element serializer can't calculate size, so we can't either
                size = 0;
                return false;
            }
        }

        return true;
    }

    public void WriteTo(ref TCollection value, ref Span<byte> span)
    {
        // Write the size of the collection
        var slice = span.Slice(0, sizeof(int));
        BitConverter.TryWriteBytes(slice, value.Count);

        // Write the elements of the collection
        int offset = sizeof(int);
        foreach (var item in value)
        {
            var element = item;
            if (_scalarSerializer.TryCalculateSize(ref element, out var elementSize))
            {
                slice = span.Slice(offset, sizeof(int));
                BitConverter.TryWriteBytes(slice, elementSize);
                offset += sizeof(int);

                slice = span.Slice(offset, elementSize);
                _scalarSerializer.WriteTo(ref element, ref slice);
                offset += elementSize;
            }
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

        // Read the elements of the collection
        int offset = sizeof(int);
        for (int i = 0; i < size; i++)
        {
            slice = buffer.Slice(offset, sizeof(int));
            var elementSize = BitConverter.ToInt32(slice);
            offset += sizeof(int);

            slice = buffer.Slice(offset, elementSize);
            var element = _scalarSerializer.Deserialize(slice);
            AddElement(collection, element);
            offset += elementSize;
        }

        return collection;
    }
}

