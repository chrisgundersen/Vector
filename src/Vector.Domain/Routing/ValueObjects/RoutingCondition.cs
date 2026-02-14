using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines.Enums;

namespace Vector.Domain.Routing.ValueObjects;

/// <summary>
/// Represents a condition for routing rules.
/// </summary>
public class RoutingCondition : ValueObject
{
    public RuleField Field { get; private set; }
    public RuleOperator Operator { get; private set; }
    public string Value { get; private set; }
    public string? SecondaryValue { get; private set; }

    // EF Core constructor
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value
    private RoutingCondition() { }
#pragma warning restore CS8618

    private RoutingCondition(
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

    public static Result<RoutingCondition> Create(
        RuleField field,
        RuleOperator @operator,
        string value,
        string? secondaryValue = null)
    {
        if (string.IsNullOrWhiteSpace(value) &&
            @operator != RuleOperator.IsEmpty &&
            @operator != RuleOperator.IsNotEmpty)
        {
            return Result.Failure<RoutingCondition>(new Error(
                "RoutingCondition.ValueRequired",
                "Value is required for this operator."));
        }

        if (@operator == RuleOperator.Between && string.IsNullOrWhiteSpace(secondaryValue))
        {
            return Result.Failure<RoutingCondition>(new Error(
                "RoutingCondition.SecondaryValueRequired",
                "Secondary value is required for Between operator."));
        }

        return Result.Success(new RoutingCondition(field, @operator, value, secondaryValue));
    }

    public static RoutingCondition Equals(RuleField field, string value)
        => new(field, RuleOperator.Equals, value, null);

    public static RoutingCondition In(RuleField field, params string[] values)
        => new(field, RuleOperator.In, string.Join(",", values), null);

    public static RoutingCondition GreaterThan(RuleField field, string value)
        => new(field, RuleOperator.GreaterThan, value, null);

    public static RoutingCondition Between(RuleField field, string minValue, string maxValue)
        => new(field, RuleOperator.Between, minValue, maxValue);

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
