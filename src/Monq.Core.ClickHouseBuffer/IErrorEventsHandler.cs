using System.Collections.Generic;
using System.Threading.Tasks;

namespace Monq.Core.ClickHouseBuffer
{
    /// <summary>
    /// The interface of the ClickHouse event handling service, which caused the ClickHouse recording error.
    /// </summary>
    public interface IErrorEventsHandler
    {
        /// <summary>
        /// Handle events.
        /// </summary>
        /// <param name="events">List of events.</param>
        /// <returns></returns>
        Task Handle(IEnumerable<EventItem> events);
    }
}
