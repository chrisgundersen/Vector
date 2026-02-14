using Vector.Domain.Common;

namespace Vector.Application.Common.Interfaces;

/// <summary>
/// Interface for integrating with external CRM/Agency Management systems.
/// </summary>
public interface IExternalCrmService
{
    /// <summary>
    /// Syncs a producer/agency to the external CRM.
    /// </summary>
    /// <param name="producer">The producer information to sync.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the external CRM reference.</returns>
    Task<Result<CrmSyncResult>> SyncProducerAsync(
        ProducerSyncRequest producer,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs customer/insured information to the external CRM.
    /// </summary>
    /// <param name="customer">The customer information to sync.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the external CRM reference.</returns>
    Task<Result<CrmSyncResult>> SyncCustomerAsync(
        CustomerSyncRequest customer,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a submission event/activity in the CRM.
    /// </summary>
    /// <param name="activity">The activity to record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> RecordActivityAsync(
        CrmActivityRequest activity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves producer information from the external CRM.
    /// </summary>
    /// <param name="externalProducerId">The external CRM producer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The producer information.</returns>
    Task<Result<ExternalProducerInfo>> GetProducerAsync(
        string externalProducerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves customer information from the external CRM.
    /// </summary>
    /// <param name="externalCustomerId">The external CRM customer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The customer information.</returns>
    Task<Result<ExternalCustomerInfo>> GetCustomerAsync(
        string externalCustomerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for producers in the external CRM.
    /// </summary>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="maxResults">Maximum number of results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching producers.</returns>
    Task<Result<IReadOnlyList<ExternalProducerInfo>>> SearchProducersAsync(
        string searchTerm,
        int maxResults = 20,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request to sync a producer to the external CRM.
/// </summary>
public sealed record ProducerSyncRequest(
    Guid InternalProducerId,
    string? ExternalProducerId,
    string ProducerName,
    string? ProducerCode,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    AddressInfo? Address,
    bool IsActive,
    IDictionary<string, string>? AdditionalData);

/// <summary>
/// Request to sync a customer to the external CRM.
/// </summary>
public sealed record CustomerSyncRequest(
    Guid InternalCustomerId,
    string? ExternalCustomerId,
    string CustomerName,
    string? DbaName,
    string? FeinNumber,
    string? Industry,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    AddressInfo? Address,
    IDictionary<string, string>? AdditionalData);

/// <summary>
/// Request to record an activity in the CRM.
/// </summary>
public sealed record CrmActivityRequest(
    string? ExternalProducerId,
    string? ExternalCustomerId,
    string ActivityType,
    string Subject,
    string? Description,
    DateTime ActivityDate,
    string? ReferenceNumber,
    IDictionary<string, string>? AdditionalData);

/// <summary>
/// Result of a CRM sync operation.
/// </summary>
public sealed record CrmSyncResult(
    string ExternalId,
    bool WasCreated,
    DateTime SyncedAt,
    string? ExternalSystemReference);

/// <summary>
/// Producer information from the external CRM.
/// </summary>
public sealed record ExternalProducerInfo(
    string ExternalProducerId,
    string ProducerName,
    string? ProducerCode,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? Address,
    bool IsActive,
    DateTime LastUpdated);

/// <summary>
/// Customer information from the external CRM.
/// </summary>
public sealed record ExternalCustomerInfo(
    string ExternalCustomerId,
    string CustomerName,
    string? DbaName,
    string? Industry,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? Address,
    DateTime LastUpdated);
