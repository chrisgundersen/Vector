namespace Vector.Domain.Common;

/// <summary>
/// Generic repository interface for aggregate root persistence.
/// </summary>
/// <typeparam name="TAggregate">The type of the aggregate root.</typeparam>
/// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
public interface IRepository<TAggregate, in TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    /// <summary>
    /// Gets the unit of work associated with this repository.
    /// </summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Retrieves an aggregate by its identifier.
    /// </summary>
    /// <param name="id">The aggregate identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The aggregate if found; otherwise, null.</returns>
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new aggregate to the repository.
    /// </summary>
    /// <param name="aggregate">The aggregate to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing aggregate in the repository.
    /// </summary>
    /// <param name="aggregate">The aggregate to update.</param>
    void Update(TAggregate aggregate);

    /// <summary>
    /// Removes an aggregate from the repository.
    /// </summary>
    /// <param name="aggregate">The aggregate to remove.</param>
    void Remove(TAggregate aggregate);
}

/// <summary>
/// Generic repository interface for aggregate roots with GUID identifiers.
/// </summary>
/// <typeparam name="TAggregate">The type of the aggregate root.</typeparam>
public interface IRepository<TAggregate> : IRepository<TAggregate, Guid>
    where TAggregate : AggregateRoot<Guid>
{
}
