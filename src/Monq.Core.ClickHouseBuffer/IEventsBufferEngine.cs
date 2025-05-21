using Monq.Core.ClickHouseBuffer.Attributes;
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
    /// Add the event to the buffer. You must extract the property values yourself in the order defined by the schema or attribute <see cref="ClickHouseColumnAttribute"/>.
    /// </summary>
    /// <param name="item">The item with user-provided values array.</param>
    void Add(EventItem item);

    /// <summary>
    /// Add the event to the buffer. The values and column names will be calculated based on ITableSchema schema if present
    /// or based on attribute <see cref="ClickHouseColumnAttribute"/>.
    /// </summary>
    /// <typeparam name="T">The type of the event to save to the storage.</typeparam>
    /// <param name="event">Event object to save to ClickHouse.</param>
    /// <param name="tableName">ClickHouse table name to save to.</param>
    void AddEvent<T>([NotNull] T @event, string tableName)
        where T : class;
}
