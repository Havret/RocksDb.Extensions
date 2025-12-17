using System.Buffers;
using RocksDb.Extensions.MergeOperators;

namespace RocksDb.Extensions;

/// <summary>
/// Serializes CollectionOperation&lt;T&gt; which contains an operation type (Add/Remove) and a list of items.
/// </summary>
/// <remarks>
/// The serialized format consists of:
/// - 1 byte: Operation type (0 = Add, 1 = Remove)
/// - Remaining bytes: Serialized list using VariableSizeListSerializer format
/// </remarks>
internal class ListOperationSerializer<T> : ISerializer<CollectionOperation<T>>
{
    private readonly ISerializer<IList<T>> _listSerializer;

    public ListOperationSerializer(ISerializer<T> itemSerializer)
    {
        _listSerializer = new VariableSizeListSerializer<T>(itemSerializer);
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
