using System.Collections.Concurrent;

namespace Monq.Core.ClickHouseBuffer.Schemas;

public class TypeAdapterSettings
{
    private readonly ConcurrentBag<InvokerModel> _resolvers = new ConcurrentBag<InvokerModel>();

    public ConcurrentBag<InvokerModel> Resolvers => _resolvers;

    internal bool Compiled { get; set; }
}