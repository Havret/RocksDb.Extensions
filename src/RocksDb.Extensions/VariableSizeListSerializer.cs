namespace RocksDb.Extensions;

internal class VariableSizeListSerializer<T> : VariableSizeCollectionSerializer<IList<T>, T>
{
    public VariableSizeListSerializer(ISerializer<T> scalarSerializer) : base(scalarSerializer)
    {
    }

    protected override IList<T> CreateCollection(int capacity) => new List<T>(capacity);

    protected override void AddElement(IList<T> collection, T element) => collection.Add(element);
}