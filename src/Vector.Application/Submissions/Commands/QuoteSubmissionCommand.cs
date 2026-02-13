using MediatR;
using Vector.Application.Common.Behaviors;
using Vector.Domain.Common;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Command to quote a submission.
/// </summary>
public sealed record QuoteSubmissionCommand(
    Guid SubmissionId,
    decimal PremiumAmount,
    string Currency = "USD") : IRequest<Result>, ITransactionalCommand;
