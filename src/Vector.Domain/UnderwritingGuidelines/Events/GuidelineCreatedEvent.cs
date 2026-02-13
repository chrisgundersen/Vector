using Vector.Domain.Common;

namespace Vector.Domain.UnderwritingGuidelines.Events;

/// <summary>
/// Event raised when a new underwriting guideline is created.
/// </summary>
public sealed record GuidelineCreatedEvent(Guid GuidelineId, Guid TenantId, string Name) : DomainEvent;
