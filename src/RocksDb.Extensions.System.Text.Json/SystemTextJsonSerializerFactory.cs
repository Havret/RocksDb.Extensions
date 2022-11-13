using System;
using System.Text.Json;

namespace RocksDb.Extensions.System.Text.Json;

public class SystemTextJsonSerializerFactory : ISerializerFactory
{
    private readonly JsonSerializerOptions? _options;

    public SystemTextJsonSerializerFactory(JsonSerializerOptions? options = null)
    {
        _options = options;
    }

    public bool CanCreateSerializer<T>()
    {
        return true;
    }

    public ISerializer<T> CreateSerializer<T>()
    {
        var type = typeof(SystemTextJsonSerializer<>).MakeGenericType(typeof(T));
        return (ISerializer<T>)Activator.CreateInstance(type, _options);
    }


}
