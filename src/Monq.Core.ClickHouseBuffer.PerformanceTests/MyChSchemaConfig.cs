using Monq.Core.ClickHouseBuffer.Schemas;

namespace Monq.Core.ClickHouseBuffer.PerformanceTests;

public class MyFullSchemaConfig : ITableSchema
{
    public void Register(ClickHouseSchemaConfig config)
    {
        config.NewConfig<MyEvent>("logs")
            .Map("_streamId", x => x.StreamId)
            .Map("_streamName", x => x.StreamName)
            .Map("_aggregatedAt", x => x.AggregatedAt)
            .Map("_userspaceId", x => x.UserspaceId)
            .Map("_rawJson", x => x.Value);
    }
}

