using NUnit.Framework;
using RocksDb.Extensions.Tests.Utils;
using Shouldly;

namespace RocksDb.Extensions.Tests;

public class PutRangeTests
{
    [Test]
    public void should_put_range_data_to_store()
    {
        // Arrange
        using var testFixture = CreateTestFixture<int, string>();

        var store = testFixture.GetStore<RocksDbGenericStore<int, string>>();
        var cacheKeys = Enumerable.Range(0, 100)
            .Select(x => (key: x, value: x.ToString()))
            .ToList();

        // Act
        store.PutRange(cacheKeys);

        // Assert
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