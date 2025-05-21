# Monq.Core.ClickHouseBuffer

*English*

The Clickhouse buffer library can collect and write rows to tables with batches (time based or count based).

As you know, ClickHouse inserts the data being written in a batch manner, and if you perform inserts one at a time,
then ClickHouse will start to eat up CPU time and consume IO of the disk subsystem at a very high rate.
In order for ClickHouse to work correctly and quickly, you need to insert data in batches, or reset the accumulated data
once at a certain time. The library implements such a mechanism.

The current version has sevearal limitations:

1. There is no asynchronous post-processing of events after saving them to the database or upon saving error.
2. There is no nice handling of write errors.

## Installation the library

```powershell
Install-Package Monq.Core.ClickHouseBuffer
```

## Using the library

In Program.cs for console applications or in Startup.cs for asp.net, you need to add a buffer configuration method.

```csharp
services.ConfigureCHBuffer(Configuration.GetSection(BufferEngineOptions), clickHouseConnectionString);
```

`clickHouseConnectionString` - the databbase connection string that looks like

```
Host=clickhouse<-http-host>;Port=80;Username=<user>;Password=<password>;Database=<database>;
```

### Configuration from the appsettings.json

The library cat use configuration with schema

```json
{
	"EventsFlushPeriodSec": 2,
	"EventsFlushCount": 500,
	"MaxDegreeOfParallelism": 1
}
```

### An example implementation of the Monq.Core.ClickHouseBuffer with RabbitMQCoreClient and BMonq.Core.BasicDotNetMicroservice libraries.

`Program.cs`

```csharp
.ConfigureServices((hostContext, services) =>
{
    var configuration = hostContext.Configuration;

    var clickHouseConnectionString = hostContext.Configuration["ClickHouseConnectionString"];

    services.ConfigureCHBuffer(
        hostContext.Configuration.GetSection("BufferEngineOptions"), clickHouseConnectionString);

    services
        .AddRabbitMQCoreClientConsumer(configuration.GetSection("RabbitMq"))
        .AddHandler<PersistFooHandler>(
            new[] { "fooEntity.persist" },
            new ConsumerHandlerOptions
            {
                RetryKey = "fooEntity.persist.buffer-retry"
            });
})
```

`PersistFooHandler.cs`

```csharp
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQCoreClient;
using RabbitMQCoreClient.Models;
using System.Threading.Tasks;

namespace FooProcessor.Buffer.QueueHandlers
{
    public class PersistFooHandler : MessageHandlerJson<FooEntity>
    {
        readonly IEventsBufferEngine<FooEntity> _eventsBufferEngine;
        readonly ILogger<PersistFooHandler> _logger;

        public PersistFooHandler(
            IEventsBufferEngine<FooEntity> eventsBufferEngine,
            ILogger<PersistFooHandler> logger)
        {
            _eventsBufferEngine = eventsBufferEngine;
            _logger = logger;
        }

        protected override ValueTask OnParseError(string json, JsonException e, RabbitMessageEventArgs args)
        {
            var message = $"Error while deserializing json to type {nameof(FooEntity)}.";
            _logger.LogCritical(message);
            ErrorMessageRouter.MoveToDeadLetter();
            return base.OnParseError(json, e, args);
        }

        protected override Task HandleMessage(FooEntity message, RabbitMessageEventArgs args)
        {
            if (message is null)
                return Task.CompletedTask;

            return _eventsBufferEngine.AddEvent(message, "clickHouseTable");
        }
    }
}
```

### Extended configuration

You can define your own repository implementation for processing events to ClickHouse storage.

#### Interfaces

`IPostHandler` - implement this interface and add it to DI if you want to postprocess messages saved to ClickHouse storage.
Has no default implementation.

Example:

```csharp
public class PostEventHandler : IPostHandler
{
    readonly IQueueService _queueService;

    public PostEventHandler(IQueueService queueService)
    {
        _queueService = queueService;
    }

    public async Task Handle(IEnumerable<EventItem> events)
    {
        // implementation
    }
}
```

Dependency injection:

```csharp
services.AddTransient<IPostHandler, PostEventHandler>();
```

`IErrorEventsHandler` - implement this interface and add it to DI if you want to process messages 
that had errors while saving to ClickHouse storage. Has no default implementation.

Example:

```csharp
public class ErrorEventsHandler : IErrorEventsHandler
{
    readonly IQueueService _queueService;
    readonly IOptions<AppOptions> _appOptions;

    public ErrorEventsHandler(IQueueService queueService, IOptions<AppOptions> appOptions)
    {
        _queueService = queueService;
        _appOptions = appOptions;
    }

    public async Task Handle(IEnumerable<EventItem> events)
    {
        // implementation
    }
}
```

Dependency injection:

```csharp
services.AddTransient<IErrorEventsHandler, ErrorEventsHandler>();
```

`IPersistRepository` - implement this interface and add it to DI if you want to use custom save to storage logics while saving to ClickHouse storage. 
There is default implementation of the repository. If you add to DI your own implementation you will override the default one.
For convenience, you can use the abstract class `BaseRepository`,
which contains a method for getting a new ClickHouse context and a set of options read from the configuration.

Example:

```csharp
public class ClickHousePersistRepository : BaseRepository, IPersistRepository
{
    /// <inheritdoc />
    public ClickHousePersistRepository(IOptions<EngineOptions> engineOptions) : base(engineOptions)
    {
    }

    /// <inheritdoc />
    public async Task WriteBatch(IEnumerable<EventItem> events, string tableName)
    {
        await using var connection = GetConnection();

        var eventItems = GetDenormalizeEvents(events, tableName);

        var columns = eventItems.Select(x => x.Columns).FirstOrDefault();
        var values = eventItems.Select(x => x.Values.ToArray());

        using var command = new ClickHouseBulkCopy(connection)
        {
            MaxDegreeOfParallelism = Options.MaxDegreeOfParallelism,
            BatchSize = Options.EventsFlushCount,
            DestinationTableName = connection.Database + "." + tableName
        };
        await command.WriteToServerAsync(values, columns);
    }

    IEnumerable<EventItem> GetDenormalizeEvents(IEnumerable<EventItem> events, string tableName)
    {
        var result = new List<EventItem>();

        foreach (var @event in events)
        {
            var items = ((EtlBuildCreateModel)@event.Event).GetBuilds()
                .Select(x => new EventItem(x, tableName, @event.UseCamelCase))
                .ToList();

            result.AddRange(items);
        }
        return result;
    }
}
```

Dependency injection:

```csharp
services.AddTransient<IPersistRepository, ClickHousePersistRepository>();
```

#### Attributes

The library support property attributes:

`[ClickHouseColumn("ColumnName")]` - use this attribute if the ClickHouse column name is different that class property name.

`[ClickHouseIgnore]` - use this attribute if serializer must ignore property while saving the value to database.