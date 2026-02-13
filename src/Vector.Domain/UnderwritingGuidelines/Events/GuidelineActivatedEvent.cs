using Vector.Domain.Common;

namespace Vector.Domain.UnderwritingGuidelines.Events;

/// <summary>
/// Event raised when a guideline is activated.
/// </summary>
public sealed record GuidelineActivatedEvent(Guid GuidelineId, Guid TenantId) : DomainEvent;
