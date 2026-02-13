using MediatR;
using Vector.Application.Common.Behaviors;
using Vector.Domain.Common;

namespace Vector.Application.EmailIntake.Commands;

/// <summary>
/// Command to extract and store an email attachment.
/// </summary>
public sealed record ExtractAttachmentCommand(
    Guid InboundEmailId,
    string FileName,
    string ContentType,
    byte[] Content) : IRequest<Result<Guid>>, ITransactionalCommand;
