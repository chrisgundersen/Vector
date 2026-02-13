namespace Vector.Domain.Common;

/// <summary>
/// Interface for unit of work pattern providing transactional boundaries.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all changes made in this unit of work to the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of state entries written to the data store.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
