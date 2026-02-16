using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Entities;
using Vector.Domain.Submission.Enums;
using Vector.Domain.Submission.Events;

namespace Vector.Domain.UnitTests.Submission;

public class SubmissionClearanceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    private Domain.Submission.Aggregates.Submission CreateReceivedSubmission()
    {
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();
        submission.ClearDomainEvents();
        return submission;
    }

    private ClearanceMatch CreateMatch(Guid submissionId, ClearanceMatchType matchType = ClearanceMatchType.FeinMatch)
    {
        return new ClearanceMatch(
            Guid.NewGuid(),
            submissionId,
            Guid.NewGuid(),
            "SUB-2024-000099",
            matchType,
            1.0,
            "Test match details");
    }

    [Fact]
    public void CompleteClearance_WithNoMatches_SetsStatusToPassed()
    {
        var submission = CreateReceivedSubmission();

        var result = submission.CompleteClearance([]);

        result.IsSuccess.Should().BeTrue();
        submission.ClearanceStatus.Should().Be(ClearanceStatus.Passed);
        submission.Status.Should().Be(SubmissionStatus.Received);
        submission.ClearanceCheckedAt.Should().NotBeNull();
        submission.ClearanceMatches.Should().BeEmpty();
    }

    [Fact]
    public void CompleteClearance_WithMatches_SetsStatusToFailedAndPendingClearance()
    {
        var submission = CreateReceivedSubmission();
        var match = CreateMatch(submission.Id);

        var result = submission.CompleteClearance([match]);

        result.IsSuccess.Should().BeTrue();
        submission.ClearanceStatus.Should().Be(ClearanceStatus.Failed);
        submission.Status.Should().Be(SubmissionStatus.PendingClearance);
        submission.ClearanceMatches.Should().HaveCount(1);
    }

    [Fact]
    public void CompleteClearance_RaisesClearanceCompletedEvent()
    {
        var submission = CreateReceivedSubmission();

        submission.CompleteClearance([]);

        submission.DomainEvents.Should().ContainSingle(e => e is ClearanceCompletedEvent);
        var evt = submission.DomainEvents.OfType<ClearanceCompletedEvent>().Single();
        evt.SubmissionId.Should().Be(submission.Id);
        evt.Status.Should().Be(ClearanceStatus.Passed);
        evt.MatchCount.Should().Be(0);
    }

    [Fact]
    public void CompleteClearance_WithMatches_RaisesStatusChangedEvent()
    {
        var submission = CreateReceivedSubmission();
        var match = CreateMatch(submission.Id);

        submission.CompleteClearance([match]);

        submission.DomainEvents.Should().Contain(e => e is SubmissionStatusChangedEvent);
        submission.DomainEvents.Should().Contain(e => e is ClearanceCompletedEvent);
    }

    [Fact]
    public void CompleteClearance_WhenNotReceived_ReturnsFailure()
    {
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;
        // Status is Draft, not Received

        var result = submission.CompleteClearance([]);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.InvalidStatusTransition");
    }

    [Fact]
    public void OverrideClearance_WhenPendingClearance_ReturnsToReceived()
    {
        var submission = CreateReceivedSubmission();
        var match = CreateMatch(submission.Id);
        submission.CompleteClearance([match]);
        submission.ClearDomainEvents();

        var userId = Guid.NewGuid();
        var result = submission.OverrideClearance("Confirmed not a duplicate", userId);

        result.IsSuccess.Should().BeTrue();
        submission.ClearanceStatus.Should().Be(ClearanceStatus.Overridden);
        submission.Status.Should().Be(SubmissionStatus.Received);
        submission.ClearanceOverrideReason.Should().Be("Confirmed not a duplicate");
        submission.ClearanceOverriddenByUserId.Should().Be(userId);
        submission.ClearanceOverriddenAt.Should().NotBeNull();
    }

    [Fact]
    public void OverrideClearance_WhenNotPendingClearance_ReturnsFailure()
    {
        var submission = CreateReceivedSubmission();

        var result = submission.OverrideClearance("Some reason", Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.NotPendingClearance");
    }

    [Fact]
    public void OverrideClearance_WithEmptyReason_ReturnsFailure()
    {
        var submission = CreateReceivedSubmission();
        var match = CreateMatch(submission.Id);
        submission.CompleteClearance([match]);

        var result = submission.OverrideClearance("", Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.ClearanceOverrideReasonRequired");
    }

    [Fact]
    public void OverrideClearance_RaisesOverriddenAndStatusChangedEvents()
    {
        var submission = CreateReceivedSubmission();
        var match = CreateMatch(submission.Id);
        submission.CompleteClearance([match]);
        submission.ClearDomainEvents();

        submission.OverrideClearance("Confirmed OK", Guid.NewGuid());

        submission.DomainEvents.Should().Contain(e => e is ClearanceOverriddenEvent);
        submission.DomainEvents.Should().Contain(e => e is SubmissionStatusChangedEvent);
    }

    [Fact]
    public void Expire_WhenReceived_SetsStatusToExpired()
    {
        var submission = CreateReceivedSubmission();

        var result = submission.Expire("Policy period passed");

        result.IsSuccess.Should().BeTrue();
        submission.Status.Should().Be(SubmissionStatus.Expired);
    }

    [Fact]
    public void Expire_WhenQuoted_SetsStatusToExpired()
    {
        var submission = CreateReceivedSubmission();
        submission.AssignToUnderwriter(Guid.NewGuid(), "John Smith");
        submission.Quote(Domain.Submission.ValueObjects.Money.FromDecimal(10000));

        var result = submission.Expire("Quote expired");

        result.IsSuccess.Should().BeTrue();
        submission.Status.Should().Be(SubmissionStatus.Expired);
    }

    [Fact]
    public void Expire_WhenPendingClearance_SetsStatusToExpired()
    {
        var submission = CreateReceivedSubmission();
        var match = CreateMatch(submission.Id);
        submission.CompleteClearance([match]);

        var result = submission.Expire("No longer valid");

        result.IsSuccess.Should().BeTrue();
        submission.Status.Should().Be(SubmissionStatus.Expired);
    }

    [Fact]
    public void Expire_WhenDraft_ReturnsFailure()
    {
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured").Value;

        var result = submission.Expire("Cannot expire draft");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.InvalidStatusTransition");
    }

    [Fact]
    public void Expire_WhenBound_ReturnsFailure()
    {
        var submission = CreateReceivedSubmission();
        submission.AssignToUnderwriter(Guid.NewGuid(), "John Smith");
        submission.Quote(Domain.Submission.ValueObjects.Money.FromDecimal(10000));
        submission.Bind();

        var result = submission.Expire("Too late");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.InvalidStatusTransition");
    }

    [Fact]
    public void Expire_WhenAlreadyExpired_ReturnsFailure()
    {
        var submission = CreateReceivedSubmission();
        submission.Expire("First expire");

        var result = submission.Expire("Second expire");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.InvalidStatusTransition");
    }

    [Fact]
    public void Expire_RaisesStatusChangedEvent()
    {
        var submission = CreateReceivedSubmission();
        submission.ClearDomainEvents();

        submission.Expire("Expired");

        submission.DomainEvents.Should().ContainSingle(e => e is SubmissionStatusChangedEvent);
    }

    [Fact]
    public void Withdraw_WhenExpired_ReturnsFailure()
    {
        var submission = CreateReceivedSubmission();
        submission.Expire("Expired");

        var result = submission.Withdraw("Too late to withdraw");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.InvalidStatusTransition");
    }

    [Fact]
    public void AssignToUnderwriter_WhenPendingClearance_ReturnsFailure()
    {
        var submission = CreateReceivedSubmission();
        var match = CreateMatch(submission.Id);
        submission.CompleteClearance([match]);

        var result = submission.AssignToUnderwriter(Guid.NewGuid(), "John Smith");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.CannotAssignClosedSubmission");
    }

    [Fact]
    public void AssignToUnderwriter_WhenExpired_ReturnsFailure()
    {
        var submission = CreateReceivedSubmission();
        submission.Expire("Expired");

        var result = submission.AssignToUnderwriter(Guid.NewGuid(), "John Smith");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.CannotAssignClosedSubmission");
    }
}
