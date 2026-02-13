namespace Vector.Domain.Common;

/// <summary>
/// Base class for value objects with structural equality.
/// Value objects are immutable and compared by their component values rather than identity.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Gets the components used for equality comparison.
    /// </summary>
    /// <returns>An enumerable of equality components.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((ValueObject)obj);
    }

    public bool Equals(ValueObject? other)
    {
        if (other is null)
        {
            return false;
        }

        return GetEqualityComponents()
            .SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !Equals(left, right);
    }
}
