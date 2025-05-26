using ClickHouse.Client;
using ClickHouse.Client.ADO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monq.Core.ClickHouseBuffer.Impl;
using System;

namespace Monq.Core.ClickHouseBuffer.DependencyInjection;

/// <summary>
/// The extensions for IServiceCollection to configure ClickHouseBuffer.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configuring the ClickHouse buffer engine using options from <paramref name="configuration"/>.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">IConfiguration section with engine options, configured.</param>
    /// <returns></returns>
    public static IServiceCollection ConfigureCHBuffer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cfg = new EngineOptions();

        configuration.Bind(cfg);
        services.ConfigureCHBufferCore(cfg);

        return services;
    }

    /// <summary>
    /// Configuring the ClickHouse buffer engine using action <paramref name="options"/>.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="options">The action that can be used to configure ClickHouseBuffer.</param>
    /// <returns></returns>
    public static IServiceCollection ConfigureCHBuffer(
        this IServiceCollection services,
        Action<EngineOptions> options)
    {
        var cfg = new EngineOptions();

        options(cfg);
        services.ConfigureCHBufferCore(cfg);

        return services;
    }

    /// <summary>
    /// Configuring the ClickHouse buffer engine using options from <paramref name="configuration"/> and then apply <paramref name="options"/> action.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">IConfiguration section with engine options, configured.</param>
    /// <param name="options">The action that can be used to configure ClickHouseBuffer.</param>
    /// <returns></returns>
    public static IServiceCollection ConfigureCHBuffer(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<EngineOptions> options)
    {
        var cfg = new EngineOptions();

        configuration.Bind(cfg);
        options(cfg);
        services.ConfigureCHBufferCore(cfg);

        return services;
    }

    /// <summary>
    /// Configuring the ClickHouse buffer engine just using connection string.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="connectionString">ClickHouse connection string.</param>
    /// <returns></returns>
    public static IServiceCollection ConfigureCHBuffer(
        this IServiceCollection services,
        string connectionString)
    {
        var cfg = new EngineOptions()
        {
            ConnectionString = connectionString
        };

        services.ConfigureCHBufferCore(cfg);

        return services;
    }

    static IServiceCollection ConfigureCHBufferCore(
        this IServiceCollection services,
        EngineOptions configuration)
    {
        services.AddOptions();

        services.TryAddSingleton(configuration);

        if (!string.IsNullOrEmpty(configuration.ConnectionString))
        {
#if NET8_0_OR_GREATER
            services.AddClickHouseDataSource(configuration.ConnectionString);
#else
        services.TryAddTransient<IClickHouseConnection>((s) => new ClickHouseConnection(configuration.ConnectionString));
#endif
        }
        services.TryAddTransient<IEventsWriter, DefaultClickHouseEventsWriter>();
        // Must be singleton.
        services.AddSingleton<IEventsBufferEngine, EventsBufferEngine>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<EngineOptions>>();

            return new EventsBufferEngine(sp.GetRequiredService<IEventsWriter>(),
                options?.Value?.EventsFlushCount ?? 10000,
                TimeSpan.FromSeconds(options?.Value?.EventsFlushPeriodSec ?? 2),
                sp.GetService<IEventsHandler>(),
                sp.GetService<ILogger<EventsBufferEngine>>());
        });

        return services;
    }
}
