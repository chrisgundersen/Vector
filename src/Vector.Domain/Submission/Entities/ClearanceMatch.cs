using Vector.Domain.Common;
using Vector.Domain.Submission.Enums;

namespace Vector.Domain.Submission.Entities;

/// <summary>
/// Entity representing a potential duplicate match found during clearance checking.
/// </summary>
public sealed class ClearanceMatch : Entity
{
    public Guid SubmissionId { get; private set; }
    public Guid MatchedSubmissionId { get; private set; }
    public string MatchedSubmissionNumber { get; private set; } = string.Empty;
    public ClearanceMatchType MatchType { get; private set; }
    public double ConfidenceScore { get; private set; }
    public string MatchDetails { get; private set; } = string.Empty;
    public DateTime DetectedAt { get; private set; }

    private ClearanceMatch()
    {
    }

    public ClearanceMatch(
        Guid id,
        Guid submissionId,
        Guid matchedSubmissionId,
        string matchedSubmissionNumber,
        ClearanceMatchType matchType,
        double confidenceScore,
        string matchDetails) : base(id)
    {
        SubmissionId = submissionId;
        MatchedSubmissionId = matchedSubmissionId;
        MatchedSubmissionNumber = matchedSubmissionNumber;
        MatchType = matchType;
        ConfidenceScore = Math.Clamp(confidenceScore, 0.0, 1.0);
        MatchDetails = matchDetails;
        DetectedAt = DateTime.UtcNow;
    }
}
