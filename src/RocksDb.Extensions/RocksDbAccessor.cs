using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.HighPerformance.Buffers;
using RocksDbSharp;

namespace RocksDb.Extensions;

internal class RocksDbAccessor<TKey, TValue> : IRocksDbAccessor<TKey, TValue>, ISpanDeserializer<TValue>
{
    private const int MaxStackSize = 256;

    private readonly ISerializer<TKey> _keySerializer;
    private readonly ISerializer<TValue> _valueSerializer;
    private readonly RocksDbSharp.RocksDb _rocksDb;
    private readonly ColumnFamilyHandle _columnFamilyHandle;
    private readonly bool _checkIfExists;

    public RocksDbAccessor(RocksDbSharp.RocksDb rocksDb,
        ColumnFamilyHandle columnFamilyHandle,
        ISerializer<TKey> keySerializer,
        ISerializer<TValue> valueSerializer)
    {
        _rocksDb = rocksDb;
        _columnFamilyHandle = columnFamilyHandle;
        _keySerializer = keySerializer;
        _valueSerializer = valueSerializer;

        _checkIfExists = typeof(TValue).IsValueType;
    }

    public void Remove(TKey key)
    {
        byte[]? rentedKeyBuffer = null;
        bool useSpan;

        // ReSharper disable once AssignmentInConditionalExpression
        Span<byte> keyBuffer = (useSpan = _keySerializer.TryCalculateSize(ref key, out var keySize))
            ? keySize < MaxStackSize
                ? stackalloc byte[keySize]
                : (rentedKeyBuffer = ArrayPool<byte>.Shared.Rent(keySize)).AsSpan(0, keySize)
            : Span<byte>.Empty;

        ReadOnlySpan<byte> keySpan = keyBuffer;
        ArrayPoolBufferWriter<byte>? keyBufferWriter = null;

        try
        {
            if (useSpan)
            {
                _keySerializer.WriteTo(ref key, ref keyBuffer);
            }
            else
            {
                keyBufferWriter = new ArrayPoolBufferWriter<byte>();
                _keySerializer.WriteTo(ref key, keyBufferWriter);
                keySpan = keyBufferWriter.WrittenSpan;
            }

            _rocksDb.Remove(keySpan, _columnFamilyHandle);
        }
        finally
        {
            keyBufferWriter?.Dispose();
            if (rentedKeyBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedKeyBuffer);
            }
        }
    }

    public void Put(TKey key, TValue value)
    {
        byte[]? rentedKeyBuffer = null;
        bool useSpanAsKey;
        // ReSharper disable once AssignmentInConditionalExpression
        Span<byte> keyBuffer = (useSpanAsKey = _keySerializer.TryCalculateSize(ref key, out var keySize))
            ? keySize < MaxStackSize
                ? stackalloc byte[keySize]
                : (rentedKeyBuffer = ArrayPool<byte>.Shared.Rent(keySize)).AsSpan(0, keySize)
            : Span<byte>.Empty;

        ReadOnlySpan<byte> keySpan = keyBuffer;
        ArrayPoolBufferWriter<byte>? keyBufferWriter = null;

        byte[]? rentedValueBuffer = null;
        bool useSpanAsValue;
        // ReSharper disable once AssignmentInConditionalExpression
        Span<byte> valueBuffer = (useSpanAsValue = _valueSerializer.TryCalculateSize(ref value, out var valueSize))
            ? valueSize < MaxStackSize
                ? stackalloc byte[valueSize]
                : (rentedValueBuffer = ArrayPool<byte>.Shared.Rent(valueSize)).AsSpan(0, valueSize)
            : Span<byte>.Empty;


        ReadOnlySpan<byte> valueSpan = valueBuffer;
        ArrayPoolBufferWriter<byte>? valueBufferWriter = null;

        try
        {
            if (useSpanAsKey)
            {
                _keySerializer.WriteTo(ref key, ref keyBuffer);
            }
            else
            {
                keyBufferWriter = new ArrayPoolBufferWriter<byte>();
                _keySerializer.WriteTo(ref key, keyBufferWriter);
                keySpan = keyBufferWriter.WrittenSpan;
            }

            if (useSpanAsValue)
            {
                _valueSerializer.WriteTo(ref value, ref valueBuffer);
            }
            else
            {
                valueBufferWriter = new ArrayPoolBufferWriter<byte>();
                _valueSerializer.WriteTo(ref value, valueBufferWriter);
                valueSpan = valueBufferWriter.WrittenSpan;
            }

            _rocksDb.Put(keySpan, valueSpan, _columnFamilyHandle);
        }
        finally
        {
            keyBufferWriter?.Dispose();
            valueBufferWriter?.Dispose();
            if (rentedKeyBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedKeyBuffer);
            }

            if (rentedValueBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedValueBuffer);
            }
        }
    }

    public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        byte[]? rentedKeyBuffer = null;
        bool useSpan;

        // ReSharper disable once AssignmentInConditionalExpression
        Span<byte> keyBuffer = (useSpan = _keySerializer.TryCalculateSize(ref key, out var keySize))
            ? keySize < MaxStackSize
                ? stackalloc byte[keySize]
                : (rentedKeyBuffer = ArrayPool<byte>.Shared.Rent(keySize)).AsSpan(0, keySize)
            : Span<byte>.Empty;

        ReadOnlySpan<byte> keySpan = keyBuffer;
        ArrayPoolBufferWriter<byte>? keyBufferWriter = null;

        try
        {
            if (useSpan)
            {
                _keySerializer.WriteTo(ref key, ref keyBuffer);
            }
            else
            {
                keyBufferWriter = new ArrayPoolBufferWriter<byte>();
                _keySerializer.WriteTo(ref key, keyBufferWriter);
                keySpan = keyBufferWriter.WrittenSpan;
            }

            if (_checkIfExists && _rocksDb.HasKey(keySpan, _columnFamilyHandle) == false)
            {
                value = default;
                return false;
            }

            value = _rocksDb.Get(keySpan, this, _columnFamilyHandle);
            return value != null;
        }
        finally
        {
            keyBufferWriter?.Dispose();
            if (rentedKeyBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedKeyBuffer);
            }
        }
    }

    public TValue Deserialize(ReadOnlySpan<byte> buffer)
    {
        return _valueSerializer.Deserialize(buffer);
    }

    public void PutRange(ReadOnlySpan<TKey> keys, ReadOnlySpan<TValue> values)
    {
        if (keys.Length != values.Length)
        {
            throw new ArgumentException("keys.Count != values.Count");
        }

        using var batch = new WriteBatch();
        for (int i = 0; i < keys.Length; i++)
        {
            AddToBatch(keys[i], values[i], batch);
        }

        _rocksDb.Write(batch);
    }

    public void PutRange(ReadOnlySpan<TValue> values, Func<TValue, TKey> keySelector)
    {
        using var batch = new WriteBatch();
        for (int i = 0; i < values.Length; i++)
        {
            var value = values[i];
            var key = keySelector(value);
            AddToBatch(key, value, batch);
        }

        _rocksDb.Write(batch);
    }

    private void AddToBatch(TKey key, TValue value, WriteBatch batch)
    {
        // I don't think we can use the same optimizations as for scalar operations
        // array pooling seams infeasible as we would need to keep pooled arrays until
        // the whole batch is flushed (I'm not 100% sure that's the case - sth to be checked)
        ReadOnlySpan<byte> keySpan;
        if (_keySerializer.TryCalculateSize(ref key, out var keySize))
        {
            var keyBuffer = new byte[keySize].AsSpan();
            _keySerializer.WriteTo(ref key, ref keyBuffer);
            keySpan = keyBuffer;
        }
        else
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            _keySerializer.WriteTo(ref key, bufferWriter);
            keySpan = bufferWriter.WrittenSpan;
        }

        ReadOnlySpan<byte> valueSpan;
        if (_valueSerializer.TryCalculateSize(ref value, out var valueSize))
        {
            var valueBuffer = new byte[valueSize].AsSpan();
            _valueSerializer.WriteTo(ref value, ref valueBuffer);
            valueSpan = valueBuffer;
        }
        else
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            _valueSerializer.WriteTo(ref value, bufferWriter);
            valueSpan = bufferWriter.WrittenSpan;
        }

        _ = batch.Put(keySpan, valueSpan, _columnFamilyHandle);
    }

    public IEnumerable<TValue> GetAll()
    {
        using var iterator = _rocksDb.NewIterator(_columnFamilyHandle);
        _ = iterator.SeekToFirst();
        while (iterator.Valid())
        {
            yield return iterator.Value(this);
            _ = iterator.Next();
        }
    }
}

