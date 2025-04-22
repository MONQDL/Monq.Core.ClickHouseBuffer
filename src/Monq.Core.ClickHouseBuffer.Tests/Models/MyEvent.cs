using System;

namespace Monq.Core.ClickHouseBuffer.Tests.Models;

public class MyEvent
{
    public long StreamId { get; init; }

    public string StreamName { get; init; }

    public DateTimeOffset AggregatedAt { get; init; }

    public long UserspaceId { get; init; }

    public string Value { get; init; }
}
