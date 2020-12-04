using System;
using System.Collections.Generic;
using System.Reflection;

namespace Monq.Core.ClickHouseBuffer.Extensions
{
    /// <summary>
    /// Методы расширения для <see cref="StreamDataEventViewModel"/>.
    /// </summary>
    public static class ClickHouseBulkModelExtensions
    {
        /// <summary>
        /// Сформировать массив колонок и их значений для записи в БД.
        /// </summary>
        /// <param name="obj">Результат выполенния проверки.</param>
        /// <param name="useCamelCase"></param>
        /// <returns></returns>
        public static IDictionary<string, object> CreateDbValues(this object obj, bool useCamelCase = true)
        {
            var result = new Dictionary<string, object>();
            var objType = obj.GetType();
            foreach (var prop in objType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var colName = useCamelCase ? prop.Name.ToCamelCase() : prop.Name;
                var value = prop.GetValue(obj);
                if (prop.PropertyType.IsEnum)
                    value = Enum.ToObject(prop.PropertyType, value).ToString();
                if (prop.PropertyType == typeof(DateTimeOffset))
                    value = ((DateTimeOffset)value).UtcDateTime;
                if (value is null)
                    value = GetDefaultValue(prop);
                result.Add(colName, value);
            }

            return result;
        }

        static object GetDefaultValue(PropertyInfo prop)
        {
            if (prop.PropertyType == typeof(string))
                return (object)string.Empty;

            Func<object> f = GetDefault<object>;
            return f.Method.GetGenericMethodDefinition().MakeGenericMethod(prop.PropertyType).Invoke(null, null);
        }

        static T GetDefault<T>()
        {
            return default(T);
        }
    }
}