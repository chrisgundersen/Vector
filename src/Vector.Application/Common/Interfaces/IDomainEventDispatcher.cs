using Vector.Domain.Common;

namespace Vector.Application.Common.Interfaces;

/// <summary>
/// Dispatches domain events after aggregate persistence.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
