using System.Buffers;

namespace RocksDb.Extensions;

public interface ISerializer<T>
{
    bool TryCalculateSize(ref T value, out int size);
    void WriteTo(ref T value, ref Span<byte> span);
    void WriteTo(ref T value, IBufferWriter<byte> buffer);
    T Deserialize(ReadOnlySpan<byte> buffer);
}
