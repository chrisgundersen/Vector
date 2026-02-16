using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Application.Common;
using Vector.Application.Common.Interfaces;
using Vector.Domain.Common;

namespace Vector.Infrastructure.Services;

/// <summary>
/// Dispatches domain events by wrapping them in <see cref="DomainEventNotification{TEvent}"/>
/// and publishing via MediatR.
/// </summary>
public sealed class MediatRDomainEventDispatcher(
    IPublisher publisher,
    ILogger<MediatRDomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public async Task DispatchEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            logger.LogInformation(
                "Dispatching domain event {EventType} (EventId: {EventId})",
                domainEvent.GetType().Name,
                domainEvent.EventId);

            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = Activator.CreateInstance(notificationType, domainEvent)
                ?? throw new InvalidOperationException($"Failed to create DomainEventNotification for {domainEvent.GetType().Name}");

            await publisher.Publish(notification, cancellationToken);
        }
    }
}
