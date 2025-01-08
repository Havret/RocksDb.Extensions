using RocksDbSharp;

namespace RocksDb.Extensions;

internal class ColumnFamily
{
    public ColumnFamilyHandle Handle { get; set; }
    public string Name { get; }

    public ColumnFamily(ColumnFamilyHandle handle, string name)
    {
        Handle = handle;
        Name = name;
    }
}