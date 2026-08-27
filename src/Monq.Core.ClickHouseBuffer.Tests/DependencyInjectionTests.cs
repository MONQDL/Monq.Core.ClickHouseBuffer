using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monq.Core.ClickHouseBuffer.DependencyInjection;
using Monq.Core.ClickHouseBuffer.Schemas;
using Xunit;

namespace Monq.Core.ClickHouseBuffer.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void ConfigureCHBuffer_WithAction_RegistersConfiguredOptions()
    {
        var services = new ServiceCollection();

        services.ConfigureCHBuffer(options =>
        {
            options.EventsFlushCount = 42;
            options.EventsFlushPeriodSec = 7;
        });

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<EngineOptions>>().Value;
        var directOptions = serviceProvider.GetRequiredService<EngineOptions>();

        Assert.Equal(42, options.EventsFlushCount);
        Assert.Equal(7, options.EventsFlushPeriodSec);
        Assert.Same(options, directOptions);
    }

    [Fact]
    public void ConfigureCHBuffer_WithConfigurationAndAction_AppliesBothInOrder()
    {
        var values = new Dictionary<string, string?>
        {
            [nameof(EngineOptions.EventsFlushCount)] = "42",
            [nameof(EngineOptions.EventsFlushPeriodSec)] = "7"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var services = new ServiceCollection();

        services.ConfigureCHBuffer(configuration, options => options.EventsFlushCount = 84);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<EngineOptions>>().Value;

        Assert.Equal(84, options.EventsFlushCount);
        Assert.Equal(7, options.EventsFlushPeriodSec);
    }

    [Fact]
    public async Task ConfigureCHBuffer_AppliesOptionsToBufferEngine()
    {
        var services = new ServiceCollection();
        var writer = new CountingEventsWriter();
        services.ConfigureCHBuffer(options => options.EventsFlushCount = 1);
        services.AddSingleton<IEventsWriter>(writer);

        await using var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<IEventsBufferEngine>();

        engine.Add(new EventItem("events", typeof(object), []));
        await engine.CompleteAsync();

        Assert.Equal(1, writer.WriteCount);
    }

    sealed class CountingEventsWriter : IEventsWriter
    {
        public int WriteCount { get; private set; }

        public Task WriteBatch(IEnumerable<EventItem> events, TypeTuple key)
        {
            WriteCount++;
            return Task.CompletedTask;
        }
    }
}
