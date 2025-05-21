namespace Monq.Core.ClickHouseBuffer.Schemas;

public static class TypeAdapter
{
    /// <summary>
    /// Adapt the source object to the destination type.
    /// </summary>
    /// <typeparam name="TSource">Source type.</typeparam>
    /// <param name="source">Source object to adapt.</param>
    /// <param name="tableName">The ClickHouse table name.</param>
    /// <returns>Adapted destination type.</returns>
    public static object[] ClickHouseValues<TSource>(this TSource source, string tableName) => 
        ClickHouseSchemaConfig.GlobalSettings.GetMappedValues(source, tableName);

    public static string[] ClickHouseColumns<TSource>(this TSource source, string tableName) => 
        ClickHouseSchemaConfig.GlobalSettings.GetMappedColumns(source, tableName);

    public static bool SchemaExists<TSource>(this TSource source, string tableName) =>
        ClickHouseSchemaConfig.GlobalSettings.SchemaExists<TSource>(tableName);
}