using System.Collections.Generic;
using System.Threading.Tasks;

namespace Monq.Core.ClickHouseBuffer
{
    /// <summary>
    /// Interface of the additional message processing service, after recording them in ClickHouse.
    /// </summary>
    public interface IPostHandler
    {
        /// <summary>
        /// Handle list of events.
        /// </summary>
        /// <param name="events">List of events.</param>
        /// <returns></returns>
        Task Handle(IEnumerable<EventItem> events);
    }
}
