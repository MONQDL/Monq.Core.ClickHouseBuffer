using System;
using System.Linq.Expressions;

namespace Monq.Core.ClickHouseBuffer.Schemas;

public class TypeAdapterSetter
{
    public readonly TypeAdapterSettings Settings;
    public readonly ClickHouseSchemaConfig Config;
    public TypeAdapterSetter(TypeAdapterSettings settings, ClickHouseSchemaConfig config)
    {
        Settings = settings;
        Config = config;
    }
}

public class TypeAdapterSetter<TSource> : TypeAdapterSetter
{
    internal TypeAdapterSetter(TypeAdapterSettings settings, ClickHouseSchemaConfig parentConfig)
        : base(settings, parentConfig)
    { }

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

    static string EnsureWrappedInBackticks(string input)
    {
        // Delete all apostrophes at the beginning and end of the line
        string trimmed = input.Trim('`');
        // Wrapping the result in one apostrophe on each side
        return $"`{trimmed}`";
    }
}

public static class TypeAdapterSetterExtensions
{
    internal static void CheckCompiled<TSetter>(this TSetter setter) where TSetter : TypeAdapterSetter
    {
        if (setter.Settings.Compiled)
            throw new InvalidOperationException("TypeAdapter.Adapt was already called, please clone or create new TypeAdapterConfig.");
    }
}
