using Microsoft.AspNetCore.Components.Web;
using Monq.Core.ClickHouseBuffer.Exceptions;
using Monq.Core.ClickHouseBuffer.Extensions;
using Monq.Core.ClickHouseBuffer.Schemas;
using System;
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
    int _count;
    Task _currentFlushTask = Task.CompletedTask;

    /// <summary>
    /// The implementation constructor of the event storage buffer.
    /// Creates a new instance of the class <see cref="EventsBufferEngine"/>.
    /// </summary>
    public EventsBufferEngine(IEventsWriter writer,
        int sizeLimit,
        TimeSpan timeLimit,
        IEventsHandler? eventsHandler)
    {
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
    public void AddSchemaEvent<TSource>([NotNull] TSource @event, string tableName)
        where TSource : class
    {
        Add(new EventItem(tableName, @event.GetType(), @event.ClickHouseValues(tableName)));
    }

    /// <inheritdoc />
    public void AddEvent<T>([NotNull] T @event, string tableName, bool useCamelCase = true)
        where T : class
    {
        Add(@event.CreateFromReflection(tableName, useCamelCase));
    }

    async Task FlushByTimerAsync()
    {
        List<EventItem> itemsToFlush;
        lock (_syncRoot)
        {
            if (_count == 0) return;
            itemsToFlush = ExtractItems();
        }

        await ProcessItemsAsync(itemsToFlush).ConfigureAwait(false);
    }

    async Task FlushAsync()
    {
        List<EventItem> itemsToFlush;
        lock (_syncRoot)
        {
            itemsToFlush = ExtractItems();
        }

        await ProcessItemsAsync(itemsToFlush).ConfigureAwait(false);
    }

    List<EventItem> ExtractItems()
    {
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
        var items = new List<EventItem>(_buffer);
        _buffer.Clear();
        _count = 0;
        return items;
    }

    async Task ProcessItemsAsync(List<EventItem> events)
    {
        var tableGroups = events.GroupBy(x => x.Key);
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
                    // Log exception
                    errorEvents.AddRange(eventsGroup);
                }
            } else if (!task.IsFaulted)
            {
                completedEvents.AddRange(eventsGroup);
            }
        }

        try
        {
            if (_eventsHandler != null)
                await _eventsHandler.OnAfterWriteEvents(completedEvents).ConfigureAwait(false);
        }
        catch
        {
            // Log OnAfterWriteEvents errors.
        }

        try
        {
            if (_eventsHandler != null)
                await _eventsHandler.OnWriteErrors(errorEvents).ConfigureAwait(false);
        }
        catch
        {
            // TODO: Logging.
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