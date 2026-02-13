using Vector.Domain.Common;
using Vector.Domain.DocumentProcessing.Enums;

namespace Vector.Domain.DocumentProcessing.Events;

/// <summary>
/// Domain event raised when a document has been classified.
/// </summary>
public sealed record DocumentClassifiedEvent(
    Guid ProcessedDocumentId,
    Guid ProcessingJobId,
    DocumentType DocumentType,
    decimal ClassificationConfidence) : DomainEvent;
