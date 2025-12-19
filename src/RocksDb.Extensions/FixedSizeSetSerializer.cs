namespace RocksDb.Extensions;

internal class FixedSizeSetSerializer<T> : FixedSizeCollectionSerializer<ISet<T>, T>
{
    public FixedSizeSetSerializer(ISerializer<T> scalarSerializer) : base(scalarSerializer)
    {
    }

    protected override ISet<T> CreateCollection(int capacity) => new HashSet<T>(capacity);

    protected override void AddElement(ISet<T> collection, T element) => collection.Add(element);
}
