using Monq.Core.ClickHouseBuffer.Attributes;
using Monq.Core.ClickHouseBuffer.Schemas;
using System.Diagnostics.CodeAnalysis;

namespace Monq.Core.ClickHouseBuffer.Extensions;

/// <summary>
/// The extensions collection for the <see cref="IEventsBufferEngine"/>.
/// </summary>
public static class IEventsBufferEngineExtensions
{
    /// <summary>
    /// Add the event to the buffer. The values and column names will be calculated based on ITableSchema schema if present
    /// or based on attribute <see cref="ClickHouseColumnAttribute"/>.
    /// The method will create an object of type <see cref="EventItemWithSourceObject"/>
    /// instead of <see cref="EventItem"/>. The type <see cref="EventItemWithSourceObject"/> inherits from <see cref="EventItem"/>
    /// and contains additional field <see cref="EventItemWithSourceObject.Source"/>.
    /// NOTE: this method adds extra memory allocations. If you use this method, 
    /// then you must add you own implementation of the <see cref="IEventsWriter"/> and
    /// try to cast elements to <see cref="EventItemWithSourceObject"/> in the method implementation <see cref="IEventsWriter.WriteBatch(System.Collections.Generic.IEnumerable{EventItem}, Schemas.TypeTuple)"/>
    /// </summary>
    /// <typeparam name="TSource">The type of the event to save to the storage.</typeparam>
    /// <param name="buffer">The <see cref="IEventsBufferEngine"/> object.</param>
    /// <param name="event">Source object to save to ClickHouse.</param>
    /// <param name="tableName">ClickHouse table name to save to.</param>
    [RequiresUnreferencedCode("Uses reflection to extract property and field values")]
    public static void AddEventWithSourceObject<TSource>(this IEventsBufferEngine buffer, [NotNull] TSource @event, string tableName)
        where TSource : class
    {
        if (@event.SchemaExists(tableName))
            buffer.Add(new EventItemWithSourceObject(@event, tableName, @event.GetType(), @event.ClickHouseValues(tableName)));
        else
            buffer.Add(@event.CreateFromReflectionWithSource(tableName));
    }
}
