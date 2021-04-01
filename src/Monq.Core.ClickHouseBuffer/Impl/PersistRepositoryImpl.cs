using ClickHouse.Client.Copy;
using Microsoft.Extensions.Options;
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
        public DefaultRepository(
            IOptions<EngineOptions> engineOptions) : base(engineOptions)
        { }

        /// <inheritdoc />
        public async Task WriteBatch(IReadOnlyCollection<string> columns, List<object[]> values, string tableName)
        {
            await using var connection = GetConnection();

            using var command = new ClickHouseBulkCopy(connection)
            {
                MaxDegreeOfParallelism = Options.MaxDegreeOfParallelism,
                BatchSize = Options.EventsFlushCount,
                DestinationTableName = connection.Database + "." + tableName
            };
            await command.WriteToServerAsync(values, columns);
        }
    }
}
