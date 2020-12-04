using System.Collections.Generic;
using System.Threading.Tasks;

namespace Monq.Core.ClickHouseBuffer
{
    /// <summary>
    /// The service interface that represents methods to work with database storage.
    /// </summary>
    public interface IEventsWriter
    {
        /// <summary>
        /// Write events to database.
        /// </summary>
        /// <param name="dbValues">The array of key-value pairs of the "colunm name" -> "value".</param>
        /// <returns><see cref="Task"/> when the operation completes.</returns>
        Task Write(IDictionary<string, object>[] dbValues);
    }
}
