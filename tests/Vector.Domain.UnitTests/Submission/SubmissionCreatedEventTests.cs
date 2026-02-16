using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Events;

namespace Vector.Domain.UnitTests.Submission;

public class SubmissionCreatedEventTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void MarkAsReceived_RaisesSubmissionCreatedEvent_WithSubmissionNumber()
    {
        var submissionNumber = "SUB-2024-000099";
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId,
            submissionNumber,
            "Test Corp").Value;

        submission.MarkAsReceived();

        var createdEvent = submission.DomainEvents
            .OfType<SubmissionCreatedEvent>()
            .Single();

        createdEvent.SubmissionNumber.Should().Be(submissionNumber);
        createdEvent.SubmissionId.Should().Be(submission.Id);
        createdEvent.TenantId.Should().Be(_tenantId);
        createdEvent.InsuredName.Should().Be("Test Corp");
    }

    [Fact]
    public void MarkAsReceived_RaisesSubmissionStatusChangedEvent()
    {
        var submission = Domain.Submission.Aggregates.Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "Test Corp").Value;

        submission.MarkAsReceived();

        var statusEvent = submission.DomainEvents
            .OfType<SubmissionStatusChangedEvent>()
            .Single();

        statusEvent.PreviousStatus.Should().Be(Domain.Submission.Enums.SubmissionStatus.Draft);
        statusEvent.NewStatus.Should().Be(Domain.Submission.Enums.SubmissionStatus.Received);
    }
}
