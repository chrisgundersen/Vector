using Vector.Domain.Common;

namespace Vector.Domain.UnderwritingGuidelines.Events;

/// <summary>
/// Event raised when a guideline is deactivated.
/// </summary>
public sealed record GuidelineDeactivatedEvent(Guid GuidelineId, Guid TenantId) : DomainEvent;
