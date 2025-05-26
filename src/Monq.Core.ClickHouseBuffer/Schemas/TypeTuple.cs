using System;

namespace Monq.Core.ClickHouseBuffer.Schemas;

/// <summary>
/// The struct that handles pairs if Type -> TableName for schemas.
/// </summary>
public readonly struct TypeTuple : IEquatable<TypeTuple>
{
    /// <inheritdoc />
    public bool Equals(TypeTuple other)
    {
        return Source == other.Source && TableName == other.TableName;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (!(obj is TypeTuple))
            return false;
        return Equals((TypeTuple)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Source.GetHashCode() << 16 ^ TableName.GetHashCode() & 65535;
    }

    /// <inheritdoc />
    public static bool operator ==(TypeTuple left, TypeTuple right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc />
    public static bool operator !=(TypeTuple left, TypeTuple right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// The source object type.
    /// </summary>
    public Type Source { get; }

    /// <summary>
    /// The clickHouse table name.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Create a new object of <see cref="TypeTuple"/>.
    /// </summary>
    /// <param name="source">The source object type.</param>
    /// <param name="tableName">The clickHouse table name.</param>
    public TypeTuple(Type source, string tableName)
    {
        Source = source;
        TableName = tableName;
    }
}
