using ClickHouse.Client.Copy;
using Microsoft.Extensions.Options;
using Monq.Core.ClickHouseBuffer.Exceptions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Monq.Core.ClickHouseBuffer.Impl
{
    /// <summary>
    /// Репозиторий для чтения потоковых данных.
    /// </summary>
    public class DefaultRepository
        : BaseRepository, IPersistRepository
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="DefaultRepository" />.
        /// </summary>
        /// <param name="engineOptions">Конфигурация ClickHouse.</param>
        /// <param name="logger">The logger.</param>
        public DefaultRepository(
            IOptions<EngineOptions> engineOptions) : base(engineOptions)
        {
            if (string.IsNullOrEmpty(engineOptions.Value.TableName))
                throw new BufferConfigurationException($"{nameof(engineOptions.Value.TableName)} is null.");
        }

        /// <inheritdoc />
        public async Task WriteBatch(IReadOnlyCollection<string> columns, List<object[]> values)
        {
            await using var connection = GetConnection();

            using var command = new ClickHouseBulkCopy(connection)
            {
                MaxDegreeOfParallelism = Options.MaxDegreeOfParallelism,
                BatchSize = Options.EventsFlushCount,
                DestinationTableName = connection.Database + "." + Options.TableName
            };
            await command.WriteToServerAsync(values, columns);
        }
    }
}
