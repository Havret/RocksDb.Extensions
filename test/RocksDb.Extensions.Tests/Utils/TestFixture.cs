using Microsoft.Extensions.DependencyInjection;
using RocksDb.Extensions.Protobuf;

namespace RocksDb.Extensions.Tests.Utils;

public class TestFixture : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    private TestFixture(ServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static TestFixture Create(Action<IRocksDbBuilder> configureRocksDbBuilder, Action<RocksDbOptions>? configureRocksDbOptions = null)
    {
        var services = new ServiceCollection();
        var rocksDbBuilder = services.AddRocksDb(options =>
        {
            options.Path = $"./{Guid.NewGuid().ToString()}";
            options.SerializerFactories.Add(new PrimitiveTypesSerializerFactory());
            options.SerializerFactories.Add(new ProtobufSerializerFactory());
            configureRocksDbOptions?.Invoke(options);
        });
        configureRocksDbBuilder.Invoke(rocksDbBuilder);
        var serviceProvider = services.BuildServiceProvider();
        return new TestFixture(serviceProvider);
    }

    public T GetStore<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    public IServiceProvider ServiceProvider => _serviceProvider;

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}
