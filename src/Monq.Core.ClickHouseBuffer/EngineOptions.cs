namespace Monq.Core.ClickHouseBuffer
{
    /// <summary>
    /// ChickHouse buffer options.
    /// </summary>
    public sealed class EngineOptions
    {
        /// <summary>
        /// Период сброса (записи) событий в БД (секунды).
        /// Default: 2 sec.
        /// </summary>
        public int EventsFlushPeriodSec { get; set; } = 2;

        /// <summary>
        /// Количество событий, при достижении которого сбрасывать (записывать) в БД.
        /// Default: 500.
        /// </summary>
        public int EventsFlushCount { get; set; } = 500;

        /// <summary>
        /// Строка подключения в ClickHouse.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Название таблицы ClickHouse для записи.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Maximum number of threads that will be used to write data to the database.
        /// Default: 1.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = 1;
    }
}
