using MediatR;
using Vector.Application.Common.Behaviors;
using Vector.Domain.Common;

namespace Vector.Application.DocumentProcessing.Commands;

/// <summary>
/// Command to start a document processing job for an inbound email.
/// </summary>
public sealed record StartProcessingJobCommand(
    Guid TenantId,
    Guid InboundEmailId) : IRequest<Result<Guid>>, ITransactionalCommand;
