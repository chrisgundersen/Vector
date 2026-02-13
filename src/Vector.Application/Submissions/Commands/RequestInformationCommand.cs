using MediatR;
using Vector.Application.Common.Behaviors;
using Vector.Domain.Common;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Command to request additional information on a submission.
/// </summary>
public sealed record RequestInformationCommand(
    Guid SubmissionId,
    string Reason) : IRequest<Result>, ITransactionalCommand;
