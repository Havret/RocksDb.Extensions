using System.ComponentModel;

namespace RocksDb.Extensions;

#pragma warning disable CS1591

/// <summary>
/// This interface is not intended to be used directly by the clients of the library.
/// It provides merge operation support with a separate operand type.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMergeAccessor<in TKey, in TOperand>
{
    void Merge(TKey key, TOperand operand);
}

#pragma warning restore CS1591
