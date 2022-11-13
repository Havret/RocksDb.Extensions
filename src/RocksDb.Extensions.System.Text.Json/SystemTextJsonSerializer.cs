using System;
using System.Buffers;
using System.Text.Json;

namespace RocksDb.Extensions.System.Text.Json;

internal class SystemTextJsonSerializer<T> : ISerializer<T>
{
    private readonly JsonSerializerOptions? _options;

    public SystemTextJsonSerializer(JsonSerializerOptions? options)
    {
        _options = options;
    }

    public bool TryCalculateSize(ref T value, out int size)
    {
        size = 0;
        return false;
    }

    public void WriteTo(ref T value, ref Span<byte> span)
    {
        throw new NotImplementedException();
    }

    public void WriteTo(ref T value, IBufferWriter<byte> buffer)
    {
        using var utf8JsonWriter = new Utf8JsonWriter(buffer);
        JsonSerializer.Serialize(utf8JsonWriter, value, _options);
    }

    public T Deserialize(ReadOnlySpan<byte> buffer)
    {
        return JsonSerializer.Deserialize<T>(buffer, _options)!;
    }
}
