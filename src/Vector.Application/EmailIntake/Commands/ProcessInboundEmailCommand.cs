using MediatR;
using Vector.Application.Common.Behaviors;
using Vector.Domain.Common;

namespace Vector.Application.EmailIntake.Commands;

/// <summary>
/// Command to process a newly received inbound email.
/// </summary>
public sealed record ProcessInboundEmailCommand(
    Guid TenantId,
    string MailboxId,
    string ExternalMessageId,
    string FromAddress,
    string Subject,
    string BodyPreview,
    string BodyContent,
    DateTime ReceivedAt) : IRequest<Result<Guid>>, ITransactionalCommand;
