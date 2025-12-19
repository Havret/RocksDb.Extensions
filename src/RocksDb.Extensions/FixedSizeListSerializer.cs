namespace RocksDb.Extensions;

internal class FixedSizeListSerializer<T> : FixedSizeCollectionSerializer<IList<T>, T>
{
    public FixedSizeListSerializer(ISerializer<T> scalarSerializer) : base(scalarSerializer)
    {
    }

    protected override IList<T> CreateCollection(int capacity) => new List<T>(capacity);

    protected override void AddElement(IList<T> collection, T element) => collection.Add(element);
}