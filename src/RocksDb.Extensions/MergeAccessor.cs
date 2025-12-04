using System.Buffers;
using CommunityToolkit.HighPerformance.Buffers;
using RocksDbSharp;

namespace RocksDb.Extensions;

internal class MergeAccessor<TKey, TOperand> : IMergeAccessor<TKey, TOperand>
{
    private const int MaxStackSize = 256;

    private readonly ISerializer<TKey> _keySerializer;
    private readonly ISerializer<TOperand> _operandSerializer;
    private readonly RocksDbSharp.RocksDb _db;
    private readonly ColumnFamilyHandle _columnFamilyHandle;

    public MergeAccessor(
        RocksDbSharp.RocksDb db,
        ColumnFamilyHandle columnFamilyHandle,
        ISerializer<TKey> keySerializer,
        ISerializer<TOperand> operandSerializer)
    {
        _db = db;
        _columnFamilyHandle = columnFamilyHandle;
        _keySerializer = keySerializer;
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

            _db.Merge(keySpan, operandSpan, _columnFamilyHandle);
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
