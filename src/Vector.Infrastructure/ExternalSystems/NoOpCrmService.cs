using Microsoft.Extensions.Logging;
using Vector.Application.Common.Interfaces;
using Vector.Domain.Common;

namespace Vector.Infrastructure.ExternalSystems;

/// <summary>
/// No-op implementation of IExternalCrmService for development and testing.
/// Logs operations but does not integrate with any real CRM.
/// </summary>
public sealed class NoOpCrmService(ILogger<NoOpCrmService> logger) : IExternalCrmService
{
    public Task<Result<CrmSyncResult>> SyncProducerAsync(
        ProducerSyncRequest producer,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "NoOp: Would sync producer {ProducerName} ({ProducerCode}) to CRM",
            producer.ProducerName, producer.ProducerCode);

        var result = new CrmSyncResult(
            ExternalId: producer.ExternalProducerId ?? $"CRM-P-{Guid.NewGuid():N}",
            WasCreated: producer.ExternalProducerId is null,
            SyncedAt: DateTime.UtcNow,
            ExternalSystemReference: "NoOp-CRM-Simulator");

        return Task.FromResult(Result.Success(result));
    }

    public Task<Result<CrmSyncResult>> SyncCustomerAsync(
        CustomerSyncRequest customer,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "NoOp: Would sync customer {CustomerName} to CRM",
            customer.CustomerName);

        var result = new CrmSyncResult(
            ExternalId: customer.ExternalCustomerId ?? $"CRM-C-{Guid.NewGuid():N}",
            WasCreated: customer.ExternalCustomerId is null,
            SyncedAt: DateTime.UtcNow,
            ExternalSystemReference: "NoOp-CRM-Simulator");

        return Task.FromResult(Result.Success(result));
    }

    public Task<Result> RecordActivityAsync(
        CrmActivityRequest activity,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "NoOp: Would record CRM activity '{ActivityType}': {Subject}",
            activity.ActivityType, activity.Subject);

        return Task.FromResult(Result.Success());
    }

    public Task<Result<ExternalProducerInfo>> GetProducerAsync(
        string externalProducerId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "NoOp: Would get producer {ExternalProducerId} from CRM",
            externalProducerId);

        var info = new ExternalProducerInfo(
            ExternalProducerId: externalProducerId,
            ProducerName: $"Sample Producer {externalProducerId[..8]}",
            ProducerCode: $"PRD-{externalProducerId[..6]}",
            ContactName: "John Doe",
            ContactEmail: "john.doe@example.com",
            ContactPhone: "(555) 123-4567",
            Address: "123 Main St, New York, NY 10001",
            IsActive: true,
            LastUpdated: DateTime.UtcNow);

        return Task.FromResult(Result.Success(info));
    }

    public Task<Result<ExternalCustomerInfo>> GetCustomerAsync(
        string externalCustomerId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "NoOp: Would get customer {ExternalCustomerId} from CRM",
            externalCustomerId);

        var info = new ExternalCustomerInfo(
            ExternalCustomerId: externalCustomerId,
            CustomerName: $"Sample Customer {externalCustomerId[..8]}",
            DbaName: null,
            Industry: "Manufacturing",
            ContactName: "Jane Smith",
            ContactEmail: "jane.smith@example.com",
            ContactPhone: "(555) 987-6543",
            Address: "456 Oak Ave, Chicago, IL 60601",
            LastUpdated: DateTime.UtcNow);

        return Task.FromResult(Result.Success(info));
    }

    public Task<Result<IReadOnlyList<ExternalProducerInfo>>> SearchProducersAsync(
        string searchTerm,
        int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "NoOp: Would search for producers matching '{SearchTerm}' in CRM",
            searchTerm);

        var results = new List<ExternalProducerInfo>
        {
            new(
                ExternalProducerId: $"CRM-P-{Guid.NewGuid():N}",
                ProducerName: $"{searchTerm} Insurance Agency",
                ProducerCode: "PRD-001",
                ContactName: "Agent One",
                ContactEmail: "agent1@example.com",
                ContactPhone: "(555) 111-1111",
                Address: "100 First St, Boston, MA 02101",
                IsActive: true,
                LastUpdated: DateTime.UtcNow)
        };

        return Task.FromResult(Result.Success<IReadOnlyList<ExternalProducerInfo>>(results));
    }
}
