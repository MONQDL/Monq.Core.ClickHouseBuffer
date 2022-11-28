using System;

namespace Monq.Core.ClickHouseBuffer.Attributes
{
    /// <summary>
    /// Use this attribute if the ClickHouse column name is different that class property name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ClickHouseColumnAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ClickHouseColumnAttribute"/>.
        /// </summary>
        /// <param name="name">The name of the field in the database.</param>
        public ClickHouseColumnAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException($"{nameof(name)} is null or empty.", nameof(name));

            Name = name;
        }

        /// <summary>
        /// The name of the field in the database.
        /// </summary>
        public string Name { get; }
    }
}