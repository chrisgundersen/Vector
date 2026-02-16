using Microsoft.Extensions.Logging;
using Vector.Application.Submissions.Commands;
using Vector.Domain.Submission;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Entities;
using Vector.Domain.Submission.Enums;

namespace Vector.Application.UnitTests.Submissions.Commands;

public class OverrideClearanceCommandHandlerTests
{
    private readonly Mock<ISubmissionRepository> _submissionRepositoryMock;
    private readonly Mock<ILogger<OverrideClearanceCommandHandler>> _loggerMock;
    private readonly OverrideClearanceCommandHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _submissionId = Guid.NewGuid();

    public OverrideClearanceCommandHandlerTests()
    {
        _submissionRepositoryMock = new Mock<ISubmissionRepository>();
        _loggerMock = new Mock<ILogger<OverrideClearanceCommandHandler>>();

        _handler = new OverrideClearanceCommandHandler(
            _submissionRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_OverridesClearance()
    {
        var userId = Guid.NewGuid();
        var command = new OverrideClearanceCommand(_submissionId, "Confirmed not a duplicate", userId);

        var submission = Submission.Create(_tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();
        var match = new ClearanceMatch(
            Guid.NewGuid(), submission.Id, Guid.NewGuid(),
            "SUB-2024-000099", ClearanceMatchType.FeinMatch, 1.0, "FEIN match");
        submission.CompleteClearance([match]);

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        submission.ClearanceStatus.Should().Be(ClearanceStatus.Overridden);
        submission.Status.Should().Be(SubmissionStatus.Received);
        _submissionRepositoryMock.Verify(x => x.Update(submission), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSubmissionNotFound_ReturnsFailure()
    {
        var command = new OverrideClearanceCommand(_submissionId, "Reason", Guid.NewGuid());

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Submission?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.NotFound");
    }

    [Fact]
    public async Task Handle_WhenNotPendingClearance_ReturnsFailure()
    {
        var command = new OverrideClearanceCommand(_submissionId, "Reason", Guid.NewGuid());

        var submission = Submission.Create(_tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _submissionRepositoryMock.Verify(x => x.Update(It.IsAny<Submission>()), Times.Never);
    }
}
