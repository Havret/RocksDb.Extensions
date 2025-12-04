using System.Buffers;
using RocksDb.Extensions.MergeOperators;

namespace RocksDb.Extensions;

/// <summary>
/// Serializes CollectionOperation&lt;T&gt; which contains an operation type (Add/Remove) and a list of items.
/// </summary>
/// <remarks>
/// <para>
/// The serialized format consists of:
/// - 1 byte: Operation type (0 = Add, 1 = Remove)
/// - Remaining bytes: Serialized list using FixedSizeListSerializer (for primitives) or VariableSizeListSerializer (for complex types)
/// </para>
/// <para>
/// Space efficiency optimization:
/// - For primitive types (int, long, bool, etc.), uses FixedSizeListSerializer which stores:
///   - 4 bytes: list count
///   - N * elementSize bytes: all elements (no per-element size prefix)
///   Example: List&lt;int&gt; with 3 elements = 4 + (3 * 4) = 16 bytes
/// </para>
/// <para>
/// - For non-primitive types (strings, objects, protobuf messages), uses VariableSizeListSerializer which stores:
///   - 4 bytes: list count
///   - For each element: 4 bytes size prefix + element data
///   Example: List&lt;string&gt; with ["ab", "cde"] = 4 + (4+2) + (4+3) = 17 bytes
/// </para>
/// </remarks>
internal class ListOperationSerializer<T> : ISerializer<CollectionOperation<T>>
{
    private readonly ISerializer<IList<T>> _listSerializer;

    public ListOperationSerializer(ISerializer<T> itemSerializer)
    {
        // Use FixedSizeListSerializer for primitive types to avoid storing size for each element
        // Use VariableSizeListSerializer for non-primitive types where elements may vary in size
        _listSerializer = typeof(T).IsPrimitive
            ? new FixedSizeListSerializer<T>(itemSerializer)
            : new VariableSizeListSerializer<T>(itemSerializer);
    }

    public bool TryCalculateSize(ref CollectionOperation<T> value, out int size)
    {
        // 1 byte for operation type + size of the list
        size = sizeof(byte);
        
        var items = value.Items;
        if (_listSerializer.TryCalculateSize(ref items, out var listSize))
        {
            size += listSize;
            return true;
        }

        return false;
    }

    public void WriteTo(ref CollectionOperation<T> value, ref Span<byte> span)
    {
        // Write operation type (1 byte)
        span[0] = (byte)value.Type;

        // Write the list using the list serializer
        var listSpan = span.Slice(sizeof(byte));
        var items = value.Items;
        _listSerializer.WriteTo(ref items, ref listSpan);
    }

    public void WriteTo(ref CollectionOperation<T> value, IBufferWriter<byte> buffer)
    {
        throw new NotImplementedException();
    }

    public CollectionOperation<T> Deserialize(ReadOnlySpan<byte> buffer)
    {
        // Read operation type
        var operationType = (OperationType)buffer[0];

        // Read the list using the list serializer
        var listBuffer = buffer.Slice(sizeof(byte));
        var items = _listSerializer.Deserialize(listBuffer);

        return new CollectionOperation<T>(operationType, items);
    }
}
