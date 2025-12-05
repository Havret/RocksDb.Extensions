using System.Buffers;

namespace RocksDb.Extensions;

/// <summary>
/// Serializes lists containing variable-size elements like strings or complex objects where each element
/// may occupy a different number of bytes when serialized.
/// </summary>
/// <remarks>
/// Use this serializer for lists containing elements that may have different sizes (strings, nested objects, etc.).
/// The serialized format consists of:
/// - 4 bytes: List length (number of elements)
/// - For each element:
///   - 4 bytes: Size of the serialized element
///   - N bytes: Serialized element data
/// </remarks>
internal class VariableSizeListSerializer<T> : ISerializer<IList<T>>
{
    private readonly ISerializer<T> _scalarSerializer;

    public VariableSizeListSerializer(ISerializer<T> scalarSerializer)
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

        for (int i = 0; i < value.Count; i++)
        {
            var element = value[i];
            if (_scalarSerializer.TryCalculateSize(ref element, out var elementSize))
            {
                size += sizeof(int);
                size += elementSize;
            }
        }

        return true;
    }

    public void WriteTo(ref IList<T> value, ref Span<byte> span)
    {
        // Write the size of the list
        var slice = span.Slice(0, sizeof(int));
        BitConverter.TryWriteBytes(slice, value.Count);
        
        // Write the elements of the list
        int offset = sizeof(int);
        for (int i = 0; i < value.Count; i++)
        {
            var element = value[i];
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

    public void WriteTo(ref IList<T> value, IBufferWriter<byte> buffer)
    {
        throw new NotImplementedException();
    }

    public IList<T> Deserialize(ReadOnlySpan<byte> buffer)
    {
        // DEBUG: Log buffer info
        Console.WriteLine($"[VariableSizeListSerializer.Deserialize] buffer.Length={buffer.Length}");
        if (buffer.Length > 0 && buffer.Length < 100)
        {
            Console.WriteLine($"[VariableSizeListSerializer.Deserialize] bytes={BitConverter.ToString(buffer.ToArray())}");
        }
        
        // Read the size of the list
        var slice = buffer.Slice(0, sizeof(int));
        var size = BitConverter.ToInt32(slice);
        Console.WriteLine($"[VariableSizeListSerializer.Deserialize] list size={size}");

        var list = new List<T>(size);
        
        // Read the elements of the list
        int offset = sizeof(int);
        for (int i = 0; i < size; i++)
        {
            slice = buffer.Slice(offset, sizeof(int));
            var elementSize = BitConverter.ToInt32(slice);
            offset += sizeof(int);
            
            slice = buffer.Slice(offset, elementSize);
            var element = _scalarSerializer.Deserialize(slice);
            list.Add(element);
            offset += elementSize;
        }

        return list;
    }
}