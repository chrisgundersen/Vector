using Microsoft.Extensions.Logging;
using Vector.Application.Submissions.Commands;
using Vector.Domain.Submission;
using Vector.Domain.Submission.Aggregates;

namespace Vector.Application.UnitTests.Submissions.Commands;

public class DeclineSubmissionCommandHandlerTests
{
    private readonly Mock<ISubmissionRepository> _submissionRepositoryMock;
    private readonly Mock<ILogger<DeclineSubmissionCommandHandler>> _loggerMock;
    private readonly DeclineSubmissionCommandHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _submissionId = Guid.NewGuid();

    public DeclineSubmissionCommandHandlerTests()
    {
        _submissionRepositoryMock = new Mock<ISubmissionRepository>();
        _loggerMock = new Mock<ILogger<DeclineSubmissionCommandHandler>>();

        _handler = new DeclineSubmissionCommandHandler(
            _submissionRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidSubmission_ReturnsSuccess()
    {
        // Arrange
        var command = new DeclineSubmissionCommand(_submissionId, "Outside appetite - geographic restrictions");

        var submissionResult = Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "ABC Manufacturing Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(
                _submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        submission.Status.Should().Be(Domain.Submission.Enums.SubmissionStatus.Declined);

        _submissionRepositoryMock.Verify(x => x.Update(submission), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentSubmission_ReturnsFailure()
    {
        // Arrange
        var command = new DeclineSubmissionCommand(_submissionId, "Not in appetite");

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(
                _submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Submission?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.NotFound");

        _submissionRepositoryMock.Verify(x => x.Update(
            It.IsAny<Submission>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAlreadyDeclinedSubmission_ReturnsFailure()
    {
        // Arrange
        var command = new DeclineSubmissionCommand(_submissionId, "Another reason");

        var submissionResult = Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "ABC Manufacturing Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();
        submission.Decline("First decline reason");

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(
                _submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();

        _submissionRepositoryMock.Verify(x => x.Update(
            It.IsAny<Submission>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithBoundSubmission_ReturnsFailure()
    {
        // Arrange
        var command = new DeclineSubmissionCommand(_submissionId, "Too late to decline");

        var submissionResult = Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "ABC Manufacturing Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "Test UW");
        submission.Quote(Domain.Submission.ValueObjects.Money.FromDecimal(10000, "USD"));
        submission.Bind();

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(
                _submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();

        _submissionRepositoryMock.Verify(x => x.Update(
            It.IsAny<Submission>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInReviewSubmission_ReturnsSuccess()
    {
        // Arrange
        var command = new DeclineSubmissionCommand(_submissionId, "Poor loss history");

        var submissionResult = Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "ABC Manufacturing Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "Test Underwriter");

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(
                _submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        submission.Status.Should().Be(Domain.Submission.Enums.SubmissionStatus.Declined);
    }
}
