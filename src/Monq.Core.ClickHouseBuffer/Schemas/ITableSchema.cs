
namespace Monq.Core.ClickHouseBuffer.Schemas;

public interface ITableSchema
{
    /// <summary>
    /// Registers the models-to-tables schemas at <paramref name="config"/>.
    /// </summary>
    /// <param name="config"></param>
    public void Register(ClickHouseSchemaConfig config);
}
