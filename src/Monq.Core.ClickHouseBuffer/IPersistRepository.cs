using System.Collections.Generic;
using System.Threading.Tasks;

namespace Monq.Core.ClickHouseBuffer
{
    /// <summary>
    /// Interface represents a sets of methods to write the data to the ClickHouse.
    /// </summary>
    public interface IPersistRepository
    {
        /// <summary>
        /// Writes the batch to the ClickHouse.
        /// </summary>
        /// <param name="columns">The columns.</param>
        /// <param name="values">The values.</param>
        /// <param name="tableName">The table name in whitch records will be inserted.</param>
        /// <returns></returns>
        Task WriteBatch(IReadOnlyCollection<string> columns, List<object[]> values, string tableName);
    }
}
