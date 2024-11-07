namespace Monq.Core.ClickHouseBuffer
{
    /// <summary>
    /// ChickHouse buffer options.
    /// </summary>
    public sealed class EngineOptions
    {
        /// <summary>
        /// Event reset (recording) period in the database (seconds)ю
        /// Default: 2 sec.
        /// </summary>
        public int EventsFlushPeriodSec { get; set; } = 2;

        /// <summary>
        /// The number of events, when reached, to reset (write) to the database.
        /// Default: 500.
        /// </summary>
        public int EventsFlushCount { get; set; } = 500;

        /// <summary>
        /// Connection string in ClickHouse.
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Maximum number of threads that will be used to write data to the database.
        /// Default: 1.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = 1;
    }
}
