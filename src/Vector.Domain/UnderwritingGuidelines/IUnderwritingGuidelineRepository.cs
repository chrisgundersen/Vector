using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines.Aggregates;
using Vector.Domain.UnderwritingGuidelines.Enums;

namespace Vector.Domain.UnderwritingGuidelines;

/// <summary>
/// Repository interface for underwriting guidelines.
/// </summary>
public interface IUnderwritingGuidelineRepository : IRepository<UnderwritingGuideline>
{
    /// <summary>
    /// Gets a guideline by ID with all rules loaded.
    /// </summary>
    Task<UnderwritingGuideline?> GetByIdWithRulesAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active guidelines for a tenant.
    /// </summary>
    Task<IReadOnlyList<UnderwritingGuideline>> GetActiveForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets guidelines by status.
    /// </summary>
    Task<IReadOnlyList<UnderwritingGuideline>> GetByStatusAsync(
        Guid tenantId,
        GuidelineStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets applicable guidelines for a submission context.
    /// </summary>
    Task<IReadOnlyList<UnderwritingGuideline>> GetApplicableGuidelinesAsync(
        Guid tenantId,
        string? coverageType = null,
        string? state = null,
        string? naicsCode = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a guideline with the given name exists for the tenant.
    /// </summary>
    Task<bool> ExistsByNameAsync(
        Guid tenantId,
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);
}
