using Monq.Core.ClickHouseBuffer.Schemas;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Monq.Core.ClickHouseBuffer;

/// <summary>
/// The service interface that represents methods to work with database storage.
/// </summary>
public interface IEventsWriter
{
    /// <summary>
    /// WriteBatch events to database.
    /// </summary>
    /// <param name="events">List of events to record in ClickHouse.</param>
    /// <param name="key">The table name and the <paramref name="events"/> type.</param>
    /// <returns><see cref="Task"/> when the operation completes.</returns>
    /// <returns></returns>
    Task WriteBatch(IEnumerable<EventItem> events, TypeTuple key);
}
