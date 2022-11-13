using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RocksDb.Extensions;

public static class RocksDbExtensions
{
    public static IRocksDbBuilder AddRocksDb(this IServiceCollection services, Action<RocksDbOptions> optionsAction)
    {
        services.AddOptions<RocksDbOptions>();
        services.Configure(optionsAction);
        services.TryAddSingleton<RocksDbContext>();
        return new RocksDbBuilder(services);
    }
}
