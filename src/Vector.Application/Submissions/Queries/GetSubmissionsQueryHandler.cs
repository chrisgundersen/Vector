using MediatR;
using Vector.Application.Common.Interfaces;
using Vector.Application.Submissions.DTOs;
using Vector.Domain.Common;
using Vector.Domain.Submission;

namespace Vector.Application.Submissions.Queries;

/// <summary>
/// Handler for retrieving submissions with filtering and pagination.
/// </summary>
public sealed class GetSubmissionsQueryHandler(
    ISubmissionRepository submissionRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetSubmissionsQuery, Result<SubmissionsPagedResult>>
{
    public async Task<Result<SubmissionsPagedResult>> Handle(
        GetSubmissionsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = currentUserService.TenantId;
        if (tenantId is null)
        {
            return Result.Failure<SubmissionsPagedResult>(new Error("Submissions.TenantRequired", "Tenant ID is required"));
        }

        var (submissions, totalCount) = await submissionRepository.SearchAsync(
            tenantId.Value,
            producerId: null,
            request.Status,
            request.SearchTerm,
            request.Page,
            request.PageSize,
            cancellationToken);

        // Additional filter by underwriter if specified
        if (request.UnderwriterId.HasValue)
        {
            submissions = submissions
                .Where(s => s.AssignedUnderwriterId == request.UnderwriterId.Value)
                .ToList();
        }

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var summaries = submissions.Select(s => new SubmissionSummaryDto(
            s.Id,
            s.SubmissionNumber,
            s.Insured.Name,
            s.Status.ToString(),
            s.ReceivedAt,
            s.EffectiveDate,
            s.AssignedUnderwriterName,
            s.AppetiteScore,
            s.WinnabilityScore,
            s.DataQualityScore,
            s.Coverages.Count,
            s.Locations.Count,
            s.Locations.Sum(l => l.TotalInsuredValue.Amount))).ToList();

        return Result<SubmissionsPagedResult>.Success(new SubmissionsPagedResult(
            summaries,
            totalCount,
            request.Page,
            request.PageSize,
            totalPages));
    }
}
