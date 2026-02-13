using MediatR;
using Vector.Application.Common.Behaviors;
using Vector.Domain.Common;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Command to decline a submission.
/// </summary>
public sealed record DeclineSubmissionCommand(
    Guid SubmissionId,
    string Reason) : IRequest<Result>, ITransactionalCommand;
