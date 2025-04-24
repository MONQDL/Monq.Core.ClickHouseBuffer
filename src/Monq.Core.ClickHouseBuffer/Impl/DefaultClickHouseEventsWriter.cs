using ClickHouse.Client;
using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monq.Core.ClickHouseBuffer.Schemas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Monq.Core.ClickHouseBuffer.Impl;

/// <summary>
/// Implementation of the service for recording events in the database.
/// </summary>
public sealed class DefaultClickHouseEventsWriter : IEventsWriter
{
    readonly IClickHouseConnection _connection;
    readonly EngineOptions _options;

    /// <summary>
    /// Service implementation designer for recording events in the database.
    /// Creates a new instance of the class <see cref="DefaultClickHouseEventsWriter"/>.
    /// </summary>
    public DefaultClickHouseEventsWriter(IClickHouseConnection connection,
        IOptions<EngineOptions> engineOptions)
    {
        _connection = connection;

        if (engineOptions == null)
            throw new ArgumentNullException(nameof(engineOptions), $"{nameof(engineOptions)} is null.");
        if (engineOptions.Value == null)
            throw new ArgumentNullException(nameof(engineOptions.Value), $"{nameof(engineOptions.Value)} is null.");

        _options = engineOptions.Value;
    }

    /// <inheritdoc />
    public async Task WriteBatch(IEnumerable<EventItem> events, TypeTuple key)
    {
        if (!events.Any())
            return;

        // Get events column names.
        var columns = ClickHouseSchemaConfig.GlobalSettings.GetMappedColumns(key);

        var values = events.Select(x => x.Values.ToArray());

        using var bulkCopy = new ClickHouseBulkCopy((ClickHouseConnection)_connection)
        {
            MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
            BatchSize = _options.EventsFlushCount,
            DestinationTableName = _connection.Database + "." + key.TableName,
            ColumnNames = columns
        };

        // Prepares ClickHouseBulkCopy instance by loading target column types
        await bulkCopy.InitAsync().ConfigureAwait(false);

        await bulkCopy.WriteToServerAsync(values).ConfigureAwait(false);
    }
}
