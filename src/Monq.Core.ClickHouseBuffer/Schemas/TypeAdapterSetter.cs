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

        // Перестраиваем выражение с конвертацией результата в object
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
        // Удаляем все апострофы в начале и конце строки
        string trimmed = input.Trim('`');
        // Оборачиваем результат в один апостроф с каждой стороны
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
