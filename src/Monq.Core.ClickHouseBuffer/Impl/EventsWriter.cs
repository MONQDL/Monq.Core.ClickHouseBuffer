using Microsoft.Extensions.Logging;
using Monq.Core.ClickHouseBuffer.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Monq.Core.ClickHouseBuffer.Impl
{
    /// <summary>
    /// Реализация службы для записи событий в БД.
    /// </summary>
    public class EventsWriter : IEventsWriter
    {
        readonly ILogger<EventsWriter> _logger;
        readonly IPersistRepository _persistRepository;

        long _writtenCount;
        long _avgWriteTimeMs;

        /// <summary>
        /// Конструктор реализации службы для записи событий в БД.
        /// Создаёт новый экземпляр класса <see cref="EventsWriter"/>.
        /// </summary>
        public EventsWriter(
            IPersistRepository persistRepository,
            ILogger<EventsWriter> logger)
        {
            _persistRepository = persistRepository;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task Write(IDictionary<string, object>[] dbValues, string tableName)
        {
            if (dbValues.Length == 0)
                return;

            _logger.LogInformation("Start writing {rowsCount} data rows from the buffer." , dbValues.Length);

            var sw = new Stopwatch();
            sw.Start();

            var columns = dbValues[0].Keys.Select(val => $"`{val}`").ToList() as IReadOnlyCollection<string>;
            var values = dbValues.Select(val => val.Values.ToArray()).ToList();

            try
            {
                await _persistRepository.WriteBatch(columns, values, tableName);
            }
            catch(Exception e)
            {
                throw new PersistingException("Error while persisting data", dbValues, tableName, e);
            }

            sw.Stop();

            _logger.LogInformation("Buffer has written {rowsCount} rows to the database at {elapsedMilliseconds} ms.", 
                dbValues.Length, sw.ElapsedMilliseconds);

            _writtenCount += dbValues.Length;

            if (_avgWriteTimeMs == 0)
                _avgWriteTimeMs = sw.ElapsedMilliseconds;
            else
                _avgWriteTimeMs = (sw.ElapsedMilliseconds + _avgWriteTimeMs) / 2;

            _logger.LogInformation("{rowsCount} rows has been written to the database. The average writing time per row is {avgWriteTime} ms.",
                _writtenCount, _avgWriteTimeMs);
        }
    }
}
