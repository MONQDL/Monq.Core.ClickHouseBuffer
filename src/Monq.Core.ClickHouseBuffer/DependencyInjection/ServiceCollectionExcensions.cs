using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monq.Core.ClickHouseBuffer.Impl;

namespace Monq.Core.ClickHouseBuffer.DependencyInjection
{
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

            services.AddTransient<IEventsWriter, EventsWriter>();
            services.AddTransient<IPersistRepository, DefaultRepository>();
            services.AddSingleton<IEventsBufferEngine, EventsBufferEngine>();

            return services;
        }
    }
}
