using Vector.Domain.Common;
using Vector.Domain.DocumentProcessing.Enums;

namespace Vector.Domain.DocumentProcessing.Events;

/// <summary>
/// Domain event raised when document data extraction is completed.
/// </summary>
public sealed record DocumentExtractionCompletedEvent(
    Guid ProcessedDocumentId,
    Guid ProcessingJobId,
    DocumentType DocumentType,
    int ExtractedFieldCount,
    decimal AverageConfidence) : DomainEvent;
