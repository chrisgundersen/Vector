using Vector.Domain.Common;

namespace Vector.Application.Common.Interfaces;

/// <summary>
/// Interface for integrating with external Policy Admin Systems (PAS).
/// </summary>
public interface IExternalPolicyService
{
    /// <summary>
    /// Creates a policy in the external PAS from a bound submission.
    /// </summary>
    /// <param name="request">The policy creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the external policy reference.</returns>
    Task<Result<PolicyCreationResult>> CreatePolicyAsync(
        PolicyCreationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates policy information in the external PAS.
    /// </summary>
    /// <param name="externalPolicyId">The external policy identifier.</param>
    /// <param name="request">The policy update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> UpdatePolicyAsync(
        string externalPolicyId,
        PolicyUpdateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves policy status from the external PAS.
    /// </summary>
    /// <param name="externalPolicyId">The external policy identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The policy status.</returns>
    Task<Result<ExternalPolicyStatus>> GetPolicyStatusAsync(
        string externalPolicyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a policy in the external PAS.
    /// </summary>
    /// <param name="externalPolicyId">The external policy identifier.</param>
    /// <param name="reason">The cancellation reason.</param>
    /// <param name="effectiveDate">The effective date of cancellation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> CancelPolicyAsync(
        string externalPolicyId,
        string reason,
        DateTime effectiveDate,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request to create a policy in the external PAS.
/// </summary>
public sealed record PolicyCreationRequest(
    Guid SubmissionId,
    string SubmissionNumber,
    string InsuredName,
    string? InsuredFein,
    AddressInfo? InsuredAddress,
    DateTime EffectiveDate,
    DateTime ExpirationDate,
    decimal Premium,
    string Currency,
    IReadOnlyList<PolicyCoverageInfo> Coverages,
    IReadOnlyList<PolicyLocationInfo> Locations,
    string? ProducerCode,
    string? UnderwriterCode,
    IDictionary<string, string>? AdditionalData);

/// <summary>
/// Address information for PAS integration.
/// </summary>
public sealed record AddressInfo(
    string Street1,
    string? Street2,
    string City,
    string State,
    string PostalCode,
    string Country);

/// <summary>
/// Coverage information for PAS integration.
/// </summary>
public sealed record PolicyCoverageInfo(
    string CoverageType,
    decimal Limit,
    decimal Deductible,
    decimal? Premium);

/// <summary>
/// Location information for PAS integration.
/// </summary>
public sealed record PolicyLocationInfo(
    int LocationNumber,
    AddressInfo Address,
    string? BuildingDescription,
    string? OccupancyType,
    string? ConstructionType,
    int? YearBuilt,
    decimal BuildingValue,
    decimal ContentsValue,
    decimal BusinessIncomeValue,
    decimal TotalInsuredValue);

/// <summary>
/// Result of policy creation in the external PAS.
/// </summary>
public sealed record PolicyCreationResult(
    string ExternalPolicyId,
    string PolicyNumber,
    DateTime CreatedAt,
    string? ExternalSystemReference);

/// <summary>
/// Request to update a policy in the external PAS.
/// </summary>
public sealed record PolicyUpdateRequest(
    string? InsuredName,
    AddressInfo? InsuredAddress,
    DateTime? EffectiveDate,
    DateTime? ExpirationDate,
    decimal? Premium,
    IReadOnlyList<PolicyCoverageInfo>? Coverages,
    IReadOnlyList<PolicyLocationInfo>? Locations,
    IDictionary<string, string>? AdditionalData);

/// <summary>
/// Status of a policy in the external PAS.
/// </summary>
public sealed record ExternalPolicyStatus(
    string ExternalPolicyId,
    string PolicyNumber,
    string Status,
    DateTime? EffectiveDate,
    DateTime? ExpirationDate,
    DateTime? CancellationDate,
    decimal? WrittenPremium,
    DateTime LastUpdated);
