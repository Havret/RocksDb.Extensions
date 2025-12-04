using NUnit.Framework;
using RocksDb.Extensions.MergeOperators;
using RocksDb.Extensions.ProtoBufNet;
using RocksDb.Extensions.Tests.Protos;
using RocksDb.Extensions.Tests.Utils;

namespace RocksDb.Extensions.Tests;

public class MergeOperatorTests
{
    [Test]
    public void should_add_to_existing_list_using_merge_operation()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            rockDb.AddMergeableStore<string, IList<string>, TagsStore, CollectionOperation<string>>("tags", new ListMergeOperator<string>());
        });

        var store = testFixture.GetStore<TagsStore>();
        var key = "article-1";

        // Act
        store.Put(key, new List<string> { "csharp", "dotnet" });
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
    public void should_add_items_to_list_using_list_merge_operator()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            rockDb.AddMergeableStore<string, IList<string>, TagsStore, CollectionOperation<string>>("tags", new ListMergeOperator<string>());
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
            rockDb.AddMergeableStore<string, IList<string>, TagsStore, CollectionOperation<string>>("tags", new ListMergeOperator<string>());
        });

        var store = testFixture.GetStore<TagsStore>();
        var key = "article-1";

        // Act - Add items, then remove some
        store.Merge(key, CollectionOperation<string>.Add("csharp", "dotnet", "java", "python"));
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
            rockDb.AddMergeableStore<string, IList<string>, TagsStore, CollectionOperation<string>>("tags", new ListMergeOperator<string>());
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
            rockDb.AddMergeableStore<string, IList<string>, TagsStore, CollectionOperation<string>>("tags", new ListMergeOperator<string>());
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
            rockDb.AddMergeableStore<string, IList<string>, TagsStore, CollectionOperation<string>>("tags", new ListMergeOperator<string>());
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
    
    [Test]
    public void should_add_primitive_types_to_list_using_list_merge_operator()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            rockDb.AddMergeableStore<string, IList<int>, ScoresStore, CollectionOperation<int>>(
                "scores", 
                new ListMergeOperator<int>());
        });

        var store = testFixture.GetStore<ScoresStore>();
        var key = "player-1";

        // Act
        store.AddScores(key, 100, 200);
        store.AddScores(key, 300);

        // Assert
        Assert.That(store.TryGet(key, out var scores), Is.True);
        Assert.That(scores, Is.Not.Null);
        Assert.That(scores!.Count, Is.EqualTo(3));
        Assert.That(scores, Does.Contain(100));
        Assert.That(scores, Does.Contain(200));
        Assert.That(scores, Does.Contain(300));
    }

    [Test]
    public void should_add_and_remove_primitive_types_using_list_merge_operator()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            rockDb.AddMergeableStore<string, IList<int>, ScoresStore, CollectionOperation<int>>(
                "scores", 
                new ListMergeOperator<int>());
        });

        var store = testFixture.GetStore<ScoresStore>();
        var key = "player-1";

        // Act - Add items, then remove some
        store.AddScores(key, 100, 200, 300, 400);
        store.RemoveScores(key, 200, 400); // Remove middle values

        // Assert
        Assert.That(store.TryGet(key, out var scores), Is.True);
        Assert.That(scores, Is.Not.Null);
        Assert.That(scores!.Count, Is.EqualTo(2));
        Assert.That(scores, Does.Contain(100));
        Assert.That(scores, Does.Contain(300));
        Assert.That(scores, Does.Not.Contain(200));
        Assert.That(scores, Does.Not.Contain(400));
    }

    [Test]
    public void should_preserve_merge_operator_after_clear()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            rockDb.AddMergeableStore<string, IList<string>, TagsStore, CollectionOperation<string>>("tags", new ListMergeOperator<string>());
        });

        var store = testFixture.GetStore<TagsStore>();
        var key = "article-1";

        // Act - Add items, clear the store, then use merge operation again
        store.AddTags(key, "csharp", "dotnet");
        Assert.That(store.TryGet(key, out var tagsBeforeClear), Is.True);
        Assert.That(tagsBeforeClear!.Count, Is.EqualTo(2));

        store.Clear();

        // After clear, merge operations should still work
        store.AddTags(key, "rocksdb", "database");
        store.AddTags(key, "performance");

        // Assert
        Assert.That(store.TryGet(key, out var tagsAfterClear), Is.True);
        Assert.That(tagsAfterClear, Is.Not.Null);
        Assert.That(tagsAfterClear!.Count, Is.EqualTo(3));
        Assert.That(tagsAfterClear, Does.Contain("rocksdb"));
        Assert.That(tagsAfterClear, Does.Contain("database"));
        Assert.That(tagsAfterClear, Does.Contain("performance"));
        
        // Old data should be gone
        Assert.That(tagsAfterClear, Does.Not.Contain("csharp"));
        Assert.That(tagsAfterClear, Does.Not.Contain("dotnet"));
    }

    [Test]
    public void should_not_lose_data_when_using_protobufnet_serializer_with_merge_operators()
    {
        // Arrange
        using var testFixture = TestFixture.Create(rockDb =>
        {
            rockDb.AddMergeableStore<string, IList<ProtoNetTag>, ProtoNetTagsStore, CollectionOperation<ProtoNetTag>>(
                "protonet-tags", 
                new ListMergeOperator<ProtoNetTag>());
        }, options =>
        {
            options.SerializerFactories.Clear();
            options.SerializerFactories.Add(new PrimitiveTypesSerializerFactory()); // For string keys
            options.SerializerFactories.Add(new ProtoBufNetSerializerFactory()); // For ProtoNetTag values
        });

        var store = testFixture.GetStore<ProtoNetTagsStore>();
        var key = "article-1";
        
        var tag1 = new ProtoNetTag { Id = 1, Name = "csharp" };
        var tag2 = new ProtoNetTag { Id = 2, Name = "dotnet" };
        var tag3 = new ProtoNetTag { Id = 3, Name = "rocksdb" };

        // Act - Use Put first to establish initial value, then Merge
        store.Put(key, new List<ProtoNetTag> { tag1 });
        store.AddTags(key, tag2);
        store.AddTags(key, tag3);

        // Assert
        Assert.That(store.TryGet(key, out var tags), Is.True);
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags!.Count, Is.EqualTo(3), "All tags should be preserved");
        Assert.That(tags, Does.Contain(tag1));
        Assert.That(tags, Does.Contain(tag2));
        Assert.That(tags, Does.Contain(tag3));
    }

    private class TagsStore : MergeableRocksDbStore<string, IList<string>, CollectionOperation<string>>
    {
        public TagsStore(IMergeAccessor<string, IList<string>, CollectionOperation<string>> mergeAccessor)
            : base(mergeAccessor)
        {
        }

        public void AddTags(string key, params string[] tags)
        {
            Merge(key, CollectionOperation<string>.Add(tags));
        }

        public void RemoveTags(string key, params string[] tags)
        {
            Merge(key, CollectionOperation<string>.Remove(tags));
        }
    }

    private class ScoresStore : MergeableRocksDbStore<string, IList<int>, CollectionOperation<int>>
    {
        public ScoresStore(IMergeAccessor<string, IList<int>, CollectionOperation<int>> mergeAccessor)
            : base(mergeAccessor)
        {
        }

        public void AddScores(string key, params int[] scores)
        {
            Merge(key, CollectionOperation<int>.Add(scores));
        }

        public void RemoveScores(string key, params int[] scores)
        {
            Merge(key, CollectionOperation<int>.Remove(scores));
        }
    }

    private class ProtoNetTagsStore : MergeableRocksDbStore<string, IList<ProtoNetTag>, CollectionOperation<ProtoNetTag>>
    {
        public ProtoNetTagsStore(IMergeAccessor<string, IList<ProtoNetTag>, CollectionOperation<ProtoNetTag>> mergeAccessor)
            : base(mergeAccessor)
        {
        }

        public void AddTags(string key, params ProtoNetTag[] tags)
        {
            Merge(key, CollectionOperation<ProtoNetTag>.Add(tags));
        }

        public void RemoveTags(string key, params ProtoNetTag[] tags)
        {
            Merge(key, CollectionOperation<ProtoNetTag>.Remove(tags));
        }
    }    
}