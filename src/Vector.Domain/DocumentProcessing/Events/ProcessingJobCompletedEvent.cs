using Vector.Domain.Common;

namespace Vector.Domain.DocumentProcessing.Events;

/// <summary>
/// Domain event raised when all documents in a processing job have been processed.
/// </summary>
public sealed record ProcessingJobCompletedEvent(
    Guid ProcessingJobId,
    Guid InboundEmailId,
    int TotalDocuments,
    int SuccessfullyProcessed,
    int FailedToProcess,
    int RequiringManualReview) : DomainEvent;
