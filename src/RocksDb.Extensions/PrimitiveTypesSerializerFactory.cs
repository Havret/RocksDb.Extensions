using System.Buffers;
using System.Text;

namespace RocksDb.Extensions;

/// <summary>
/// Factory for creating serializers for primitive types such as int, long, and string.
/// </summary>
public class PrimitiveTypesSerializerFactory : ISerializerFactory
{
    /// <inheritdoc/>
    public bool CanCreateSerializer<T>()
    {
        var type = typeof(T);
        if (type == typeof(int))
        {
            return true;
        }
        if (type == typeof(long))
        {
            return true;
        }
        if (type == typeof(string))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public ISerializer<T> CreateSerializer<T>()
    {
        var type = typeof(T);
        if (type == typeof(int))
        {
            return (ISerializer<T>)Activator.CreateInstance(typeof(IntSerializer));
        }
        if (type == typeof(long))
        {
            return (ISerializer<T>)Activator.CreateInstance(typeof(LongSerializer));
        }
        if (type == typeof(string))
        {
            return (ISerializer<T>)Activator.CreateInstance(typeof(StringSerializer));
        }

        throw new ArgumentException($"Type {type.FullName} is not supported.");
    }

    private class IntSerializer : ISerializer<int>
    {
        public bool TryCalculateSize(ref int value, out int size)
        {
            size = sizeof(int);
            return true;
        }

        public void WriteTo(ref int value, ref Span<byte> span)
        {
            BitConverter.TryWriteBytes(span, value);
        }

        public void WriteTo(ref int value, IBufferWriter<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public int Deserialize(ReadOnlySpan<byte> buffer)
        {
            return BitConverter.ToInt32(buffer);
        }
    }

    private class LongSerializer : ISerializer<long>
    {
        public bool TryCalculateSize(ref long value, out int size)
        {
            size = sizeof(long);
            return true;
        }

        public void WriteTo(ref long value, ref Span<byte> span)
        {
            BitConverter.TryWriteBytes(span, value);
        }

        public void WriteTo(ref long value, IBufferWriter<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public long Deserialize(ReadOnlySpan<byte> buffer)
        {
            return BitConverter.ToInt64(buffer);
        }
    }

    private class StringSerializer : ISerializer<string>
    {
        public bool TryCalculateSize(ref string value, out int size)
        {
            size = Encoding.UTF8.GetByteCount(value);
            return true;
        }

        public void WriteTo(ref string value, ref Span<byte> span)
        {
            _ = Encoding.UTF8.GetBytes(value, span);
        }

        public void WriteTo(ref string value, IBufferWriter<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public string Deserialize(ReadOnlySpan<byte> buffer)
        {
            return Encoding.UTF8.GetString(buffer);
        }
    }
}
