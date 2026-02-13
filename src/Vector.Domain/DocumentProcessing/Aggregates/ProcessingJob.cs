using Vector.Domain.Common;
using Vector.Domain.DocumentProcessing.Enums;
using Vector.Domain.DocumentProcessing.Events;

namespace Vector.Domain.DocumentProcessing.Aggregates;

/// <summary>
/// Aggregate root representing a document processing job for an inbound email.
/// </summary>
public sealed class ProcessingJob : AuditableAggregateRoot, IMultiTenantEntity
{
    private readonly List<ProcessedDocument> _documents = [];

    public Guid TenantId { get; private set; }
    public Guid InboundEmailId { get; private set; }
    public ProcessingStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    public IReadOnlyCollection<ProcessedDocument> Documents => _documents.AsReadOnly();

    private ProcessingJob()
    {
    }

    private ProcessingJob(
        Guid id,
        Guid tenantId,
        Guid inboundEmailId) : base(id)
    {
        TenantId = tenantId;
        InboundEmailId = inboundEmailId;
        Status = ProcessingStatus.Pending;
        StartedAt = DateTime.UtcNow;
    }

    public static ProcessingJob Create(Guid tenantId, Guid inboundEmailId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        }

        if (inboundEmailId == Guid.Empty)
        {
            throw new ArgumentException("Inbound email ID is required.", nameof(inboundEmailId));
        }

        return new ProcessingJob(Guid.NewGuid(), tenantId, inboundEmailId);
    }

    public ProcessedDocument AddDocument(
        Guid sourceAttachmentId,
        string originalFileName,
        string blobStorageUrl)
    {
        var document = new ProcessedDocument(
            Guid.NewGuid(),
            sourceAttachmentId,
            originalFileName,
            blobStorageUrl);

        _documents.Add(document);
        return document;
    }

    public void StartClassification()
    {
        Status = ProcessingStatus.Classifying;
    }

    public void OnDocumentClassified(Guid documentId, DocumentType documentType, decimal confidence)
    {
        var document = _documents.FirstOrDefault(d => d.Id == documentId);
        if (document is null) return;

        document.Classify(documentType, confidence);

        AddDomainEvent(new DocumentClassifiedEvent(
            documentId,
            Id,
            documentType,
            confidence));
    }

    public void StartExtraction()
    {
        Status = ProcessingStatus.Extracting;
    }

    public void OnDocumentExtractionCompleted(Guid documentId)
    {
        var document = _documents.FirstOrDefault(d => d.Id == documentId);
        if (document is null) return;

        document.CompleteExtraction();

        AddDomainEvent(new DocumentExtractionCompletedEvent(
            documentId,
            Id,
            document.DocumentType,
            document.ExtractedFields.Count,
            document.GetAverageConfidence()));
    }

    public void StartValidation()
    {
        Status = ProcessingStatus.Validating;
    }

    public void Complete()
    {
        Status = ProcessingStatus.Completed;
        CompletedAt = DateTime.UtcNow;

        var successful = _documents.Count(d => d.Status == ProcessingStatus.Completed);
        var failed = _documents.Count(d => d.Status == ProcessingStatus.Failed);
        var review = _documents.Count(d => d.Status == ProcessingStatus.ManualReviewRequired);

        AddDomainEvent(new ProcessingJobCompletedEvent(
            Id,
            InboundEmailId,
            _documents.Count,
            successful,
            failed,
            review));
    }

    public void Fail(string errorMessage)
    {
        Status = ProcessingStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }

    public ProcessedDocument? GetDocument(Guid documentId)
    {
        return _documents.FirstOrDefault(d => d.Id == documentId);
    }

    public IEnumerable<ProcessedDocument> GetDocumentsByType(DocumentType documentType)
    {
        return _documents.Where(d => d.DocumentType == documentType);
    }

    public bool HasAcordForms => _documents.Any(d =>
        d.DocumentType is DocumentType.Acord125 or DocumentType.Acord126 or
            DocumentType.Acord130 or DocumentType.Acord140 or
            DocumentType.Acord127 or DocumentType.Acord137);

    public bool HasLossRuns => _documents.Any(d => d.DocumentType == DocumentType.LossRunReport);

    public bool HasExposureSchedule => _documents.Any(d => d.DocumentType == DocumentType.ExposureSchedule);
}
