using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.HighPerformance.Buffers;
using RocksDbSharp;

namespace RocksDb.Extensions;

internal class RocksDbAccessor<TKey, TValue> : IRocksDbAccessor<TKey, TValue>, ISpanDeserializer<TValue>
{
    private protected const int MaxStackSize = 256;

    protected readonly ISerializer<TKey> _keySerializer;
    protected private readonly ISerializer<TValue> _valueSerializer;
    protected private readonly RocksDbContext _rocksDbContext;
    private protected readonly ColumnFamily _columnFamily;
    private readonly bool _checkIfExists;

    public RocksDbAccessor(RocksDbContext rocksDbContext,
        ColumnFamily columnFamily,
        ISerializer<TKey> keySerializer,
        ISerializer<TValue> valueSerializer)
    {
        _rocksDbContext = rocksDbContext;
        _columnFamily = columnFamily;
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

            _rocksDbContext.Db.Remove(keySpan, _columnFamily.Handle);
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

            _rocksDbContext.Db.Put(keySpan, valueSpan, _columnFamily.Handle);
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

            if (_checkIfExists && _rocksDbContext.Db.HasKey(keySpan, _columnFamily.Handle) == false)
            {
                value = default;
                return false;
            }

            value = _rocksDbContext.Db.Get(keySpan, this, _columnFamily.Handle);
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

    TValue ISpanDeserializer<TValue>.Deserialize(ReadOnlySpan<byte> buffer)
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

        _rocksDbContext.Db.Write(batch);
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

        _rocksDbContext.Db.Write(batch);
    }

    public void PutRange(IReadOnlyList<(TKey key, TValue value)> items)
    {
        using var batch = new WriteBatch();
        for (var index = 0; index < items.Count; index++)
        {
            var (key, value) = items[index];
            AddToBatch(key, value, batch);
        }

        _rocksDbContext.Db.Write(batch);
    }

    private void AddToBatch(TKey key, TValue value, WriteBatch batch)
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

            _ = batch.Put(keySpan, valueSpan, _columnFamily.Handle);
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

    public IEnumerable<TKey> GetAllKeys()
    {
        using var iterator = _rocksDbContext.Db.NewIterator(_columnFamily.Handle);
        _ = iterator.SeekToFirst();
        while (iterator.Valid())
        {
            yield return _keySerializer.Deserialize(iterator.Key());
            _ = iterator.Next();
        }
    }

    public IEnumerable<TValue> GetAllValues()
    {
        using var iterator = _rocksDbContext.Db.NewIterator(_columnFamily.Handle);
        _ = iterator.SeekToFirst();
        while (iterator.Valid())
        {
            yield return iterator.Value(this);
            _ = iterator.Next();
        }
    }

    public int Count()
    {
        using var iterator = _rocksDbContext.Db.NewIterator(_columnFamily.Handle);
        _ = iterator.SeekToFirst();
        var count = 0;
        while (iterator.Valid())
        {
            count++;
            _ = iterator.Next();
        }

        return count;
    }

    public bool HasKey(TKey key)
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

            return _rocksDbContext.Db.HasKey(keySpan, _columnFamily.Handle);
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

    public void Clear()
    {
        var prevColumnFamilyHandle = _columnFamily.Handle;
        _rocksDbContext.Db.DropColumnFamily(_columnFamily.Name);
        _columnFamily.Handle = _rocksDbContext.Db.CreateColumnFamily(_rocksDbContext.ColumnFamilyOptions, _columnFamily.Name);

        Native.Instance.rocksdb_column_family_handle_destroy(prevColumnFamilyHandle.Handle);
    }

    public void Merge(TKey key, TValue operand)
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

        var value = operand;
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

            _rocksDbContext.Db.Merge(keySpan, valueSpan, _columnFamily.Handle);
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
}

