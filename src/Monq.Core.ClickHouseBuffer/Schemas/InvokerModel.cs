using System;

namespace Monq.Core.ClickHouseBuffer.Schemas;

/// <summary>
/// Model class that represents an invocation of a property getter for mapping to a ClickHouse column.
/// </summary>
public class InvokerModel
{
    /// <summary>
    /// Gets or sets the name of the ClickHouse column.
    /// </summary>
    public string ColumnName { get; set; }
    
    /// <summary>
    /// Gets or sets the delegate used to invoke the property getter.
    /// </summary>
    public Delegate? Invoker { get; set; }
    
    /// <summary>
    /// Gets or sets the type of the property being invoked.
    /// </summary>
    public Type PropertyType { get; set; }
}
