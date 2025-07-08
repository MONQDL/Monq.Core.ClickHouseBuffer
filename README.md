# Monq.Core.ClickHouseBuffer

The ClickHouse buffer library can collect and write rows to tables with batches (time based or count based).

As you know, ClickHouse inserts the data being written in a batch manner, and if you perform inserts one at a time,
then ClickHouse will start to eat up CPU time and consume IO of the disk subsystem at a very high rate.
In order for ClickHouse to work correctly and quickly, you need to insert data in batches, or reset the accumulated data
once at a certain time. The library implements such a mechanism.

## Installation the library

```powershell
Install-Package Monq.Core.ClickHouseBuffer
```

## Using the library

The library is intended for use in systems with dependency injection.

### Dependency injection

Add dependency in Service collection.

```csharp
builder.Services.ConfigureCHBuffer(clickHouseConnectionString);
```

`clickHouseConnectionString` - the database connection string that looks like `
Host=clickhouse<-http-host>;Port=80;Username=<user>;Password=<password>;Database=<database>;
`

The library also supports configuration from IConfiguration. You must provide a section with configuration. 

appsettings.json example:

```json
{
    "bufferOptions": {
      "ConnectionString": "Host=clickhouse<-http-host>;Port=80;Username=<user>;Password=<password>;Database=<database>;",
      "EventsFlushPeriodSec": 2,
      "EventsFlushCount": 10000,
      "DatabaseBatchSize": 100000,
      "DatabaseMaxDegreeOfParallelism": 1
    }
}
```

So, if you use IConfiguration configure the library as follows

```csharp
builder.Services.ConfigureCHBuffer(Configuration.GetSection("bufferOptions"));
```

### Basic usage

To add objects to buffer you must inject `IEventsBufferEngine` at service and use `AddEvent` method.

```csharp
public class MyService
{
    readonly IEventsBufferEngine _engine;

    public MyService(IEventsBufferEngine engine)
    {
        _engine = engine;
    }

    void Processor() 
    {
        ...
        _engine.AddEvent(item, "table");
    }
}
```

### Defining model mapping

In order to start using the library, you need to decide on an approach to saving data in a ClickHouse. 
The library supports two modes of mapping properties to table columns: 
predefined schema and reflection.

Predefined schema is match more memory and GC less consuming mechanism. 
The performance test on the laptop Intel i9, 16Gb memory:

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.3775)
12th Gen Intel Core i9-12900H, 1 CPU, 20 logical and 14 physical cores
.NET SDK 9.0.300-preview.0.25177.5
[Host]     : .NET 8.0.14 (8.0.1425.11118), X64 RyuJIT AVX2
DefaultJob : .NET 8.0.14 (8.0.1425.11118), X64 RyuJIT AVX2

| Method                                               | Mean      | Error     | StdDev    | Completed Work Items | Lock Contentions | Gen0      | Gen1      | Gen2    | Allocated |
|----------------------------------------------------- |----------:|----------:|----------:|---------------------:|-----------------:|----------:|----------:|--------:|----------:|
| Bench_Schema_TenHundredsEvents_500BufferedEvents     |  2.237 ms | 0.0447 ms | 0.0722 ms |                    - |                - |  488.2813 |  175.7813 |       - |   5.88 MB |
| Bench_Reflection_TenHundredsEvents_500BufferedEvents | 38.913 ms | 0.7778 ms | 0.7639 ms |                    - |                - | 2428.5714 | 1000.0000 | 71.4286 |  29.49 MB |

#### Mapping Configuration with ClickHouseSchemaConfig.GlobalSettings

Define mapping model properties to column names at global configuration.

Example:

```csharp
ClickHouseSchemaConfig.GlobalSettings.NewConfig<MyEvent>("logs")
    .Map("_streamId", x => x.StreamId)
    .Map("_streamName", x => x.StreamName)
    .Map("_aggregatedAt", x => x.AggregatedAt)
    .Map("_userspaceId", x => x.UserspaceId)
    .Map("_rawJson", x => x.Value);
```

#### Mapping Configuration With "ITableSchema" Interface

Before using this feature you have to add this line:

```csharp
ClickHouseSchemaConfig.GlobalSettings.Scan(typeof(ClickHouseBench).Assembly);
```

With adding above line to your Startup.cs or Program.cs or any other way to run at startup, 
you can write mapping configs in the destination class that implements ITableSchema interface

Example:

```csharp
using Monq.Core.ClickHouseBuffer.Schemas;

public sealed class MyFullSchemaConfig : ITableSchema
{
    public void Register(ClickHouseSchemaConfig config)
    {
        config.NewConfig<MyEvent>("logs")
            .Map("_streamId", x => x.StreamId)
            .Map("_streamName", x => x.StreamName)
            .Map("_aggregatedAt", x => x.AggregatedAt)
            .Map("_userspaceId", x => x.UserspaceId)
            .Map("_rawJson", x => x.Value);
    }
}
```

#### Mapping Configuration With Reflection Attribute

If you can't use configuration schema then use reflection attribute `[ClickHouseColumn("fieldName")]`.

Keep in mind that this method is much slower than with the predefined scheme.

Only columns with attribute `[ClickHouseColumn]` will be pushed to database.

Example:

```csharp
class TestObject
{
    [ClickHouseColumn("publicProp")]
    public string PublicProp { get; set; } = Guid.NewGuid().ToString();

    [ClickHouseColumn("privateProp")]
    private string PrivateProp { get; set; } = Guid.NewGuid().ToString();

    public string GetPrivateProp() => PrivateProp;

    [ClickHouseColumn("publicField")]
    public string PublicField = Guid.NewGuid().ToString();

    [ClickHouseColumn("privateField")]
    private string _privateField = Guid.NewGuid().ToString();

    public string GetPrivateField() => _privateField;

    public string IgnoredProp { get; set; } = Guid.NewGuid().ToString();
}
```

### Extended configuration

By default the library uses [ClickHouse.Client](https://github.com/DarkWanderer/ClickHouse.Client) 
bulk insert to save buffered items. 
If you need you own implementation then implement the interface `IEventsWriter` and add it to DI after `builder.Services.ConfigureCHBuffer()`:

```csharp
builder.Services.AddTransient<IEventsWriter, CustomEventsWriter>();
```

Implementation example:

```csharp
internal sealed class DefaultClickHouseEventsWriter : IEventsWriter
{
    readonly IClickHouseConnection _connection;
    readonly EngineOptions _options;

    public DefaultClickHouseEventsWriter(IClickHouseConnection connection,
        EngineOptions engineOptions)
    {
        _connection = connection;

        if (engineOptions == null)
            throw new ArgumentNullException(nameof(engineOptions), $"{nameof(engineOptions)} is null.");

        _options = engineOptions;
    }

    /// <inheritdoc />
    public async Task WriteBatch(IEnumerable<EventItem> events, TypeTuple key)
    {
        if (!events.Any())
            return;

        // Get events column names.
        var columns = ClickHouseSchemaConfig.GlobalSettings.GetMappedColumns(key);

        var values = events.Select(x => x.Values);

        using var bulkCopy = new ClickHouseBulkCopy((ClickHouseConnection)_connection)
        {
            MaxDegreeOfParallelism = _options.DatabaseMaxDegreeOfParallelism,
            BatchSize = _options.EventsFlushCount,
            DestinationTableName = _connection.Database + "." + key.TableName,
            ColumnNames = columns
        };

        // Prepares ClickHouseBulkCopy instance by loading target column types
        await bulkCopy.InitAsync().ConfigureAwait(false);

        await bulkCopy.WriteToServerAsync(values).ConfigureAwait(false);
    }
}
```

You also can implement `IEventsHandler` to run you methods `OnAfterWriteEvents` or `OnWriteErrors`.
You must add you custom implementation to DI:

```csharp
builder.Services.AddTransient<IEventsHandler, CustomEventsHandler>();
```

### Multiple ClickHouses

In rare cases, you need to use a buffer and write data to different ClickHouse databases.
In this case, follow the steps: 
1. Do not pass ConnectionString in the options at Program.cs (`builder.Services.ConfigureCHBuffer`).
2. Register named ClickHouse configurations using the built-in library ClickHouse.Client (or use custom connections factory) at Program.cs.
```csharp
builder.Services.AddClickHouseDataSource(connectionString1, serviceKey: "ch1");
builder.Services.AddClickHouseDataSource(connectionString2, serviceKey: "ch2");
```
3. Perform a custom implementation of the IEventsWriter interface.
```csharp
internal sealed class MultipleClickHouseEventsWriter : IEventsWriter
{
    readonly IServiceProvider _serviceProvider;
    readonly EngineOptions _options;

    public MultiClickHouseEventsWriter(IServiceProvider serviceProvider,
        EngineOptions engineOptions)
    {
        _serviceProvider = serviceProvider;

        if (engineOptions == null)
            throw new ArgumentNullException(nameof(engineOptions), $"{nameof(engineOptions)} is null.");

        _options = engineOptions;
    }

    /// <inheritdoc />
    public async Task WriteBatch(IEnumerable<EventItem> events, TypeTuple key)
    {
        if (!events.Any())
            return;

        // Get events column names.
        var columns = ClickHouseSchemaConfig.GlobalSettings.GetMappedColumns(key);

        var values = events.Select(x => x.Values);

        var connection = GetConnection(key.TableName);

        using var bulkCopy = new ClickHouseBulkCopy(connection)
        {
            MaxDegreeOfParallelism = _options.DatabaseMaxDegreeOfParallelism,
            BatchSize = _options.EventsFlushCount,
            DestinationTableName = _connection.Database + "." + key.TableName,
            ColumnNames = columns
        };

        // Prepares ClickHouseBulkCopy instance by loading target column types
        await bulkCopy.InitAsync().ConfigureAwait(false);

        await bulkCopy.WriteToServerAsync(values).ConfigureAwait(false);
    }
}

ClickHouseConnection GetConnection(string tableName) =>
        _serviceProvider.GetRequiredKeyedService<ClickHouseConnection>(
            tableName == "logs"
                ? "ch1"
                : "ch2");
```
4. Register you custom IEventsWriter implementation in DI.
```csharp
// after builder.Services.ConfigureCHBuffer();
builder.Services.AddTransient<IEventsWriter, MultipleClickHouseEventsWriter>();
```

### Custom EventItem model

If you need to add custom properties to EventItem model and handle these properties in `IEventsWriter.WriteBatch`
you can inherit from `EventItem` and use `IEventsBufferEngine.Add`. There is a ready to use class `EventItemWithSourceObject`.
This class contains the source object on the basis of which an array of columns and values for writing to ClickHouse was calculated.
Use extension method `IEventsBufferEngine.AddEventWithSourceObject` to add events with source. 
At `IEventsWriter.WriteBatch` cast to the class `EventItemWithSourceObject`.

```csharp
    public async Task WriteBatch(IEnumerable<EventItem> events, TypeTuple key)
    {
        ...
        foreach (EventItem item in events)
        {
            if (item is EventItemWithSourceObject eventWithSource)
            {
                // Working с item.Source
            }
        }
        ...
    }
```

### Migration guide from v2 to v3.

1. Update the library.
2. Make the transition to the predefined model schema, if possible. If not possible annotate all fields and properties that must be written to DB by attribute `[ClickHouseColumn]`.
3. Remove attribute `[ClickHouseIgnore]`.
3. If a custom implementation of IPersistRepository is used, then
    - `IPersistRepository` rename to на `IEventsWriter`.
    - Method `public Task WriteBatch(IEnumerable<EventItem> events, string tableName)` rename to `public Task WriteBatch(IEnumerable<EventItem> events, TypeTuple key)`.
    - To get a list of columns, use the method in `Task WriteBatch`method use: `var columns = ClickHouseSchemaConfig.GlobalSettings.GetMappedColumns(key);`
    - To get the final list of values to insert data, use: `var values = events.Select(x => x.Values);`
    - To get the table name, use `key.TableName` instead of `tableName`.
4. Calls to the `await _eventsBufferEngine.AddEvent()` replace with synchronous`_eventsBufferEngine.AddEvent()`.
5. Replace `builder.Services.ConfigureCHBuffer(builder.Configuration.GetSection(BufferEngineOptions), clickHouseConnectionString);` to
`builder.Services.ConfigureCHBuffer(builder.Configuration.GetSection(AppConstants.Configuration.BufferEngineOptions), x => x.ConnectionString = clickHouseConnectionString!);`
5. If `IErrorEventsHandler` or `IPostHandler` is replace, then merge the implementations to the `IEventsHandler`.
`IErrorEventsHandler.Handle` moved to `IEventsHandler.OnWriteErrors`, `IPostHandler.Handle` moved to `IEventsHandler.OnAfterWriteEvents`