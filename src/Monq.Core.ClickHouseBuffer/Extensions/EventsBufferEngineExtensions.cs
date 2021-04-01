using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Monq.Core.ClickHouseBuffer.Extensions
{
    public static class EventsBufferEngineExtensions
    {

        public static Task AddEvent<T>(this IEventsBufferEngine engine, [NotNull] T message, string tableName)
            where T : class
        {
            return engine.AddEvent(message, tableName);
        }
    }
}
