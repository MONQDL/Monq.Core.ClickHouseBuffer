namespace Monq.Core.ClickHouseBuffer;

/// <summary>
/// ChickHouse buffer options.
/// </summary>
public sealed class EngineOptions
{
    /// <summary>
    /// Connection string in ClickHouse.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Source reset (recording) period in the database (seconds).
    /// Default: 2 sec.
    /// </summary>
    public int EventsFlushPeriodSec { get; set; } = 2;

    /// <summary>
    /// The number of events, when reached, to reset (write) to the database.
    /// Default: 10000.
    /// </summary>
    public int EventsFlushCount { get; set; } = 10000;

    /// <summary>
    /// The size of objects batch to be saved to ClickHouse used by ClickHouse Batch inserter.
    /// </summary>
    public int DatabaseBatchSize { get; set; } = 100000;

    /// <summary>
    /// The maximum number of parallel processing tasks used by ClickHouse Batch inserter.
    /// </summary>
    public int DatabaseMaxDegreeOfParallelism { get; set; } = 1;
}
