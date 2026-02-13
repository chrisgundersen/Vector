namespace Vector.Domain.Common;

/// <summary>
/// Interface for domain event handlers.
/// </summary>
/// <typeparam name="TEvent">The type of domain event to handle.</typeparam>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
