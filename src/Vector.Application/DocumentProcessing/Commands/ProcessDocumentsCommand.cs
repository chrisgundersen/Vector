using MediatR;
using Vector.Application.Common.Behaviors;
using Vector.Domain.Common;

namespace Vector.Application.DocumentProcessing.Commands;

/// <summary>
/// Command to process all documents in a processing job (classify and extract).
/// </summary>
public sealed record ProcessDocumentsCommand(
    Guid ProcessingJobId) : IRequest<Result>, ITransactionalCommand;
