using System.Collections.Generic;
using System.Threading.Tasks;

namespace Monq.Core.ClickHouseBuffer
{
    /// <summary>
    /// The service interface that represents methods to work with database storage.
    /// </summary>
    public interface IEventsWriter
    {
        /// <summary>
        /// Write events to database.
        /// </summary>
        /// <param name="events">List of events to record in ClickHouse.</param>
        /// <param name="tableName">The table.</param>
        /// <returns><see cref="Task"/> when the operation completes.</returns>
        Task Write(IEnumerable<EventItem> events, string tableName);
    }
}
