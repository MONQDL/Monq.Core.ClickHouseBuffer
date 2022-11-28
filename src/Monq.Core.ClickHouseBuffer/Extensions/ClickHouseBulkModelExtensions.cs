using Monq.Core.ClickHouseBuffer.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace Monq.Core.ClickHouseBuffer.Extensions
{
    /// <summary>
    /// A class extension to work with ClickHouse.
    /// </summary>
    public static class ClickHouseBulkModelExtensions
    {
        /// <summary>
        /// Generate an array of columns and their values to be written to the database.
        /// </summary>
        /// <param name="obj">Check result.</param>
        /// <param name="useCamelCase">Flag indicating whether the event should be written to camelCase.</param>
        /// <returns></returns>
        public static IDictionary<string, object> CreateDbValues(this object? obj, bool useCamelCase = true)
        {
            var result = new Dictionary<string, object>();
            if (obj is null)
                return result;

            var objType = obj.GetType();
            foreach (var prop in objType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // Ignore column if IgnoreAttrinute is present.
                if (Attribute.GetCustomAttribute(prop, typeof(ClickHouseIgnoreAttribute), true) is ClickHouseIgnoreAttribute)
                    continue;

                string colName;
                if (Attribute.GetCustomAttribute(prop, typeof(ClickHouseColumnAttribute), true) is ClickHouseColumnAttribute clickHouseColumn)
                    colName = clickHouseColumn.Name;
                else
                    colName = useCamelCase ? prop.Name.ToCamelCase() : prop.Name;

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
                else if (prop.PropertyType == typeof(DateTimeOffset))
                {
                    if (value is null)
                        value = default(DateTimeOffset).UtcDateTime;
                    else
                        value = ((DateTimeOffset)value).UtcDateTime;
                }

                if (value is null)
                    value = GetDefaultValue(prop);

                if (value != null)
                    result.Add(colName, value);
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
    }
}
