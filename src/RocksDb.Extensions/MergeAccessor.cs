using System.Buffers;
using CommunityToolkit.HighPerformance.Buffers;

namespace RocksDb.Extensions;

internal class MergeAccessor<TKey, TValue, TOperand> : RocksDbAccessor<TKey, TValue>, IMergeAccessor<TKey, TValue, TOperand>
{
    private readonly ISerializer<TOperand> _operandSerializer;

    public MergeAccessor(
        RocksDbContext db,
        ColumnFamily columnFamily,
        ISerializer<TKey> keySerializer,
        ISerializer<TValue> valueSerializer,
        ISerializer<TOperand> operandSerializer) : base(db, columnFamily, keySerializer, valueSerializer)
    {
        _operandSerializer = operandSerializer;
    }

    public void Merge(TKey key, TOperand operand)
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

        byte[]? rentedOperandBuffer = null;
        bool useSpanAsOperand;
        // ReSharper disable once AssignmentInConditionalExpression
        Span<byte> operandBuffer = (useSpanAsOperand = _operandSerializer.TryCalculateSize(ref operand, out var operandSize))
            ? operandSize < MaxStackSize
                ? stackalloc byte[operandSize]
                : (rentedOperandBuffer = ArrayPool<byte>.Shared.Rent(operandSize)).AsSpan(0, operandSize)
            : Span<byte>.Empty;


        ReadOnlySpan<byte> operandSpan = operandBuffer;
        ArrayPoolBufferWriter<byte>? operandBufferWriter = null;

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

            if (useSpanAsOperand)
            {
                _operandSerializer.WriteTo(ref operand, ref operandBuffer);
            }
            else
            {
                operandBufferWriter = new ArrayPoolBufferWriter<byte>();
                _operandSerializer.WriteTo(ref operand, operandBufferWriter);
                operandSpan = operandBufferWriter.WrittenSpan;
            }

            _rocksDbContext.Db.Merge(keySpan, operandSpan, _columnFamily.Handle);
        }
        finally
        {
            keyBufferWriter?.Dispose();
            operandBufferWriter?.Dispose();
            if (rentedKeyBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedKeyBuffer);
            }

            if (rentedOperandBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedOperandBuffer);
            }
        }
    }
}
