using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines.Enums;
using Vector.Domain.UnderwritingGuidelines.ValueObjects;

namespace Vector.Domain.UnderwritingGuidelines.Entities;

/// <summary>
/// Represents a single underwriting rule within a guideline.
/// </summary>
public class UnderwritingRule : Entity
{
    private readonly List<RuleCondition> _conditions = [];

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public RuleType Type { get; private set; }
    public RuleAction Action { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>
    /// Score adjustment when action is AdjustScore (-100 to +100).
    /// </summary>
    public int? ScoreAdjustment { get; private set; }

    /// <summary>
    /// Pricing modifier when action is ApplyModifier (e.g., 1.25 = 25% increase).
    /// </summary>
    public decimal? PricingModifier { get; private set; }

    /// <summary>
    /// Message to display when rule is triggered.
    /// </summary>
    public string? Message { get; private set; }

    public IReadOnlyCollection<RuleCondition> Conditions => _conditions.AsReadOnly();

    // Required by EF Core
    private UnderwritingRule() : base(Guid.NewGuid())
    {
        Name = string.Empty;
    }

    private UnderwritingRule(
        Guid id,
        string name,
        RuleType type,
        RuleAction action,
        int priority) : base(id)
    {
        Name = name;
        Type = type;
        Action = action;
        Priority = priority;
        IsActive = true;
    }

    public static UnderwritingRule Create(
        string name,
        RuleType type,
        RuleAction action,
        int priority = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        return new UnderwritingRule(Guid.NewGuid(), name.Trim(), type, action, priority);
    }

    public void UpdateDetails(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
    }

    public void SetPriority(int priority)
    {
        Priority = priority;
    }

    public void SetScoreAdjustment(int adjustment)
    {
        if (adjustment is < -100 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(adjustment),
                "Score adjustment must be between -100 and 100.");
        }

        ScoreAdjustment = adjustment;
    }

    public void SetPricingModifier(decimal modifier)
    {
        if (modifier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(modifier),
                "Pricing modifier must be greater than 0.");
        }

        PricingModifier = modifier;
    }

    public void SetMessage(string? message)
    {
        Message = message?.Trim();
    }

    public void AddCondition(RuleCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        _conditions.Add(condition);
    }

    public void RemoveCondition(RuleCondition condition)
    {
        _conditions.Remove(condition);
    }

    public void ClearConditions()
    {
        _conditions.Clear();
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Evaluates if all conditions in this rule are satisfied.
    /// </summary>
    /// <param name="fieldValues">Dictionary of field values to evaluate against.</param>
    /// <returns>True if all conditions are satisfied, false otherwise.</returns>
    public bool Evaluate(IDictionary<RuleField, string?> fieldValues)
    {
        if (!IsActive) return false;
        if (_conditions.Count == 0) return true;

        return _conditions.All(condition =>
        {
            fieldValues.TryGetValue(condition.Field, out var value);
            return condition.Evaluate(value);
        });
    }
}
