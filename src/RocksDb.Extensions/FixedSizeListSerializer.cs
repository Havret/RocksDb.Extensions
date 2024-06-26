using System.Buffers;

namespace RocksDb.Extensions;

/// <summary>
/// Serializes lists of fixed-size elements like primitive types (int, long, etc.) where each element
/// occupies the same number of bytes in memory. This implementation optimizes for performance by
/// pre-calculating buffer sizes based on element count.
/// </summary>
/// <remarks>
/// Use this serializer when working with lists of primitive types or structs where all elements
/// have identical size. The serialized format consists of:
/// - 4 bytes: List length (number of elements)
/// - Remaining bytes: Contiguous array of serialized elements
/// </remarks>
internal class FixedSizeListSerializer<T> : ISerializer<IList<T>>
{
    private readonly ISerializer<T> _scalarSerializer;

    public FixedSizeListSerializer(ISerializer<T> scalarSerializer)
    {
        _scalarSerializer = scalarSerializer;
    }
        
    public bool TryCalculateSize(ref IList<T> value, out int size)
    {
        size = sizeof(int); // size of the list
        if (value.Count == 0)
        {
            return true;
        }
            
        var referentialElement = value[0];
        if (_scalarSerializer.TryCalculateSize(ref referentialElement, out var elementSize))
        {
            size += value.Count * elementSize;
            return true;
        }
            
        return false;
    }

    public void WriteTo(ref IList<T> value, ref Span<byte> span)
    {
        // Write the size of the list
        var slice = span.Slice(0, sizeof(int));
        BitConverter.TryWriteBytes(slice, value.Count);
            
        // Write the elements of the list
        int offset = sizeof(int);
        var elementSize = (span.Length - offset) / value.Count;
        for (int i = 0; i < value.Count; i++)
        {
            var element = value[i];
            slice = span.Slice(offset, elementSize);
            _scalarSerializer.WriteTo(ref element, ref slice);
            offset += elementSize;
        }
    }

    public void WriteTo(ref IList<T> value, IBufferWriter<byte> buffer)
    {
        throw new NotImplementedException();
    }

    public IList<T> Deserialize(ReadOnlySpan<byte> buffer)
    {
        // Read the size of the list
        var slice = buffer.Slice(0, sizeof(int));
        var size = BitConverter.ToInt32(slice);

        var list = new List<T>(size);
            
        // Read the elements of the list
        int offset = sizeof(int);
        var elementSize = (buffer.Length - offset) / size;
        for (int i = 0; i < size; i++)
        {
            slice = buffer.Slice(offset, elementSize);
            var element = _scalarSerializer.Deserialize(slice);
            list.Add(element);
            offset += elementSize;
        }

        return list;
    }
}