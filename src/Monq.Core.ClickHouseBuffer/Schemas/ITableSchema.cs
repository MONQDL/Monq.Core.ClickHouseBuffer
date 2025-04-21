
namespace Monq.Core.ClickHouseBuffer.Schemas;

public interface ITableSchema
{
    public void Register(ClickHouseSchemaConfig config);
}
