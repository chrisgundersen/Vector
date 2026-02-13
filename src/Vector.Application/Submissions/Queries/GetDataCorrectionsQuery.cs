using MediatR;
using Vector.Application.Submissions.DTOs;

namespace Vector.Application.Submissions.Queries;

/// <summary>
/// Query to get data correction requests for a submission.
/// </summary>
public sealed record GetDataCorrectionsQuery(Guid SubmissionId) : IRequest<IReadOnlyList<DataCorrectionDto>>;
