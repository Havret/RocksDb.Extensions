using NUnit.Framework;
using RocksDb.Extensions.Tests.Utils;
using Shouldly;

namespace RocksDb.Extensions.Tests;

public class ClearStoreTests
{
    [Test]
    public void should_reset_store_range_data_to_store()
    {
        // Setup RocksDbStore
        using var testFixture = CreateTestFixture<int, string>();

        // Put some data
        var store = testFixture.GetStore<RocksDbGenericStore<int, string>>();
        var cacheKeys = Enumerable.Range(0, 100)
            .Select(x => (key: x, value: x.ToString()))
            .ToList();

        store.PutRange(cacheKeys);

        // Verify that data was added
        foreach (var (key, expectedValue) in cacheKeys)
        {
            store.HasKey(key).ShouldBeTrue();
            store.TryGet(key, out var value).ShouldBeTrue();
            value.ShouldBe(expectedValue);
        }

        // Clear the store
        store.Clear();

        // Verify that data is no longer there
        foreach (var (key, expectedValue) in cacheKeys)
        {
            store.HasKey(key).ShouldBeFalse();
            store.TryGet(key, out _).ShouldBeFalse();
        }

        // Try to put the data again
        store.PutRange(cacheKeys);

        // Verify that data is available again
        foreach (var (key, expectedValue) in cacheKeys)
        {
            store.HasKey(key).ShouldBeTrue();
            store.TryGet(key, out var value).ShouldBeTrue();
            value.ShouldBe(expectedValue);
        }
    }

    private static TestFixture CreateTestFixture<TKey, TValue>()
    {
        var testFixture = TestFixture.Create(rockDb =>
        {
            _ = rockDb.AddStore<TKey, TValue, RocksDbGenericStore<TKey, TValue>>("my-store");
        });
        return testFixture;
    }
}