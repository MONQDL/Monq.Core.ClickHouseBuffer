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

        /// <summary>
        /// Флаг, говорящий о том, необходимо ли записывать событие в camelCase. 
        /// </summary>
        public bool UseCamelCase { get; set; }

        public EventItem(object @event, string tableName, bool useCamelCase)
        {
            Event = @event;
            TableName = tableName;
            UseCamelCase = useCamelCase;
        }
    }
}
