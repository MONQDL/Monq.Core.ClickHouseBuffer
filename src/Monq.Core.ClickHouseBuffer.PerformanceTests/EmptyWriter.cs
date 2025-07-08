using Monq.Core.ClickHouseBuffer.Schemas;

namespace Monq.Core.ClickHouseBuffer.PerformanceTests;

public class EmptyWriter : IEventsWriter
{
    public Task WriteBatch(IEnumerable<EventItem> events, TypeTuple key)
    {
        var columns = ClickHouseSchemaConfig.GlobalSettings.GetMappedColumns(key);
        return Task.CompletedTask;
    }
}
