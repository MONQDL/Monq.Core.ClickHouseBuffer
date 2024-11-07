using ClickHouse.Client.Copy;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Monq.Core.ClickHouseBuffer.Impl
{
    /// <summary>
    /// Repository for writing data in ClickHouse.
    /// </summary>
    public class DefaultRepository
        : BaseRepository, IPersistRepository
    {
        /// <summary>
        /// Initializes a new instance of the class <see cref="DefaultRepository" />.
        /// </summary>
        /// <param name="engineOptions">ClickHouse configuration.</param>
        public DefaultRepository(
            IOptions<EngineOptions> engineOptions) : base(engineOptions)
        { }

        /// <inheritdoc />
        public async Task WriteBatch(IEnumerable<EventItem> events, string tableName)
        {
            await using var connection = GetConnection();

            var columns = events.Select(x => x.Columns).FirstOrDefault();
            var values = events.Select(x => x.Values.ToArray());

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
