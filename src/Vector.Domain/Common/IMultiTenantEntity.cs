namespace Vector.Domain.Common;

/// <summary>
/// Interface for entities that support multi-tenancy.
/// </summary>
public interface IMultiTenantEntity
{
    /// <summary>
    /// Gets the tenant identifier for this entity.
    /// </summary>
    Guid TenantId { get; }
}
