using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines.Enums;

namespace Vector.Domain.UnderwritingGuidelines.Events;

/// <summary>
/// Event raised when a rule is added to a guideline.
/// </summary>
public sealed record RuleAddedEvent(
    Guid GuidelineId,
    Guid RuleId,
    string RuleName,
    RuleType RuleType) : DomainEvent;
