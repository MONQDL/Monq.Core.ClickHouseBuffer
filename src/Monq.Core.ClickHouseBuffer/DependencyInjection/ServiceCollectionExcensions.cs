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
        /// <typeparam name="T">The type of the entity, that will be wtiting to the database</typeparam>
        /// <param name="services"></param>
        /// <param name="configuration">IConfiguration section with engine options, configured.</param>
        /// <param name="clickHouseConnectionString">ClickHouse connection string.</param>
        /// <returns></returns>
        public static IServiceCollection ConfigureCHBuffer<T>(
            this IServiceCollection services,
            IConfiguration configuration,
            string clickHouseConnectionString)
            where T : class
        {
            services.AddOptions();

            services.Configure<EngineOptions>(configuration);
            services.Configure<EngineOptions>(c => c.ConnectionString = clickHouseConnectionString);

            services.AddTransient<IEventsWriter, EventsWriter>();
            services.AddTransient<IPersistRepository, DefaultRepository>();
            services.AddSingleton<IEventsBufferEngine<T>, EventsBufferEngine<T>>();

            return services;
        }
    }
}
