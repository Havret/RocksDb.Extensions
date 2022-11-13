namespace RocksDb.Extensions;

public interface ISerializerFactory
{
    bool CanCreateSerializer<T>();
    ISerializer<T> CreateSerializer<T>();
}
