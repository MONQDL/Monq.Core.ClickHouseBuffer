namespace Monq.Core.ClickHouseBuffer.Extensions
{
    /// <summary>
    /// A class extension for working with strings.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Converts the string <paramref name="value"/> to camelcase.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string ToCamelCase(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return char.ToLower(value[0]) + value.Substring(1, value.Length - 1);
        }
    }
}
