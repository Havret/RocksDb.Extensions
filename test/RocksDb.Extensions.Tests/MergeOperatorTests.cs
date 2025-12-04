using NUnit.Framework;
using RocksDb.Extensions.MergeOperators;
using RocksDb.Extensions.Tests.Utils;

namespace RocksDb.Extensions.Tests;

/// <summary>
/// Store that uses merge operations for testing counters.
/// </summary>
public class CounterStore : MergeableRocksDbStore<string, long>
{
    public CounterStore(IRocksDbAccessor<string, long> rocksDbAccessor) : base(rocksDbAccessor)
    {
    }

    public void Increment(string key, long delta = 1) => Merge(key, delta);
}

/// <summary>
/// Store that uses merge operations for testing list appends.
/// </summary>
public class EventLogStore : MergeableRocksDbStore<string, IList<string>>
{
    public EventLogStore(IRocksDbAccessor<string, IList<string>> rocksDbAccessor) : base(rocksDbAccessor)
    {
    }

    public void AppendEvent(string key, string eventData) => Merge(key, new List<string> { eventData });
}

/// <summary>
/// Store that uses merge operations for testing list operations (add/remove).
/// </summary>
public class TagsStore : MergeableRocksDbStore<string, IList<ListOperation<string>>>
{
    public TagsStore(IRocksDbAccessor<string, IList<ListOperation<string>>> rocksDbAccessor) : base(rocksDbAccessor)
    {
    }

    public void AddTags(string key, params string[] tags)
    {
        Merge(key, new List<ListOperation<string>> { ListOperation<string>.Add(tags) });
    }

    public void RemoveTags(string key, params string[] tags)
    {
        Merge(key, new List<ListOperation<string>> { ListOperation<string>.Remove(tags) });
    }

    public IList<string>? GetTags(string key)
    {
        if (TryGet(key, out var operations) && operations != null && operations.Count > 0)
        {
            // The merged result is a single Add operation containing all items
            return operations[0].Items;
        }
        return null;
    }
}

public class MergeOperatorTests
{
    [Test]
    public void should_increment_counter_using_merge_operation()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            rockDb.AddMergeableStore<string, long, CounterStore>("counters", new Int64AddMergeOperator());
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
            rockDb.AddMergeableStore<string, long, CounterStore>("counters", new Int64AddMergeOperator());
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
            rockDb.AddMergeableStore<string, IList<string>, EventLogStore>("events", new ListAppendMergeOperator<string>());
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
            rockDb.AddMergeableStore<string, IList<string>, EventLogStore>("events", new ListAppendMergeOperator<string>());
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
            rockDb.AddMergeableStore<string, long, CounterStore>("counters", new Int64AddMergeOperator());
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

    [Test]
    public void should_add_items_to_list_using_list_merge_operator()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            rockDb.AddMergeableStore<string, IList<ListOperation<string>>, TagsStore>("tags", new ListMergeOperator<string>());
        });

        var store = testFixture.GetStore<TagsStore>();
        var key = "article-1";

        // Act
        store.AddTags(key, "csharp", "dotnet");
        store.AddTags(key, "rocksdb");

        // Assert
        var tags = store.GetTags(key);
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags!.Count, Is.EqualTo(3));
        Assert.That(tags, Does.Contain("csharp"));
        Assert.That(tags, Does.Contain("dotnet"));
        Assert.That(tags, Does.Contain("rocksdb"));
    }

    [Test]
    public void should_remove_items_from_list_using_list_merge_operator()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            rockDb.AddMergeableStore<string, IList<ListOperation<string>>, TagsStore>("tags", new ListMergeOperator<string>());
        });

        var store = testFixture.GetStore<TagsStore>();
        var key = "article-1";

        // Act - Add items, then remove some
        store.AddTags(key, "csharp", "dotnet", "java", "python");
        store.RemoveTags(key, "java", "python");

        // Assert
        var tags = store.GetTags(key);
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags!.Count, Is.EqualTo(2));
        Assert.That(tags, Does.Contain("csharp"));
        Assert.That(tags, Does.Contain("dotnet"));
        Assert.That(tags, Does.Not.Contain("java"));
        Assert.That(tags, Does.Not.Contain("python"));
    }

    [Test]
    public void should_handle_mixed_add_and_remove_operations()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            rockDb.AddMergeableStore<string, IList<ListOperation<string>>, TagsStore>("tags", new ListMergeOperator<string>());
        });

        var store = testFixture.GetStore<TagsStore>();
        var key = "article-1";

        // Act - Interleave adds and removes
        store.AddTags(key, "a", "b", "c");
        store.RemoveTags(key, "b");
        store.AddTags(key, "d", "e");
        store.RemoveTags(key, "a", "e");

        // Assert - Should have: c, d
        var tags = store.GetTags(key);
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags!.Count, Is.EqualTo(2));
        Assert.That(tags, Does.Contain("c"));
        Assert.That(tags, Does.Contain("d"));
    }

    [Test]
    public void should_handle_remove_nonexistent_item_gracefully()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            rockDb.AddMergeableStore<string, IList<ListOperation<string>>, TagsStore>("tags", new ListMergeOperator<string>());
        });

        var store = testFixture.GetStore<TagsStore>();
        var key = "article-1";

        // Act - Try to remove items that don't exist
        store.AddTags(key, "csharp");
        store.RemoveTags(key, "nonexistent");

        // Assert - Original item should still be there
        var tags = store.GetTags(key);
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags!.Count, Is.EqualTo(1));
        Assert.That(tags, Does.Contain("csharp"));
    }

    [Test]
    public void should_remove_only_first_occurrence_of_duplicate_items()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            rockDb.AddMergeableStore<string, IList<ListOperation<string>>, TagsStore>("tags", new ListMergeOperator<string>());
        });

        var store = testFixture.GetStore<TagsStore>();
        var key = "article-1";

        // Act - Add duplicate items, then remove one
        store.AddTags(key, "tag", "tag", "tag");
        store.RemoveTags(key, "tag");

        // Assert - Should have 2 remaining
        var tags = store.GetTags(key);
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags!.Count, Is.EqualTo(2));
        Assert.That(tags[0], Is.EqualTo("tag"));
        Assert.That(tags[1], Is.EqualTo("tag"));
    }
}
