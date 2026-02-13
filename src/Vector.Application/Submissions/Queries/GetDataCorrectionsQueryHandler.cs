using MediatR;
using Vector.Application.Submissions.DTOs;
using Vector.Domain.Submission;

namespace Vector.Application.Submissions.Queries;

/// <summary>
/// Handler for retrieving data correction requests for a submission.
/// </summary>
public sealed class GetDataCorrectionsQueryHandler(
    IDataCorrectionRepository correctionRepository) : IRequestHandler<GetDataCorrectionsQuery, IReadOnlyList<DataCorrectionDto>>
{
    public async Task<IReadOnlyList<DataCorrectionDto>> Handle(
        GetDataCorrectionsQuery request,
        CancellationToken cancellationToken)
    {
        var corrections = await correctionRepository.GetBySubmissionIdAsync(
            request.SubmissionId,
            cancellationToken);

        return corrections.Select(c => new DataCorrectionDto(
            c.Id,
            c.SubmissionId,
            c.Type.ToString(),
            c.FieldName,
            c.CurrentValue,
            c.ProposedValue,
            c.Justification,
            c.Status.ToString(),
            c.RequestedAt,
            c.RequestedBy,
            c.ReviewedAt,
            c.ReviewedBy,
            c.ReviewNotes)).ToList();
    }
}
