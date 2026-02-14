using MediatR;
using Vector.Application.Common.Behaviors;
using Vector.Domain.Common;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Command to bind a submission, creating a policy.
/// </summary>
public sealed record BindSubmissionCommand(Guid SubmissionId) : IRequest<Result<BindSubmissionResult>>, ITransactionalCommand;

/// <summary>
/// Result of binding a submission.
/// </summary>
public sealed record BindSubmissionResult(
    Guid SubmissionId,
    string SubmissionNumber,
    string? ExternalPolicyId,
    string? PolicyNumber,
    DateTime BoundAt);
