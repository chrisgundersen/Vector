using Vector.Domain.DocumentProcessing.Enums;

namespace Vector.Application.Common.Interfaces;

/// <summary>
/// Interface for document AI/intelligence operations.
/// </summary>
public interface IDocumentIntelligenceService
{
    /// <summary>
    /// Classifies a document and returns its type.
    /// </summary>
    Task<DocumentClassificationResult> ClassifyDocumentAsync(
        Stream documentStream,
        string fileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts fields from an ACORD form.
    /// </summary>
    Task<DocumentExtractionResult> ExtractAcordFormAsync(
        Stream documentStream,
        DocumentType documentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts data from a loss run report.
    /// </summary>
    Task<LossRunExtractionResult> ExtractLossRunAsync(
        Stream documentStream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts data from an exposure schedule (SOV).
    /// </summary>
    Task<ExposureScheduleExtractionResult> ExtractExposureScheduleAsync(
        Stream documentStream,
        CancellationToken cancellationToken = default);
}

public record DocumentClassificationResult(
    DocumentType DocumentType,
    decimal Confidence,
    string? ModelVersion);

public record DocumentExtractionResult(
    IReadOnlyDictionary<string, ExtractedFieldResult> Fields,
    decimal AverageConfidence,
    int PageCount);

public record ExtractedFieldResult(
    string? Value,
    decimal Confidence,
    string? BoundingBox,
    int? PageNumber);

public record LossRunExtractionResult(
    IReadOnlyList<ExtractedLoss> Losses,
    string? CarrierName,
    DateTime? ReportDate,
    decimal AverageConfidence);

public record ExtractedLoss(
    DateTime? DateOfLoss,
    string? ClaimNumber,
    string? Description,
    decimal? PaidAmount,
    decimal? ReservedAmount,
    decimal? IncurredAmount,
    string? Status,
    string? CoverageType);

public record ExposureScheduleExtractionResult(
    IReadOnlyList<ExtractedLocation> Locations,
    decimal AverageConfidence);

public record ExtractedLocation(
    int? LocationNumber,
    string? Street1,
    string? Street2,
    string? City,
    string? State,
    string? PostalCode,
    string? BuildingDescription,
    decimal? BuildingValue,
    decimal? ContentsValue,
    decimal? BusinessIncomeValue,
    string? ConstructionType,
    int? YearBuilt,
    int? SquareFootage);
