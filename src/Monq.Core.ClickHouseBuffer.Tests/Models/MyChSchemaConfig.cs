using Monq.Core.ClickHouseBuffer.Schemas;

namespace Monq.Core.ClickHouseBuffer.Tests.Models;

public class MyFullSchemaConfig : ITableSchema
{
    public void Register(ClickHouseSchemaConfig config)
    {
        config.NewConfig<MyEvent>("logs")
            .Map("_streamId", x => x.StreamId)
            .Map("_streamName", x => x.StreamName)
            .Map("_aggregatedAt", x => x.AggregatedAt)
            .Map("_userspaceId", x => x.UserspaceId)
            .Map("_rawJson", x => x.Value)
            .Map("_enum", x => x.EnumValue);
    }
}

public class MyShortSchemaConfig : ITableSchema
{
    public void Register(ClickHouseSchemaConfig config)
    {
        config.NewConfig<MyEvent>("logs_short")
            .Map("_streamId", x => x.StreamId)
            .Map("_streamName", x => x.StreamName);
    }
}
