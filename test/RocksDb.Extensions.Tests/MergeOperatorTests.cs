using NUnit.Framework;
using RocksDb.Extensions.MergeOperators;
using RocksDb.Extensions.Tests.Utils;

namespace RocksDb.Extensions.Tests;

/// <summary>
/// Store that exposes the protected Merge method for testing counters.
/// </summary>
public class CounterStore : RocksDbStore<string, long>
{
    public CounterStore(IRocksDbAccessor<string, long> rocksDbAccessor) : base(rocksDbAccessor)
    {
    }

    public void Increment(string key, long delta = 1) => Merge(key, delta);
}

/// <summary>
/// Store that exposes the protected Merge method for testing list appends.
/// </summary>
public class EventLogStore : RocksDbStore<string, IList<string>>
{
    public EventLogStore(IRocksDbAccessor<string, IList<string>> rocksDbAccessor) : base(rocksDbAccessor)
    {
    }

    public void AppendEvent(string key, string eventData) => Merge(key, new List<string> { eventData });
}

public class MergeOperatorTests
{
    [Test]
    public void should_increment_counter_using_merge_operation()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            rockDb.AddStore<string, long, CounterStore>("counters", new Int64AddMergeOperator());
        });

        var store = testFixture.GetStore<CounterStore>();
        var key = "page-views";

        // Act
        store.Increment(key, 1);
        store.Increment(key, 5);
        store.Increment(key, 10);

        // Assert
        Assert.That(store.TryGet(key, out var value), Is.True);
        Assert.That(value, Is.EqualTo(16));
    }

    [Test]
    public void should_handle_counter_with_initial_value()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            rockDb.AddStore<string, long, CounterStore>("counters", new Int64AddMergeOperator());
        });

        var store = testFixture.GetStore<CounterStore>();
        var key = "page-views";

        // Act - Put initial value, then merge
        store.Put(key, 100);
        store.Increment(key, 50);

        // Assert
        Assert.That(store.TryGet(key, out var value), Is.True);
        Assert.That(value, Is.EqualTo(150));
    }

    [Test]
    public void should_append_to_list_using_merge_operation()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            rockDb.AddStore<string, IList<string>, EventLogStore>("events", new ListAppendMergeOperator<string>());
        });

        var store = testFixture.GetStore<EventLogStore>();
        var key = "user-actions";

        // Act
        store.AppendEvent(key, "login");
        store.AppendEvent(key, "view-page");
        store.AppendEvent(key, "logout");

        // Assert
        Assert.That(store.TryGet(key, out var events), Is.True);
        Assert.That(events, Is.Not.Null);
        Assert.That(events.Count, Is.EqualTo(3));
        Assert.That(events[0], Is.EqualTo("login"));
        Assert.That(events[1], Is.EqualTo("view-page"));
        Assert.That(events[2], Is.EqualTo("logout"));
    }

    [Test]
    public void should_append_to_existing_list_using_merge_operation()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            rockDb.AddStore<string, IList<string>, EventLogStore>("events", new ListAppendMergeOperator<string>());
        });

        var store = testFixture.GetStore<EventLogStore>();
        var key = "user-actions";

        // Act - Put initial value, then merge
        store.Put(key, new List<string> { "initial-event" });
        store.AppendEvent(key, "new-event");

        // Assert
        Assert.That(store.TryGet(key, out var events), Is.True);
        Assert.That(events, Is.Not.Null);
        Assert.That(events.Count, Is.EqualTo(2));
        Assert.That(events[0], Is.EqualTo("initial-event"));
        Assert.That(events[1], Is.EqualTo("new-event"));
    }

    [Test]
    public void should_handle_multiple_keys_with_merge_operations()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            rockDb.AddStore<string, long, CounterStore>("counters", new Int64AddMergeOperator());
        });

        var store = testFixture.GetStore<CounterStore>();

        // Act
        store.Increment("key1", 10);
        store.Increment("key2", 20);
        store.Increment("key1", 5);
        store.Increment("key2", 10);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(store.TryGet("key1", out var value1), Is.True);
            Assert.That(value1, Is.EqualTo(15));

            Assert.That(store.TryGet("key2", out var value2), Is.True);
            Assert.That(value2, Is.EqualTo(30));
        });
    }
}
