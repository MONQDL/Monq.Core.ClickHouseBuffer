using ClickHouse.Client.Utility;
using Microsoft.Extensions.Logging;
using Monq.Core.ClickHouseBuffer.Extensions;
using Monq.Core.ClickHouseBuffer.Schemas;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace Monq.Core.ClickHouseBuffer.Impl;

/// <summary>
/// Implementation of the event storage buffer.
/// </summary>
public sealed class EventsBufferEngine : IEventsBufferEngine, IDisposable
{
    readonly Queue<EventItem> _buffer = new Queue<EventItem>();
    readonly object _syncRoot = new object();
    readonly Timer _timer;
    readonly int _sizeLimit;
    readonly TimeSpan _timeLimit;
    readonly IEventsWriter _writer;
    readonly IEventsHandler? _eventsHandler;
    readonly ILogger<EventsBufferEngine>? _log;
    int _count;
    Task _currentFlushTask = Task.CompletedTask;

    const string _errorWhileWritingEvents = "There was an error while writing events. Details: {ErrorMessage}";
    const string _errorOnAfterWriteEvents = "There was an error execution OnAfterWriteEvents method. Details: {ErrorMessage}";
    const string _errorOnWriteErrors = "There was an error execution OnWriteErrors method. Details: {ErrorMessage}";

    /// <summary>
    /// The implementation constructor of the event storage buffer.
    /// Creates a new instance of the class <see cref="EventsBufferEngine"/>.
    /// </summary>
    public EventsBufferEngine(IEventsWriter writer,
        int sizeLimit,
        TimeSpan timeLimit,
        IEventsHandler? eventsHandler,
        ILogger<EventsBufferEngine>? log)
    {
        _log = log;
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _eventsHandler = eventsHandler;
        _sizeLimit = sizeLimit;
        _timeLimit = timeLimit;
        _timer = new Timer(_ => _ = FlushByTimerAsync(), null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <inheritdoc />
    public void Add(EventItem item)
    {
        lock (_syncRoot)
        {
            _buffer.Enqueue(item);
            _count++;

            if (_count == 1)
                _timer.Change(_timeLimit, Timeout.InfiniteTimeSpan);

            if (_count >= _sizeLimit)
                _currentFlushTask = FlushAsync();
        }
    }

    /// <inheritdoc />
    public void AddEvent<TSource>([NotNull] TSource @event, string tableName)
        where TSource : class
    {
        if (@event.SchemaExists(tableName))
            Add(new EventItem(tableName, @event.GetType(), @event.ClickHouseValues(tableName)));
        else
            Add(@event.CreateFromReflection(tableName));
    }

    async Task FlushByTimerAsync()
    {
        EventItem[]? array = null;
        int count = 0;

        lock (_syncRoot)
        {
            if (_count == 0) 
                return;
            (array, count) = ExtractItems();
        }

        await ProcessItemsAsync(array, count).ConfigureAwait(false);
    }

    async Task FlushAsync()
    {
        EventItem[]? array = null;
        int count = 0;

        lock (_syncRoot)
        {
            (array, count) = ExtractItems();
        }

        await ProcessItemsAsync(array, count).ConfigureAwait(false);
    }

    (EventItem[] Array, int Count) ExtractItems()
    {
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
        int count = _buffer.Count;
        var array = ArrayPool<EventItem>.Shared.Rent(count);

        _buffer.CopyTo(array, 0);
        _buffer.Clear();
        _count = 0;

        return (array, count);
    }

    async Task ProcessItemsAsync(EventItem[] array, int count)
    {
        try
        {
            var batch = new ArraySegment<EventItem>(array, 0, count);
            var tableGroups = batch.GroupBy(x => x.Key);
            var tasksWithData = new List<(Task Task, IEnumerable<EventItem> Events)>();
            var tasks = new List<Task>();
            foreach (var tableGroup in tableGroups)
            {
                var task = _writer.WriteBatch(tableGroup, tableGroup.Key);
                tasks.Add(task);
                tasksWithData.Add((Task: task, Events: tableGroup.AsEnumerable()));
            }

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch
            {
                // Игнорируем одиночное исключение, так как обработаем все ниже
            }

            var exceptions = new List<Exception>();
            var errorEvents = new List<EventItem>();
            var completedEvents = new List<EventItem>();

            foreach (var tuple in tasksWithData)
            {
                var task = tuple.Task;
                var eventsGroup = tuple.Events;

                if (task.IsFaulted && task.Exception != null)
                {
                    foreach (var innerEx in task.Exception.InnerExceptions)
                    {
                        _log?.LogError(innerEx, _errorWhileWritingEvents, innerEx.Message);
                        // Log exception
                        errorEvents.AddRange(eventsGroup);
                    }
                }
                else if (!task.IsFaulted)
                {
                    completedEvents.AddRange(eventsGroup);
                }
            }

            try
            {
                if (_eventsHandler != null)
                    await _eventsHandler.OnAfterWriteEvents(completedEvents).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log?.LogError(e, _errorOnAfterWriteEvents, e.Message);
            }

            try
            {
                if (_eventsHandler != null)
                    await _eventsHandler.OnWriteErrors(errorEvents).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log?.LogError(e, _errorOnWriteErrors, e.Message);
            }
        }
        finally
        {
            ArrayPool<EventItem>.Shared.Return(array);
        }
    }

    /// <inheritdoc />
    public async Task CompleteAsync()
    {
        Task flushTask;
        lock (_syncRoot)
        {
            flushTask = _currentFlushTask;
        }

        await flushTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}