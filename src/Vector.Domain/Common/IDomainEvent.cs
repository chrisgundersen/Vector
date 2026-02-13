namespace Vector.Domain.Common;

/// <summary>
/// Marker interface for domain events that capture business-significant state changes.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    DateTime OccurredAt { get; }
}
