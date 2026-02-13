using Vector.Domain.Common;
using Vector.Domain.Submission.Enums;

namespace Vector.Domain.Submission.Entities;

/// <summary>
/// Represents a request from a producer to correct data on a submission.
/// </summary>
public sealed class DataCorrectionRequest : Entity<Guid>
{
    private DataCorrectionRequest() { } // EF Core

    public Guid SubmissionId { get; private set; }
    public DataCorrectionType Type { get; private set; }
    public string FieldName { get; private set; } = string.Empty;
    public string? CurrentValue { get; private set; }
    public string ProposedValue { get; private set; } = string.Empty;
    public string Justification { get; private set; } = string.Empty;
    public DataCorrectionStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public string? RequestedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? ReviewedBy { get; private set; }
    public string? ReviewNotes { get; private set; }

    public static DataCorrectionRequest Create(
        Guid submissionId,
        DataCorrectionType type,
        string fieldName,
        string? currentValue,
        string proposedValue,
        string justification,
        string? requestedBy)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            throw new ArgumentException("Field name is required", nameof(fieldName));

        if (string.IsNullOrWhiteSpace(proposedValue))
            throw new ArgumentException("Proposed value is required", nameof(proposedValue));

        if (string.IsNullOrWhiteSpace(justification))
            throw new ArgumentException("Justification is required", nameof(justification));

        return new DataCorrectionRequest
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            Type = type,
            FieldName = fieldName,
            CurrentValue = currentValue,
            ProposedValue = proposedValue,
            Justification = justification,
            Status = DataCorrectionStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            RequestedBy = requestedBy
        };
    }

    public void StartReview(string reviewedBy)
    {
        if (Status != DataCorrectionStatus.Pending)
            throw new InvalidOperationException("Only pending corrections can be reviewed");

        Status = DataCorrectionStatus.UnderReview;
        ReviewedBy = reviewedBy;
    }

    public void Approve(string reviewedBy, string? notes = null)
    {
        if (Status is not DataCorrectionStatus.Pending and not DataCorrectionStatus.UnderReview)
            throw new InvalidOperationException("Only pending or under-review corrections can be approved");

        Status = DataCorrectionStatus.Approved;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewedBy;
        ReviewNotes = notes;
    }

    public void Reject(string reviewedBy, string notes)
    {
        if (Status is not DataCorrectionStatus.Pending and not DataCorrectionStatus.UnderReview)
            throw new InvalidOperationException("Only pending or under-review corrections can be rejected");

        if (string.IsNullOrWhiteSpace(notes))
            throw new ArgumentException("Rejection notes are required", nameof(notes));

        Status = DataCorrectionStatus.Rejected;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewedBy;
        ReviewNotes = notes;
    }

    public void MarkAsApplied()
    {
        if (Status != DataCorrectionStatus.Approved)
            throw new InvalidOperationException("Only approved corrections can be marked as applied");

        Status = DataCorrectionStatus.Applied;
    }
}
