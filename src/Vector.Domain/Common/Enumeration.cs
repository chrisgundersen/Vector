using System.Reflection;

namespace Vector.Domain.Common;

/// <summary>
/// Base class for strongly-typed enumerations with additional behavior.
/// </summary>
/// <typeparam name="TEnum">The enumeration type.</typeparam>
public abstract class Enumeration<TEnum> : IEquatable<Enumeration<TEnum>>
    where TEnum : Enumeration<TEnum>
{
    private static readonly Dictionary<int, TEnum> Enumerations = CreateEnumerations();

    public int Value { get; protected init; }
    public string Name { get; protected init; } = string.Empty;

    protected Enumeration(int value, string name)
    {
        Value = value;
        Name = name;
    }

    public static TEnum? FromValue(int value)
    {
        return Enumerations.TryGetValue(value, out var enumeration)
            ? enumeration
            : null;
    }

    public static TEnum? FromName(string name)
    {
        return Enumerations.Values
            .SingleOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyCollection<TEnum> GetAll()
    {
        return Enumerations.Values.ToList().AsReadOnly();
    }

    public bool Equals(Enumeration<TEnum>? other)
    {
        if (other is null) return false;
        return GetType() == other.GetType() && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Enumeration<TEnum> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Name;
    }

    private static Dictionary<int, TEnum> CreateEnumerations()
    {
        var enumerationType = typeof(TEnum);

        var fieldsForType = enumerationType
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(fieldInfo => enumerationType.IsAssignableFrom(fieldInfo.FieldType))
            .Select(fieldInfo => (TEnum)fieldInfo.GetValue(default)!);

        return fieldsForType.ToDictionary(x => x.Value);
    }
}
