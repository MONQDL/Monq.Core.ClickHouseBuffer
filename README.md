# Monq.Core.ClickHouseBuffer

*English*

The Clickhouse buffer library can collect and write rows with batches (time based or count based).

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

```
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

You can define your own repository implementation for recording events in ClickHouse.
This requires the implementation of the `IPersistRepository` interface. For convenience, you can use the abstract class `BaseRepository`,
which contains a method for getting a new ClickHouse context and a set of options read from the configuration.

`Program.cs`

```csharp
.ConfigureServices((hostContext, services) =>
{
    ...
    services.ConfigureCHBuffer(
        hostContext.Configuration.GetSection("BufferEngineOptions"), clickHouseConnectionString);

    services.AddTransient<IPersistRepository, MyPersistRepositoryImpl>();
    ...
})
```

---

*Russian*

Библиотека дает возможность записывать данные в ClickHouse с использование механизма буферизации.

Как известно, ClickHouse выполняет вставку записываемых данных пакетным образом и если выполнять вставки по одной записи,
то ClickHouse начнет поедать процессорное время и потреблять IO дисковой подсистеме с очень высоким темпом. 
Для того, чтобы ClickHouse работал корректно и быстро, требуется выполнять вставку данных пачками, или сбрасывать накопившиеся данные
раз в определенное время. Библиотека реализовывает такой механизм.

В предварительной версии пока что нету:

1. Выполнения асинхронной постобработки событий после их сохранения в БД или при ошибке сохранения.
2. Нет красивой обработки ошибок записи.

## Установка

```powershell
Install-Package Monq.Core.ClickHouseBuffer
```

## Подключение

В Program.cs для консольных приложений или в Startup.cs для asp.net требуется добавить метод конфигурации буфера.

```csharp
services.ConfigureCHBuffer(Configuration.GetSection(BufferEngineOptions), clickHouseConnectionString);
```

`clickHouseConnectionString` - строка вида

```
Host=clickhouse<-http-host>;Port=80;Username=<user>;Password=<password>;Database=<database>;
```

### Конфигурация в appsettings.json

Буфер принимает конфигурацию по такой схеме

```json
{
	"EventsFlushPeriodSec": 2,
	"EventsFlushCount": 500,
	"MaxDegreeOfParallelism": 1
}
```

### Пример реализации библиотеки Monq.Core.ClickHouseBuffer с RabbitMQCoreClient и Monq.Core.BasicDotNetMicroservice.

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

```
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

### Расширенная настройка

Можно определить собственную реализацию репозитория для записи событий в ClickHouse. 
Для этого требуется реализовать интерфейс `IPersistRepository`. Для удобства, можно использовать абстрактный класс `BaseRepository`,
который содержит метод получения нового контекста ClickHouse и набор опций, прочитанных из конфигурации.

`Program.cs`

```csharp
.ConfigureServices((hostContext, services) =>
{
    ...
    services.ConfigureCHBuffer(
        hostContext.Configuration.GetSection("BufferEngineOptions"), clickHouseConnectionString);

    services.AddTransient<IPersistRepository, MyPersistRepositoryImpl>();
    ...
})
```