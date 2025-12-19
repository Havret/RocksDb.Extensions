using NUnit.Framework;
using RocksDb.Extensions.System.Text.Json;
using RocksDb.Extensions.Tests.Protos;
using RocksDb.Extensions.Tests.Utils;
using Shouldly;

namespace RocksDb.Extensions.Tests;

public class RocksDbStoreWithJsonSerializerTests
{
    [Test]
    public void should_put_and_retrieve_data_from_store()
    {
        // Arrange
        using var testFixture = CreateTestFixture<ProtoNetCacheKey, ProtoNetCacheValue>();

        var store = testFixture.GetStore<RocksDbGenericStore<ProtoNetCacheKey, ProtoNetCacheValue>>();
        var cacheKey = new ProtoNetCacheKey
        {
            Id = 1,
        };
        var cacheValue = new ProtoNetCacheValue
        {
            Id = 1,
            Value = "Test",
        };

        // Act
        store.Put(cacheKey, cacheValue);

        // Assert
        store.HasKey(cacheKey).ShouldBeTrue();
        store.TryGet(cacheKey, out var value).ShouldBeTrue();
        value.ShouldBeEquivalentTo(cacheValue);
    }

    [Test]
    public void should_put_and_remove_data_from_store()
    {
        // Arrange
        using var testFixture = CreateTestFixture<ProtoNetCacheKey, ProtoNetCacheValue>();

        var store = testFixture.GetStore<RocksDbGenericStore<ProtoNetCacheKey, ProtoNetCacheValue>>();
        var cacheKey = new ProtoNetCacheKey
        {
            Id = 1,
        };
        var cacheValue = new ProtoNetCacheValue
        {
            Id = 1,
            Value = "Test",
        };
        store.Put(cacheKey, cacheValue);

        // Act
        store.Remove(cacheKey);

        // Assert
        store.HasKey(cacheKey).ShouldBeFalse();
        store.TryGet(cacheKey, out _).ShouldBeFalse();
    }

    [Test]
    public void should_put_range_of_data_to_store()
    {
        // Arrange
        using var testFixture = CreateTestFixture<ProtoNetCacheKey, ProtoNetCacheValue>();
        var store = testFixture.GetStore<RocksDbGenericStore<ProtoNetCacheKey, ProtoNetCacheValue>>();

        // Act
        var cacheKeys = Enumerable.Range(0, 100)
            .Select(x => new ProtoNetCacheKey { Id = x })
            .ToArray();
        var cacheValues = Enumerable.Range(0, 100)
            .Select(x => new ProtoNetCacheValue { Id = x, Value = $"value-{x}" })
            .ToArray();
        store.PutRange(cacheKeys, cacheValues);

        // Assert
        for (var index = 0; index < cacheKeys.Length; index++)
        {
            var cacheKey = cacheKeys[index];
            store.HasKey(cacheKey).ShouldBeTrue();
            store.TryGet(cacheKey, out var cacheValue).ShouldBeTrue();
            cacheValue.ShouldBeEquivalentTo(cacheValues[index]);
        }
    }

    [Test]
    public void should_put_range_of_data_to_store_when_key_is_derived_from_value()
    {
        // Arrange
        using var testFixture = CreateTestFixture<ProtoNetCacheKey, ProtoNetCacheValue>();
        var store = testFixture.GetStore<RocksDbGenericStore<ProtoNetCacheKey, ProtoNetCacheValue>>();

        // Act
        var cacheValues = Enumerable.Range(0, 100)
            .Select(x => new ProtoNetCacheValue { Id = x, Value = $"value-{x}" })
            .ToArray();
        store.PutRange(cacheValues, value => new ProtoNetCacheKey { Id = value.Id });

        // Assert
        foreach (var expectedCacheValue in cacheValues)
        {
            var key = new ProtoNetCacheKey { Id = expectedCacheValue.Id };
            store.HasKey(key).ShouldBeTrue();
            store.TryGet(key, out var cacheValue).ShouldBeTrue();
            cacheValue.ShouldBeEquivalentTo(expectedCacheValue);
        }
    }
    
    [Test]
    public void should_put_and_retrieve_data_with_lists_from_store()
    {
        // Arrange
        using var testFixture = CreateTestFixture<IList<ProtoNetCacheKey>, IList<ProtoNetCacheValue>>();
        var store = testFixture.GetStore<RocksDbGenericStore<IList<ProtoNetCacheKey>, IList<ProtoNetCacheValue>>>();

        // Act
        var cacheKey = Enumerable.Range(0, 100)
            .Select(x => new ProtoNetCacheKey 
            { 
                Id = x,
            })
            .ToList();
            
        var cacheValue = Enumerable.Range(0, 100)
            .Select(x => new ProtoNetCacheValue 
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
        using var testFixture = CreateTestFixture<ISet<ProtoNetCacheKey>, ISet<ProtoNetCacheValue>>();
        var store = testFixture.GetStore<RocksDbGenericStore<ISet<ProtoNetCacheKey>, ISet<ProtoNetCacheValue>>>();

        // Act
        var cacheKey = Enumerable.Range(0, 100)
            .Select(x => new ProtoNetCacheKey 
            { 
                Id = x,
            })
            .ToHashSet();
            
        var cacheValue = Enumerable.Range(0, 100)
            .Select(x => new ProtoNetCacheValue 
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
        using var testFixture = CreateTestFixture<ISet<ProtoNetCacheKey>, ISet<ProtoNetCacheValue>>();
        var store = testFixture.GetStore<RocksDbGenericStore<ISet<ProtoNetCacheKey>, ISet<ProtoNetCacheValue>>>();

        // Act
        var emptyCacheKey = new HashSet<ProtoNetCacheKey>();
        var emptyCacheValue = new HashSet<ProtoNetCacheValue>();
            
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
        using var testFixture = CreateTestFixture<IList<ProtoNetCacheKey>, IList<ProtoNetCacheValue>>();
        var store = testFixture.GetStore<RocksDbGenericStore<IList<ProtoNetCacheKey>, IList<ProtoNetCacheValue>>>();

        // Act
        var emptyCacheKey = new List<ProtoNetCacheKey>();
        var emptyCacheValue = new List<ProtoNetCacheValue>();
            
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
        using var testFixture = CreateTestFixture<ProtoNetCacheKey, ISet<ProtoNetCacheValue>>();
        var store = testFixture.GetStore<RocksDbGenericStore<ProtoNetCacheKey, ISet<ProtoNetCacheValue>>>();

        // Act
        var emptySet1 = new HashSet<ProtoNetCacheValue>();
        var emptySet2 = new HashSet<ProtoNetCacheValue>();
        var emptySet3 = new HashSet<ProtoNetCacheValue>();
        
        var key1 = new ProtoNetCacheKey { Id = 1 };
        var key2 = new ProtoNetCacheKey { Id = 2 };
        var key3 = new ProtoNetCacheKey { Id = 3 };
            
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
            options.SerializerFactories.Add(new SystemTextJsonSerializerFactory());
        });
        return testFixture;
    }    
}
