using System.Collections.Generic;
using System.Threading.Tasks;

namespace Monq.Core.ClickHouseBuffer;

/// <summary>
/// Interface of the additional message processing service, after recording them in ClickHouse.
/// </summary>
public interface IEventsHandler
{
    /// <summary>
    /// Executes after events batch was written to the <see cref="IEventsWriter"/>.
    /// </summary>
    /// <param name="events">List of written events.</param>
    /// <returns></returns>
    Task OnAfterWriteEvents(IEnumerable<EventItem> events);

    /// <summary>
    /// Executes on batch write process throws error.
    /// </summary>
    /// <param name="events">List of errored events.</param>
    /// <returns></returns>
    Task OnWriteErrors(IEnumerable<EventItem> events);
}
