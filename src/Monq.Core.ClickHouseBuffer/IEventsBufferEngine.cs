using System.Threading.Tasks;

namespace Monq.Core.ClickHouseBuffer
{
    /// <summary>
    /// Event storage buffer interface.
    /// </summary>
    public interface IEventsBufferEngine
    {
        /// <summary>
        /// Add an event to record in ClickHouse.
        /// </summary>
        /// <param name="event">The object to be written to ClickHouse.</param>
        /// <param name="tableName">Name of the table to write the event.</param>
        /// <param name="useCamelCase">Flag indicating whether the event should be written to camelCase.</param>
        /// <returns><see cref="Task"/>, showing completion of the operation.</returns>
        Task AddEvent(object @event, string tableName, bool useCamelCase = true);
    }
}
