# RocksDb.Extensions

[![Build](https://github.com/Havret/RocksDb.Extensions/actions/workflows/build.yml/badge.svg)](https://github.com/Havret/RocksDb.Extensions/actions/workflows/build.yml)

RocksDB.Extensions is a .NET library that provides an opinionated integration of RocksDB with the .NET dependency injection system. This library enables you to easily create key/value data stores backed by RocksDB, and provides several built-in serializer factories to serialize your data.

|NuGet|Status|
|------|-------------|
|RocksDb.Extensions|[![NuGet](https://img.shields.io/nuget/vpre/RocksDb.Extensions.svg)](https://www.nuget.org/packages/RocksDb.Extensions/)
|RocksDb.Extensions.Protobuf|[![NuGet](https://img.shields.io/nuget/vpre/RocksDb.Extensions.Protobuf.svg)](https://www.nuget.org/packages/RocksDb.Extensions.Protobuf/)
|RocksDb.Extensions.System.Text.Json|[![NuGet](https://img.shields.io/nuget/vpre/RocksDb.Extensions.System.Text.Json.svg)](https://www.nuget.org/packages/RocksDb.Extensions.System.Text.Json/)
|RocksDb.Extensions.ProtoBufNet|[![NuGet](https://img.shields.io/nuget/vpre/RocksDb.Extensions.ProtoBufNet.svg)](https://www.nuget.org/packages/RocksDb.Extensions.ProtoBufNet/)

## Quickstart

### Install the package

To use RocksDb.Extensions in your project, add the NuGet package using the dotnet CLI:

```
dotnet add package RocksDb.Extensions
```

### Create a key/value store

The library abstracts away the internals of RocksDb behind the concept of key/value data stores. To create your first store, you need a class that inherits from the base class `RocksDbStore<TKey, TValue>`. For example:

```csharp
public class UsersStore : RocksDbStore<string, User>
{
    public UsersStore(IRocksDbAccessor<string, User> rocksDbAccessor) : base(rocksDbAccessor)
    {
    }
}
```

In this example, we're using `string` as the key and a custom type `User` as the value. The library is flexible regarding how you can serialize your keys and values. There are several serialization options available, and you can add your own by inheriting from the `ISerializerFactory` interface.

### Choose a serializer

The library provides 4 built-in serializer factories:

- PrimitiveTypesSerializerFactory
- SystemTextJsonSerializerFactory
- ProtoBufNetSerializerFactory
- ProtobufSerializerFactory

You can use one or more of these factories to serialize your data. For example, if you want to use `PrimitiveTypesSerializerFactory` to serialize your keys and `SystemTextJsonSerializerFactory` to serialize your values, you can configure your RocksDb instance as follows:

```csharp
var rocksDbBuilder = builder.Services.AddRocksDb(options =>
{

    // Specify the path for the RocksDb database
    options.Path = "./my-rocks-db";

    // Clear pre-configured SerializerFactories so we can have full control over the order in which serializers will be resolved.
    options.SerializerFactories.Clear();

    // Add the serializer factories for the key and value types
    options.SerializerFactories.Add(new PrimitiveTypesSerializerFactory());
    options.SerializerFactories.Add(new SystemTextJsonSerializerFactory());
});
```

### Register your store

Before your store can be used, you need to register it with RocksDb. You can do this as follows:

```csharp
rocksDbBuilder.AddStore<string, User, UsersStore>("users-store");
```

This registers an instance of `UsersStore` with RocksDb under the name "users-store".

#### Keyed Service Resolution
You can also resolve your store as a keyed service using the column family name:

```csharp
var usersStore = serviceProvider.GetRequiredKeyedService<UsersStore>("users-store");
```

This approach allows you to register and retrieve multiple stores of the same type, each differentiated by their column family name.

### Use your store

Once you have registered your store, you can use it to add, get, and remove data from RocksDb. For example:

```csharp
[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly UsersStore _usersStore;

    public UsersController(UsersStore usersStore)
    {
        _usersStore = usersStore;
    }

    [HttpPost]
    public IActionResult Post([FromBody] User user)
    {
        var id = Guid.NewGuid().ToString();
        user.Id = id;
        _usersStore.Put(id, user);
        return Ok();
    }

    [HttpGet]
    public IEnumerable<User> Get()
    {
        return _usersStore.GetAll();
    }

    [HttpDelete]
    public IActionResult Delete(string userId)
    {
        _usersStore.Remove(userId);
        return Ok();
    }
}
```

## Clean start

For certain scenarios, such as automated testing, it may be useful to remove the existing RocksDB database on startup to ensure a clean start. This can be achieved using the `DeleteExistingDatabaseOnStartup` configuration option.

To use this option, set it to true when configuring the `RocksDbOptions` object:

```csharp
var rocksDbBuilder = builder.Services.AddRocksDb(options =>
{
    // Your RocksDb configuration... 
    options.DeleteExistingDatabaseOnStartup = true;
});
```

When this option is set to true, the existing database will be deleted on startup and a new one will be created. Note that all data in the existing database will be lost when this option is used.

By default, the `DeleteExistingDatabaseOnStartup` option is set to false to preserve the current behavior of not automatically deleting the database. If you need to ensure a clean start for your application, set this option to true in your configuration.

## Collections Support

RocksDb.Extensions provides built-in support for collections across different serialization packages:

### System.Text.Json and ProtoBufNet

The `RocksDb.Extensions.System.Text.Json` and `RocksDb.Extensions.ProtoBufNet` packages support collections out of the box. You can use any collection type like `List<T>` or arrays without additional configuration.

### Protocol Buffers and Primitive Types Support

The library includes specialized support for collections when working with:

1. Protocol Buffer message types
2. Primitive types (int, long, string, etc.)

When using `IList<T>` with these types, the library automatically handles serialization/deserialization without requiring wrapper message types. This is particularly useful for Protocol Buffers, where `RepeatedField<T>` typically cannot be serialized as a standalone entity.

The serialization format varies depending on the element type:

#### Fixed-Size Types (int, long, etc.)

```
[4 bytes: List length][Contiguous array of serialized elements]
```

#### Variable-Size Types (string, protobuf messages)

```
[4 bytes: List length][For each element: [4 bytes: Element size][N bytes: Element data]]
```

Example types that work automatically with this support:

- Protocol Buffer message types: `IList<YourProtobufMessage>`
- Primitive types: `IList<int>`, `IList<long>`, `IList<string>`, etc.

## Merge Operators

RocksDb.Extensions provides powerful support for RocksDB's merge operators, enabling efficient atomic read-modify-write operations without requiring separate read and write operations. This is particularly useful for counters, list operations, and other accumulative data structures.

### What are Merge Operators?

Merge operators allow you to apply atomic transformations to values in RocksDB without needing to:
1. Read the current value
2. Modify it in your application code
3. Write it back

Instead, you submit a "merge operand" (the change to apply), and RocksDB handles the merge internally. This provides several benefits:

- **Performance**: Eliminates the round-trip read before write
- **Atomicity**: Operations are applied atomically at the database level
- **Correctness**: Avoids race conditions in concurrent scenarios
- **Efficiency**: Merge operands can be combined during compaction

### Creating a Mergeable Store

To use merge operators, inherit from `MergeableRocksDbStore<TKey, TValue, TOperand>` instead of `RocksDbStore<TKey, TValue>`:

```csharp
public class TagsStore : MergeableRocksDbStore<string, IList<string>, CollectionOperation<string>>
{
    public TagsStore(IMergeAccessor<string, IList<string>, CollectionOperation<string>> mergeAccessor) 
        : base(mergeAccessor) { }
    
    public void AddTags(string key, params string[] tags)
    {
        Merge(key, CollectionOperation<string>.Add(tags));
    }
    
    public void RemoveTags(string key, params string[] tags)
    {
        Merge(key, CollectionOperation<string>.Remove(tags));
    }
}
```

Key differences from regular stores:
- Three type parameters: `TKey`, `TValue`, and `TOperand`
- `TValue` is the actual value stored in the database
- `TOperand` is the merge operand (the change/delta to apply)
- `Merge()` method for applying operations

### Registering a Mergeable Store

Register your mergeable store with a merge operator implementation:

```csharp
rocksDbBuilder.AddMergeableStore<string, IList<string>, TagsStore, CollectionOperation<string>>(
    columnFamily: "tags", 
    mergeOperator: new ListMergeOperator<string>()
);
```

### Built-in Merge Operator: ListMergeOperator

The library includes `ListMergeOperator<T>` for atomic list operations with add/remove support:

**Features:**
- Add multiple items to a list atomically
- Remove multiple items from a list atomically
- Operations are applied in order
- Removes only the first occurrence of each item (like `List<T>.Remove()`)
- Non-existent items in remove operations are silently ignored
- Efficient partial merge: combines multiple Add operations during compaction

**Example Usage:**

```csharp
// Add tags without reading the current list
tagsStore.AddTags("article-1", "csharp", "dotnet", "rocksdb");

// Remove specific tags
tagsStore.RemoveTags("article-1", "rocksdb");

// Read the current value
if (tagsStore.TryGet("article-1", out var tags))
{
    // tags now contains: ["csharp", "dotnet"]
}
```

### Creating Custom Merge Operators

You can create custom merge operators by implementing `IMergeOperator<TValue, TOperand>`:

```csharp
public class CounterMergeOperator : IMergeOperator<long, long>
{
    public string Name => "CounterMergeOperator";
    
    public (bool Success, long Value) FullMerge(long? existingValue, ReadOnlySpan<long> operands)
    {
        var result = existingValue ?? 0;
        foreach (var operand in operands)
        {
            result += operand;
        }
        return (true, result);
    }
    
    public (bool Success, long? Operand) PartialMerge(ReadOnlySpan<long> operands)
    {
        // Counters are commutative - we can combine all increments
        long sum = 0;
        foreach (var operand in operands)
        {
            sum += operand;
        }
        return (true, sum);
    }
}

// Usage in a store
public class CounterStore : MergeableRocksDbStore<string, long, long>
{
    public CounterStore(IMergeAccessor<string, long, long> mergeAccessor) 
        : base(mergeAccessor) { }
    
    public void Increment(string key, long delta = 1) => Merge(key, delta);
    public void Decrement(string key, long delta = 1) => Merge(key, -delta);
}

// Registration
rocksDbBuilder.AddMergeableStore<string, long, CounterStore, long>(
    "counters", 
    new CounterMergeOperator()
);
```

### Understanding IMergeOperator Methods

**FullMerge:**
- Called during Get operations to produce the final value
- Receives the existing value (or null/default) and all pending merge operands
- Must apply all operands in order and return the final result
- Returns `(bool Success, TValue Value)` - Success=false indicates merge failure

**PartialMerge:**
- Called during compaction to optimize storage
- Combines multiple operands without knowing the existing value
- Returns `(bool Success, TOperand? Operand)`:
  - Success=true: operands were successfully combined
  - Success=false: operands cannot be safely combined (RocksDB keeps them separate)
- Optimization only - if partial merge fails, RocksDB will call FullMerge later

**When to return Success=false in PartialMerge:**
- Operations are order-dependent (like Add followed by Remove)
- Operations require knowledge of the existing value
- Operations cannot be combined safely

### Use Cases for Merge Operators

1. **Counters**: Increment/decrement without reading
   - `TValue=long`, `TOperand=long`

2. **List Append**: Add items to lists
   - `TValue=IList<T>`, `TOperand=IList<T>` or `CollectionOperation<T>`

3. **Set Operations**: Union, intersection, difference
   - `TValue=ISet<T>`, `TOperand=SetOperation<T>`

4. **JSON Updates**: Merge JSON objects or arrays
   - `TValue=JsonDocument`, `TOperand=JsonPatch`

5. **Time Series**: Append time-stamped events
   - `TValue=IList<Event>`, `TOperand=Event`

### Best Practices

- Use merge operators when you need atomic updates without reading first
- Implement PartialMerge for commutative/associative operations to improve compaction efficiency
- Return Success=false in PartialMerge when operations cannot be safely combined
- The merge operator's `Name` property must remain consistent across database opens
- Test your merge operators thoroughly, especially edge cases with null/empty values
- Consider serialization overhead - simpler operands often perform better
