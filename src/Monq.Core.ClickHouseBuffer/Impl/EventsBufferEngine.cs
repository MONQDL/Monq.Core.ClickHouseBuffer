using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monq.Core.ClickHouseBuffer.Exceptions;
using Monq.Core.ClickHouseBuffer.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Monq.Core.ClickHouseBuffer.Impl
{
    /// <summary>
    /// Реализация буфера хранилища событий потоковых данных.
    /// </summary>
    public sealed class EventsBufferEngine : IEventsBufferEngine, IDisposable
    {
#pragma warning disable IDE0052 // Удалить непрочитанные закрытые члены
        readonly Timer _flushTimer;
#pragma warning restore IDE0052 // Удалить непрочитанные закрытые члены
        readonly List<EventItem> _events = new List<EventItem>();
        readonly IEventsWriter _eventsWriter;
        readonly EngineOptions _engineOptions;
        readonly ILogger<EventsBufferEngine> _logger;

        static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Конструктор реализации буфера хранилища событий.
        /// Создаёт новый экземпляр класса <see cref="EventsBufferEngine"/>.
        /// </summary>
        public EventsBufferEngine(
            IOptions<EngineOptions> engineOptions,
            IEventsWriter eventsWriter,
            ILogger<EventsBufferEngine> logger)
        {
            if (engineOptions == null)
                throw new ArgumentNullException(nameof(engineOptions), $"{nameof(engineOptions)} is null.");
            if (engineOptions.Value == null)
                throw new ArgumentNullException(nameof(engineOptions.Value), $"{nameof(engineOptions.Value)} is null.");

            _engineOptions = engineOptions.Value;
            _eventsWriter = eventsWriter;
            _logger = logger;

            _flushTimer = new Timer(async obj => await FlushTimerDelegate(obj), null,
                _engineOptions.EventsFlushPeriodSec * 1000,
                _engineOptions.EventsFlushPeriodSec * 1000);
        }

        /// <inheritdoc />
        public async Task AddEvent(object webTaskResultEvent, string tableName, bool useCamelCase = true)
        {
            await _semaphore.WaitAsync();

            try
            {
                _events.Add(new EventItem(webTaskResultEvent, tableName, useCamelCase));
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

        async Task HandleEvents(IEnumerable<EventItem> streamDataEvents)
        {
            var tableGroups = streamDataEvents.GroupBy(x => x.TableName);

            var tasks = new List<Task>();

            foreach (var tableGroup in tableGroups)
            {
                var dbValues = tableGroup.Select(val => val.Event.CreateDbValues(val.UseCamelCase)).ToArray();
                tasks.Add(_eventsWriter.Write(dbValues, tableGroup.Key));
            }
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while trying to write batch of data to Storage.");

                var exceptions = tasks
                    .Where(t => t.Exception != null)
                    .Select(t => t.Exception)
                    .ToList();
                foreach (var aggregateException in exceptions)
                {
                    var persistException = aggregateException?.InnerExceptions?.First() as PersistingException;
                    if (persistException != null)
                    {
                        var extendedError = string.Join(Environment.NewLine, new[]
                        {
                            $"Table: {persistException.TableName}. Source: ", 
                                JsonConvert.SerializeObject(persistException.DbValues, Formatting.Indented)
                        });
                        _logger.LogDebug(extendedError);
                    }
                    // Незаписанные события не отправляем никуда. Для текущей реализации это не смертельно.
                }
            }
        }

        /// <summary>
        /// Освободить ресурсы.
        /// </summary>
        public void Dispose()
        {
            _flushTimer?.Dispose();
        }
    }
}
