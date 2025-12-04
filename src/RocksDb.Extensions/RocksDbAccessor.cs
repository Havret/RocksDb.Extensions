using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.HighPerformance.Buffers;
using RocksDbSharp;

namespace RocksDb.Extensions;

internal class RocksDbAccessor<TKey, TValue> : IRocksDbAccessor<TKey, TValue>, ISpanDeserializer<TValue>
{
    private protected const int MaxStackSize = 256;

    protected readonly ISerializer<TKey> KeySerializer;
    private readonly ISerializer<TValue> _valueSerializer;
    private protected readonly RocksDbContext RocksDbContext;
    private protected readonly ColumnFamily ColumnFamily;
    private readonly bool _checkIfExists;
    
    private readonly object _syncRoot = new();

    public RocksDbAccessor(RocksDbContext rocksDbContext,
        ColumnFamily columnFamily,
        ISerializer<TKey> keySerializer,
        ISerializer<TValue> valueSerializer)
    {
        RocksDbContext = rocksDbContext;
        ColumnFamily = columnFamily;
        KeySerializer = keySerializer;
        _valueSerializer = valueSerializer;

        _checkIfExists = typeof(TValue).IsValueType;
    }

    public void Remove(TKey key)
    {
        byte[]? rentedKeyBuffer = null;
        bool useSpan;

        // ReSharper disable once AssignmentInConditionalExpression
        Span<byte> keyBuffer = (useSpan = KeySerializer.TryCalculateSize(ref key, out var keySize))
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
                KeySerializer.WriteTo(ref key, ref keyBuffer);
            }
            else
            {
                keyBufferWriter = new ArrayPoolBufferWriter<byte>();
                KeySerializer.WriteTo(ref key, keyBufferWriter);
                keySpan = keyBufferWriter.WrittenSpan;
            }

            RocksDbContext.Db.Remove(keySpan, ColumnFamily.Handle, RocksDbContext.WriteOptions);
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
        Span<byte> keyBuffer = (useSpanAsKey = KeySerializer.TryCalculateSize(ref key, out var keySize))
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
                KeySerializer.WriteTo(ref key, ref keyBuffer);
            }
            else
            {
                keyBufferWriter = new ArrayPoolBufferWriter<byte>();
                KeySerializer.WriteTo(ref key, keyBufferWriter);
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

            RocksDbContext.Db.Put(keySpan, valueSpan, ColumnFamily.Handle, RocksDbContext.WriteOptions);
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
        Span<byte> keyBuffer = (useSpan = KeySerializer.TryCalculateSize(ref key, out var keySize))
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
                KeySerializer.WriteTo(ref key, ref keyBuffer);
            }
            else
            {
                keyBufferWriter = new ArrayPoolBufferWriter<byte>();
                KeySerializer.WriteTo(ref key, keyBufferWriter);
                keySpan = keyBufferWriter.WrittenSpan;
            }

            if (_checkIfExists && RocksDbContext.Db.HasKey(keySpan, ColumnFamily.Handle) == false)
            {
                value = default;
                return false;
            }

            value = RocksDbContext.Db.Get(keySpan, this, ColumnFamily.Handle);
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

        RocksDbContext.Db.Write(batch, RocksDbContext.WriteOptions);
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

        RocksDbContext.Db.Write(batch, RocksDbContext.WriteOptions);
    }

    public void PutRange(IReadOnlyList<(TKey key, TValue value)> items)
    {
        using var batch = new WriteBatch();
        for (var index = 0; index < items.Count; index++)
        {
            var (key, value) = items[index];
            AddToBatch(key, value, batch);
        }

        RocksDbContext.Db.Write(batch, RocksDbContext.WriteOptions);
    }

    private void AddToBatch(TKey key, TValue value, WriteBatch batch)
    {
        byte[]? rentedKeyBuffer = null;
        bool useSpanAsKey;
        // ReSharper disable once AssignmentInConditionalExpression
        Span<byte> keyBuffer = (useSpanAsKey = KeySerializer.TryCalculateSize(ref key, out var keySize))
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
                KeySerializer.WriteTo(ref key, ref keyBuffer);
            }
            else
            {
                keyBufferWriter = new ArrayPoolBufferWriter<byte>();
                KeySerializer.WriteTo(ref key, keyBufferWriter);
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

            _ = batch.Put(keySpan, valueSpan, ColumnFamily.Handle);
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
        using var iterator = RocksDbContext.Db.NewIterator(ColumnFamily.Handle);
        _ = iterator.SeekToFirst();
        while (iterator.Valid())
        {
            yield return KeySerializer.Deserialize(iterator.Key());
            _ = iterator.Next();
        }
    }

    public IEnumerable<TValue> GetAllValues()
    {
        using var iterator = RocksDbContext.Db.NewIterator(ColumnFamily.Handle);
        _ = iterator.SeekToFirst();
        while (iterator.Valid())
        {
            yield return iterator.Value(this);
            _ = iterator.Next();
        }
    }

    public int Count()
    {
        using var iterator = RocksDbContext.Db.NewIterator(ColumnFamily.Handle);
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
        Span<byte> keyBuffer = (useSpan = KeySerializer.TryCalculateSize(ref key, out var keySize))
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
                KeySerializer.WriteTo(ref key, ref keyBuffer);
            }
            else
            {
                keyBufferWriter = new ArrayPoolBufferWriter<byte>();
                KeySerializer.WriteTo(ref key, keyBufferWriter);
                keySpan = keyBufferWriter.WrittenSpan;
            }

            return RocksDbContext.Db.HasKey(keySpan, ColumnFamily.Handle);
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
        lock (_syncRoot)
        {
            var prevColumnFamilyHandle = ColumnFamily.Handle;
            RocksDbContext.Db.DropColumnFamily(ColumnFamily.Name);
        
            var cfOptions = RocksDbContext.CreateColumnFamilyOptions(ColumnFamily.Name);
            ColumnFamily.Handle = RocksDbContext.Db.CreateColumnFamily(cfOptions, ColumnFamily.Name);

            Native.Instance.rocksdb_column_family_handle_destroy(prevColumnFamilyHandle.Handle);
        }
    }
}