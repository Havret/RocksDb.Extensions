using NUnit.Framework;
using RocksDb.Extensions.Tests.Utils;
using Shouldly;

namespace RocksDb.Extensions.Tests;

public class RocksDbStoreWithPrimitiveSerializerTests
{
    [Test]
    public void should_put_and_retrieve_data_from_store_using_int_32_types()
    {
        // Arrange
        using var testFixture = CreateTestFixture<int, int>();
        var store = testFixture.GetStore<RocksDbGenericStore<int, int>>();

        // Act
        store.Put(1, 2);

        // Assert
        store.HasKey(1).ShouldBeTrue();
        store.TryGet(1, out var value).ShouldBeTrue();
        value.ShouldBe(2);
    }

    [Test]
    public void should_put_and_remove_data_from_store_using_int32_types()
    {
        // Arrange
        using var testFixture = CreateTestFixture<int, int>();
        var store = testFixture.GetStore<RocksDbGenericStore<int, int>>();
        store.Put(1, 2);

        // Act
        store.Remove(1);

        // Assert
        store.HasKey(1).ShouldBeFalse();
        store.TryGet(1, out _).ShouldBeFalse();
    }

    [Test]
    public void should_put_range_of_data_to_store_using_int_32_types()
    {
        // Arrange
        using var testFixture = CreateTestFixture<int, int>();
        var store = testFixture.GetStore<RocksDbGenericStore<int, int>>();

        // Act
        var cacheKeys = Enumerable.Range(0, 100)
            .ToArray();
        var cacheValues = Enumerable.Range(0, 100)
            .ToArray();
        store.PutRange(cacheKeys.AsSpan(), cacheValues.AsSpan());

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
    public void should_put_and_retrieve_data_from_store_using_int_64_types()
    {
        // Arrange
        using var testFixture = CreateTestFixture<long, long>();
        var store = testFixture.GetStore<RocksDbGenericStore<long, long>>();

        // Act
        store.Put(1, 2);

        // Assert
        store.HasKey(1).ShouldBeTrue();
        store.TryGet(1, out var value).ShouldBeTrue();
        value.ShouldBe(2);
    }

    [Test]
    public void should_put_and_remove_data_from_store_using_int64_types()
    {
        // Arrange
        using var testFixture = CreateTestFixture<long, long>();
        var store = testFixture.GetStore<RocksDbGenericStore<long, long>>();
        store.Put(1, 2);

        // Act
        store.Remove(1);

        // Assert
        store.HasKey(1).ShouldBeFalse();
        store.TryGet(1, out _).ShouldBeFalse();
    }

    [Test]
    public void should_put_range_of_data_to_store_using_int_64_types()
    {
        // Arrange
        using var testFixture = CreateTestFixture<long, long>();
        var store = testFixture.GetStore<RocksDbGenericStore<long, long>>();

        // Act
        var cacheKeys = Enumerable.Range(0, 100)
            .Select(x => (long)x)
            .ToArray();
        var cacheValues = Enumerable.Range(0, 100)
            .Select(x => (long)x)
            .ToArray();
        store.PutRange(cacheKeys.AsSpan(), cacheValues.AsSpan());

        // Assert
        for (var index = 0; index < cacheKeys.Length; index++)
        {
            var cacheKey = cacheKeys[index];
            store.HasKey(cacheKey).ShouldBeTrue();
            store.TryGet(cacheKey, out var cacheValue).ShouldBeTrue();
            cacheValue.ShouldBe(cacheValues[index]);
        }
    }

    [TestCase(0)]
    [TestCase(WellKnownValues.MaxStackSize)]
    public void should_put_and_retrieve_data_from_store_using_string_types(int payloadSize)
    {
        // Arrange
        using var testFixture = CreateTestFixture<string, string>();
        var store = testFixture.GetStore<RocksDbGenericStore<string, string>>();
        var key = "key" + TestUtils.CreateStringOf(payloadSize);
        var value = "value" + TestUtils.CreateStringOf(payloadSize);

        // Act
        store.Put(key, value);

        // Assert
        store.HasKey(key);
        store.TryGet(key, out var retrievedValue).ShouldBeTrue();
        retrievedValue.ShouldBe(value);
    }

    [TestCase(0)]
    [TestCase(WellKnownValues.MaxStackSize)]
    public void should_put_and_remove_data_from_store_using_string_types(int payloadSize)
    {
        // Arrange
        using var testFixture = CreateTestFixture<string, string>();
        var store = testFixture.GetStore<RocksDbGenericStore<string, string>>();
        var key = "key" + TestUtils.CreateStringOf(payloadSize);
        var value = "value" + TestUtils.CreateStringOf(payloadSize);
        store.Put(key, value);

        // Act
        store.Remove(key);


        // Assert
        store.HasKey(key).ShouldBeFalse();
        store.TryGet(key, out _).ShouldBeFalse();
    }

    [Test]
    public void should_put_range_of_data_to_store_using_string_types()
    {
        // Arrange
        using var testFixture = CreateTestFixture<string, string>();
        var store = testFixture.GetStore<RocksDbGenericStore<string, string>>();

        // Act
        var cacheKeys = Enumerable.Range(0, 100)
            .Select(x => x.ToString())
            .ToArray();
        var cacheValues = Enumerable.Range(0, 100)
            .Select(x => x.ToString())
            .ToArray();
        store.PutRange(cacheKeys.AsSpan(), cacheValues.AsSpan());

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
    public void should_put_range_of_data_when_key_is_derived_from_value()
    {
        // Arrange
        using var testFixture = CreateTestFixture<string, string>();
        var store = testFixture.GetStore<RocksDbGenericStore<string, string>>();

        // Act
        var cacheValues = Enumerable.Range(0, 100)
            .Select(x => x.ToString())
            .ToArray();
        store.PutRange(cacheValues, value => $"{value}+");

        // Assert
        foreach (var expectedCacheValue in cacheValues)
        {
            var key = $"{expectedCacheValue}+";
            store.HasKey(key).ShouldBeTrue();
            store.TryGet(key, out var cacheValue).ShouldBeTrue();
            cacheValue.ShouldBeEquivalentTo(expectedCacheValue);
        }
    }
    
    [Test]
    public void should_put_and_retrieve_data_from_store_using_bool_type()
    {
        // Arrange
        using var testFixture = CreateTestFixture<bool, bool>();
        var store = testFixture.GetStore<RocksDbGenericStore<bool, bool>>();

        // Act
        store.Put(true, false);

        // Assert
        store.HasKey(true).ShouldBeTrue();
        store.TryGet(true, out var value).ShouldBeTrue();
        value.ShouldBe(false);
    }

    [Test]
    public void should_put_and_remove_data_from_store_using_bool_type()
    {
        // Arrange
        using var testFixture = CreateTestFixture<bool, bool>();
        var store = testFixture.GetStore<RocksDbGenericStore<bool, bool>>();
        store.Put(true, true);

        // Act
        store.Remove(true);

        // Assert
        store.HasKey(true).ShouldBeFalse();
        store.TryGet(true, out _).ShouldBeFalse();
    }

    [Test]
    public void should_put_range_of_data_to_store_using_bool_types()
    {
        // Arrange
        using var testFixture = CreateTestFixture<bool, bool>();
        var store = testFixture.GetStore<RocksDbGenericStore<bool, bool>>();

        // Act
        var cacheKeys = new[] { true, false };
        var cacheValues = new[] { false, true };
        
        store.PutRange(cacheKeys.AsSpan(), cacheValues.AsSpan());

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
    public void should_put_and_retrieve_data_from_store_using_list_of_primitive_types()
    {
        // Arrange
        using var testFixture = CreateTestFixture<IList<int>, IList<long>>();
        var store = testFixture.GetStore<RocksDbGenericStore<IList<int>, IList<long>>>();

        // Act
        var key = new List<int> { 1, 2 };
        var value = new List<long> { 3, 4 };
        store.Put(key, value);

        // Assert
        store.HasKey(key).ShouldBeTrue();
        store.TryGet(key, out var cacheValue).ShouldBeTrue();
        cacheValue.ShouldBe(value);
    }

    [Test]
    public void should_put_and_retrieve_data_from_store_using_list_of_strings()
    {
        // Arrange
        using var testFixture = CreateTestFixture<IList<string>, IList<string>>();
        var store = testFixture.GetStore<RocksDbGenericStore<IList<string>, IList<string>>>();
        var key = new List<string> { "key1", "key2", string.Empty, "key3" };
        var value = new List<string> { "value1", string.Empty, "value2", "value3" };

        // Act
        store.Put(key, value);

        // Assert
        store.HasKey(key).ShouldBeTrue();
        store.TryGet(key, out var retrievedValue).ShouldBeTrue();
        retrievedValue.ShouldBe(value);
    }

    private static TestFixture CreateTestFixture<TKey, TValue>()
    {
        var testFixture = TestFixture.Create(rockDb =>
        {
            _ = rockDb.AddStore<TKey, TValue, RocksDbGenericStore<TKey, TValue>>("my-store");
        }, options =>
        {
            options.SerializerFactories.Clear();
            options.SerializerFactories.Add(new PrimitiveTypesSerializerFactory());
        });
        return testFixture;
    }
}
