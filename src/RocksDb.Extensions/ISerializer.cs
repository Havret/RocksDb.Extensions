using System.Buffers;

namespace RocksDb.Extensions;

/// <summary>
/// Defines the contract for a serializer that can serialize and deserialize objects of type T to and from a byte buffer.
/// </summary>
/// <typeparam name="T">The type of object to be serialized.</typeparam>
public interface ISerializer<T>
{
    /// <summary>
    /// Attempts to calculate the size in bytes required to serialize the specified object.
    /// </summary>
    /// <param name="value">The object to be serialized.</param>
    /// <param name="size">The size of the buffer required to serialize the object.</param>
    /// <returns><c>true</c> if the size was calculated successfully; otherwise, <c>false</c>.</returns>
    bool TryCalculateSize(ref T value, out int size);
    
    /// <summary>
    /// Serializes the specified object to a byte buffer.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <param name="span">The byte span to write the serialized object to.</param>
    /// <remarks>
    /// This method will be used when <see cref="TryCalculateSize"/> returns <c>true</c>.
    /// </remarks>
    void WriteTo(ref T value, ref Span<byte> span);
    
    /// <summary>
    /// Serializes the specified object to a byte buffer using the provided buffer writer.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <param name="buffer">The <see cref="IBufferWriter{T}"/> to write the serialized object to.</param>
    /// <remarks>
    /// This method will be used when <see cref="TryCalculateSize"/> returns <c>false</c>.
    /// </remarks>
    void WriteTo(ref T value, IBufferWriter<byte> buffer);
    
    /// <summary>
    /// Deserializes an object from the provided byte span.
    /// </summary>
    /// <param name="buffer">The byte span containing the serialized object.</param>
    /// <returns>The deserialized object.</returns>
    T Deserialize(ReadOnlySpan<byte> buffer);
}
