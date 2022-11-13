namespace RocksDb.Extensions.Examples.AspNetCore;

public class UsersStore : RocksDbStore<string, User>
{
    public UsersStore(IRocksDbAccessor<string, User> rocksDbAccessor) : base(rocksDbAccessor)
    {
    }
}
