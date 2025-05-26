using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monq.Core.ClickHouseBuffer.Impl;
using Monq.Core.ClickHouseBuffer.Schemas;

namespace Monq.Core.ClickHouseBuffer.PerformanceTests;

[ThreadingDiagnoser]
[MemoryDiagnoser]
public class ClickHouseBench
{
    readonly IEventsBufferEngine _engine;
    public ClickHouseBench()
    {
        ClickHouseSchemaConfig.GlobalSettings.Scan(typeof(ClickHouseBench).Assembly);

        var loggerFactory = new LoggerFactory();
        var services = new ServiceCollection();
        _engine = new EventsBufferEngine(
            new EmptyWriter(),
            500,
            TimeSpan.FromSeconds(2),
            null,
            null);
    }

    [Benchmark]
    public void Bench_Schema_TenHundredsEvents_500BufferedEvents()
    {
        var now = DateTimeOffset.UtcNow;
        var streamName = "Stream10";
        var table = "logs";
        foreach (var item in Enumerable.Range(1, 10000).Select(x => new MyEvent()
        {
            StreamId = 10,
            AggregatedAt = now,
            StreamName = streamName,
            UserspaceId = 1,
            Value = """
            91.98.35.212 - - [23/Jan/2019:14:45:13 +0330] "GET /static/css/font/wyekan/font.ttf HTTP/1.1" 200 27459 "https://znbl.ir/static/bundle-bundle_site_head.css" "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36" "-"
            """
        }))
        {
            _engine.AddEvent(item, table);
        }
    }

    [Benchmark]
    public void Bench_Reflection_TenHundredsEvents_500BufferedEvents()
    {
        var now = DateTimeOffset.UtcNow;
        var streamName = "Stream10";
        var table = "logb"; // different table name, so schema not found and reflection will be used.
        foreach (var item in Enumerable.Range(1, 10000).Select(x => new MyEvent()
        {
            StreamId = 10,
            AggregatedAt = now,
            StreamName = streamName,
            UserspaceId = 1,
            Value = """
            91.98.35.212 - - [23/Jan/2019:14:45:13 +0330] "GET /static/css/font/wyekan/font.ttf HTTP/1.1" 200 27459 "https://znbl.ir/static/bundle-bundle_site_head.css" "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36" "-"
            """
        }))
        {
            _engine.AddEvent(item, table);
        }
    }
}
