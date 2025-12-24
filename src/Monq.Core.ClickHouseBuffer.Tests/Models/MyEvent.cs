namespace Monq.Core.ClickHouseBuffer.Tests.Models;

class MyEvent
{
    public long StreamId { get; init; }

    public string StreamName { get; init; }

    public DateTimeOffset AggregatedAt { get; init; }

    public long UserspaceId { get; init; }

    public string Value { get; init; }

    public TestEnum? EnumValue { get; set; } = TestEnum.Value;
}
