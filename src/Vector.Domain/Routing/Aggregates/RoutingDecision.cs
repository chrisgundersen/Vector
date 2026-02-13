using Vector.Domain.Common;
using Vector.Domain.Routing.Enums;

namespace Vector.Domain.Routing.Aggregates;

/// <summary>
/// Aggregate root representing a routing decision for a submission.
/// Records how and why a submission was assigned to a specific underwriter.
/// </summary>
public sealed class RoutingDecision : AggregateRoot
{
    public Guid SubmissionId { get; private set; }
    public string SubmissionNumber { get; private set; } = string.Empty;
    public RoutingDecisionStatus Status { get; private set; }
    public RoutingStrategy Strategy { get; private set; }

    public Guid? AssignedUnderwriterId { get; private set; }
    public string? AssignedUnderwriterName { get; private set; }
    public Guid? AssignedTeamId { get; private set; }
    public string? AssignedTeamName { get; private set; }

    public Guid? MatchedRuleId { get; private set; }
    public string? MatchedRuleName { get; private set; }
    public Guid? MatchedPairingId { get; private set; }

    public string? RoutingReason { get; private set; }
    public int? AppetiteScore { get; private set; }
    public int? WinnabilityScore { get; private set; }

    public DateTime DecidedAt { get; private set; }
    public DateTime? AssignedAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? DeclinedAt { get; private set; }
    public string? DeclineReason { get; private set; }

    private readonly List<RoutingHistoryEntry> _history = [];
    public IReadOnlyCollection<RoutingHistoryEntry> History => _history.AsReadOnly();

    private RoutingDecision()
    {
    }

    private RoutingDecision(
        Guid id,
        Guid submissionId,
        string submissionNumber,
        RoutingStrategy strategy) : base(id)
    {
        SubmissionId = submissionId;
        SubmissionNumber = submissionNumber;
        Strategy = strategy;
        Status = RoutingDecisionStatus.Pending;
        DecidedAt = DateTime.UtcNow;
    }

    public static Result<RoutingDecision> Create(
        Guid submissionId,
        string submissionNumber,
        RoutingStrategy strategy)
    {
        if (submissionId == Guid.Empty)
        {
            return Result.Failure<RoutingDecision>(RoutingDecisionErrors.SubmissionIdRequired);
        }

        if (string.IsNullOrWhiteSpace(submissionNumber))
        {
            return Result.Failure<RoutingDecision>(RoutingDecisionErrors.SubmissionNumberRequired);
        }

        var decision = new RoutingDecision(Guid.NewGuid(), submissionId, submissionNumber, strategy);
        decision.AddDomainEvent(new RoutingDecisionCreatedEvent(decision.Id, submissionId, submissionNumber));

        return Result.Success(decision);
    }

    public void SetMatchedRule(Guid ruleId, string ruleName)
    {
        MatchedRuleId = ruleId;
        MatchedRuleName = ruleName;
    }

    public void SetMatchedPairing(Guid pairingId)
    {
        MatchedPairingId = pairingId;
    }

    public void SetScores(int? appetiteScore, int? winnabilityScore)
    {
        AppetiteScore = appetiteScore;
        WinnabilityScore = winnabilityScore;
    }

    public void SetRoutingReason(string reason)
    {
        RoutingReason = reason;
    }

    public Result AssignToUnderwriter(Guid underwriterId, string underwriterName, string? reason = null)
    {
        if (Status is RoutingDecisionStatus.Accepted or RoutingDecisionStatus.Declined)
        {
            return Result.Failure(RoutingDecisionErrors.CannotReassign);
        }

        var previousUnderwriterId = AssignedUnderwriterId;
        var previousUnderwriterName = AssignedUnderwriterName;

        if (Status == RoutingDecisionStatus.Assigned && previousUnderwriterId.HasValue)
        {
            Status = RoutingDecisionStatus.Reassigned;
            _history.Add(new RoutingHistoryEntry(
                DateTime.UtcNow,
                "Reassigned",
                $"From {previousUnderwriterName} to {underwriterName}",
                reason));
        }
        else
        {
            Status = RoutingDecisionStatus.Assigned;
            _history.Add(new RoutingHistoryEntry(
                DateTime.UtcNow,
                "Assigned",
                $"Assigned to {underwriterName}",
                reason));
        }

        AssignedUnderwriterId = underwriterId;
        AssignedUnderwriterName = underwriterName;
        AssignedTeamId = null;
        AssignedTeamName = null;
        AssignedAt = DateTime.UtcNow;

        AddDomainEvent(new SubmissionAssignedEvent(
            Id,
            SubmissionId,
            SubmissionNumber,
            underwriterId,
            underwriterName));

        return Result.Success();
    }

    public Result AssignToTeam(Guid teamId, string teamName, string? reason = null)
    {
        if (Status is RoutingDecisionStatus.Accepted or RoutingDecisionStatus.Declined)
        {
            return Result.Failure(RoutingDecisionErrors.CannotReassign);
        }

        Status = RoutingDecisionStatus.Assigned;
        AssignedTeamId = teamId;
        AssignedTeamName = teamName;
        AssignedUnderwriterId = null;
        AssignedUnderwriterName = null;
        AssignedAt = DateTime.UtcNow;

        _history.Add(new RoutingHistoryEntry(
            DateTime.UtcNow,
            "Assigned to Team",
            $"Assigned to team {teamName}",
            reason));

        return Result.Success();
    }

    public Result Accept(string? notes = null)
    {
        if (Status != RoutingDecisionStatus.Assigned && Status != RoutingDecisionStatus.Reassigned)
        {
            return Result.Failure(RoutingDecisionErrors.NotAssigned);
        }

        Status = RoutingDecisionStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;

        _history.Add(new RoutingHistoryEntry(
            DateTime.UtcNow,
            "Accepted",
            $"Accepted by {AssignedUnderwriterName}",
            notes));

        AddDomainEvent(new RoutingAcceptedEvent(
            Id,
            SubmissionId,
            AssignedUnderwriterId!.Value,
            AssignedUnderwriterName!));

        return Result.Success();
    }

    public Result Decline(string reason)
    {
        if (Status != RoutingDecisionStatus.Assigned && Status != RoutingDecisionStatus.Reassigned)
        {
            return Result.Failure(RoutingDecisionErrors.NotAssigned);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(RoutingDecisionErrors.DeclineReasonRequired);
        }

        Status = RoutingDecisionStatus.Declined;
        DeclinedAt = DateTime.UtcNow;
        DeclineReason = reason;

        _history.Add(new RoutingHistoryEntry(
            DateTime.UtcNow,
            "Declined",
            $"Declined by {AssignedUnderwriterName}",
            reason));

        AddDomainEvent(new RoutingDeclinedEvent(
            Id,
            SubmissionId,
            AssignedUnderwriterId!.Value,
            reason));

        return Result.Success();
    }

    public void Escalate(string reason)
    {
        Status = RoutingDecisionStatus.Escalated;

        _history.Add(new RoutingHistoryEntry(
            DateTime.UtcNow,
            "Escalated",
            "Escalated for manager review",
            reason));

        AddDomainEvent(new RoutingEscalatedEvent(Id, SubmissionId, reason));
    }
}

public sealed record RoutingHistoryEntry(
    DateTime Timestamp,
    string Action,
    string Details,
    string? Notes);

public static class RoutingDecisionErrors
{
    public static readonly Error SubmissionIdRequired = new("RoutingDecision.SubmissionIdRequired", "Submission ID is required.");
    public static readonly Error SubmissionNumberRequired = new("RoutingDecision.SubmissionNumberRequired", "Submission number is required.");
    public static readonly Error CannotReassign = new("RoutingDecision.CannotReassign", "Cannot reassign a submission that has been accepted or declined.");
    public static readonly Error NotAssigned = new("RoutingDecision.NotAssigned", "Submission must be assigned before it can be accepted or declined.");
    public static readonly Error DeclineReasonRequired = new("RoutingDecision.DeclineReasonRequired", "A reason must be provided when declining an assignment.");
}

public sealed record RoutingDecisionCreatedEvent(
    Guid DecisionId,
    Guid SubmissionId,
    string SubmissionNumber) : DomainEvent;

public sealed record SubmissionAssignedEvent(
    Guid DecisionId,
    Guid SubmissionId,
    string SubmissionNumber,
    Guid UnderwriterId,
    string UnderwriterName) : DomainEvent;

public sealed record RoutingAcceptedEvent(
    Guid DecisionId,
    Guid SubmissionId,
    Guid UnderwriterId,
    string UnderwriterName) : DomainEvent;

public sealed record RoutingDeclinedEvent(
    Guid DecisionId,
    Guid SubmissionId,
    Guid UnderwriterId,
    string DeclineReason) : DomainEvent;

public sealed record RoutingEscalatedEvent(
    Guid DecisionId,
    Guid SubmissionId,
    string EscalationReason) : DomainEvent;
