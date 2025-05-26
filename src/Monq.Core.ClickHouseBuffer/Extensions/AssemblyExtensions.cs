using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Monq.Core.ClickHouseBuffer.Extensions;

/// <summary>
/// The collection of the <see cref="Assembly"/> class extensions to configure ClickHouseBuffer schema.
/// </summary>
public static class AssemblyExtensions
{
    /// <summary>
    /// Get loadable types from the <paramref name="assembly"/>.
    /// </summary>
    /// <param name="assembly">The assembly from which the ITableSchema interfaces are searched.</param>
    /// <returns></returns>
    public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null).Cast<Type>();
        }
    }
}
