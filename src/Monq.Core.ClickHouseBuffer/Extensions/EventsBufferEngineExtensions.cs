using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Monq.Core.ClickHouseBuffer.Extensions
{
    /// <summary>
    /// A class extension to work with the buffer engine.
    /// </summary>
    public static class EventsBufferEngineExtensions
    {
        /// <summary>
        /// Add the event to the buffer.
        /// </summary>
        /// <typeparam name="T">The type of the event to save to the storage.</typeparam>
        /// <param name="engine">Buffer engine.</param>
        /// <param name="event">Event object.</param>
        /// <param name="tableName">ClickHouse table name to save to.</param>
        /// <param name="useCamelCase">Flag indicating whether the event should be written to camelCase.</param>
        /// <returns></returns>
        public static Task AddEvent<T>(this IEventsBufferEngine engine, [NotNull] T @event, string tableName, bool useCamelCase = true)
            where T : class
        {
            return engine.AddEvent(@event, tableName, useCamelCase);
        }
    }
}
