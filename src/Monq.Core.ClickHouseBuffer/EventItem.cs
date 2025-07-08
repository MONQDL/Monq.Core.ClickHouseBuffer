using Monq.Core.ClickHouseBuffer.Schemas;
using System;

namespace Monq.Core.ClickHouseBuffer;

/// <summary>
/// Basic buffer event. It contains only tableName and column values ready for persisting.
/// </summary>
public class EventItem
{
    /// <summary>
    /// Basic buffer event constructor.
    /// </summary>
    /// <param name="tableName">Table name in ClickHouse.</param>
    /// <param name="eventType">The type of the event, based on which the <see cref="Values"/> was created.</param>
    /// <param name="values">Column values for recording an event in CH.</param>
    public EventItem(string tableName, Type eventType, object?[] values)
    {
        Key = new TypeTuple(eventType, tableName);
        Values = values;
    }

    /// <summary>
    /// The key of the event base on table name and event type.
    /// </summary>
    public TypeTuple Key { get; }

    /// <summary>
    /// Column values for recording an event in CH.
    /// </summary>
    public object?[] Values { get; }
}

/// <summary>
/// The buffer event with the source event.
/// </summary>
public class EventItemWithSourceObject : EventItem
{
    /// <summary>
    /// The source object of the event that you want to write to the storage.
    /// </summary>
    public object Source { get; }

    /// <summary>
    /// Buffer event constructor.
    /// </summary>
    /// <param name="event">Source object.</param>
    /// <param name="tableName">Table name in ClickHouse.</param>
    /// <param name="eventType">The type of the event, based on which the <paramref name="values"/> was created.</param>
    /// <param name="values">Column values for recording an event in CH.</param>
    public EventItemWithSourceObject(object @event, string tableName, Type eventType, object?[] values)
        : base(tableName, eventType, values)
    {
        Source = @event;
    }
}
