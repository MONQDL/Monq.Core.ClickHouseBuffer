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
        string memberName,
        Expression<Func<TSource, TSourceMember>> source)
    {
        //this.CheckCompiled();

        var invoker = Expression.Lambda(source.Body, Expression.Parameter(typeof(object)));

        Settings.Resolvers.Add(new InvokerModel
        {
            ColumnName = memberName,
            Invoker = invoker,
        });
        return this;
    }
}
