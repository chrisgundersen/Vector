using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Application.DocumentProcessing.DTOs;
using Vector.Domain.DocumentProcessing;

namespace Vector.Application.DocumentProcessing.Queries;

/// <summary>
/// Handler for GetProcessingJobQuery.
/// </summary>
public sealed class GetProcessingJobQueryHandler(
    IProcessingJobRepository repository,
    ILogger<GetProcessingJobQueryHandler> logger) : IRequestHandler<GetProcessingJobQuery, ProcessingJobDto?>
{
    public async Task<ProcessingJobDto?> Handle(
        GetProcessingJobQuery request,
        CancellationToken cancellationToken)
    {
        var job = await repository.GetByIdAsync(request.JobId, cancellationToken);

        if (job is null)
        {
            logger.LogDebug("Processing job {JobId} not found", request.JobId);
            return null;
        }

        return job.ToDto();
    }
}
