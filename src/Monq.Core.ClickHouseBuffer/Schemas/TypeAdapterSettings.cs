using System.Collections.Concurrent;

namespace Monq.Core.ClickHouseBuffer.Schemas;

public class TypeAdapterSettings
{
    private readonly ConcurrentQueue<InvokerModel> _resolvers = new ConcurrentQueue<InvokerModel>();

    public ConcurrentQueue<InvokerModel> Resolvers => _resolvers;

    internal bool Compiled { get; set; }
}