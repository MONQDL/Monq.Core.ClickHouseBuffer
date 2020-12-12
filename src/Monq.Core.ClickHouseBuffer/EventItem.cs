namespace Monq.Core.ClickHouseBuffer
{
    public struct EventItem<T>
    {
        /// <summary>
        /// Событие, которое требуется записать в БД.
        /// </summary>
        public T Event { get; }

        /// <summary>
        /// Название таблицы, в которую требуется записать событие.
        /// </summary>
        public string TableName { get; }

        public EventItem(T @event, string tableName)
        {
            Event = @event;
            TableName = tableName;
        }
    }
}
