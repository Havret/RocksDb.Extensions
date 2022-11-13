namespace RocksDb.Extensions;

public class RocksDbOptions
{
    public string Path { get; set; }
    internal List<string> ColumnFamilies { get; } = new();
    public List<ISerializerFactory> SerializerFactories { get; } = new();
}
