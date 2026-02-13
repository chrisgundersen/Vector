using Vector.Domain.Common;
using Vector.Domain.Submission.Entities;
using Vector.Domain.Submission.Enums;
using Vector.Domain.Submission.Events;
using Vector.Domain.Submission.ValueObjects;

namespace Vector.Domain.Submission.Aggregates;

/// <summary>
/// Aggregate root representing an insurance submission.
/// </summary>
public sealed class Submission : AuditableAggregateRoot, IMultiTenantEntity
{
    private readonly List<Coverage> _coverages = [];
    private readonly List<ExposureLocation> _locations = [];
    private readonly List<LossHistory> _lossHistory = [];

    public Guid TenantId { get; private set; }
    public string SubmissionNumber { get; private set; } = string.Empty;
    public Guid? ProcessingJobId { get; private set; }
    public Guid? InboundEmailId { get; private set; }
    public InsuredParty Insured { get; private set; } = null!;
    public SubmissionStatus Status { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public DateTime? EffectiveDate { get; private set; }
    public DateTime? ExpirationDate { get; private set; }

    // Producer information
    public Guid? ProducerId { get; private set; }
    public string? ProducerName { get; private set; }
    public string? ProducerContactEmail { get; private set; }

    // Assignment
    public Guid? AssignedUnderwriterId { get; private set; }
    public string? AssignedUnderwriterName { get; private set; }
    public DateTime? AssignedAt { get; private set; }

    // Scoring
    public int? AppetiteScore { get; private set; }
    public int? WinnabilityScore { get; private set; }
    public int? DataQualityScore { get; private set; }

    // Decline/Quote info
    public string? DeclineReason { get; private set; }
    public Money? QuotedPremium { get; private set; }

    public IReadOnlyCollection<Coverage> Coverages => _coverages.AsReadOnly();
    public IReadOnlyCollection<ExposureLocation> Locations => _locations.AsReadOnly();
    public IReadOnlyCollection<LossHistory> LossHistory => _lossHistory.AsReadOnly();

    private Submission()
    {
    }

    private Submission(
        Guid id,
        Guid tenantId,
        string submissionNumber,
        string insuredName) : base(id)
    {
        TenantId = tenantId;
        SubmissionNumber = submissionNumber;
        Insured = new InsuredParty(Guid.NewGuid(), insuredName);
        Status = SubmissionStatus.Draft;
        ReceivedAt = DateTime.UtcNow;
    }

    public static Result<Submission> Create(
        Guid tenantId,
        string submissionNumber,
        string insuredName,
        Guid? processingJobId = null,
        Guid? inboundEmailId = null)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<Submission>(SubmissionErrors.InvalidTenant);
        }

        if (string.IsNullOrWhiteSpace(submissionNumber))
        {
            return Result.Failure<Submission>(SubmissionErrors.SubmissionNumberRequired);
        }

        if (string.IsNullOrWhiteSpace(insuredName))
        {
            return Result.Failure<Submission>(SubmissionErrors.InsuredNameRequired);
        }

        var submission = new Submission(
            Guid.NewGuid(),
            tenantId,
            submissionNumber,
            insuredName)
        {
            ProcessingJobId = processingJobId,
            InboundEmailId = inboundEmailId
        };

        return Result.Success(submission);
    }

    public void MarkAsReceived()
    {
        if (Status != SubmissionStatus.Draft)
        {
            return;
        }

        var previousStatus = Status;
        Status = SubmissionStatus.Received;

        AddDomainEvent(new SubmissionCreatedEvent(
            Id,
            TenantId,
            ProcessingJobId,
            Insured.Name,
            ReceivedAt));

        AddDomainEvent(new SubmissionStatusChangedEvent(
            Id,
            previousStatus,
            Status,
            null));
    }

    public Result AssignToUnderwriter(Guid underwriterId, string underwriterName)
    {
        if (Status is SubmissionStatus.Declined or SubmissionStatus.Bound or SubmissionStatus.Withdrawn)
        {
            return Result.Failure(SubmissionErrors.CannotAssignClosedSubmission);
        }

        AssignedUnderwriterId = underwriterId;
        AssignedUnderwriterName = underwriterName;
        AssignedAt = DateTime.UtcNow;

        if (Status == SubmissionStatus.Received)
        {
            var previousStatus = Status;
            Status = SubmissionStatus.InReview;

            AddDomainEvent(new SubmissionStatusChangedEvent(
                Id,
                previousStatus,
                Status,
                $"Assigned to {underwriterName}"));
        }

        AddDomainEvent(new SubmissionAssignedEvent(
            Id,
            underwriterId,
            underwriterName,
            AssignedAt.Value));

        return Result.Success();
    }

    public Result RequestInformation(string reason)
    {
        if (Status is not (SubmissionStatus.InReview or SubmissionStatus.Received))
        {
            return Result.Failure(SubmissionErrors.InvalidStatusTransition);
        }

        var previousStatus = Status;
        Status = SubmissionStatus.PendingInformation;

        AddDomainEvent(new SubmissionStatusChangedEvent(
            Id,
            previousStatus,
            Status,
            reason));

        return Result.Success();
    }

    public Result Quote(Money premium)
    {
        if (Status is not (SubmissionStatus.InReview or SubmissionStatus.PendingInformation))
        {
            return Result.Failure(SubmissionErrors.InvalidStatusTransition);
        }

        var previousStatus = Status;
        Status = SubmissionStatus.Quoted;
        QuotedPremium = premium;

        AddDomainEvent(new SubmissionStatusChangedEvent(
            Id,
            previousStatus,
            Status,
            $"Quoted at {premium}"));

        return Result.Success();
    }

    public Result Decline(string reason)
    {
        if (Status is SubmissionStatus.Bound or SubmissionStatus.Declined)
        {
            return Result.Failure(SubmissionErrors.InvalidStatusTransition);
        }

        var previousStatus = Status;
        Status = SubmissionStatus.Declined;
        DeclineReason = reason;

        AddDomainEvent(new SubmissionStatusChangedEvent(
            Id,
            previousStatus,
            Status,
            reason));

        return Result.Success();
    }

    public Result Bind()
    {
        if (Status != SubmissionStatus.Quoted)
        {
            return Result.Failure(SubmissionErrors.MustBeQuotedToBind);
        }

        var previousStatus = Status;
        Status = SubmissionStatus.Bound;

        AddDomainEvent(new SubmissionStatusChangedEvent(
            Id,
            previousStatus,
            Status,
            "Policy bound"));

        return Result.Success();
    }

    public Result Withdraw(string reason)
    {
        if (Status is SubmissionStatus.Bound or SubmissionStatus.Withdrawn)
        {
            return Result.Failure(SubmissionErrors.InvalidStatusTransition);
        }

        var previousStatus = Status;
        Status = SubmissionStatus.Withdrawn;

        AddDomainEvent(new SubmissionStatusChangedEvent(
            Id,
            previousStatus,
            Status,
            reason));

        return Result.Success();
    }

    public void UpdateProducerInfo(Guid? producerId, string? producerName, string? producerEmail)
    {
        ProducerId = producerId;
        ProducerName = producerName?.Trim();
        ProducerContactEmail = producerEmail?.Trim();
    }

    public void UpdatePolicyDates(DateTime? effectiveDate, DateTime? expirationDate)
    {
        EffectiveDate = effectiveDate;
        ExpirationDate = expirationDate;
    }

    public void UpdateScores(int? appetiteScore, int? winnabilityScore, int? dataQualityScore)
    {
        if (appetiteScore is >= 0 and <= 100)
        {
            AppetiteScore = appetiteScore;
        }
        if (winnabilityScore is >= 0 and <= 100)
        {
            WinnabilityScore = winnabilityScore;
        }
        if (dataQualityScore is >= 0 and <= 100)
        {
            DataQualityScore = dataQualityScore;
        }
    }

    public Coverage AddCoverage(CoverageType type)
    {
        var coverage = new Coverage(Guid.NewGuid(), type);
        _coverages.Add(coverage);
        return coverage;
    }

    public void RemoveCoverage(Guid coverageId)
    {
        var coverage = _coverages.FirstOrDefault(c => c.Id == coverageId);
        if (coverage is not null)
        {
            _coverages.Remove(coverage);
        }
    }

    public ExposureLocation AddLocation(Address address)
    {
        var locationNumber = _locations.Count + 1;
        var location = new ExposureLocation(Guid.NewGuid(), locationNumber, address);
        _locations.Add(location);
        return location;
    }

    public void RemoveLocation(Guid locationId)
    {
        var location = _locations.FirstOrDefault(l => l.Id == locationId);
        if (location is not null)
        {
            _locations.Remove(location);
            RenumberLocations();
        }
    }

    private void RenumberLocations()
    {
        var sorted = _locations.OrderBy(l => l.LocationNumber).ToList();
        for (var i = 0; i < sorted.Count; i++)
        {
            // Location numbers are read-only after creation for audit purposes
        }
    }

    public LossHistory AddLoss(DateTime dateOfLoss, string description)
    {
        var loss = new LossHistory(Guid.NewGuid(), dateOfLoss, description);
        _lossHistory.Add(loss);
        return loss;
    }

    public void RemoveLoss(Guid lossId)
    {
        var loss = _lossHistory.FirstOrDefault(l => l.Id == lossId);
        if (loss is not null)
        {
            _lossHistory.Remove(loss);
        }
    }

    public Money TotalInsuredValue
    {
        get
        {
            var total = Money.Zero();
            foreach (var location in _locations)
            {
                total = total.Add(location.TotalInsuredValue);
            }
            return total;
        }
    }

    public Money TotalIncurredLosses
    {
        get
        {
            var total = Money.Zero();
            foreach (var loss in _lossHistory)
            {
                total = total.Add(loss.TotalIncurred);
            }
            return total;
        }
    }

    public int LossCount => _lossHistory.Count;

    public bool HasOpenClaims => _lossHistory.Any(l =>
        l.Status is LossStatus.Open or LossStatus.Reopened);
}

public static class SubmissionErrors
{
    public static readonly Error InvalidTenant = new("Submission.InvalidTenant", "Tenant ID is required.");
    public static readonly Error SubmissionNumberRequired = new("Submission.SubmissionNumberRequired", "Submission number is required.");
    public static readonly Error InsuredNameRequired = new("Submission.InsuredNameRequired", "Insured name is required.");
    public static readonly Error CannotAssignClosedSubmission = new("Submission.CannotAssignClosedSubmission", "Cannot assign a closed submission.");
    public static readonly Error InvalidStatusTransition = new("Submission.InvalidStatusTransition", "Invalid status transition.");
    public static readonly Error MustBeQuotedToBind = new("Submission.MustBeQuotedToBind", "Submission must be quoted before binding.");
}
