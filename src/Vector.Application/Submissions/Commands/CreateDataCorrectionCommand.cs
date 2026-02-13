using MediatR;
using Vector.Domain.Common;
using Vector.Domain.Submission.Enums;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Command to create a data correction request.
/// </summary>
public sealed record CreateDataCorrectionCommand(
    Guid SubmissionId,
    DataCorrectionType Type,
    string FieldName,
    string? CurrentValue,
    string ProposedValue,
    string Justification) : IRequest<Result<Guid>>;
