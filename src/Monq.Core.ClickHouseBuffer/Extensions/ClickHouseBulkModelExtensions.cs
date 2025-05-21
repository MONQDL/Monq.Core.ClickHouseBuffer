using Monq.Core.ClickHouseBuffer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Monq.Core.ClickHouseBuffer.Extensions;

/// <summary>
/// A class extension to work with ClickHouse.
/// </summary>
public static class ClickHouseBulkModelExtensions
{
    const BindingFlags _flags = BindingFlags.Public |
                              BindingFlags.NonPublic |
                              BindingFlags.Instance;

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

        var members = objType
        .GetMembers(_flags)
        .Where(m => m.MemberType is MemberTypes.Property or MemberTypes.Field)
        .Select(m => new
        {
            Member = m,
            Attribute = m.GetCustomAttribute<ClickHouseColumnAttribute>()
        })
        .Where(x => x.Attribute != null)
        .ToList();

        foreach (var member in members)
        {
            object? value = null;
            Type memberType = null!;

            if (member.Member is PropertyInfo property)
            {
                memberType = property.PropertyType;
                var getMethod = property.GetGetMethod(nonPublic: true);
                value = getMethod?.Invoke(obj, null);
            }
            else if (member.Member is FieldInfo field)
            {
                memberType = field.FieldType;
                value = field.GetValue(obj);
            }

            // Rule 1: For a string, replace null with an empty string.
            if (memberType == typeof(string) && value == null)
            {
                value = string.Empty;
            }
            // Rule 2: For Enum, we return the string representation
            else if (memberType.IsEnum || Nullable.GetUnderlyingType(memberType)?.IsEnum == true)
            {
                var baseType = Nullable.GetUnderlyingType(memberType) ?? memberType;
                value = value != null ? Enum.GetName(baseType, value) : null;
            }

            result.Add(value);
        }

        return result.ToArray();
    }

    /// <summary>
    /// Generate an array of columns names of the ClickHouse table extracted from object property names.
    /// </summary>
    /// <param name="obj">The object from which the column names array will be extracted.</param>
    /// <returns></returns>
    public static IReadOnlyList<string> ExtractDbColumnNames(this object? obj)
    {
        if (obj is null)
            return Array.Empty<string>();

        var objType = obj.GetType();

        return objType
            .GetMembers(_flags)
            .Where(m => m.MemberType is MemberTypes.Property or MemberTypes.Field)
            .Select(m => new
            {
                Member = m,
                Attribute = m.GetCustomAttribute<ClickHouseColumnAttribute>()
            })
            .Where(x => x.Attribute != null)
            .Select(x => x.Attribute!.Name)
            .ToList();
    }

    /// <summary>
    /// Creates an object of <see cref="EventItemWithEventObject"/>. The column values are extracting from object by reading property values by reflection.
    /// </summary>
    /// <param name="event">The source event, which will be saved until it persists.</param>
    /// <param name="tableName">Table name in ClickHouse.</param>
    /// <returns></returns>
    public static EventItemWithEventObject CreateFromReflection(this object @event, string tableName)
    {
        var dbValues = @event.ExtractDbColumnValues();

        return new EventItemWithEventObject(@event, @event.GetType(), tableName, dbValues);
    }
}
