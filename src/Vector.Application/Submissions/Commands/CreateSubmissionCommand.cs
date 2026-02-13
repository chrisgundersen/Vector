using MediatR;
using Vector.Application.Common.Behaviors;
using Vector.Domain.Common;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Command to create a new submission.
/// </summary>
public sealed record CreateSubmissionCommand(
    Guid TenantId,
    string InsuredName,
    Guid? ProcessingJobId = null,
    Guid? InboundEmailId = null) : IRequest<Result<Guid>>, ITransactionalCommand;
