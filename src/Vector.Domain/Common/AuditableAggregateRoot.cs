namespace Vector.Domain.Common;

/// <summary>
/// Base class for aggregate roots that track audit information.
/// </summary>
/// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
public abstract class AuditableAggregateRoot<TId> : AggregateRoot<TId>, IAuditableEntity
    where TId : notnull
{
    public DateTime CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTime? LastModifiedAt { get; private set; }
    public string? LastModifiedBy { get; private set; }

    protected AuditableAggregateRoot()
    {
    }

    protected AuditableAggregateRoot(TId id) : base(id)
    {
    }

    /// <summary>
    /// Sets the creation audit information.
    /// Called by infrastructure during persistence.
    /// </summary>
    public void SetCreatedAudit(string? userId)
    {
        CreatedAt = DateTime.UtcNow;
        CreatedBy = userId;
    }

    /// <summary>
    /// Sets the modification audit information.
    /// Called by infrastructure during persistence.
    /// </summary>
    public void SetModifiedAudit(string? userId)
    {
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = userId;
    }
}

/// <summary>
/// Base class for aggregate roots with GUID identifier that track audit information.
/// </summary>
public abstract class AuditableAggregateRoot : AuditableAggregateRoot<Guid>
{
    protected AuditableAggregateRoot() : base(Guid.NewGuid())
    {
    }

    protected AuditableAggregateRoot(Guid id) : base(id)
    {
    }
}
