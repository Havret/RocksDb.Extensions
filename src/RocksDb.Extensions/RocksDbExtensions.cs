using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RocksDb.Extensions;

/// <summary>
/// Extension methods for setting up RocksDB related services in an <see cref="IServiceCollection" />.
/// </summary>
public static class RocksDbExtensions
{
    /// <summary>
    /// This method adds the necessary services to the dependency injection container required for RocksDB integration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="optionsAction">A delegate that is used to configure the options for the RocksDB instance.</param>
    /// <returns>An instance of <see cref="IRocksDbBuilder"/> that can be used to build and configure a RocksDB instance.</returns>
    public static IRocksDbBuilder AddRocksDb(this IServiceCollection services, Action<RocksDbOptions> optionsAction)
    {
        services.AddOptions<RocksDbOptions>();
        services.Configure(optionsAction);
        services.TryAddSingleton<RocksDbContext>();
        return new RocksDbBuilder(services);
    }
}