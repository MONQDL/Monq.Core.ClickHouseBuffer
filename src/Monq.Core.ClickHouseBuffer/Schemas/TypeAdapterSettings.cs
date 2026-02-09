using System.Collections.Concurrent;

namespace Monq.Core.ClickHouseBuffer.Schemas;

/// <summary>
/// Settings class for type adapters, containing resolvers for mapping object properties to ClickHouse columns.
/// </summary>
public class TypeAdapterSettings
{
    readonly ConcurrentQueue<InvokerModel> _resolvers = new ConcurrentQueue<InvokerModel>();

    /// <summary>
    /// Gets the queue of invoker models used for resolving property values.
    /// </summary>
    public ConcurrentQueue<InvokerModel> Resolvers => _resolvers;

    /// <summary>
    /// Gets or sets whether the settings have been compiled.
    /// </summary>
    internal bool Compiled { get; set; }
}