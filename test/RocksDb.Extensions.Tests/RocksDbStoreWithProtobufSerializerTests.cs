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
        using var testFixture = CreateTestFixture();

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
            store.TryGet(cacheKeys[i], out var value).ShouldBeTrue();
            value.ShouldBe(cacheValues[i]);
        }
    }

    [TestCase(0)]
    [TestCase(WellKnownValues.MaxStackSize)]
    public void should_put_and_remove_data_from_store(int payloadSize)
    {
        // Arrange
        using var testFixture = CreateTestFixture();

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
            store.TryGet(cacheKey, out _).ShouldBeFalse();
        }
    }

    [Test]
    public void should_put_range_of_data_to_store()
    {
        // Arrange
        using var testFixture = CreateTestFixture();
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
            store.TryGet(cacheKey, out var cacheValue).ShouldBeTrue();
            cacheValue.ShouldBe(cacheValues[index]);
        }
    }

    [Test]
    public void should_retrieve_all_elements_from_store()
    {
        // Arrange
        using var testFixture = CreateTestFixture();

        var store = testFixture.GetStore<RocksDbGenericStore<CacheKey, CacheValue>>();
        var cacheKeys = Enumerable.Range(0, 100)
            .Select(x => new CacheKey { Id = x })
            .ToArray();
        var cacheValues = Enumerable.Range(0, 100)
            .Select(x => new CacheValue { Id = x, Value = $"value-{x}" })
            .ToArray();

        store.PutRange(cacheKeys, cacheValues);

        // Act
        var values = store.GetAll().ToList();

        // Assert
        values.Count.ShouldBe(cacheKeys.Length);
        values.ShouldAllBe(x => cacheValues.Contains(x));
    }

    [Test]
    public void should_put_range_of_data_when_key_is_derived_from_value()
    {
        // Arrange
        using var testFixture = CreateTestFixture();
        var store = testFixture.GetStore<RocksDbGenericStore<CacheKey, CacheValue>>();

        // Act
        var cacheValues = Enumerable.Range(0, 100)
            .Select(x => new CacheValue { Id = x, Value = $"value-{x}" })
            .ToArray();
        store.PutRange(cacheValues, value => new CacheKey { Id = value.Id });

        // Assert
        foreach (var expectedCacheValue in cacheValues)
        {
            store.TryGet(new CacheKey { Id = expectedCacheValue.Id }, out var cacheValue).ShouldBeTrue();
            cacheValue.ShouldBeEquivalentTo(expectedCacheValue);
        }
    }

    private static TestFixture CreateTestFixture()
    {
        var testFixture = TestFixture.Create(rockDb =>
        {
            _ = rockDb.AddStore<CacheKey, CacheValue, RocksDbGenericStore<CacheKey, CacheValue>>("my-store");
        }, options =>
        {
            options.SerializerFactories.Clear();
            options.SerializerFactories.Add(new ProtobufSerializerFactory());
        });
        return testFixture;
    }
}
