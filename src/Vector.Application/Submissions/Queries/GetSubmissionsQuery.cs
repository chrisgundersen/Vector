using MediatR;
using Vector.Application.Submissions.DTOs;
using Vector.Domain.Common;
using Vector.Domain.Submission.Enums;

namespace Vector.Application.Submissions.Queries;

/// <summary>
/// Query to retrieve submissions for underwriting dashboard.
/// </summary>
public sealed record GetSubmissionsQuery : IRequest<Result<SubmissionsPagedResult>>
{
    /// <summary>
    /// Filter by status.
    /// </summary>
    public SubmissionStatus? Status { get; init; }

    /// <summary>
    /// Filter by assigned underwriter ID.
    /// </summary>
    public Guid? UnderwriterId { get; init; }

    /// <summary>
    /// Search term for submission number or insured name.
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Paginated result of submissions.
/// </summary>
public sealed record SubmissionsPagedResult(
    IReadOnlyList<SubmissionSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
