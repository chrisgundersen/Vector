using MediatR;
using Vector.Application.Common.Behaviors;
using Vector.Domain.Common;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Command to override a failed clearance check on a submission.
/// </summary>
public sealed record OverrideClearanceCommand(
    Guid SubmissionId,
    string Reason,
    Guid OverriddenByUserId) : IRequest<Result>, ITransactionalCommand;
