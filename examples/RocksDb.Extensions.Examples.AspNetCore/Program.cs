using RocksDb.Extensions;
using RocksDb.Extensions.Examples.AspNetCore;
using RocksDb.Extensions.Protobuf;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var rocksDbBuilder = builder.Services.AddRocksDb(options =>
{
    options.Path = "./my-rocks-db";

    // It's a good idea to clear preconfigured SerializerFactories
    // so we can have full control over the order in which serializers
    // will be resolved.
    options.SerializerFactories.Clear();

    options.SerializerFactories.Add(new PrimitiveTypesSerializerFactory());
    options.SerializerFactories.Add(new ProtobufSerializerFactory());
});
rocksDbBuilder.AddStore<string, User, UsersStore>("users-store");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
