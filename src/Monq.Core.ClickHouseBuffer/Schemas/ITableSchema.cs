
namespace Monq.Core.ClickHouseBuffer.Schemas;

/// <summary>
/// Interface for defining table schemas that map object properties to ClickHouse table columns.
/// Implement this interface to define custom mappings between your models and ClickHouse tables.
/// </summary>
public interface ITableSchema
{
    /// <summary>
    /// Registers the models-to-tables schemas at <paramref name="config"/>.
    /// </summary>
    /// <param name="config">The ClickHouse schema configuration to register mappings with.</param>
    void Register(ClickHouseSchemaConfig config);
}
