using ClickHouse.Driver;
using ClickHouse.Driver.ADO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monq.Core.ClickHouseBuffer.Impl;
using System;
using System.Diagnostics.CodeAnalysis;

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
    [RequiresUnreferencedCode("Configuration binding requires unreferenced code")]
    public static IServiceCollection ConfigureCHBuffer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.ConfigureCHBufferCore(options => configuration.Bind(options));
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
        return services.ConfigureCHBufferCore(options);
    }

    /// <summary>
    /// Configuring the ClickHouse buffer engine using options from <paramref name="configuration"/> and then apply <paramref name="options"/> action.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">IConfiguration section with engine options, configured.</param>
    /// <param name="options">The action that can be used to configure ClickHouseBuffer.</param>
    /// <returns></returns>
    [RequiresUnreferencedCode("Configuration binding requires unreferenced code")]
    public static IServiceCollection ConfigureCHBuffer(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<EngineOptions> options)
    {
        return services.ConfigureCHBufferCore(engineOptions =>
        {
            configuration.Bind(engineOptions);
            options(engineOptions);
        });
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
        return services.ConfigureCHBufferCore(options => options.ConnectionString = connectionString);
    }

    static IServiceCollection ConfigureCHBufferCore(
        this IServiceCollection services,
        Action<EngineOptions> configureOptions)
    {
        var configuration = new EngineOptions();
        configureOptions(configuration);

        services.AddOptions<EngineOptions>()
            .Configure(configureOptions);
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<EngineOptions>>().Value);

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
            var options = sp.GetRequiredService<IOptions<EngineOptions>>().Value;

            return new EventsBufferEngine(sp.GetRequiredService<IEventsWriter>(),
                options.EventsFlushCount,
                TimeSpan.FromSeconds(options.EventsFlushPeriodSec),
                sp.GetService<IEventsHandler>(),
                sp.GetService<ILogger<EventsBufferEngine>>());
        });

        return services;
    }
}
