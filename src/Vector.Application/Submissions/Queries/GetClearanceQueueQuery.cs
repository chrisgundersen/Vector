using MediatR;
using Vector.Application.Submissions.DTOs;

namespace Vector.Application.Submissions.Queries;

/// <summary>
/// Query to retrieve submissions pending clearance review.
/// </summary>
public sealed record GetClearanceQueueQuery(
    int Limit = 50) : IRequest<IReadOnlyList<SubmissionSummaryDto>>;
