namespace RocksDb.Extensions;

internal class VariableSizeSetSerializer<T> : VariableSizeCollectionSerializer<ISet<T>, T>
{
    public VariableSizeSetSerializer(ISerializer<T> scalarSerializer) : base(scalarSerializer)
    {
    }

    protected override ISet<T> CreateCollection(int capacity) => new HashSet<T>(capacity);

    protected override void AddElement(ISet<T> collection, T element) => collection.Add(element);
}
