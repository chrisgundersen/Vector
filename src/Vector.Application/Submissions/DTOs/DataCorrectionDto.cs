namespace Vector.Application.Submissions.DTOs;

/// <summary>
/// Data transfer object for data correction requests.
/// </summary>
public sealed record DataCorrectionDto(
    Guid Id,
    Guid SubmissionId,
    string Type,
    string FieldName,
    string? CurrentValue,
    string ProposedValue,
    string Justification,
    string Status,
    DateTime RequestedAt,
    string? RequestedBy,
    DateTime? ReviewedAt,
    string? ReviewedBy,
    string? ReviewNotes);
