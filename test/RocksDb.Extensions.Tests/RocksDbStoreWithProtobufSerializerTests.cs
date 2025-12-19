using NUnit.Framework;
using RocksDb.Extensions.Protobuf;
using RocksDb.Extensions.Tests.Utils;
using Shouldly;

namespace RocksDb.Extensions.Tests;

public class RocksDbStoreWithProtobufSerializerTests
{
    [TestCase(0)]
    [TestCase(WellKnownValues.MaxStackSize)]
    public void should_put_and_retrieve_data_from_store(int payloadSize)
    {
        // Arrange
        using var testFixture = CreateTestFixture<CacheKey, CacheValue>();

        var store = testFixture.GetStore<RocksDbGenericStore<CacheKey, CacheValue>>();
        var cacheKeys = Enumerable.Range(0, 100)
            .Select(x => new CacheKey { Id = x, Payload = TestUtils.CreateByteStringOf(payloadSize) })
            .ToArray();
        var cacheValues = Enumerable.Range(0, 100)
            .Select(x => new CacheValue { Id = x, Value = $"value-{x}", Payload = TestUtils.CreateByteStringOf(payloadSize) })
            .ToArray();

        // Act
        for (int i = 0; i < cacheKeys.Length; i++)
        {
            store.Put(cacheKeys[i], cacheValues[i]);
        }

        // Assert
        for (int i = 0; i < cacheKeys.Length; i++)
        {
            store.HasKey(cacheKeys[i]).ShouldBeTrue();
            store.TryGet(cacheKeys[i], out var value).ShouldBeTrue();
            value.ShouldBe(cacheValues[i]);
        }
    }

    [TestCase(0)]
    [TestCase(WellKnownValues.MaxStackSize)]
    public void should_put_and_remove_data_from_store(int payloadSize)
    {
        // Arrange
        using var testFixture = CreateTestFixture<CacheKey, CacheValue>();

        var store = testFixture.GetStore<RocksDbGenericStore<CacheKey, CacheValue>>();
        var cacheKeys = Enumerable.Range(0, 100)
            .Select(x => new CacheKey { Id = x, Payload = TestUtils.CreateByteStringOf(payloadSize) })
            .ToArray();
        var cacheValues = Enumerable.Range(0, 100)
            .Select(x => new CacheValue { Id = x, Value = $"value-{x}", Payload = TestUtils.CreateByteStringOf(payloadSize) })
            .ToArray();
        store.PutRange(cacheKeys, cacheValues);

        // Act
        foreach (var cacheKey in cacheKeys)
        {
            store.Remove(cacheKey);
        }

        // Assert
        foreach (var cacheKey in cacheKeys)
        {
            store.HasKey(cacheKey).ShouldBeFalse();
            store.TryGet(cacheKey, out _).ShouldBeFalse();
        }
    }

    [Test]
    public void should_put_range_of_data_to_store()
    {
        // Arrange
        using var testFixture = CreateTestFixture<CacheKey, CacheValue>();
        var store = testFixture.GetStore<RocksDbGenericStore<CacheKey, CacheValue>>();

        // Act
        var cacheKeys = Enumerable.Range(0, 100)
            .Select(x => new CacheKey { Id = x })
            .ToArray();
        var cacheValues = Enumerable.Range(0, 100)
            .Select(x => new CacheValue { Id = x, Value = $"value-{x}" })
            .ToArray();
        store.PutRange(cacheKeys, cacheValues);

        // Assert
        for (var index = 0; index < cacheKeys.Length; index++)
        {
            var cacheKey = cacheKeys[index];
            store.HasKey(cacheKey).ShouldBeTrue();
            store.TryGet(cacheKey, out var cacheValue).ShouldBeTrue();
            cacheValue.ShouldBe(cacheValues[index]);
        }
    }

    [Test]
    public void should_retrieve_all_elements_from_store()
    {
        // Arrange
        using var testFixture = CreateTestFixture<CacheKey, CacheValue>();

        var store = testFixture.GetStore<RocksDbGenericStore<CacheKey, CacheValue>>();
        var cacheKeys = Enumerable.Range(0, 100)
            .Select(x => new CacheKey { Id = x })
            .ToArray();
        var cacheValues = Enumerable.Range(0, 100)
            .Select(x => new CacheValue { Id = x, Value = $"value-{x}" })
            .ToArray();

        store.PutRange(cacheKeys, cacheValues);

        // Act
        var values = store.GetAllValues().ToList();

        // Assert
        values.Count.ShouldBe(cacheKeys.Length);
        values.ShouldAllBe(x => cacheValues.Contains(x));
    }

    [Test]
    public void should_put_range_of_data_when_key_is_derived_from_value()
    {
        // Arrange
        using var testFixture = CreateTestFixture<CacheKey, CacheValue>();
        var store = testFixture.GetStore<RocksDbGenericStore<CacheKey, CacheValue>>();

        // Act
        var cacheValues = Enumerable.Range(0, 100)
            .Select(x => new CacheValue { Id = x, Value = $"value-{x}" })
            .ToArray();
        store.PutRange(cacheValues, value => new CacheKey { Id = value.Id });

        // Assert
        foreach (var expectedCacheValue in cacheValues)
        {
            var key = new CacheKey { Id = expectedCacheValue.Id };
            store.HasKey(key).ShouldBeTrue();
            store.TryGet(key, out var cacheValue).ShouldBeTrue();
            cacheValue.ShouldBeEquivalentTo(expectedCacheValue);
        }
    }
    
    [Test]
    public void should_put_and_retrieve_data_with_lists_from_store()
    {
        // Arrange
        using var testFixture = CreateTestFixture<IList<CacheKey>, IList<CacheValue>>();
        var store = testFixture.GetStore<RocksDbGenericStore<IList<CacheKey>, IList<CacheValue>>>();

        // Act
        var cacheKey = Enumerable.Range(0, 100)
            .Select(x => new CacheKey 
            { 
                Id = x,
            })
            .ToList();
            
        var cacheValue = Enumerable.Range(0, 100)
            .Select(x => new CacheValue 
            { 
                Id = x, 
                Value = $"value-{x}",
            })
            .ToList();
            
        store.Put(cacheKey, cacheValue);

        store.HasKey(cacheKey).ShouldBeTrue();
        store.TryGet(cacheKey, out var value).ShouldBeTrue();
        value.ShouldBeEquivalentTo(cacheValue);
    }
    
    [Test]
    public void should_put_and_retrieve_data_with_sets_from_store()
    {
        // Arrange
        using var testFixture = CreateTestFixture<ISet<CacheKey>, ISet<CacheValue>>();
        var store = testFixture.GetStore<RocksDbGenericStore<ISet<CacheKey>, ISet<CacheValue>>>();

        // Act
        var cacheKey = Enumerable.Range(0, 100)
            .Select(x => new CacheKey 
            { 
                Id = x,
            })
            .ToHashSet();
            
        var cacheValue = Enumerable.Range(0, 100)
            .Select(x => new CacheValue 
            { 
                Id = x, 
                Value = $"value-{x}",
            })
            .ToHashSet();
            
        store.Put(cacheKey, cacheValue);

        store.HasKey(cacheKey).ShouldBeTrue();
        store.TryGet(cacheKey, out var value).ShouldBeTrue();
        value.ShouldBeEquivalentTo(cacheValue);
    }
    
    [Test]
    public void should_put_and_retrieve_empty_set_with_non_primitive_types()
    {
        // Arrange
        using var testFixture = CreateTestFixture<ISet<CacheKey>, ISet<CacheValue>>();
        var store = testFixture.GetStore<RocksDbGenericStore<ISet<CacheKey>, ISet<CacheValue>>>();

        // Act
        var emptyCacheKey = new HashSet<CacheKey>();
        var emptyCacheValue = new HashSet<CacheValue>();
            
        store.Put(emptyCacheKey, emptyCacheValue);

        // Assert
        store.HasKey(emptyCacheKey).ShouldBeTrue();
        store.TryGet(emptyCacheKey, out var value).ShouldBeTrue();
        value.ShouldNotBeNull();
        value.Count.ShouldBe(0);
        value.ShouldBeEquivalentTo(emptyCacheValue);
    }
    
    [Test]
    public void should_put_and_retrieve_empty_list_with_non_primitive_types()
    {
        // Arrange
        using var testFixture = CreateTestFixture<IList<CacheKey>, IList<CacheValue>>();
        var store = testFixture.GetStore<RocksDbGenericStore<IList<CacheKey>, IList<CacheValue>>>();

        // Act
        var emptyCacheKey = new List<CacheKey>();
        var emptyCacheValue = new List<CacheValue>();
            
        store.Put(emptyCacheKey, emptyCacheValue);

        // Assert
        store.HasKey(emptyCacheKey).ShouldBeTrue();
        store.TryGet(emptyCacheKey, out var value).ShouldBeTrue();
        value.ShouldNotBeNull();
        value.Count.ShouldBe(0);
        value.ShouldBeEquivalentTo(emptyCacheValue);
    }
    
    [Test]
    public void should_handle_multiple_empty_sets_with_different_keys()
    {
        // Arrange
        using var testFixture = CreateTestFixture<CacheKey, ISet<CacheValue>>();
        var store = testFixture.GetStore<RocksDbGenericStore<CacheKey, ISet<CacheValue>>>();

        // Act
        var emptySet1 = new HashSet<CacheValue>();
        var emptySet2 = new HashSet<CacheValue>();
        var emptySet3 = new HashSet<CacheValue>();
        
        var key1 = new CacheKey { Id = 1 };
        var key2 = new CacheKey { Id = 2 };
        var key3 = new CacheKey { Id = 3 };
            
        store.Put(key1, emptySet1);
        store.Put(key2, emptySet2);
        store.Put(key3, emptySet3);

        // Assert
        store.HasKey(key1).ShouldBeTrue();
        store.TryGet(key1, out var value1).ShouldBeTrue();
        value1.ShouldNotBeNull();
        value1.Count.ShouldBe(0);
        
        store.HasKey(key2).ShouldBeTrue();
        store.TryGet(key2, out var value2).ShouldBeTrue();
        value2.ShouldNotBeNull();
        value2.Count.ShouldBe(0);
        
        store.HasKey(key3).ShouldBeTrue();
        store.TryGet(key3, out var value3).ShouldBeTrue();
        value3.ShouldNotBeNull();
        value3.Count.ShouldBe(0);
    }
    
    private static TestFixture CreateTestFixture<TKey, TValue>()
    {
        var testFixture = TestFixture.Create(rockDb =>
        {
            _ = rockDb.AddStore<TKey, TValue, RocksDbGenericStore<TKey, TValue>>("my-store");
        }, options =>
        {
            options.SerializerFactories.Clear();
            options.SerializerFactories.Add(new ProtobufSerializerFactory());
        });
        return testFixture;
    }    
}
