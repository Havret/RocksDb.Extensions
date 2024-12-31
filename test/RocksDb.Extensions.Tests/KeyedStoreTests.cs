using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using RocksDb.Extensions.Protobuf;
using RocksDb.Extensions.Tests.Utils;

namespace RocksDb.Extensions.Tests;

public class KeyedStoreTests
{
    [Test]
    public void should_resolve_rocksdb_stores_as_keyed_services_when_registered_under_different_column_names()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            _ = rockDb.AddStore<CacheKey, CacheValue, RocksDbGenericStore<CacheKey, CacheValue>>("my-store-1");
            _ = rockDb.AddStore<CacheKey, CacheValue, RocksDbGenericStore<CacheKey, CacheValue>>("my-store-2");
        }, options =>
        {
            options.SerializerFactories.Clear();
            options.SerializerFactories.Add(new ProtobufSerializerFactory());
        });

        // Act
        var store1 = testFixture.ServiceProvider.GetRequiredKeyedService<RocksDbGenericStore<CacheKey, CacheValue>>("my-store-1");
        var store2 = testFixture.ServiceProvider.GetRequiredKeyedService<RocksDbGenericStore<CacheKey, CacheValue>>("my-store-2");
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(store1, Is.Not.Null);
            Assert.That(store2, Is.Not.Null);
            Assert.That(ReferenceEquals(store1, store2), Is.False);
        });
    }

    [Test]
    public void should_throw_when_resolving_non_existent_store()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            _ = rockDb.AddStore<CacheKey, CacheValue, RocksDbGenericStore<CacheKey, CacheValue>>("my-store-1");
        }, options =>
        {
            options.SerializerFactories.Clear();
            options.SerializerFactories.Add(new ProtobufSerializerFactory());
        });

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => testFixture.ServiceProvider.GetRequiredKeyedService<RocksDbGenericStore<CacheKey, CacheValue>>("non-existent-store"));
    }
    
    [Test]
    public void should_resolve_default_store_as_first_registered_keyed_service()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            _ = rockDb.AddStore<CacheKey, CacheValue, RocksDbGenericStore<CacheKey, CacheValue>>("my-store-1");
            _ = rockDb.AddStore<CacheKey, CacheValue, RocksDbGenericStore<CacheKey, CacheValue>>("my-store-2");
        }, options =>
        {
            options.SerializerFactories.Clear();
            options.SerializerFactories.Add(new ProtobufSerializerFactory());
        });

        // Act
        var store1 = testFixture.ServiceProvider.GetRequiredKeyedService<RocksDbGenericStore<CacheKey, CacheValue>>("my-store-1");
        var store2 = testFixture.ServiceProvider.GetRequiredKeyedService<RocksDbGenericStore<CacheKey, CacheValue>>("my-store-2");
        var store = testFixture.ServiceProvider.GetRequiredService<RocksDbGenericStore<CacheKey, CacheValue>>();
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ReferenceEquals(store1, store), Is.True);
            Assert.That(ReferenceEquals(store2, store), Is.False);
        });
    }
}