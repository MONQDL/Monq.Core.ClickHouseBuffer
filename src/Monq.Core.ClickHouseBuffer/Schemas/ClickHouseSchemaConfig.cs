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

    ConcurrentDictionary<TypeTuple, TypeAdapterSettings> _rulesMap = new ConcurrentDictionary<TypeTuple, TypeAdapterSettings>();
    public ConcurrentDictionary<TypeTuple, TypeAdapterSettings> RulesMap { get => _rulesMap; }

    ConcurrentDictionary<TypeTuple, string[]> _columnsMap = new ConcurrentDictionary<TypeTuple, string[]>();

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
                    var fn = (Func<TSource, object>)x.Invoker!;
                    return fn(source);
                })
                .ToArray();
        }
        else
            throw new Exception($"The type map \"{sourceType.Name}\" to \"{tableName}\" was not found");
    }

    public string[] GetMappedColumns<TSource>(TSource? source, string tableName)
    {
        if (source is null)
            return Array.Empty<string>();

        return GetMappedColumns(new TypeTuple(source.GetType(), tableName));
    }

    public string[] GetMappedColumns(in TypeTuple key)
    {
        return _columnsMap.GetOrAdd(key, static (k, rules) =>
        {
            if (!rules.TryGetValue(k, out var settings))
                throw new Exception($"The type map \"{k.Source.Name}\" to \"{k.TableName}\" was not found");

            return settings.Resolvers.Select(x => x.ColumnName).ToArray();
        }, _rulesMap);
    }

    /// <summary>
    /// Scans and registers mappings from specified assemblies.
    /// </summary>
    /// <param name="assemblies">Assemblies to scan.</param>
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
