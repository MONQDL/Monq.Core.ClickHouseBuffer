using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Monq.Core.ClickHouseBuffer;

/// <summary>
/// Event storage buffer interface.
/// </summary>
public interface IEventsBufferEngine
{
    /// <summary>
    /// Await the completion of the writing events.
    /// </summary>
    /// <returns></returns>
    Task CompleteAsync();

    /// <summary>
    /// Add the event to the buffer. You must extract values to save. The schema will try to extract column names first by schema and second by reflection.
    /// </summary>
    /// <param name="item">The item with user provided values array.</param>
    void Add(EventItem item);

    /// <summary>
    /// Add the event to the buffer. The values and column names will be calculated based on ITableSchema schema.
    /// If no schema configured then the Exception will be thrown.
    /// </summary>
    /// <typeparam name="T">The type of the event to save to the storage.</typeparam>
    /// <param name="event">Event object.</param>
    /// <param name="tableName">ClickHouse table name to save to.</param>
    void AddSchemaEvent<T>([NotNull] T @event, string tableName)
        where T : class;

    /// <summary>
    /// Add the event to the buffer. The values and column names will be calculated based on reflection.
    /// </summary>
    /// <typeparam name="T">The type of the event to save to the storage.</typeparam>
    /// <param name="event">Event object.</param>
    /// <param name="tableName">ClickHouse table name to save to.</param>
    /// <param name="useCamelCase">Flag indicating whether the event should be written to camelCase.</param>
    /// <returns></returns>
    void AddEvent<T>([NotNull] T @event, string tableName, bool useCamelCase = true)
        where T : class;
}
