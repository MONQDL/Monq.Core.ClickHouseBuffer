using ClickHouse.Client.ADO;
using Microsoft.Extensions.Options;
using Monq.Core.ClickHouseBuffer.Exceptions;
using System;

namespace Monq.Core.ClickHouseBuffer
{
    /// <summary>
    /// Базовый репозиторий для взаимодействия с ClickHouse.
    /// </summary>
    public abstract class BaseRepository
    {
        readonly string _connectionString;

        protected readonly EngineOptions Options;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="BaseRepository"/>.
        /// </summary>
        /// <param name="engineOptions">Конфигурация ClickHouse.</param>
        protected BaseRepository(IOptions<EngineOptions> engineOptions)
        {
            if (engineOptions == null)
                throw new ArgumentNullException(nameof(engineOptions), $"{nameof(engineOptions)} is null.");
            if (engineOptions.Value == null)
                throw new ArgumentNullException(nameof(engineOptions.Value), $"{nameof(engineOptions.Value)} is null.");

            if (string.IsNullOrEmpty(engineOptions.Value.ConnectionString))
                throw new BufferConfigurationException($"{nameof(engineOptions.Value.ConnectionString)} is null or empty.");

            Options = engineOptions.Value;
            _connectionString = engineOptions.Value.ConnectionString;
        }

        /// <summary>
        /// Получить объект соединения с ClickHouse.
        /// </summary>
        /// <returns></returns>
        protected ClickHouseConnection GetConnection()
        {
            var con = new ClickHouseConnection(_connectionString);
            return con;
        }
    }
}