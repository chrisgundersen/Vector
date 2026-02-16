using Vector.Domain.Common;
using Vector.Domain.Routing.Enums;
using Vector.Domain.Routing.ValueObjects;
using Vector.Domain.UnderwritingGuidelines.Enums;

namespace Vector.Domain.Routing.Aggregates;

/// <summary>
/// Aggregate root for routing rules that determine how submissions are assigned to underwriters.
/// </summary>
public sealed class RoutingRule : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Priority { get; private set; }
    public RoutingRuleStatus Status { get; private set; }
    public RoutingStrategy Strategy { get; private set; }
    public Guid? TargetUnderwriterId { get; private set; }
    public string? TargetUnderwriterName { get; private set; }
    public Guid? TargetTeamId { get; private set; }
    public string? TargetTeamName { get; private set; }

    private readonly List<RoutingCondition> _conditions = [];
    public IReadOnlyCollection<RoutingCondition> Conditions => _conditions.AsReadOnly();

    public DateTime CreatedAt { get; private set; }
    public DateTime? LastModifiedAt { get; private set; }
    public string? CreatedBy { get; private set; }

    private RoutingRule()
    {
    }

    private RoutingRule(Guid id, string name, string description, RoutingStrategy strategy)
        : base(id)
    {
        Name = name;
        Description = description;
        Strategy = strategy;
        Status = RoutingRuleStatus.Draft;
        Priority = 100;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<RoutingRule> Create(
        string name,
        string description,
        RoutingStrategy strategy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<RoutingRule>(RoutingRuleErrors.NameRequired);
        }

        if (name.Length > 200)
        {
            return Result.Failure<RoutingRule>(RoutingRuleErrors.NameTooLong);
        }

        var rule = new RoutingRule(Guid.NewGuid(), name.Trim(), description?.Trim() ?? string.Empty, strategy);
        rule.AddDomainEvent(new RoutingRuleCreatedEvent(rule.Id, rule.Name, rule.Strategy));

        return Result.Success(rule);
    }

    public void UpdateDetails(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Name cannot exceed 200 characters.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void SetStrategy(RoutingStrategy strategy)
    {
        Strategy = strategy;
        LastModifiedAt = DateTime.UtcNow;
    }

    public Result AddCondition(RoutingCondition condition)
    {
        if (_conditions.Contains(condition))
        {
            return Result.Failure(RoutingRuleErrors.DuplicateCondition);
        }

        _conditions.Add(condition);
        LastModifiedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public void RemoveCondition(RoutingCondition condition)
    {
        _conditions.Remove(condition);
        LastModifiedAt = DateTime.UtcNow;
    }

    public void ClearConditions()
    {
        _conditions.Clear();
        LastModifiedAt = DateTime.UtcNow;
    }

    public void SetPriority(int priority)
    {
        if (priority < 1 || priority > 1000)
            throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be between 1 and 1000.");

        Priority = priority;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void SetTargetUnderwriter(Guid underwriterId, string underwriterName)
    {
        TargetUnderwriterId = underwriterId;
        TargetUnderwriterName = underwriterName;
        TargetTeamId = null;
        TargetTeamName = null;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void SetTargetTeam(Guid teamId, string teamName)
    {
        TargetTeamId = teamId;
        TargetTeamName = teamName;
        TargetUnderwriterId = null;
        TargetUnderwriterName = null;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void ClearTarget()
    {
        TargetUnderwriterId = null;
        TargetUnderwriterName = null;
        TargetTeamId = null;
        TargetTeamName = null;
        LastModifiedAt = DateTime.UtcNow;
    }

    public Result Activate()
    {
        if (Status == RoutingRuleStatus.Active)
        {
            return Result.Failure(RoutingRuleErrors.AlreadyActive);
        }

        // Direct routing requires a target underwriter
        if (Strategy == RoutingStrategy.Direct && TargetUnderwriterId is null)
        {
            return Result.Failure(RoutingRuleErrors.TargetRequired);
        }

        Status = RoutingRuleStatus.Active;
        LastModifiedAt = DateTime.UtcNow;
        AddDomainEvent(new RoutingRuleActivatedEvent(Id, Name));

        return Result.Success();
    }

    public void Deactivate()
    {
        if (Status != RoutingRuleStatus.Active) return;

        Status = RoutingRuleStatus.Inactive;
        LastModifiedAt = DateTime.UtcNow;
        AddDomainEvent(new RoutingRuleDeactivatedEvent(Id, Name));
    }

    public void Archive()
    {
        Status = RoutingRuleStatus.Archived;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Evaluates whether this rule matches the given field values.
    /// </summary>
    public bool Matches(IReadOnlyDictionary<RuleField, string?> fieldValues)
    {
        if (Status != RoutingRuleStatus.Active) return false;
        if (_conditions.Count == 0) return true; // No conditions = matches all

        // All conditions must match (AND logic)
        return _conditions.All(condition =>
        {
            fieldValues.TryGetValue(condition.Field, out var actualValue);
            return condition.Evaluate(actualValue);
        });
    }
}

public static class RoutingRuleErrors
{
    public static readonly Error NameRequired = new("RoutingRule.NameRequired", "Routing rule name is required.");
    public static readonly Error NameTooLong = new("RoutingRule.NameTooLong", "Routing rule name cannot exceed 200 characters.");
    public static readonly Error DuplicateCondition = new("RoutingRule.DuplicateCondition", "This condition already exists on the rule.");
    public static readonly Error AlreadyActive = new("RoutingRule.AlreadyActive", "Routing rule is already active.");
    public static readonly Error TargetRequired = new("RoutingRule.TargetRequired", "Direct routing strategy requires a target underwriter.");
}

public sealed record RoutingRuleCreatedEvent(
    Guid RuleId,
    string RuleName,
    RoutingStrategy Strategy) : DomainEvent;

public sealed record RoutingRuleActivatedEvent(
    Guid RuleId,
    string RuleName) : DomainEvent;

public sealed record RoutingRuleDeactivatedEvent(
    Guid RuleId,
    string RuleName) : DomainEvent;
