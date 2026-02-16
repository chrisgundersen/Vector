using MediatR;
using Vector.Application.Common.Behaviors;
using Vector.Domain.Common;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Command to expire a submission.
/// </summary>
public sealed record ExpireSubmissionCommand(
    Guid SubmissionId,
    string? Reason) : IRequest<Result>, ITransactionalCommand;
