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
        using var testFixture = CreateTestFixture();

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
        store.TryGet(cacheKey, out var value).ShouldBeTrue();
        value.ShouldBeEquivalentTo(cacheValue);
    }

    [Test]
    public void should_put_and_remove_data_from_store()
    {
        // Arrange
        using var testFixture = CreateTestFixture();

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
        store.TryGet(cacheKey, out _).ShouldBeFalse();
    }

    [Test]
    public void should_put_range_of_data_to_store()
    {
        // Arrange
        using var testFixture = CreateTestFixture();
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
            store.TryGet(cacheKey, out var cacheValue).ShouldBeTrue();
            cacheValue.ShouldBeEquivalentTo(cacheValues[index]);
        }
    }
    
    [Test]
    public void should_put_range_of_data_to_store_when_key_is_derived_from_value()
    {
        // Arrange
        using var testFixture = CreateTestFixture();
        var store = testFixture.GetStore<RocksDbGenericStore<ProtoNetCacheKey, ProtoNetCacheValue>>();

        // Act
        var cacheValues = Enumerable.Range(0, 100)
            .Select(x => new ProtoNetCacheValue { Id = x, Value = $"value-{x}" })
            .ToArray();
        store.PutRange(cacheValues, value => new ProtoNetCacheKey { Id = value.Id });

        // Assert
        foreach (var expectedCacheValue in cacheValues)
        {
            store.TryGet(new ProtoNetCacheKey { Id = expectedCacheValue.Id }, out var cacheValue).ShouldBeTrue();
            cacheValue.ShouldBeEquivalentTo(expectedCacheValue);
        }
    }

    private static TestFixture CreateTestFixture()
    {
        var testFixture = TestFixture.Create(rockDb =>
        {
            _ = rockDb.AddStore<ProtoNetCacheKey, ProtoNetCacheValue, RocksDbGenericStore<ProtoNetCacheKey, ProtoNetCacheValue>>("my-store");
        }, options =>
        {
            options.SerializerFactories.Clear();
            options.SerializerFactories.Add(new SystemTextJsonSerializerFactory());
        });
        return testFixture;
    }
}
