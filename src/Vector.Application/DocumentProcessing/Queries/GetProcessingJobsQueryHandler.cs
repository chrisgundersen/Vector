using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Application.DocumentProcessing.DTOs;
using Vector.Domain.DocumentProcessing;
using Vector.Domain.DocumentProcessing.Enums;

namespace Vector.Application.DocumentProcessing.Queries;

/// <summary>
/// Handler for GetProcessingJobsQuery.
/// </summary>
public sealed class GetProcessingJobsQueryHandler(
    IProcessingJobRepository repository,
    ILogger<GetProcessingJobsQueryHandler> logger) : IRequestHandler<GetProcessingJobsQuery, IReadOnlyList<ProcessingJobSummaryDto>>
{
    public async Task<IReadOnlyList<ProcessingJobSummaryDto>> Handle(
        GetProcessingJobsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Getting processing jobs - TenantId: {TenantId}, Status: {Status}, Limit: {Limit}",
            request.TenantId,
            request.Status,
            request.Limit);

        // If status is specified, use GetByStatusAsync
        if (request.Status.HasValue)
        {
            var jobsByStatus = await repository.GetByStatusAsync(
                request.Status.Value,
                request.Limit,
                cancellationToken);

            // Filter by tenant if specified
            var filtered = request.TenantId.HasValue
                ? jobsByStatus.Where(j => j.TenantId == request.TenantId.Value).ToList()
                : jobsByStatus;

            return filtered.Select(j => j.ToSummaryDto()).ToList();
        }

        // If tenant is specified, get active jobs for tenant
        if (request.TenantId.HasValue)
        {
            var tenantJobs = await repository.GetActiveJobsForTenantAsync(
                request.TenantId.Value,
                cancellationToken);

            return tenantJobs
                .Take(request.Limit)
                .Select(j => j.ToSummaryDto())
                .ToList();
        }

        // Default: get pending jobs
        var pendingJobs = await repository.GetByStatusAsync(
            ProcessingStatus.Pending,
            request.Limit,
            cancellationToken);

        return pendingJobs.Select(j => j.ToSummaryDto()).ToList();
    }
}
