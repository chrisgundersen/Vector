using MediatR;
using Vector.Application.Common.Behaviors;
using Vector.Domain.Common;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Command to assign a submission to an underwriter.
/// </summary>
public sealed record AssignSubmissionCommand(
    Guid SubmissionId,
    Guid UnderwriterId,
    string UnderwriterName) : IRequest<Result>, ITransactionalCommand;
