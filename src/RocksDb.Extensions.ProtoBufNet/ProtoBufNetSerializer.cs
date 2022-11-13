using System;
using System.Buffers;
using ProtoBuf;

namespace RocksDb.Extensions.ProtoBufNet;

internal class ProtoBufNetSerializer<T> : ISerializer<T>
{
    public bool TryCalculateSize(ref T value, out int size)
    {
        size = -1;
        return false;
    }

    public void WriteTo(ref T value, ref Span<byte> span)
    {
        throw new NotImplementedException();
    }

    public void WriteTo(ref T value, IBufferWriter<byte> buffer)
    {
        Serializer.Serialize(buffer, value);
    }

    public T Deserialize(ReadOnlySpan<byte> buffer)
    {
        return Serializer.Deserialize<T>(buffer);
    }
}
