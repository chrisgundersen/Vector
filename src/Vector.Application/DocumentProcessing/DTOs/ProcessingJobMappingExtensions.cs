using Vector.Domain.DocumentProcessing.Aggregates;
using Vector.Domain.DocumentProcessing.Enums;

namespace Vector.Application.DocumentProcessing.DTOs;

/// <summary>
/// Extension methods for mapping ProcessingJob domain objects to DTOs.
/// </summary>
public static class ProcessingJobMappingExtensions
{
    public static ProcessingJobDto ToDto(this ProcessingJob job)
    {
        return new ProcessingJobDto(
            job.Id,
            job.TenantId,
            job.InboundEmailId,
            job.Status,
            job.StartedAt,
            job.CompletedAt,
            job.ErrorMessage,
            job.Documents.Count,
            job.Documents.Count(d => d.Status == ProcessingStatus.Completed),
            job.Documents.Count(d => d.Status == ProcessingStatus.Failed),
            job.Documents.Count(d => d.Status == ProcessingStatus.ManualReviewRequired),
            job.Documents.Select(d => d.ToDto()).ToList());
    }

    public static ProcessingJobSummaryDto ToSummaryDto(this ProcessingJob job)
    {
        return new ProcessingJobSummaryDto(
            job.Id,
            job.TenantId,
            job.InboundEmailId,
            job.Status,
            job.StartedAt,
            job.CompletedAt,
            job.Documents.Count,
            job.Documents.Count(d => d.Status == ProcessingStatus.Completed),
            job.Documents.Count(d => d.Status == ProcessingStatus.Failed));
    }

    public static ProcessedDocumentDto ToDto(this ProcessedDocument document)
    {
        return new ProcessedDocumentDto(
            document.Id,
            document.SourceAttachmentId,
            document.OriginalFileName,
            document.BlobStorageUrl,
            document.DocumentType,
            document.ClassificationConfidence.Score,
            document.Status,
            document.ProcessedAt,
            document.FailureReason,
            document.ExtractedFields.Count,
            document.GetAverageConfidence(),
            document.ValidationErrors.ToList(),
            document.ExtractedFields.Select(f => f.ToDto()).ToList());
    }

    public static ExtractedFieldDto ToDto(this Domain.DocumentProcessing.ValueObjects.ExtractedField field)
    {
        return new ExtractedFieldDto(
            field.FieldName,
            field.Value,
            field.Confidence.Score,
            field.BoundingBox,
            field.PageNumber);
    }
}
