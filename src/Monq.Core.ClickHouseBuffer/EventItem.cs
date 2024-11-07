using Monq.Core.ClickHouseBuffer.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Monq.Core.ClickHouseBuffer
{
    /// <summary>
    /// Buffer event.
    /// </summary>
    public struct EventItem
    {
        /// <summary>
        /// The object of the event that you want to write to the database.
        /// </summary>
        public object Event { get; }

        /// <summary>
        /// Name of the table in which to write the event.
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// Flag indicating whether the event should be written to camelCase.
        /// </summary>
        public bool UseCamelCase { get; set; }

        /// <summary>
        /// The names of the columns for recording an event in CH.
        /// </summary>
        public IReadOnlyCollection<string> Columns { get; }

        /// <summary>
        /// Column values for recording an event in CH.
        /// </summary>
        public IReadOnlyCollection<object> Values { get; }

        /// <summary>
        /// Buffer event constructor.
        /// </summary>
        /// <param name="event">Event object.</param>
        /// <param name="tableName">Table name in ClickHouse.</param>
        /// <param name="useCamelCase">Flag indicating whether the event should be written to camelCase </param>
        public EventItem(object @event, string tableName, bool useCamelCase)
        {
            Event = @event;
            TableName = tableName;
            UseCamelCase = useCamelCase;

            var dbValues = @event.CreateDbValues(UseCamelCase);
            Columns = dbValues.Keys.Select(val => $"`{val}`").ToList().AsReadOnly();
            Values = dbValues.Values.ToList().AsReadOnly();
        }
    }
}
