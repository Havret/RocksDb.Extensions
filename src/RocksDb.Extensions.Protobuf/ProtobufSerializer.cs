using System;
using System.Buffers;
using Google.Protobuf;

namespace RocksDb.Extensions.Protobuf;

internal class ProtobufSerializer<T> : ISerializer<T> where T : class, IMessage, new()
{
    public bool TryCalculateSize(ref T value, out int size)
    {
        size = value.CalculateSize();
        return true;
    }

    public void WriteTo(ref T value, ref Span<byte> span)
    {
        value.WriteTo(span);
    }

    public void WriteTo(ref T value, IBufferWriter<byte> buffer)
    {
        throw new NotImplementedException();
    }

    public T Deserialize(ReadOnlySpan<byte> buffer)
    {
        var message = new T();
        message.MergeFrom(buffer);
        return message;
    }
}
