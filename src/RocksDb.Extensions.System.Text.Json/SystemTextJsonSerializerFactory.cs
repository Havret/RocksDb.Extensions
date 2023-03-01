using System.Text.Json;

namespace RocksDb.Extensions.System.Text.Json;

/// <summary>
/// A serializer factory implementation that creates instances of SystemTextJsonSerializer for any type.
/// </summary>
public class SystemTextJsonSerializerFactory : ISerializerFactory
{
    private readonly JsonSerializerOptions? _options;
    
    /// <summary>
    /// Initializes a new instance of the SystemTextJsonSerializerFactory class with optional JsonSerializerOptions.
    /// </summary>
    /// <param name="options">Optional JsonSerializerOptions to use for serialization.</param>
    public SystemTextJsonSerializerFactory(JsonSerializerOptions? options = null)
    {
        _options = options;
    }

    /// <inheritdoc/>
    public bool CanCreateSerializer<T>()
    {
        return true;
    }

    /// <inheritdoc/>
    public ISerializer<T> CreateSerializer<T>()
    {
        var type = typeof(SystemTextJsonSerializer<>).MakeGenericType(typeof(T));
        return (ISerializer<T>) Activator.CreateInstance(type, _options);
    }
}