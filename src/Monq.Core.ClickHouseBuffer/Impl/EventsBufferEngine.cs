using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monq.Core.ClickHouseBuffer.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Monq.Core.ClickHouseBuffer.Impl
{
    /// <summary>
    /// Implementation of the event storage buffer.
    /// </summary>
    public sealed class EventsBufferEngine : IEventsBufferEngine, IDisposable
    {
#pragma warning disable IDE0052 // Delete unread closed members
        readonly Timer _flushTimer;
#pragma warning restore IDE0052 // Delete unread closed members
        readonly List<EventItem> _events = new List<EventItem>();
        readonly IEventsWriter _eventsWriter;
        readonly IPostHandler _postHandler;
        readonly IErrorEventsHandler _errorEventsHandler;
        readonly EngineOptions _engineOptions;
        readonly ILogger<EventsBufferEngine> _logger;

        static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// The implementation constructor of the event storage buffer.
        /// Creates a new instance of the class <see cref="EventsBufferEngine"/>.
        /// </summary>
        public EventsBufferEngine(
            IOptions<EngineOptions> engineOptions,
            IEventsWriter eventsWriter,
            ILogger<EventsBufferEngine> logger,
            IServiceProvider serviceProvider
            )
        {
            if (engineOptions == null)
                throw new ArgumentNullException(nameof(engineOptions), $"{nameof(engineOptions)} is null.");
            if (engineOptions.Value == null)
                throw new ArgumentNullException(nameof(engineOptions.Value), $"{nameof(engineOptions.Value)} is null.");

            _postHandler = (IPostHandler)serviceProvider.GetService(typeof(IPostHandler));
            _errorEventsHandler = (IErrorEventsHandler)serviceProvider.GetService(typeof(IErrorEventsHandler));
            _engineOptions = engineOptions.Value;
            _eventsWriter = eventsWriter;
            _logger = logger;

            _flushTimer = new Timer(async obj => await FlushTimerDelegate(obj), null,
                _engineOptions.EventsFlushPeriodSec * 1000,
                _engineOptions.EventsFlushPeriodSec * 1000);
        }

        /// <inheritdoc />
        public async Task AddEvent(object @event, string tableName, bool useCamelCase = true)
        {
            await _semaphore.WaitAsync();

            try
            {
                _events.Add(new EventItem(@event, tableName, useCamelCase));
                if (_events.Count < _engineOptions.EventsFlushCount)
                    return;

                await Flush();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        async Task FlushTimerDelegate(object? _)
        {
            await _semaphore.WaitAsync();
            try
            {
                await Flush();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        Task Flush()
        {
            if (_events.Count == 0)
                return Task.CompletedTask;

            var eventsCache = _events.ToArray();
            _events.Clear();

            return HandleEvents(eventsCache);
        }

        async Task HandleEvents(IEnumerable<EventItem> events)
        {
            var tableGroups = events.GroupBy(x => x.TableName);
            var tasks = new List<Task>();

            foreach (var tableGroup in tableGroups)
                tasks.Add(_eventsWriter.Write(tableGroup, tableGroup.Key));

            try
            {
                await Task.WhenAll(tasks);

                if (_postHandler != null)
                    await _postHandler.Handle(events);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while trying to write batch of data to Storage.");

                var exceptions = tasks
                    .Where(t => t.Exception != null)
                    .Select(t => t.Exception)
                    .ToList();

                var errorEvents = new List<EventItem>();
                foreach (var aggregateException in exceptions)
                {
                    var persistException = aggregateException?.InnerExceptions?.First() as PersistingException;
                    if (persistException != null)
                    {
                        var json = JsonConvert.SerializeObject(persistException.Events, Formatting.Indented);
                        var extendedError = string.Join(Environment.NewLine, new[]
                        {
                            $"Table: {persistException.TableName}. Source: ", json
                        });
                        _logger.LogDebug(extendedError);

                        errorEvents.AddRange(persistException.Events);
                    }
                }

                if (_errorEventsHandler != null)
                    await _errorEventsHandler.Handle(errorEvents);
            }
        }

        /// <summary>
        /// Free up resources.
        /// </summary>
        public void Dispose()
        {
            _flushTimer?.Dispose();
        }
    }
}
