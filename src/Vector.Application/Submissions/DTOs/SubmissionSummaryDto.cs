namespace Vector.Application.Submissions.DTOs;

/// <summary>
/// Lightweight submission DTO for list views.
/// </summary>
public sealed record SubmissionSummaryDto(
    Guid Id,
    string SubmissionNumber,
    string InsuredName,
    string Status,
    DateTime ReceivedAt,
    DateTime? EffectiveDate,
    string? AssignedUnderwriterName,
    int? AppetiteScore,
    int? WinnabilityScore,
    int? DataQualityScore,
    int CoverageCount,
    int LocationCount,
    decimal? TotalInsuredValue,
    string ClearanceStatus);
