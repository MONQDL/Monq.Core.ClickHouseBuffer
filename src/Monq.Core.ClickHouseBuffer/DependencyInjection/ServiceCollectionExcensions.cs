using ClickHouse.Client;
using ClickHouse.Client.ADO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Monq.Core.ClickHouseBuffer.Impl;

namespace Monq.Core.ClickHouseBuffer.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configuring the ClickHouse buffer engine.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">IConfiguration section with engine options, configured.</param>
    /// <param name="clickHouseConnectionString">ClickHouse connection string.</param>
    /// <returns></returns>
    public static IServiceCollection ConfigureCHBuffer(
        this IServiceCollection services,
        IConfiguration configuration,
        string clickHouseConnectionString)
    {
        services.AddOptions();

        services.Configure<EngineOptions>(configuration);
        services.Configure<EngineOptions>(c => c.ConnectionString = clickHouseConnectionString);
#if NET8_0_OR_GREATER
        services.AddClickHouseDataSource(clickHouseConnectionString);
#else
        services.TryAddTransient<IClickHouseConnection>((s) => new ClickHouseConnection(clickHouseConnectionString));
#endif
        services.AddTransient<IEventsWriter, DefaultClickHouseEventsWriter>();
        // Must be singleton.
        services.AddSingleton<IEventsBufferEngine, EventsBufferEngine>();

        return services;
    }
}
