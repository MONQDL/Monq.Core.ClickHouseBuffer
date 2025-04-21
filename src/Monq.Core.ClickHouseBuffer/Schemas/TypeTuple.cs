using System;

namespace Monq.Core.ClickHouseBuffer.Schemas;

public readonly struct TypeTuple : IEquatable<TypeTuple>
{
    public bool Equals(TypeTuple other)
    {
        return Source == other.Source && TableName == other.TableName;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is TypeTuple))
            return false;
        return Equals((TypeTuple)obj);
    }

    public override int GetHashCode()
    {
        return Source.GetHashCode() << 16 ^ TableName.GetHashCode() & 65535;
    }

    public static bool operator ==(TypeTuple left, TypeTuple right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TypeTuple left, TypeTuple right)
    {
        return !left.Equals(right);
    }

    public Type Source { get; }
    public string TableName { get; }

    public TypeTuple(Type source, string tableName)
    {
        Source = source;
        TableName = tableName;
    }
}
