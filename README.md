# RocksDB.Extensions

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