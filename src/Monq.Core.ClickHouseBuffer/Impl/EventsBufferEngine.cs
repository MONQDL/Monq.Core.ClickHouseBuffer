using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    public sealed class EventsBufferEngine<T> : IEventsBufferEngine<T>, IDisposable
        where T : class
    {
#pragma warning disable IDE0052 // Удалить непрочитанные закрытые члены
        readonly Timer _flushTimer;
#pragma warning restore IDE0052 // Удалить непрочитанные закрытые члены
        readonly List<T> _events = new List<T>();
        readonly IEventsWriter _eventsWriter;
        readonly EngineOptions _engineOptions;
        readonly ILogger<EventsBufferEngine<T>> _logger;

        static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Конструктор реализации буфера хранилища событий.
        /// Создаёт новый экземпляр класса <see cref="EventsBufferEngine{T}"/>.
        /// </summary>
        public EventsBufferEngine(
            IOptions<EngineOptions> engineOptions,
            IEventsWriter eventsWriter,
            ILogger<EventsBufferEngine<T>> logger)
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
        public async Task AddEvent(T webTaskResultEvent)
        {
            await _semaphore.WaitAsync();

            try
            {
                _events.Add(webTaskResultEvent);
                if (_events.Count < _engineOptions.EventsFlushCount)
                    return;

                await Flush();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        async Task FlushTimerDelegate(object _)
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

        async Task HandleEvents(IEnumerable<T> streamDataEvents)
        {
            var dbValues = streamDataEvents.Select(val => val.CreateDbValues(true)).ToArray();

            try
            {
                await _eventsWriter.Write(dbValues);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while trying to write batch of data to Storage.");
                var extendedError = string.Join(Environment.NewLine, new[]
                {
                    "Источник:",
                    JsonConvert.SerializeObject(dbValues, Formatting.Indented)
                });
                _logger.LogDebug(extendedError);
                // Незаписанные события не отправляем никуда. Для текущей реализации это не смертельно.
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
