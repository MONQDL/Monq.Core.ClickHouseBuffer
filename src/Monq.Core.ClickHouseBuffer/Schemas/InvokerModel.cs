using System.Linq.Expressions;

namespace Monq.Core.ClickHouseBuffer.Schemas;

public class InvokerModel
{
    public string ColumnName { get; set; }
    public LambdaExpression? Invoker { get; set; }
}
