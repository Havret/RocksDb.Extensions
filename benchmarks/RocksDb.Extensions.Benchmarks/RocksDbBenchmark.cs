using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using RocksDb.Extensions.Protobuf;

namespace RocksDb.Extensions.Benchmarks;

[MemoryDiagnoser]
public class RocksDbBenchmark : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly RocksDbGenericStore<CacheKey, CacheValue> _store;

    private readonly CacheKey[] _keys;
    private readonly CacheValue[] _values;
    private readonly Random _random;
    
    public RocksDbBenchmark()
    {
        var services = new ServiceCollection();
        _ = services.AddRocksDb(options =>
        {
            options.Path = $"./{Guid.NewGuid().ToString()}";
            options.SerializerFactories.Add(new PrimitiveTypesSerializerFactory());
            options.SerializerFactories.Add(new ProtobufSerializerFactory());
        }).AddStore<CacheKey, CacheValue, RocksDbGenericStore<CacheKey, CacheValue>>("my-store");

        _serviceProvider = services.BuildServiceProvider();

        _store = _serviceProvider.GetRequiredService<RocksDbGenericStore<CacheKey, CacheValue>>();

        _random = new Random();
        _keys = new CacheKey[100_000];
        _values = new CacheValue[100_000];
        for (int i = 0; i < 100_000; i++)
        {
            _keys[i] = new CacheKey { Id = i };
            _values[i] = new CacheValue()
            {
                BoolProperty = _random.Next() % 2 == 0,
                DoubleProperty = _random.NextDouble(),
                Fixed32Property = (uint)_random.Next(),
                Fixed64Property = (ulong)_random.NextInt64(),
                FloatProperty = _random.NextSingle(),
                Int32Property = _random.Next(),
                Int64Property = _random.NextInt64(),
                Sfixed32Property = _random.Next(),
                Sfixed64Property = _random.NextInt64(),
                Sint32Property = _random.Next(),
                Sint64Property = _random.NextInt64(),
                StringProperty = _random.NextInt64().ToString(),
                Uint32Property = (uint)_random.Next(),
                Uint64Property = (uint)_random.NextInt64(),
            };
        }
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    [Benchmark]
    public void Put()
    {
        var id = _random.Next(0, 100_000);
        _store.Put(_keys[id], _values[id]);
    }

    // [Benchmark]
    public void Put_1000_one_by_one()
    {
        var start = _random.Next(0, 100_000 - 1001);
        for (int i = start; i < start + 1000; i++)
        {
            _store.Put(_keys[i], _values[i]);
        }
    }

    // [Benchmark]
    public void Put_1000_range()
    {
        var start = _random.Next(0, 100_000 - 1001);
        _store.PutRange(_keys.AsSpan(start, 1000), _values.AsSpan(start, 1000));
    }
}
