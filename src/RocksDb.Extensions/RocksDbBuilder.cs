using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace RocksDb.Extensions;

internal class RocksDbBuilder : IRocksDbBuilder
{
    private readonly IServiceCollection _serviceCollection;
    private readonly HashSet<string> _columnFamilyLookup = new(StringComparer.InvariantCultureIgnoreCase);

    public RocksDbBuilder(IServiceCollection serviceCollection)
    {
        _serviceCollection = serviceCollection;
    }

    public IRocksDbBuilder AddStore<TKey, TValue, TStore>(string columnFamily) where TStore : RocksDbStore<TKey, TValue>
    {
        if (!_columnFamilyLookup.Add(columnFamily))
        {
            throw new InvalidOperationException($"{columnFamily} is already registered.");
        }

        _ = _serviceCollection.Configure<RocksDbOptions>(options => { options.ColumnFamilies.Add(columnFamily); });

        _ = _serviceCollection.AddSingleton(provider =>
        {
            var rocksDbContext = provider.GetRequiredService<RocksDbContext>();
            var columnFamilyHandle = rocksDbContext.Db.GetColumnFamily(columnFamily);
            var rocksDbOptions = provider.GetRequiredService<IOptions<RocksDbOptions>>();
            var keySerializer = CreateSerializer<TKey>(rocksDbOptions.Value.SerializerFactories);
            var valueSerializer = CreateSerializer<TValue>(rocksDbOptions.Value.SerializerFactories);
            var rocksDbAccessor = new RocksDbAccessor<TKey, TValue>(
                rocksDbContext.Db,
                columnFamilyHandle,
                keySerializer,
                valueSerializer
            );
            return ActivatorUtilities.CreateInstance<TStore>(provider, rocksDbAccessor);
        });
        return this;
    }

    private static ISerializer<T> CreateSerializer<T>(IReadOnlyList<ISerializerFactory> serializerFactories)
    {
        var type = typeof(T);

        foreach (var serializerFactory in serializerFactories)
        {
            if (serializerFactory.CanCreateSerializer<T>())
            {
                return serializerFactory.CreateSerializer<T>();
            }
        }

        throw new InvalidOperationException($"Type {type.FullName} cannot be used as RocksDbStore key/value. " +
                                            $"Consider registering {nameof(ISerializerFactory)} that support this type.");
    }
}
