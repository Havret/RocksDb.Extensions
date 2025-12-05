using System.Buffers;
using RocksDb.Extensions.MergeOperators;

namespace RocksDb.Extensions;

/// <summary>
/// Serializes ListOperation&lt;T&gt; which contains an operation type (Add/Remove) and a list of items.
/// </summary>
/// <remarks>
/// The serialized format consists of:
/// - 1 byte: Operation type (0 = Add, 1 = Remove)
/// - 4 bytes: Number of items
/// - For each item:
///   - 4 bytes: Size of the serialized item
///   - N bytes: Serialized item data
/// </remarks>
internal class ListOperationSerializer<T> : ISerializer<ListOperation<T>>
{
    private readonly ISerializer<T> _itemSerializer;

    public ListOperationSerializer(ISerializer<T> itemSerializer)
    {
        _itemSerializer = itemSerializer;
    }

    public bool TryCalculateSize(ref ListOperation<T> value, out int size)
    {
        // 1 byte for operation type + 4 bytes for count
        size = sizeof(byte) + sizeof(int);

        for (int i = 0; i < value.Items.Count; i++)
        {
            var item = value.Items[i];
            if (!_itemSerializer.TryCalculateSize(ref item, out var itemSize))
            {
                // If any item can't have its size calculated, we can't calculate the total size
                size = 0;
                return false;
            }
            size += sizeof(int); // size prefix for each item
            size += itemSize;
        }

        return true;
    }

    public void WriteTo(ref ListOperation<T> value, ref Span<byte> span)
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
            if (!_itemSerializer.TryCalculateSize(ref item, out var itemSize))
            {
                throw new InvalidOperationException($"Cannot calculate size for item at index {i}. " +
                    "All items must support size calculation when using span-based serialization.");
            }
            
            slice = span.Slice(offset, sizeof(int));
            BitConverter.TryWriteBytes(slice, itemSize);
            offset += sizeof(int);

            slice = span.Slice(offset, itemSize);
            _itemSerializer.WriteTo(ref item, ref slice);
            offset += itemSize;
        }
    }

    public void WriteTo(ref ListOperation<T> value, IBufferWriter<byte> buffer)
    {
        // Write operation type (1 byte)
        var opSpan = buffer.GetSpan(sizeof(byte));
        opSpan[0] = (byte)value.Type;
        buffer.Advance(sizeof(byte));

        // Write count (4 bytes)
        var countSpan = buffer.GetSpan(sizeof(int));
        BitConverter.TryWriteBytes(countSpan, value.Items.Count);
        buffer.Advance(sizeof(int));

        // Write each item with size prefix and data
        for (int i = 0; i < value.Items.Count; i++)
        {
            var item = value.Items[i];
            if (_itemSerializer.TryCalculateSize(ref item, out var itemSize))
            {
                // Write size prefix (4 bytes)
                var sizeSpan = buffer.GetSpan(sizeof(int));
                BitConverter.TryWriteBytes(sizeSpan, itemSize);
                buffer.Advance(sizeof(int));

                // Write item data
                var itemSpan = buffer.GetSpan(itemSize);
                var tmpSpan = itemSpan.Slice(0, itemSize);
                _itemSerializer.WriteTo(ref item, ref tmpSpan);
                buffer.Advance(itemSize);
            }
            else
            {
                throw new InvalidOperationException($"Cannot calculate size for item at index {i}. " +
                    "All items must support size calculation for serialization.");
            }
        }
    }

    public ListOperation<T> Deserialize(ReadOnlySpan<byte> buffer)
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

        return new ListOperation<T>(operationType, items);
    }
}
