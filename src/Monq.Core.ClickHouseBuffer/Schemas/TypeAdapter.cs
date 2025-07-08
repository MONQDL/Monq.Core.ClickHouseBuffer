namespace Monq.Core.ClickHouseBuffer.Schemas;

/// <summary>
/// The extension methods collection to handle Schemas.
/// </summary>
public static class TypeAdapter
{
    /// <summary>
    /// Get the array of object properties and fields values calculated from schema or reflection attribute.
    /// </summary>
    /// <typeparam name="TSource">The source object type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="tableName">The ClickHouse table name.</param>
    /// <returns>Array of objects that were calculated from <paramref name="source"/>.</returns>
    public static object[] ClickHouseValues<TSource>(this TSource source, string tableName) => 
        ClickHouseSchemaConfig.GlobalSettings.GetMappedValues(source, tableName);

    /// <summary>
    /// Get the array of the ClickHouse columns calculated from schema or reflection attribute.
    /// </summary>
    /// <typeparam name="TSource">The source object type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="tableName">The ClickHouse table name.</param>
    /// <returns>Array of column names that were calculated from <paramref name="source"/>.</returns>
    public static string[] ClickHouseColumns<TSource>(this TSource source, string tableName) => 
        ClickHouseSchemaConfig.GlobalSettings.GetMappedColumns(source, tableName);

    /// <summary>
    /// Check if schema exists for pair <typeparamref name="TSource"/> and <paramref name="tableName"/>.
    /// </summary>
    /// <typeparam name="TSource">The source object type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="tableName">The ClickHouse table name.</param>
    /// <returns>True - if schema exists, otherwise - false.</returns>
    public static bool SchemaExists<TSource>(this TSource source, string tableName) =>
        ClickHouseSchemaConfig.GlobalSettings.SchemaExists<TSource>(tableName);
}