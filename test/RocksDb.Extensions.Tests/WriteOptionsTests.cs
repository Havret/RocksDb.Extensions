using NUnit.Framework;
using RocksDb.Extensions.Tests.Utils;
using Shouldly;

namespace RocksDb.Extensions.Tests;

public class WriteOptionsTests
{
    [Test]
    public void should_configure_wait_for_flush_option()
    {
        using var testFixture = TestFixture.Create(rockDb =>
        {
            _ = rockDb.AddStore<int, int, RocksDbGenericStore<int, int>>("my-store");
        }, options =>
        {
            options.WaitForFlush = false;
        });

        var store = testFixture.GetStore<RocksDbGenericStore<int, int>>();
        
        // Verify that write operations work with the configured options
        store.Put(1, 100);
        store.TryGet(1, out var value).ShouldBeTrue();
        value.ShouldBe(100);
    }

    [Test]
    public void should_use_write_options_for_put_operations()
    {
        using var testFixture = TestFixture.Create(rockDb =>
        {
            _ = rockDb.AddStore<string, string, RocksDbGenericStore<string, string>>("my-store");
        });

        var store = testFixture.GetStore<RocksDbGenericStore<string, string>>();
        
        // Verify that Put operation works (uses WriteOptions internally)
        store.Put("key1", "value1");
        store.TryGet("key1", out var value).ShouldBeTrue();
        value.ShouldBe("value1");
    }

    [Test]
    public void should_use_write_options_for_remove_operations()
    {
        using var testFixture = TestFixture.Create(rockDb =>
        {
            _ = rockDb.AddStore<string, string, RocksDbGenericStore<string, string>>("my-store");
        });

        var store = testFixture.GetStore<RocksDbGenericStore<string, string>>();
        
        // Verify that Remove operation works (uses WriteOptions internally)
        store.Put("key1", "value1");
        store.Remove("key1");
        store.TryGet("key1", out _).ShouldBeFalse();
    }

    [Test]
    public void should_use_write_options_for_put_range_operations()
    {
        using var testFixture = TestFixture.Create(rockDb =>
        {
            _ = rockDb.AddStore<int, int, RocksDbGenericStore<int, int>>("my-store");
        });

        var store = testFixture.GetStore<RocksDbGenericStore<int, int>>();
        
        // Verify that PutRange operation works (uses WriteOptions internally)
        var items = new[] { (1, 100), (2, 200), (3, 300) };
        store.PutRange(items);
        
        store.TryGet(1, out var value1).ShouldBeTrue();
        value1.ShouldBe(100);
        store.TryGet(2, out var value2).ShouldBeTrue();
        value2.ShouldBe(200);
        store.TryGet(3, out var value3).ShouldBeTrue();
        value3.ShouldBe(300);
    }
}
