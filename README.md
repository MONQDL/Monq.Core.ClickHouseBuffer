# Monq.Core.ClickHouseBuffer

Библиотека дает возможность записывать данные в ClickHouse с использование механизма буферизации.

Как известно, ClickHouse выполняет вставку записываемых данных пакетным образом и если выполнять вставки по одной записи,
то ClickHouse начнет поедать процессорное время и потреблять IO дисковой подсистеме с очень высоким темпом. 
Для того, чтобы ClickHouse работал корректно и быстро, требуется выполнять вставку данных пачками, или сбрасывать накопившиеся данные
раз в определенное время. Библиотека реализовывает такой механизм.

В предварительной версии пока что нету:

1. Проброса записей в рэббит после сохранения (я добавлю интерфейс постобработчика, так что на самом деле можно будет написать что угодно, и оно вызовется после успешного сохранения)
2. Нет поддержки мультимодельности, это когда буфер может работать с несколькими моделями и несколькими таблицами. Т.е. пока что это библиотека -> одна модель из рэббита, например.
3. Нет красивой обработки ошибок записи.

## Установка

```powershell
Install-Package RabbitMQCoreClient -Source http://nuget.monq.ru/nuget/Default
```

## Подключение

В Program.cs для консольных приложений или в Startup.cs для asp.net требуется добавить метод конфигурации буфера.

```csharp
services.ConfigureCHBuffer<FooEntity>(Configuration.GetSection(BufferEngineOptions), clickHouseConnectionString);
```

`clickHouseConnectionString` - срока вида

```
Host=clickhouse<-http-host>;Port=80;Username=<user>;Password=<password>;Database=<database>;
```

### Конфигурация в appsettings.json

Буфер принимает конфигурацию вида

```json
{
	"EventsFlushPeriodSec": 2,
	"EventsFlushCount": 500,
	"MaxDegreeOfParallelism": 1
}
```

### Пример реализации с RabbitMQCoreClient и библиотекой BasicDotNetMicroservice.

`Program.cs`

```csharp
.ConfigureServices((hostContext, services) =>
{
    var configuration = hostContext.Configuration;

    var clickHouseConnectionString = hostContext.Configuration["ClickHouseConnectionString"];

    services.ConfigureCHBuffer<FooEntity>(
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
    services.ConfigureCHBuffer<FooEntity>(
        hostContext.Configuration.GetSection("BufferEngineOptions"), clickHouseConnectionString);

    services.AddTransient<IPersistRepository, MyPersistRepositoryImpl>();
    ...
})
```