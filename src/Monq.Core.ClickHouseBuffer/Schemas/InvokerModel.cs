using System;

namespace Monq.Core.ClickHouseBuffer.Schemas;

public class InvokerModel
{
    public string ColumnName { get; set; }
    public Delegate? Invoker { get; set; }
    public Type PropertyType { get; set; }
}
