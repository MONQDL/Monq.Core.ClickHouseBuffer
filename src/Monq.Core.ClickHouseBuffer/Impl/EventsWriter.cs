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
    /// Implementation of the service for recording events in the database.
    /// </summary>
    public class EventsWriter : IEventsWriter
    {
        readonly ILogger<EventsWriter> _logger;
        readonly IPersistRepository _persistRepository;

        long _writtenCount;
        long _avgWriteTimeMs;

        /// <summary>
        /// Service implementation designer for recording events in the database.
        /// Creates a new instance of the class <see cref="EventsWriter"/>.
        /// </summary>
        public EventsWriter(
            IPersistRepository persistRepository,
            ILogger<EventsWriter> logger)
        {
            _persistRepository = persistRepository;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task Write(IEnumerable<EventItem> events, string tableName)
        {
            if (!events.Any())
                return;

            _logger.LogInformation("Start writing {eventCount} events from the buffer.", events.Count());

            var sw = new Stopwatch();
            sw.Start();

            try
            {
                await _persistRepository.WriteBatch(events, tableName);
            }
            catch (Exception e)
            {
                throw new PersistingException("Error while persisting data", events, tableName, e);
            }

            sw.Stop();

            _logger.LogInformation("Buffer has written {eventsCount} events to the database at {elapsedMilliseconds} ms.",
                events.Count(), sw.ElapsedMilliseconds);

            _writtenCount += events.Count();

            if (_avgWriteTimeMs == 0)
                _avgWriteTimeMs = sw.ElapsedMilliseconds;
            else
                _avgWriteTimeMs = (sw.ElapsedMilliseconds + _avgWriteTimeMs) / 2;

            _logger.LogInformation("{eventsCount} events has been written to the database. " +
                "The average writing time per event is {avgWriteTime} ms.",
                _writtenCount, _avgWriteTimeMs);
        }
    }
}
