using Monq.Core.ClickHouseBuffer.Attributes;

namespace Monq.Core.ClickHouseBuffer.PerformanceTests;

public class MyEvent
{
    [ClickHouseColumn("_streamId")]
    public long StreamId { get; init; }

    [ClickHouseColumn("_streamName")]
    public string StreamName { get; init; }

    [ClickHouseColumn("_aggregatedAt")]
    public DateTimeOffset AggregatedAt { get; init; }

    [ClickHouseColumn("_userspaceId")]
    public long UserspaceId { get; init; }

    [ClickHouseColumn("_rawJson")]
    public string Value { get; init; }
}
