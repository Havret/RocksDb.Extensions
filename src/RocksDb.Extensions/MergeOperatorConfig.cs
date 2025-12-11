using System.Buffers;
using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance.Buffers;

namespace RocksDb.Extensions;

/// <summary>
/// Internal configuration for a merge operator associated with a column family.
/// </summary>
internal class MergeOperatorConfig
{
    /// <summary>
    /// Gets the name of the merge operator.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets the full merge callback delegate.
    /// </summary>
    public global::RocksDbSharp.MergeOperators.FullMergeFunc FullMerge { get; set; } = null!;

    /// <summary>
    /// Gets the partial merge callback delegate.
    /// </summary>
    public global::RocksDbSharp.MergeOperators.PartialMergeFunc PartialMerge { get; set; } = null!;
    
    internal static MergeOperatorConfig CreateMergeOperatorConfig<TValue, TOperand>(
        IMergeOperator<TValue, TOperand> mergeOperator, 
        ISerializer<TValue> valueSerializer,
        ISerializer<TOperand> operandSerializer)
    {
        return new MergeOperatorConfig
        {
            Name = mergeOperator.Name,
            FullMerge = (ReadOnlySpan<byte> _, bool hasExistingValue, ReadOnlySpan<byte> existingValue, global::RocksDbSharp.MergeOperators.OperandsEnumerator operands, out bool success) =>
            {
                return FullMergeCallback(hasExistingValue, existingValue, operands, mergeOperator, valueSerializer, operandSerializer, out success);
            },
            PartialMerge = (ReadOnlySpan<byte> _, global::RocksDbSharp.MergeOperators.OperandsEnumerator operands, out bool success) =>
            {
                return PartialMergeCallback(operands, mergeOperator, operandSerializer, out success);
            }
        };
    }
    
       private static byte[] FullMergeCallback<TValue, TOperand>(bool hasExistingValue,
        ReadOnlySpan<byte> existingValue,
        global::RocksDbSharp.MergeOperators.OperandsEnumerator operands,
        IMergeOperator<TValue, TOperand> mergeOperator,
        ISerializer<TValue> valueSerializer,
        ISerializer<TOperand> operandSerializer,
        out bool success)
    {
        success = true;
        
        var existing = hasExistingValue ? valueSerializer.Deserialize(existingValue) : default!;
        
        var operandArray = ArrayPool<TOperand>.Shared.Rent(operands.Count);
        try
        {
            for (int i = 0; i < operands.Count; i++)
            {
                operandArray[i] = operandSerializer.Deserialize(operands.Get(i));
            }

            var operandSpan = operandArray.AsSpan(0, operands.Count);
            var result = mergeOperator.FullMerge(existing, operandSpan);

            return SerializeValue(result, valueSerializer);
        }
        catch
        {
            success = false;
            return Array.Empty<byte>();
        }
        finally
        {
            ArrayPool<TOperand>.Shared.Return(operandArray, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<TOperand>());
        }
    }

    private static byte[] PartialMergeCallback<TValue, TOperand>(global::RocksDbSharp.MergeOperators.OperandsEnumerator operands,
        IMergeOperator<TValue, TOperand> mergeOperator,
        ISerializer<TOperand> operandSerializer,
        out bool success)
    {
        var operandArray = ArrayPool<TOperand>.Shared.Rent(operands.Count);
        try
        {
            for (int i = 0; i < operands.Count; i++)
            {
                operandArray[i] = operandSerializer.Deserialize(operands.Get(i));
            }

            var operandSpan = operandArray.AsSpan(0, operands.Count);
            var result = mergeOperator.PartialMerge(operandSpan);

            if (result == null)
            {
                success = false;
                return Array.Empty<byte>();
            }

            success = true;
            return SerializeValue(result, operandSerializer);
        }
        catch
        {
            success = false;
            return Array.Empty<byte>();
        }
        finally
        {
            ArrayPool<TOperand>.Shared.Return(operandArray, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<TOperand>());
        }
    }

    private static byte[] SerializeValue<T>(T value, ISerializer<T> serializer)
    {
        if (serializer.TryCalculateSize(ref value, out var size))
        {
            var buffer = new byte[size];
            var span = buffer.AsSpan();
            serializer.WriteTo(ref value, ref span);
            return buffer;
        }
        else
        {
            using var bufferWriter = new ArrayPoolBufferWriter<byte>();
            serializer.WriteTo(ref value, bufferWriter);
            return bufferWriter.WrittenSpan.ToArray();
        }
    }
}
