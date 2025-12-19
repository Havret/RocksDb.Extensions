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

    [Test]
    public void should_put_and_retrieve_data_from_store_using_set_of_ints()
    {
        // Arrange
        using var testFixture = CreateTestFixture<ISet<int>, ISet<long>>();
        var store = testFixture.GetStore<RocksDbGenericStore<ISet<int>, ISet<long>>>();

        // Act
        var key = new HashSet<int> { 1, 2, 3 };
        var value = new HashSet<long> { 4, 5, 6 };
        store.Put(key, value);

        // Assert
        store.HasKey(key).ShouldBeTrue();
        store.TryGet(key, out var cacheValue).ShouldBeTrue();
        cacheValue.ShouldBeEquivalentTo(value);
    }

    [Test]
    public void should_put_and_retrieve_data_from_store_using_set_of_strings()
    {
        // Arrange
        using var testFixture = CreateTestFixture<ISet<string>, ISet<string>>();
        var store = testFixture.GetStore<RocksDbGenericStore<ISet<string>, ISet<string>>>();
        var key = new HashSet<string> { "key1", "key2", string.Empty, "key3" };
        var value = new HashSet<string> { "value1", string.Empty, "value2", "value3" };

        // Act
        store.Put(key, value);

        // Assert
        store.HasKey(key).ShouldBeTrue();
        store.TryGet(key, out var retrievedValue).ShouldBeTrue();
        retrievedValue.ShouldBeEquivalentTo(value);
    }

    [Test]
    public void should_put_and_retrieve_empty_set_with_int_types()
    {
        // Arrange
        using var testFixture = CreateTestFixture<ISet<int>, ISet<int>>();
        var store = testFixture.GetStore<RocksDbGenericStore<ISet<int>, ISet<int>>>();

        // Act
        var emptyIntSetKey = new HashSet<int>();
        var emptyIntSetValue = new HashSet<int>();
            
        store.Put(emptyIntSetKey, emptyIntSetValue);

        // Assert
        store.HasKey(emptyIntSetKey).ShouldBeTrue();
        store.TryGet(emptyIntSetKey, out var value).ShouldBeTrue();
        value.ShouldNotBeNull();
        value.Count.ShouldBe(0);
        value.ShouldBeEquivalentTo(emptyIntSetValue);
    }
    
    [Test]
    public void should_put_and_retrieve_empty_set_with_string_types()
    {
        // Arrange
        using var testFixture = CreateTestFixture<ISet<string>, ISet<string>>();
        var store = testFixture.GetStore<RocksDbGenericStore<ISet<string>, ISet<string>>>();

        // Act
        var emptyStringSetKey = new HashSet<string>();
        var emptyStringSetValue = new HashSet<string>();
            
        store.Put(emptyStringSetKey, emptyStringSetValue);

        // Assert
        store.HasKey(emptyStringSetKey).ShouldBeTrue();
        store.TryGet(emptyStringSetKey, out var value).ShouldBeTrue();
        value.ShouldNotBeNull();
        value.Count.ShouldBe(0);
        value.ShouldBeEquivalentTo(emptyStringSetValue);
    }
    
    [Test]
    public void should_put_and_retrieve_empty_set_with_long_types()
    {
        // Arrange
        using var testFixture = CreateTestFixture<ISet<long>, ISet<long>>();
        var store = testFixture.GetStore<RocksDbGenericStore<ISet<long>, ISet<long>>>();

        // Act
        var emptyLongSetKey = new HashSet<long>();
        var emptyLongSetValue = new HashSet<long>();
            
        store.Put(emptyLongSetKey, emptyLongSetValue);

        // Assert
        store.HasKey(emptyLongSetKey).ShouldBeTrue();
        store.TryGet(emptyLongSetKey, out var value).ShouldBeTrue();
        value.ShouldNotBeNull();
        value.Count.ShouldBe(0);
        value.ShouldBeEquivalentTo(emptyLongSetValue);
    }
    
    [Test]
    public void should_put_and_retrieve_empty_list_with_int_types()
    {
        // Arrange
        using var testFixture = CreateTestFixture<IList<int>, IList<int>>();
        var store = testFixture.GetStore<RocksDbGenericStore<IList<int>, IList<int>>>();

        // Act
        var emptyIntListKey = new List<int>();
        var emptyIntListValue = new List<int>();
            
        store.Put(emptyIntListKey, emptyIntListValue);

        // Assert
        store.HasKey(emptyIntListKey).ShouldBeTrue();
        store.TryGet(emptyIntListKey, out var value).ShouldBeTrue();
        value.ShouldNotBeNull();
        value.Count.ShouldBe(0);
        value.ShouldBeEquivalentTo(emptyIntListValue);
    }
    
    [Test]
    public void should_put_and_retrieve_empty_list_with_string_types()
    {
        // Arrange
        using var testFixture = CreateTestFixture<IList<string>, IList<string>>();
        var store = testFixture.GetStore<RocksDbGenericStore<IList<string>, IList<string>>>();

        // Act
        var emptyStringListKey = new List<string>();
        var emptyStringListValue = new List<string>();
            
        store.Put(emptyStringListKey, emptyStringListValue);

        // Assert
        store.HasKey(emptyStringListKey).ShouldBeTrue();
        store.TryGet(emptyStringListKey, out var value).ShouldBeTrue();
        value.ShouldNotBeNull();
        value.Count.ShouldBe(0);
        value.ShouldBeEquivalentTo(emptyStringListValue);
    }
    
    [Test]
    public void should_handle_multiple_empty_sets_with_different_int_keys()
    {
        // Arrange
        using var testFixture = CreateTestFixture<int, ISet<string>>();
        var store = testFixture.GetStore<RocksDbGenericStore<int, ISet<string>>>();

        // Act
        var emptySet1 = new HashSet<string>();
        var emptySet2 = new HashSet<string>();
        var emptySet3 = new HashSet<string>();
            
        store.Put(1, emptySet1);
        store.Put(2, emptySet2);
        store.Put(3, emptySet3);

        // Assert
        store.HasKey(1).ShouldBeTrue();
        store.TryGet(1, out var value1).ShouldBeTrue();
        value1.ShouldNotBeNull();
        value1.Count.ShouldBe(0);
        
        store.HasKey(2).ShouldBeTrue();
        store.TryGet(2, out var value2).ShouldBeTrue();
        value2.ShouldNotBeNull();
        value2.Count.ShouldBe(0);
        
        store.HasKey(3).ShouldBeTrue();
        store.TryGet(3, out var value3).ShouldBeTrue();
        value3.ShouldNotBeNull();
        value3.Count.ShouldBe(0);
    }
    
    [Test]
    public void should_handle_multiple_empty_sets_with_different_string_keys()
    {
        // Arrange
        using var testFixture = CreateTestFixture<string, ISet<int>>();
        var store = testFixture.GetStore<RocksDbGenericStore<string, ISet<int>>>();

        // Act
        var emptySet1 = new HashSet<int>();
        var emptySet2 = new HashSet<int>();
        var emptySet3 = new HashSet<int>();
            
        store.Put("key1", emptySet1);
        store.Put("key2", emptySet2);
        store.Put("key3", emptySet3);

        // Assert
        store.HasKey("key1").ShouldBeTrue();
        store.TryGet("key1", out var value1).ShouldBeTrue();
        value1.ShouldNotBeNull();
        value1.Count.ShouldBe(0);
        
        store.HasKey("key2").ShouldBeTrue();
        store.TryGet("key2", out var value2).ShouldBeTrue();
        value2.ShouldNotBeNull();
        value2.Count.ShouldBe(0);
        
        store.HasKey("key3").ShouldBeTrue();
        store.TryGet("key3", out var value3).ShouldBeTrue();
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
            options.SerializerFactories.Add(new PrimitiveTypesSerializerFactory());
        });
        return testFixture;
    }
}
