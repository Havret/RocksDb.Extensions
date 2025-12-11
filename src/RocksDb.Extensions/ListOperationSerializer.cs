using System.Buffers;
using RocksDb.Extensions.MergeOperators;

namespace RocksDb.Extensions;

/// <summary>
/// Serializes CollectionOperation&lt;T&gt; which contains an operation type (Add/Remove) and a list of items.
/// </summary>
/// <remarks>
/// The serialized format consists of:
/// - 1 byte: Operation type (0 = Add, 1 = Remove)
/// - 4 bytes: Number of items
/// - For each item:
///   - 4 bytes: Size of the serialized item
///   - N bytes: Serialized item data
/// </remarks>
internal class ListOperationSerializer<T> : ISerializer<CollectionOperation<T>>
{
    private readonly ISerializer<T> _itemSerializer;

    public ListOperationSerializer(ISerializer<T> itemSerializer)
    {
        _itemSerializer = itemSerializer;
    }

    public bool TryCalculateSize(ref CollectionOperation<T> value, out int size)
    {
        // 1 byte for operation type + 4 bytes for count
        size = sizeof(byte) + sizeof(int);

        for (int i = 0; i < value.Items.Count; i++)
        {
            var item = value.Items[i];
            if (_itemSerializer.TryCalculateSize(ref item, out var itemSize))
            {
                size += sizeof(int); // size prefix for each item
                size += itemSize;
            }
        }

        return true;
    }

    public void WriteTo(ref CollectionOperation<T> value, ref Span<byte> span)
    {
        int offset = 0;

        // Write operation type (1 byte)
        span[offset] = (byte)value.Type;
        offset += sizeof(byte);

        // Write count
        var slice = span.Slice(offset, sizeof(int));
        BitConverter.TryWriteBytes(slice, value.Items.Count);
        offset += sizeof(int);

        // Write each item with size prefix
        for (int i = 0; i < value.Items.Count; i++)
        {
            var item = value.Items[i];
            if (_itemSerializer.TryCalculateSize(ref item, out var itemSize))
            {
                slice = span.Slice(offset, sizeof(int));
                BitConverter.TryWriteBytes(slice, itemSize);
                offset += sizeof(int);

                slice = span.Slice(offset, itemSize);
                _itemSerializer.WriteTo(ref item, ref slice);
                offset += itemSize;
            }
        }
    }

    public void WriteTo(ref CollectionOperation<T> value, IBufferWriter<byte> buffer)
    {
        throw new NotImplementedException();
    }

    public CollectionOperation<T> Deserialize(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        // Read operation type
        var operationType = (OperationType)buffer[offset];
        offset += sizeof(byte);

        // Read count
        var slice = buffer.Slice(offset, sizeof(int));
        var count = BitConverter.ToInt32(slice);
        offset += sizeof(int);

        // Read items
        var items = new List<T>(count);
        for (int i = 0; i < count; i++)
        {
            slice = buffer.Slice(offset, sizeof(int));
            var itemSize = BitConverter.ToInt32(slice);
            offset += sizeof(int);

            slice = buffer.Slice(offset, itemSize);
            var item = _itemSerializer.Deserialize(slice);
            items.Add(item);
            offset += itemSize;
        }

        return new CollectionOperation<T>(operationType, items);
    }
}
