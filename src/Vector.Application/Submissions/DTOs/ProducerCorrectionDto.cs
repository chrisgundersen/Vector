namespace Vector.Application.Submissions.DTOs;

/// <summary>
/// Data transfer object for data correction requests with submission context for producers.
/// </summary>
public sealed record ProducerCorrectionDto(
    Guid Id,
    Guid SubmissionId,
    string SubmissionNumber,
    string InsuredName,
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
