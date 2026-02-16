namespace Vector.Application.Submissions.DTOs;

/// <summary>
/// DTO representing a clearance match found during duplicate checking.
/// </summary>
public sealed record ClearanceMatchDto(
    Guid Id,
    Guid MatchedSubmissionId,
    string MatchedSubmissionNumber,
    string MatchType,
    double ConfidenceScore,
    string MatchDetails,
    DateTime DetectedAt);
