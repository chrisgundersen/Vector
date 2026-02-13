using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines.Enums;

namespace Vector.Domain.UnderwritingGuidelines.ValueObjects;

/// <summary>
/// Represents a condition in an underwriting rule.
/// </summary>
public class RuleCondition : ValueObject
{
    public RuleField Field { get; }
    public RuleOperator Operator { get; }
    public string Value { get; }
    public string? SecondaryValue { get; } // For Between operator

    private RuleCondition(
        RuleField field,
        RuleOperator @operator,
        string value,
        string? secondaryValue)
    {
        Field = field;
        Operator = @operator;
        Value = value;
        SecondaryValue = secondaryValue;
    }

    public static Result<RuleCondition> Create(
        RuleField field,
        RuleOperator @operator,
        string value,
        string? secondaryValue = null)
    {
        if (string.IsNullOrWhiteSpace(value) &&
            @operator != RuleOperator.IsEmpty &&
            @operator != RuleOperator.IsNotEmpty)
        {
            return Result.Failure<RuleCondition>(new Error(
                "RuleCondition.ValueRequired",
                "Value is required for this operator."));
        }

        if (@operator == RuleOperator.Between && string.IsNullOrWhiteSpace(secondaryValue))
        {
            return Result.Failure<RuleCondition>(new Error(
                "RuleCondition.SecondaryValueRequired",
                "Secondary value is required for Between operator."));
        }

        return Result.Success(new RuleCondition(field, @operator, value, secondaryValue));
    }

    /// <summary>
    /// Creates a simple equals condition.
    /// </summary>
    public static RuleCondition Equals(RuleField field, string value)
        => new(field, RuleOperator.Equals, value, null);

    /// <summary>
    /// Creates a greater than condition.
    /// </summary>
    public static RuleCondition GreaterThan(RuleField field, string value)
        => new(field, RuleOperator.GreaterThan, value, null);

    /// <summary>
    /// Creates a less than condition.
    /// </summary>
    public static RuleCondition LessThan(RuleField field, string value)
        => new(field, RuleOperator.LessThan, value, null);

    /// <summary>
    /// Creates an "in list" condition.
    /// </summary>
    public static RuleCondition In(RuleField field, params string[] values)
        => new(field, RuleOperator.In, string.Join(",", values), null);

    /// <summary>
    /// Creates a between condition.
    /// </summary>
    public static RuleCondition Between(RuleField field, string minValue, string maxValue)
        => new(field, RuleOperator.Between, minValue, maxValue);

    /// <summary>
    /// Evaluates the condition against a value.
    /// </summary>
    public bool Evaluate(string? actualValue)
    {
        return Operator switch
        {
            RuleOperator.IsEmpty => string.IsNullOrWhiteSpace(actualValue),
            RuleOperator.IsNotEmpty => !string.IsNullOrWhiteSpace(actualValue),
            RuleOperator.Equals => string.Equals(actualValue, Value, StringComparison.OrdinalIgnoreCase),
            RuleOperator.NotEquals => !string.Equals(actualValue, Value, StringComparison.OrdinalIgnoreCase),
            RuleOperator.Contains => actualValue?.Contains(Value, StringComparison.OrdinalIgnoreCase) ?? false,
            RuleOperator.StartsWith => actualValue?.StartsWith(Value, StringComparison.OrdinalIgnoreCase) ?? false,
            RuleOperator.EndsWith => actualValue?.EndsWith(Value, StringComparison.OrdinalIgnoreCase) ?? false,
            RuleOperator.In => EvaluateInOperator(actualValue),
            RuleOperator.NotIn => !EvaluateInOperator(actualValue),
            RuleOperator.GreaterThan => EvaluateNumericComparison(actualValue, (a, b) => a > b),
            RuleOperator.GreaterThanOrEqual => EvaluateNumericComparison(actualValue, (a, b) => a >= b),
            RuleOperator.LessThan => EvaluateNumericComparison(actualValue, (a, b) => a < b),
            RuleOperator.LessThanOrEqual => EvaluateNumericComparison(actualValue, (a, b) => a <= b),
            RuleOperator.Between => EvaluateBetween(actualValue),
            _ => false
        };
    }

    private bool EvaluateInOperator(string? actualValue)
    {
        if (string.IsNullOrWhiteSpace(actualValue)) return false;

        var values = Value.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim())
            .ToList();

        return values.Any(v => string.Equals(v, actualValue, StringComparison.OrdinalIgnoreCase));
    }

    private bool EvaluateNumericComparison(string? actualValue, Func<decimal, decimal, bool> comparison)
    {
        if (!decimal.TryParse(actualValue, out var actual)) return false;
        if (!decimal.TryParse(Value, out var expected)) return false;

        return comparison(actual, expected);
    }

    private bool EvaluateBetween(string? actualValue)
    {
        if (!decimal.TryParse(actualValue, out var actual)) return false;
        if (!decimal.TryParse(Value, out var min)) return false;
        if (!decimal.TryParse(SecondaryValue, out var max)) return false;

        return actual >= min && actual <= max;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Field;
        yield return Operator;
        yield return Value;
        yield return SecondaryValue;
    }

    public override string ToString()
        => SecondaryValue is not null
            ? $"{Field} {Operator} {Value} AND {SecondaryValue}"
            : $"{Field} {Operator} {Value}";
}
