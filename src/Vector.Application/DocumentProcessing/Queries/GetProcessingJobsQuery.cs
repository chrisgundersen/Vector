using MediatR;
using Vector.Application.DocumentProcessing.DTOs;
using Vector.Domain.DocumentProcessing.Enums;

namespace Vector.Application.DocumentProcessing.Queries;

/// <summary>
/// Query to get processing jobs with optional filters.
/// </summary>
public sealed record GetProcessingJobsQuery(
    Guid? TenantId = null,
    ProcessingStatus? Status = null,
    int Limit = 50) : IRequest<IReadOnlyList<ProcessingJobSummaryDto>>;
