using Monq.Core.ClickHouseBuffer.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Monq.Core.ClickHouseBuffer.Schemas;

public class ClickHouseSchemaConfig
{
    public static ClickHouseSchemaConfig GlobalSettings { get; } = new ClickHouseSchemaConfig();

    static ConcurrentDictionary<TypeTuple, TypeAdapterSettings> _rulesMap = new ConcurrentDictionary<TypeTuple, TypeAdapterSettings>();

    public TypeAdapterSetter<TSource> NewConfig<TSource>(string tableName)
    {
        var key = new TypeTuple(typeof(TSource), tableName);
        if (_rulesMap.ContainsKey(key))
            _rulesMap.TryRemove(key, out var _);

        var newSettings = new TypeAdapterSettings();
        _rulesMap.TryAdd(key, newSettings);
        return new TypeAdapterSetter<TSource>(newSettings, this);
    }

    public object[] GetMappedValues<TSource>(TSource? source, string tableName)
    {
        if (source is null)
            return Array.Empty<object>();

        var sourceType = source.GetType();

        if (_rulesMap.TryGetValue(new TypeTuple(sourceType, tableName), out var settings))
        {
            return settings
                .Resolvers
                .Select(x =>
                {
                    var compile = (Func<TSource, object>)(x.Invoker.Compile());
                    return compile(source);
                })
                .ToArray();
        }
        else
            throw new Exception();
    }

    public string[] GetMappedColumns<TSource>(TSource? source, string tableName)
    {
        if (source is null)
            return Array.Empty<string>();

        var sourceType = source.GetType();

        if (_rulesMap.TryGetValue(new TypeTuple(sourceType, tableName), out var settings))
        {
            return settings
                .Resolvers
                .Select(x => x.ColumnName)
                .ToArray();
        }
        else
            throw new Exception();
    }

    /// <summary>
    /// Scans and registers mappings from specified assemblies.
    /// </summary>
    /// <param name="assemblies">assemblies to scan.</param>
    /// <returns>A list of registered mappings</returns>
    public IList<ITableSchema> Scan(params Assembly[] assemblies)
    {
        var registers = assemblies.Select(assembly => assembly.GetLoadableTypes()
            .Where(x => typeof(ITableSchema).GetTypeInfo().IsAssignableFrom(x.GetTypeInfo()) && x.GetTypeInfo().IsClass && !x.GetTypeInfo().IsAbstract))
            .SelectMany(registerTypes =>
                registerTypes.Select(registerType => (ITableSchema)Activator.CreateInstance(registerType)!))
            .ToList();

        Apply(registers);
        return registers;
    }

    /// <summary>
    /// Applies type mappings.
    /// </summary>
    /// <param name="registers">collection of IRegister interface to apply mapping.</param>
    public void Apply(IEnumerable<ITableSchema> registers)
    {
        foreach (ITableSchema register in registers)
        {
            register.Register(this);
        }
    }
}
