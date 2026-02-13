using MediatR;
using Vector.Application.Submissions.DTOs;
using Vector.Domain.Submission.Enums;

namespace Vector.Application.Submissions.Queries;

/// <summary>
/// Query to retrieve submissions for a producer.
/// </summary>
public sealed record GetProducerSubmissionsQuery(
    Guid? ProducerId,
    SubmissionStatus? Status,
    string? SearchTerm,
    int Page = 1,
    int PageSize = 20) : IRequest<ProducerSubmissionsResult>;

/// <summary>
/// Result containing paginated submissions.
/// </summary>
public sealed record ProducerSubmissionsResult(
    IReadOnlyList<SubmissionSummaryDto> Submissions,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
