using Vector.Domain.Common;
using Vector.Domain.DocumentProcessing.Enums;
using Vector.Domain.DocumentProcessing.Events;
using Vector.Domain.DocumentProcessing.ValueObjects;

namespace Vector.Domain.DocumentProcessing.Aggregates;

/// <summary>
/// Entity representing a document that has been or is being processed.
/// </summary>
public sealed class ProcessedDocument : Entity
{
    private readonly List<ExtractedField> _extractedFields = [];
    private readonly List<string> _validationErrors = [];

    public Guid SourceAttachmentId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string BlobStorageUrl { get; private set; } = string.Empty;
    public DocumentType DocumentType { get; private set; }
    public ExtractionConfidence ClassificationConfidence { get; private set; } = ExtractionConfidence.Unknown;
    public ProcessingStatus Status { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? FailureReason { get; private set; }

    public IReadOnlyCollection<ExtractedField> ExtractedFields => _extractedFields.AsReadOnly();
    public IReadOnlyCollection<string> ValidationErrors => _validationErrors.AsReadOnly();

    private ProcessedDocument()
    {
    }

    internal ProcessedDocument(
        Guid id,
        Guid sourceAttachmentId,
        string originalFileName,
        string blobStorageUrl) : base(id)
    {
        SourceAttachmentId = sourceAttachmentId;
        OriginalFileName = originalFileName;
        BlobStorageUrl = blobStorageUrl;
        DocumentType = DocumentType.Unknown;
        Status = ProcessingStatus.Pending;
    }

    public void Classify(DocumentType documentType, decimal confidence)
    {
        DocumentType = documentType;
        var confidenceResult = ExtractionConfidence.Create(confidence);
        ClassificationConfidence = confidenceResult.IsSuccess
            ? confidenceResult.Value
            : ExtractionConfidence.Unknown;
        Status = ProcessingStatus.Classified;
    }

    public void AddExtractedField(ExtractedField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        _extractedFields.Add(field);
    }

    public void AddExtractedFields(IEnumerable<ExtractedField> fields)
    {
        foreach (var field in fields)
        {
            AddExtractedField(field);
        }
    }

    public void CompleteExtraction()
    {
        Status = ProcessingStatus.Extracted;
    }

    public void AddValidationError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            _validationErrors.Add(error);
        }
    }

    public void CompleteValidation()
    {
        Status = _validationErrors.Count > 0
            ? ProcessingStatus.ManualReviewRequired
            : ProcessingStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string reason)
    {
        Status = ProcessingStatus.Failed;
        FailureReason = reason;
        ProcessedAt = DateTime.UtcNow;
    }

    public decimal GetAverageConfidence()
    {
        if (_extractedFields.Count == 0) return 0;
        return _extractedFields.Average(f => f.Confidence.Score);
    }

    public ExtractedField? GetField(string fieldName)
    {
        return _extractedFields.FirstOrDefault(f =>
            f.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
    }

    public string? GetFieldValue(string fieldName)
    {
        return GetField(fieldName)?.Value;
    }
}
