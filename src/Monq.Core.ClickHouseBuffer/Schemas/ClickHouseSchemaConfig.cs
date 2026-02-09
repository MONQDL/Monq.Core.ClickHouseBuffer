using Monq.Core.ClickHouseBuffer.Exceptions;
using Monq.Core.ClickHouseBuffer.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Monq.Core.ClickHouseBuffer.Schemas;

/// <summary>
/// Configuration class for defining mappings between object properties and ClickHouse table columns.
/// </summary>
public class ClickHouseSchemaConfig
{
    readonly ConcurrentDictionary<TypeTuple, TypeAdapterSettings> _rulesMap = new ConcurrentDictionary<TypeTuple, TypeAdapterSettings>();
    readonly ConcurrentDictionary<TypeTuple, string[]> _columnsMap = new ConcurrentDictionary<TypeTuple, string[]>();

    /// <summary>
    /// Gets the global instance of ClickHouseSchemaConfig.
    /// </summary>
    public static ClickHouseSchemaConfig GlobalSettings { get; } = new ClickHouseSchemaConfig();
    
    /// <summary>
    /// Gets the dictionary containing the mapping rules between TypeTuple and TypeAdapterSettings.
    /// </summary>
    public ConcurrentDictionary<TypeTuple, TypeAdapterSettings> RulesMap { get => _rulesMap; }

    /// <summary>
    /// Creates a new configuration for mapping the specified source type to a ClickHouse table.
    /// </summary>
    /// <typeparam name="TSource">The source object type to map.</typeparam>
    /// <param name="tableName">The name of the ClickHouse table to map to.</param>
    /// <returns>A TypeAdapterSetter instance for configuring the mapping.</returns>
    public TypeAdapterSetter<TSource> NewConfig<TSource>(string tableName)
    {
        var key = new TypeTuple(typeof(TSource), tableName);
        if (_rulesMap.ContainsKey(key))
            _rulesMap.TryRemove(key, out var _);

        var newSettings = new TypeAdapterSettings();
        _rulesMap.TryAdd(key, newSettings);
        return new TypeAdapterSetter<TSource>(newSettings, this);
    }

    /// <summary>
    /// Gets the mapped values from the source object based on the defined schema for the specified table.
    /// </summary>
    /// <typeparam name="TSource">The source object type.</typeparam>
    /// <param name="source">The source object to extract values from.</param>
    /// <param name="tableName">The name of the ClickHouse table.</param>
    /// <returns>An array of object values extracted from the source object according to the schema.</returns>
    /// <exception cref="BufferConfigurationException">Thrown when the type map for the specified source type and table name is not found.</exception>
    public object?[] GetMappedValues<TSource>(TSource? source, string tableName)
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
                    object? result;
                    // Convert Invoker to Func<TSource, object>, if possible
                    if (x.Invoker is Func<TSource, object> typedInvoker)
                    {
                        result = typedInvoker(source);
                    }
                    else
                    {
                        // If the type does not match, we try to use a dynamic call
                        // It may not be safe when trimming, but in this case
                        // we assume that the types are compatible
                        result = x.Invoker!.DynamicInvoke(source);
                    }

                    if (x.PropertyType == typeof(string) && result == null)
                        return string.Empty;
                    else
                        return result;
                })
                .ToArray();
        }
        else
            throw new BufferConfigurationException($"The type map '{sourceType.Name}' to '{tableName}' was not found");
    }

    /// <summary>
    /// Checks if a schema exists for the specified source type and table name.
    /// </summary>
    /// <typeparam name="TSource">The source object type.</typeparam>
    /// <param name="tableName">The name of the ClickHouse table.</param>
    /// <returns>True if a schema exists for the specified source type and table name, otherwise false.</returns>
    public bool SchemaExists<TSource>(string tableName) =>
        _rulesMap.ContainsKey(new TypeTuple(typeof(TSource), tableName));

    /// <summary>
    /// Gets the mapped column names from the source object based on the defined schema for the specified table.
    /// </summary>
    /// <typeparam name="TSource">The source object type.</typeparam>
    /// <param name="source">The source object to extract column names from.</param>
    /// <param name="tableName">The name of the ClickHouse table.</param>
    /// <returns>An array of column names extracted from the source object according to the schema.</returns>
    public string[] GetMappedColumns<TSource>(TSource? source, string tableName)
    {
        if (source is null)
            return Array.Empty<string>();

        return GetMappedColumns(new TypeTuple(source.GetType(), tableName));
    }

    /// <summary>
    /// Gets the mapped column names based on the TypeTuple key.
    /// </summary>
    /// <param name="key">The TypeTuple key containing the source type and table name.</param>
    /// <returns>An array of column names according to the schema.</returns>
    /// <exception cref="BufferConfigurationException">Thrown when the type map for the specified key is not found.</exception>
    public string[] GetMappedColumns(in TypeTuple key)
    {
        return _columnsMap.GetOrAdd(key, static (k, rules) =>
        {
            if (!rules.TryGetValue(k, out var settings))
                throw new BufferConfigurationException($"The type map '{k.Source.Name}' to '{k.TableName}' was not found");

            return settings.Resolvers.Select(x => x.ColumnName).ToArray();
        }, _rulesMap);
    }

    /// <summary>
    /// Scans and registers mappings from specified assemblies.
    /// </summary>
    /// <param name="assemblies">Assemblies to scan.</param>
    /// <returns>A list of registered mappings</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of the types implementing ITableSchema cannot be instantiated.</exception>
    [RequiresUnreferencedCode("assembly.GetLoadableTypes() requires unreferenced code")]
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
    /// Applies type mappings from the provided collection of ITableSchema implementations.
    /// </summary>
    /// <param name="registers">Collection of ITableSchema implementations to apply mappings from.</param>
    public void Apply(IEnumerable<ITableSchema> registers)
    {
        foreach (ITableSchema register in registers)
        {
            register.Register(this);
        }
    }
}
