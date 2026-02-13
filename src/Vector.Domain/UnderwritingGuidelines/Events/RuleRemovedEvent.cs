using Vector.Domain.Common;

namespace Vector.Domain.UnderwritingGuidelines.Events;

/// <summary>
/// Event raised when a rule is removed from a guideline.
/// </summary>
public sealed record RuleRemovedEvent(Guid GuidelineId, Guid RuleId) : DomainEvent;
