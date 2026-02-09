using System;
using System.Linq.Expressions;

namespace Monq.Core.ClickHouseBuffer.Schemas;

/// <summary>
/// Base class for configuring type adapters with settings and configuration.
/// </summary>
public class TypeAdapterSetter
{
    /// <summary>
    /// Gets the type adapter settings.
    /// </summary>
    public readonly TypeAdapterSettings Settings;
    
    /// <summary>
    /// Gets the ClickHouse schema configuration.
    /// </summary>
    public readonly ClickHouseSchemaConfig Config;
    
    /// <summary>
    /// Initializes a new instance of the TypeAdapterSetter class.
    /// </summary>
    /// <param name="settings">The type adapter settings.</param>
    /// <param name="config">The ClickHouse schema configuration.</param>
    public TypeAdapterSetter(TypeAdapterSettings settings, ClickHouseSchemaConfig config)
    {
        Settings = settings;
        Config = config;
    }
}

/// <summary>
/// Generic class for configuring type adapters for a specific source type.
/// </summary>
/// <typeparam name="TSource">The source object type.</typeparam>
public class TypeAdapterSetter<TSource> : TypeAdapterSetter
{
    internal TypeAdapterSetter(TypeAdapterSettings settings, ClickHouseSchemaConfig parentConfig)
        : base(settings, parentConfig)
    { }

    /// <summary>
    /// Maps a property or field of the source object to a ClickHouse column.
    /// </summary>
    /// <typeparam name="TSourceMember">The type of the property or field to map.</typeparam>
    /// <param name="columnName">The name of the ClickHouse column.</param>
    /// <param name="source">Expression representing the property or field to map.</param>
    /// <returns>The current TypeAdapterSetter instance for chaining.</returns>
    public TypeAdapterSetter<TSource> Map<TSourceMember>(
        string columnName,
        Expression<Func<TSource, TSourceMember>> source)
    {
        this.CheckCompiled();

        // Rebuilding the expression with the conversion of the result to an object
        var convertedExpr = Expression.Lambda<Func<TSource, object>>(
            Expression.Convert(source.Body, typeof(object)),
            source.Parameters
        );

        var invoker = convertedExpr.Compile();

        Settings.Resolvers.Enqueue(new InvokerModel
        {
            ColumnName = EnsureWrappedInBackticks(columnName),
            Invoker = invoker,
            PropertyType = typeof(TSourceMember)
        });
        return this;
    }

    /// <summary>
    /// Ensures that the input string is wrapped in backticks.
    /// </summary>
    /// <param name="input">The input string to wrap.</param>
    /// <returns>The input string wrapped in backticks.</returns>
    static string EnsureWrappedInBackticks(string input)
    {
        // Delete all apostrophes at the beginning and end of the line
        string trimmed = input.Trim('`');
        // Wrapping the result in one apostrophe on each side
        return $"`{trimmed}`";
    }
}

/// <summary>
/// Extension methods for TypeAdapterSetter.
/// </summary>
public static class TypeAdapterSetterExtensions
{
    /// <summary>
    /// Checks if the type adapter setter has already been compiled.
    /// </summary>
    /// <typeparam name="TSetter">The type of the setter, which must inherit from TypeAdapterSetter.</typeparam>
    /// <param name="setter">The type adapter setter to check.</param>
    /// <exception cref="InvalidOperationException">Thrown when the type adapter has already been compiled.</exception>
    internal static void CheckCompiled<TSetter>(this TSetter setter) where TSetter : TypeAdapterSetter
    {
        if (setter.Settings.Compiled)
            throw new InvalidOperationException("TypeAdapter.Adapt was already called, please clone or create new TypeAdapterConfig.");
    }
}
