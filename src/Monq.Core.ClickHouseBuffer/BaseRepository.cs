using ClickHouse.Client.ADO;
using Microsoft.Extensions.Options;
using Monq.Core.ClickHouseBuffer.Exceptions;
using System;

namespace Monq.Core.ClickHouseBuffer;

/// <summary>
/// Basic repository for interacting with ClickHouse.
/// </summary>
public abstract class BaseRepository
{
    readonly string _connectionString;

    readonly EngineOptions _options;

    protected EngineOptions Options => _options;

    /// <summary>
    /// Initializes a new instance of the class <see cref="BaseRepository"/>.
    /// </summary>
    /// <param name="engineOptions">ClickHouse configuration.</param>
    protected BaseRepository(IOptions<EngineOptions> engineOptions)
    {
        if (engineOptions == null)
            throw new ArgumentNullException(nameof(engineOptions), $"{nameof(engineOptions)} is null.");
        if (engineOptions.Value == null)
            throw new ArgumentNullException(nameof(engineOptions.Value), $"{nameof(engineOptions.Value)} is null.");

        if (string.IsNullOrEmpty(engineOptions.Value.ConnectionString))
            throw new BufferConfigurationException($"{nameof(engineOptions.Value.ConnectionString)} is null or empty.");

        _options = engineOptions.Value;
        _connectionString = engineOptions.Value.ConnectionString;
    }

    /// <summary>
    /// Get the ClickHouse connection object.
    /// </summary>
    /// <returns></returns>
    protected ClickHouseConnection GetConnection() =>
        new ClickHouseConnection(_connectionString);
}