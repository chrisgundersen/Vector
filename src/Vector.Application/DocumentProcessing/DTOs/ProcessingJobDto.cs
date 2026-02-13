using Vector.Domain.DocumentProcessing.Enums;

namespace Vector.Application.DocumentProcessing.DTOs;

/// <summary>
/// DTO for ProcessingJob aggregate.
/// </summary>
public record ProcessingJobDto(
    Guid Id,
    Guid TenantId,
    Guid InboundEmailId,
    ProcessingStatus Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string? ErrorMessage,
    int DocumentCount,
    int SuccessfulCount,
    int FailedCount,
    int PendingReviewCount,
    IReadOnlyList<ProcessedDocumentDto> Documents);

/// <summary>
/// DTO for ProcessedDocument entity.
/// </summary>
public record ProcessedDocumentDto(
    Guid Id,
    Guid SourceAttachmentId,
    string OriginalFileName,
    string BlobStorageUrl,
    DocumentType DocumentType,
    decimal ClassificationConfidence,
    ProcessingStatus Status,
    DateTime? ProcessedAt,
    string? FailureReason,
    int FieldCount,
    decimal AverageConfidence,
    IReadOnlyList<string> ValidationErrors,
    IReadOnlyList<ExtractedFieldDto> Fields);

/// <summary>
/// DTO for ExtractedField value object.
/// </summary>
public record ExtractedFieldDto(
    string FieldName,
    string? Value,
    decimal Confidence,
    string? BoundingBox,
    int? PageNumber);

/// <summary>
/// Summary DTO for listing processing jobs.
/// </summary>
public record ProcessingJobSummaryDto(
    Guid Id,
    Guid TenantId,
    Guid InboundEmailId,
    ProcessingStatus Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int DocumentCount,
    int SuccessfulCount,
    int FailedCount);
