using System.Buffers;
using System.Text;

namespace RocksDb.Extensions;

/// <summary>
/// Factory for creating serializers for primitive types such as int, long, bool and string.
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
        if (type == typeof(bool))
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
            return (ISerializer<T>) new IntSerializer();
        }
        if (type == typeof(long))
        {
            return (ISerializer<T>) new LongSerializer();
        }
        if (type == typeof(string))
        {
            return (ISerializer<T>) new StringSerializer();
        }
        if (type == typeof(bool))
        {
            return (ISerializer<T>) new BoolSerializer();
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
    
    private class BoolSerializer : ISerializer<bool>
    {
        public bool TryCalculateSize(ref bool value, out int size)
        {
            size = sizeof(bool);
            return true;
        }

        public void WriteTo(ref bool value, ref Span<byte> span)
        {
            BitConverter.TryWriteBytes(span, value);
        }

        public void WriteTo(ref bool value, IBufferWriter<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public bool Deserialize(ReadOnlySpan<byte> buffer)
        {
            return BitConverter.ToBoolean(buffer);
        }
    }
}
