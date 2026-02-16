using MediatR;
using Vector.Application.Common.Behaviors;
using Vector.Domain.Common;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Command to withdraw a submission.
/// </summary>
public sealed record WithdrawSubmissionCommand(
    Guid SubmissionId,
    string? Reason) : IRequest<Result>, ITransactionalCommand;
