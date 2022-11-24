using System;

namespace Monq.Core.ClickHouseBuffer.Attributes
{
    /// <summary>
    /// Use this attribute if serializer must ignore property while saving the value to database.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ClickHouseIgnoreAttribute : Attribute
    {
    }
}