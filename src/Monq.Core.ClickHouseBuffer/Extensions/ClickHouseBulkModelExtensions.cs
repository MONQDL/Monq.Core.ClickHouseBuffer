using Monq.Core.ClickHouseBuffer.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace Monq.Core.ClickHouseBuffer.Extensions;

/// <summary>
/// A class extension to work with ClickHouse.
/// </summary>
public static class ClickHouseBulkModelExtensions
{
    /// <summary>
    /// Generate an array of values to be written to columns of the database.
    /// </summary>
    /// <param name="obj">The object from which the column array will be extracted.</param>
    /// <returns></returns>
    public static object?[] ExtractDbColumnValues(this object? obj)
    {
        if (obj is null)
            return Array.Empty<object?>();

        var result = new List<object?>();

        var objType = obj.GetType();
        foreach (var prop in objType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Ignore column if IgnoreAttribute is present.
            if (Attribute.GetCustomAttribute(prop, typeof(ClickHouseIgnoreAttribute), true) is ClickHouseIgnoreAttribute)
                continue;

            var value = prop.GetValue(obj);
            if (prop.PropertyType.IsEnum)
            {
                if (value is null)
                {
                    var enumValues = Enum.GetValues(prop.PropertyType);
                    if (enumValues != null && enumValues.GetValue(0) != null)
                        value = Enum.ToObject(prop.PropertyType, enumValues!.GetValue(0)!).ToString();
                    else
                        value = null;
                }
                else
                    value = Enum.ToObject(prop.PropertyType, value).ToString();
            }

            value ??= GetDefaultValue(prop);

            result.Add(value);
        }

        return result.ToArray();
    }

    /// <summary>
    /// Generate an array of columns names of the ClickHouse table extracted from object property names.
    /// </summary>
    /// <param name="obj">The object from which the column names array will be extracted.</param>
    /// <param name="useCamelCase">Flag indicating whether the event should be written to camelCase.</param>
    /// <returns></returns>
    public static IReadOnlyList<string> ExtractDbColumnNames(this object? obj, bool useCamelCase = true)
    {
        if (obj is null)
            return Array.Empty<string>();

        var result = new List<string>();

        var objType = obj.GetType();
        foreach (var prop in objType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Ignore column if IgnoreAttribute is present.
            if (Attribute.GetCustomAttribute(prop, typeof(ClickHouseIgnoreAttribute), true) is ClickHouseIgnoreAttribute)
                continue;

            string colName;
            if (Attribute.GetCustomAttribute(prop, typeof(ClickHouseColumnAttribute), true) is ClickHouseColumnAttribute clickHouseColumn)
                colName = clickHouseColumn.Name;
            else
                colName = useCamelCase ? prop.Name.ToCamelCase() : prop.Name;

            result.Add(colName);
        }

        return result;
    }

    static object? GetDefaultValue(PropertyInfo prop)
    {
        if (prop.PropertyType == typeof(string))
            return (object)string.Empty;

        var defaultAttr = prop.GetCustomAttribute(typeof(DefaultValueAttribute));
        if (defaultAttr != null)
            return (defaultAttr as DefaultValueAttribute)?.Value;

        var propertyType = prop.PropertyType;
        return propertyType.IsValueType ? Activator.CreateInstance(propertyType) : null;
    }

    /// <summary>
    /// Creates an object of <see cref="EventItemWithEventObject"/>. The column values are extracting from object by reading property values by reflection.
    /// </summary>
    /// <param name="event">The source event, which will be saved until it persists.</param>
    /// <param name="tableName">Table name in ClickHouse.</param>
    /// <param name="useCamelCase">User camelCase when extracting ClickHouse column names without attribute <see cref="ClickHouseColumnAttribute"/>.</param>
    /// <returns></returns>
    public static EventItemWithEventObject CreateFromReflection(this object @event, string tableName, bool useCamelCase)
    {
        var dbValues = @event.ExtractDbColumnValues();
        //Columns = dbValues.Keys.Select(val => $"`{val}`").ToList().AsReadOnly();

        return new EventItemWithEventObject(@event, @event.GetType(), tableName, dbValues);
    }
}
