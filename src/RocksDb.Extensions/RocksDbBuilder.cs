using System.Buffers;
using System.Reflection;
using CommunityToolkit.HighPerformance.Buffers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RocksDbSharp;

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
        return AddStoreInternal<TKey, TValue, TStore>(columnFamily, null);
    }

    public IRocksDbBuilder AddStore<TKey, TValue, TStore>(string columnFamily, IMergeOperator<TValue> mergeOperator) where TStore : RocksDbStore<TKey, TValue>
    {
        return AddStoreInternal<TKey, TValue, TStore>(columnFamily, mergeOperator);
    }

    public IRocksDbBuilder AddMergeableStore<TKey, TValue, TStore>(string columnFamily, IMergeOperator<TValue> mergeOperator) 
        where TStore : MergeableRocksDbStore<TKey, TValue>
    {
        return AddStoreInternal<TKey, TValue, TStore>(columnFamily, mergeOperator);
    }

    private IRocksDbBuilder AddStoreInternal<TKey, TValue, TStore>(string columnFamily, IMergeOperator<TValue>? mergeOperator) where TStore : RocksDbStore<TKey, TValue>
    {
        if (!_columnFamilyLookup.Add(columnFamily))
        {
            throw new InvalidOperationException($"{columnFamily} is already registered.");
        }

        _ = _serviceCollection.Configure<RocksDbOptions>(options =>
        {
            options.ColumnFamilies.Add(columnFamily);

            if (mergeOperator != null)
            {
                var valueSerializer = CreateSerializer<TValue>(options.SerializerFactories);
                var config = CreateMergeOperatorConfig(mergeOperator, valueSerializer);
                options.MergeOperators[columnFamily] = config;
            }
        });
        
        _serviceCollection.AddKeyedSingleton<TStore>(columnFamily, (provider, _) =>
        {
            var rocksDbContext = provider.GetRequiredService<RocksDbContext>();
            var columnFamilyHandle = rocksDbContext.Db.GetColumnFamily(columnFamily);
            var rocksDbOptions = provider.GetRequiredService<IOptions<RocksDbOptions>>();
            var keySerializer = CreateSerializer<TKey>(rocksDbOptions.Value.SerializerFactories);
            var valueSerializer = CreateSerializer<TValue>(rocksDbOptions.Value.SerializerFactories);
            var rocksDbAccessor = new RocksDbAccessor<TKey, TValue>(
                rocksDbContext,
                new ColumnFamily(columnFamilyHandle, columnFamily),
                keySerializer,
                valueSerializer
            );
            return ActivatorUtilities.CreateInstance<TStore>(provider, rocksDbAccessor);
        });
        
        _serviceCollection.TryAddSingleton(typeof(TStore), provider => provider.GetRequiredKeyedService<TStore>(columnFamily));
        
        return this;
    }

    private static MergeOperatorConfig CreateMergeOperatorConfig<TValue>(IMergeOperator<TValue> mergeOperator, ISerializer<TValue> valueSerializer)
    {
        return new MergeOperatorConfig
        {
            Name = mergeOperator.Name,
            ValueSerializer = valueSerializer,
            FullMerge = (ReadOnlySpan<byte> key, bool hasExistingValue, ReadOnlySpan<byte> existingValue, global::RocksDbSharp.MergeOperators.OperandsEnumerator operands, out bool success) =>
            {
                return FullMergeCallback(key, hasExistingValue, existingValue, operands, mergeOperator, valueSerializer, out success);
            },
            PartialMerge = (ReadOnlySpan<byte> key, global::RocksDbSharp.MergeOperators.OperandsEnumerator operands, out bool success) =>
            {
                return PartialMergeCallback(key, operands, mergeOperator, valueSerializer, out success);
            }
        };
    }

    private static byte[] FullMergeCallback<TValue>(
        ReadOnlySpan<byte> key,
        bool hasExistingValue,
        ReadOnlySpan<byte> existingValue,
        global::RocksDbSharp.MergeOperators.OperandsEnumerator operands,
        IMergeOperator<TValue> mergeOperator,
        ISerializer<TValue> valueSerializer,
        out bool success)
    {
        success = true;
        
        // Deserialize existing value if present
        TValue existing = hasExistingValue ? valueSerializer.Deserialize(existingValue) : default!;

        // Deserialize all operands
        var operandList = new List<TValue>(operands.Count);
        for (int i = 0; i < operands.Count; i++)
        {
            operandList.Add(valueSerializer.Deserialize(operands.Get(i)));
        }

        // Call the user's merge operator
        var result = mergeOperator.FullMerge(key, existing, operandList);

        // Serialize the result
        return SerializeValue(result, valueSerializer);
    }

    private static byte[] PartialMergeCallback<TValue>(
        ReadOnlySpan<byte> key,
        global::RocksDbSharp.MergeOperators.OperandsEnumerator operands,
        IMergeOperator<TValue> mergeOperator,
        ISerializer<TValue> valueSerializer,
        out bool success)
    {
        // Deserialize all operands
        var operandList = new List<TValue>(operands.Count);
        for (int i = 0; i < operands.Count; i++)
        {
            operandList.Add(valueSerializer.Deserialize(operands.Get(i)));
        }

        // Call the user's partial merge operator
        var result = mergeOperator.PartialMerge(key, operandList);

        success = true;
        // Serialize the result
        return SerializeValue(result, valueSerializer);
    }

    private static byte[] SerializeValue<TValue>(TValue value, ISerializer<TValue> valueSerializer)
    {
        if (valueSerializer.TryCalculateSize(ref value, out var size))
        {
            var buffer = new byte[size];
            var span = buffer.AsSpan();
            valueSerializer.WriteTo(ref value, ref span);
            return buffer;
        }
        else
        {
            using var bufferWriter = new ArrayPoolBufferWriter<byte>();
            valueSerializer.WriteTo(ref value, bufferWriter);
            return bufferWriter.WrittenSpan.ToArray();
        }
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
        
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
        {
            var elementType = type.GetGenericArguments()[0];
            
            // Use reflection to call CreateSerializer method with generic type argument
            // This is equivalent to calling CreateSerializer<elementType>(serializerFactories)
            var scalarSerializer = typeof(RocksDbBuilder).GetMethod(nameof(CreateSerializer), BindingFlags.NonPublic | BindingFlags.Static)
                ?.MakeGenericMethod(elementType)
                .Invoke(null, new object[] { serializerFactories });
            
            if (elementType.IsPrimitive)
            {
                // Use fixed size list serializer for primitive types
                return (ISerializer<T>) Activator.CreateInstance(typeof(FixedSizeListSerializer<>).MakeGenericType(elementType), scalarSerializer)!;
            }

            // Use variable size list serializer for non-primitive types
            return (ISerializer<T>) Activator.CreateInstance(typeof(VariableSizeListSerializer<>).MakeGenericType(elementType), scalarSerializer)!;
        }

        // Handle ListOperation<T> for the ListMergeOperator
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(MergeOperators.ListOperation<>))
        {
            var itemType = type.GetGenericArguments()[0];
            
            // Create the item serializer
            var itemSerializer = typeof(RocksDbBuilder).GetMethod(nameof(CreateSerializer), BindingFlags.NonPublic | BindingFlags.Static)
                ?.MakeGenericMethod(itemType)
                .Invoke(null, new object[] { serializerFactories });
            
            // Create ListOperationSerializer<itemType>
            return (ISerializer<T>) Activator.CreateInstance(typeof(ListOperationSerializer<>).MakeGenericType(itemType), itemSerializer)!;
        }

        throw new InvalidOperationException($"Type {type.FullName} cannot be used as RocksDbStore key/value. " +
                                            $"Consider registering {nameof(ISerializerFactory)} that support this type.");
    }
}
