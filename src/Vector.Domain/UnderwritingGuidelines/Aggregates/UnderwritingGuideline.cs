using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines.Entities;
using Vector.Domain.UnderwritingGuidelines.Enums;
using Vector.Domain.UnderwritingGuidelines.Events;

namespace Vector.Domain.UnderwritingGuidelines.Aggregates;

/// <summary>
/// Represents an underwriting guideline that defines appetite and eligibility rules.
/// </summary>
public class UnderwritingGuideline : AuditableAggregateRoot, IMultiTenantEntity
{
    private readonly List<UnderwritingRule> _rules = [];

    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public GuidelineStatus Status { get; private set; }
    public DateTime? EffectiveDate { get; private set; }
    public DateTime? ExpirationDate { get; private set; }
    public int Version { get; private set; }

    /// <summary>
    /// Coverage types this guideline applies to.
    /// </summary>
    public string? ApplicableCoverageTypes { get; private set; }

    /// <summary>
    /// States/regions this guideline applies to (comma-separated).
    /// </summary>
    public string? ApplicableStates { get; private set; }

    /// <summary>
    /// NAICS codes this guideline applies to (comma-separated prefixes).
    /// </summary>
    public string? ApplicableNAICSCodes { get; private set; }

    public IReadOnlyCollection<UnderwritingRule> Rules => _rules.AsReadOnly();

    // Required by EF Core
    private UnderwritingGuideline() : base(Guid.NewGuid())
    {
        Name = string.Empty;
    }

    private UnderwritingGuideline(Guid tenantId, string name) : base(Guid.NewGuid())
    {
        TenantId = tenantId;
        Name = name;
        Status = GuidelineStatus.Draft;
        Version = 1;
    }

    public static UnderwritingGuideline Create(Guid tenantId, string name)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        var guideline = new UnderwritingGuideline(tenantId, name.Trim());
        guideline.AddDomainEvent(new GuidelineCreatedEvent(guideline.Id, tenantId, name));

        return guideline;
    }

    public void UpdateDetails(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
    }

    public void SetEffectiveDates(DateTime? effectiveDate, DateTime? expirationDate)
    {
        if (effectiveDate.HasValue && expirationDate.HasValue && effectiveDate > expirationDate)
        {
            throw new ArgumentException("Effective date must be before expiration date.");
        }

        EffectiveDate = effectiveDate;
        ExpirationDate = expirationDate;
    }

    public void SetApplicability(
        string? coverageTypes = null,
        string? states = null,
        string? naicsCodes = null)
    {
        ApplicableCoverageTypes = coverageTypes?.Trim();
        ApplicableStates = states?.ToUpperInvariant().Trim();
        ApplicableNAICSCodes = naicsCodes?.Trim();
    }

    public void Activate()
    {
        if (Status == GuidelineStatus.Active) return;

        if (_rules.Count == 0)
        {
            throw new InvalidOperationException("Cannot activate guideline with no rules.");
        }

        Status = GuidelineStatus.Active;
        AddDomainEvent(new GuidelineActivatedEvent(Id, TenantId));
    }

    public void Deactivate()
    {
        if (Status != GuidelineStatus.Active) return;

        Status = GuidelineStatus.Inactive;
        AddDomainEvent(new GuidelineDeactivatedEvent(Id, TenantId));
    }

    public void Archive()
    {
        Status = GuidelineStatus.Archived;
    }

    public UnderwritingRule AddRule(
        string name,
        RuleType type,
        RuleAction action,
        int priority = 0)
    {
        if (Status == GuidelineStatus.Archived)
        {
            throw new InvalidOperationException("Cannot modify an archived guideline.");
        }

        var rule = UnderwritingRule.Create(name, type, action, priority);
        _rules.Add(rule);

        IncrementVersion();
        AddDomainEvent(new RuleAddedEvent(Id, rule.Id, rule.Name, rule.Type));

        return rule;
    }

    public void RemoveRule(Guid ruleId)
    {
        if (Status == GuidelineStatus.Archived)
        {
            throw new InvalidOperationException("Cannot modify an archived guideline.");
        }

        var rule = _rules.FirstOrDefault(r => r.Id == ruleId);
        if (rule is null) return;

        _rules.Remove(rule);
        IncrementVersion();
        AddDomainEvent(new RuleRemovedEvent(Id, ruleId));
    }

    public UnderwritingRule? GetRule(Guid ruleId)
        => _rules.FirstOrDefault(r => r.Id == ruleId);

    public IEnumerable<UnderwritingRule> GetRulesByType(RuleType type)
        => _rules.Where(r => r.Type == type && r.IsActive).OrderBy(r => r.Priority);

    public IEnumerable<UnderwritingRule> GetActiveRules()
        => _rules.Where(r => r.IsActive).OrderBy(r => r.Priority);

    /// <summary>
    /// Checks if this guideline is applicable for a given submission context.
    /// </summary>
    public bool IsApplicable(
        string? coverageType = null,
        string? state = null,
        string? naicsCode = null)
    {
        if (Status != GuidelineStatus.Active) return false;

        // Check effective dates
        var now = DateTime.UtcNow;
        if (EffectiveDate.HasValue && now < EffectiveDate.Value) return false;
        if (ExpirationDate.HasValue && now > ExpirationDate.Value) return false;

        // Check coverage type applicability
        if (!string.IsNullOrEmpty(ApplicableCoverageTypes) && !string.IsNullOrEmpty(coverageType))
        {
            var types = ApplicableCoverageTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim());
            if (!types.Any(t => t.Equals(coverageType, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        // Check state applicability
        if (!string.IsNullOrEmpty(ApplicableStates) && !string.IsNullOrEmpty(state))
        {
            var states = ApplicableStates.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim());
            if (!states.Any(s => s.Equals(state, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        // Check NAICS applicability (prefix match)
        if (!string.IsNullOrEmpty(ApplicableNAICSCodes) && !string.IsNullOrEmpty(naicsCode))
        {
            var codes = ApplicableNAICSCodes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim());
            if (!codes.Any(c => naicsCode.StartsWith(c, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Evaluates all active rules against the provided field values.
    /// </summary>
    /// <returns>List of rules that matched.</returns>
    public IEnumerable<RuleEvaluationResult> Evaluate(IDictionary<RuleField, string?> fieldValues)
    {
        var results = new List<RuleEvaluationResult>();

        foreach (var rule in GetActiveRules())
        {
            var matched = rule.Evaluate(fieldValues);
            if (matched)
            {
                results.Add(new RuleEvaluationResult(
                    rule.Id,
                    rule.Name,
                    rule.Type,
                    rule.Action,
                    rule.ScoreAdjustment,
                    rule.PricingModifier,
                    rule.Message));
            }
        }

        return results;
    }

    private void IncrementVersion()
    {
        Version++;
    }
}

/// <summary>
/// Result of evaluating a rule.
/// </summary>
public record RuleEvaluationResult(
    Guid RuleId,
    string RuleName,
    RuleType Type,
    RuleAction Action,
    int? ScoreAdjustment,
    decimal? PricingModifier,
    string? Message);
