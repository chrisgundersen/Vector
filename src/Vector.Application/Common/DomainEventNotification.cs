using MediatR;
using Vector.Domain.Common;

namespace Vector.Application.Common;

/// <summary>
/// MediatR notification wrapper for domain events, decoupling the Domain layer from MediatR.
/// </summary>
/// <typeparam name="TEvent">The concrete domain event type.</typeparam>
public record DomainEventNotification<TEvent>(TEvent DomainEvent) : INotification
    where TEvent : IDomainEvent;
