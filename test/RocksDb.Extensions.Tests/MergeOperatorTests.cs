using NUnit.Framework;
using RocksDb.Extensions.MergeOperators;
using RocksDb.Extensions.Tests.Utils;

namespace RocksDb.Extensions.Tests;

/// <summary>
/// Store that uses merge operations for testing list appends.
/// </summary>
public class EventLogStore : MergeableRocksDbStore<string, IList<string>, IList<string>>
{
    public EventLogStore(IMergeAccessor<string, IList<string>, IList<string>> mergeAccessor)
        : base(mergeAccessor)
    {
    }

    public void AppendEvent(string key, string eventData) => Merge(key, new List<string> { eventData });
}

/// <summary>
/// Store that uses merge operations for testing list operations (add/remove).
/// </summary>
public class TagsStore : MergeableRocksDbStore<string, IList<string>, ListOperation<string>>
{
    public TagsStore(IMergeAccessor<string, IList<string>, ListOperation<string>> mergeAccessor)
        : base(mergeAccessor)
    {
    }

    public void AddTags(string key, params string[] tags)
    {
        Merge(key, ListOperation<string>.Add(tags));
    }

    public void RemoveTags(string key, params string[] tags)
    {
        Merge(key, ListOperation<string>.Remove(tags));
    }
}

public class MergeOperatorTests
{
    [Test]
    public void should_append_to_list_using_merge_operation()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            rockDb.AddMergeableStore<string, IList<string>, EventLogStore, IList<string>>("events",
                new ListAppendMergeOperator<string>());
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
            rockDb.AddMergeableStore<string, IList<string>, EventLogStore, IList<string>>("events",
                new ListAppendMergeOperator<string>());
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
    public void should_add_items_to_list_using_list_merge_operator()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            rockDb.AddMergeableStore<string, IList<string>, TagsStore, ListOperation<string>>("tags", new ListMergeOperator<string>());
        });

        var store = testFixture.GetStore<TagsStore>();
        var key = "article-1";

        // Act
        store.AddTags(key, "csharp", "dotnet");
        store.AddTags(key, "rocksdb");

        // Assert
        Assert.That(store.TryGet(key, out var tags), Is.True);
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
            rockDb.AddMergeableStore<string, IList<string>, TagsStore, ListOperation<string>>("tags", new ListMergeOperator<string>());
        });

        var store = testFixture.GetStore<TagsStore>();
        var key = "article-1";

        // Act - Add items, then remove some
        store.AddTags(key, "csharp", "dotnet", "java", "python");
        store.RemoveTags(key, "java", "python");

        // Assert
        Assert.That(store.TryGet(key, out var tags), Is.True);
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
            rockDb.AddMergeableStore<string, IList<string>, TagsStore, ListOperation<string>>("tags", new ListMergeOperator<string>());
        });

        var store = testFixture.GetStore<TagsStore>();
        var key = "article-1";

        // Act - Interleave adds and removes
        store.AddTags(key, "a", "b", "c");
        store.RemoveTags(key, "b");
        store.AddTags(key, "d", "e");
        store.RemoveTags(key, "a", "e");

        // Assert - Should have: c, d
        Assert.That(store.TryGet(key, out var tags), Is.True);
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
            rockDb.AddMergeableStore<string, IList<string>, TagsStore, ListOperation<string>>("tags", new ListMergeOperator<string>());
        });

        var store = testFixture.GetStore<TagsStore>();
        var key = "article-1";

        // Act - Try to remove items that don't exist
        store.AddTags(key, "csharp");
        store.RemoveTags(key, "nonexistent");

        // Assert - Original item should still be there
        Assert.That(store.TryGet(key, out var tags), Is.True);
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
            rockDb.AddMergeableStore<string, IList<string>, TagsStore, ListOperation<string>>("tags", new ListMergeOperator<string>());
        });

        var store = testFixture.GetStore<TagsStore>();
        var key = "article-1";

        // Act - Add duplicate items, then remove one
        store.AddTags(key, "tag", "tag", "tag");
        store.RemoveTags(key, "tag");

        // Assert - Should have 2 remaining
        Assert.That(store.TryGet(key, out var tags), Is.True);
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags!.Count, Is.EqualTo(2));
        Assert.That(tags[0], Is.EqualTo("tag"));
        Assert.That(tags[1], Is.EqualTo("tag"));
    }
}