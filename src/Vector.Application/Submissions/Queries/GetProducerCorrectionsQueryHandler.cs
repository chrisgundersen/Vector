using MediatR;
using Vector.Application.Submissions.DTOs;
using Vector.Domain.Submission;
using Vector.Domain.Submission.Enums;

namespace Vector.Application.Submissions.Queries;

/// <summary>
/// Handler for retrieving all data correction requests for a producer's submissions.
/// </summary>
public sealed class GetProducerCorrectionsQueryHandler(
    IDataCorrectionRepository correctionRepository) : IRequestHandler<GetProducerCorrectionsQuery, IReadOnlyList<ProducerCorrectionDto>>
{
    public async Task<IReadOnlyList<ProducerCorrectionDto>> Handle(
        GetProducerCorrectionsQuery request,
        CancellationToken cancellationToken)
    {
        DataCorrectionStatus? status = null;
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<DataCorrectionStatus>(request.Status, out var parsedStatus))
        {
            status = parsedStatus;
        }

        var corrections = await correctionRepository.GetByProducerIdAsync(
            request.ProducerId,
            status,
            cancellationToken);

        return corrections.Select(c => new ProducerCorrectionDto(
            c.Correction.Id,
            c.Correction.SubmissionId,
            c.SubmissionNumber,
            c.InsuredName,
            c.Correction.Type.ToString(),
            c.Correction.FieldName,
            c.Correction.CurrentValue,
            c.Correction.ProposedValue,
            c.Correction.Justification,
            c.Correction.Status.ToString(),
            c.Correction.RequestedAt,
            c.Correction.RequestedBy,
            c.Correction.ReviewedAt,
            c.Correction.ReviewedBy,
            c.Correction.ReviewNotes)).ToList();
    }
}
