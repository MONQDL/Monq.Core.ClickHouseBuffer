namespace Monq.Core.ClickHouseBuffer
{
    public struct EventItem
    {
        /// <summary>
        /// Событие, которое требуется записать в БД.
        /// </summary>
        public object Event { get; }

        /// <summary>
        /// Название таблицы, в которую требуется записать событие.
        /// </summary>
        public string TableName { get; }

        public EventItem(object @event, string tableName)
        {
            Event = @event;
            TableName = tableName;
        }
    }
}
