namespace Vector.Domain.Common;

/// <summary>
/// Base class for aggregate roots with domain event support.
/// Aggregates are consistency boundaries that encapsulate domain logic and invariants.
/// </summary>
/// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the collection of domain events raised by this aggregate.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot()
    {
    }

    protected AggregateRoot(TId id) : base(id)
    {
    }

    /// <summary>
    /// Adds a domain event to be dispatched after the aggregate is persisted.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Removes a domain event from the collection.
    /// </summary>
    /// <param name="domainEvent">The domain event to remove.</param>
    protected void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Clears all domain events from the collection.
    /// Called by infrastructure after events have been dispatched.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

/// <summary>
/// Base class for aggregate roots with a GUID identifier.
/// </summary>
public abstract class AggregateRoot : AggregateRoot<Guid>
{
    protected AggregateRoot() : base(Guid.NewGuid())
    {
    }

    protected AggregateRoot(Guid id) : base(id)
    {
    }
}
