using Microsoft.Extensions.Logging;
using Vector.Application.Submissions.Commands;
using Vector.Domain.Submission;
using Vector.Domain.Submission.Aggregates;

namespace Vector.Application.UnitTests.Submissions.Commands;

public class AssignSubmissionCommandHandlerTests
{
    private readonly Mock<ISubmissionRepository> _submissionRepositoryMock;
    private readonly Mock<ILogger<AssignSubmissionCommandHandler>> _loggerMock;
    private readonly AssignSubmissionCommandHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _submissionId = Guid.NewGuid();
    private readonly Guid _underwriterId = Guid.NewGuid();

    public AssignSubmissionCommandHandlerTests()
    {
        _submissionRepositoryMock = new Mock<ISubmissionRepository>();
        _loggerMock = new Mock<ILogger<AssignSubmissionCommandHandler>>();

        _handler = new AssignSubmissionCommandHandler(
            _submissionRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidSubmission_ReturnsSuccess()
    {
        // Arrange
        var command = new AssignSubmissionCommand(
            _submissionId,
            _underwriterId,
            "John Underwriter");

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
        submission.AssignedUnderwriterId.Should().Be(_underwriterId);
        submission.AssignedUnderwriterName.Should().Be("John Underwriter");
        submission.AssignedAt.Should().NotBeNull();

        _submissionRepositoryMock.Verify(x => x.Update(submission), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentSubmission_ReturnsFailure()
    {
        // Arrange
        var command = new AssignSubmissionCommand(
            _submissionId,
            _underwriterId,
            "John Underwriter");

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
    public async Task Handle_WithDeclinedSubmission_ReturnsFailure()
    {
        // Arrange
        var command = new AssignSubmissionCommand(
            _submissionId,
            _underwriterId,
            "John Underwriter");

        var submissionResult = Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "ABC Manufacturing Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();
        submission.Decline("Not in appetite");

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(
                _submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.CannotAssignClosedSubmission");

        _submissionRepositoryMock.Verify(x => x.Update(
            It.IsAny<Submission>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithBoundSubmission_ReturnsFailure()
    {
        // Arrange
        var command = new AssignSubmissionCommand(
            _submissionId,
            _underwriterId,
            "John Underwriter");

        var submissionResult = Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "ABC Manufacturing Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "Initial UW");
        submission.Quote(Vector.Domain.Submission.ValueObjects.Money.FromDecimal(10000, "USD"));
        submission.Bind();

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(
                _submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.CannotAssignClosedSubmission");
    }

    [Fact]
    public async Task Handle_WithWithdrawnSubmission_ReturnsFailure()
    {
        // Arrange
        var command = new AssignSubmissionCommand(
            _submissionId,
            _underwriterId,
            "John Underwriter");

        var submissionResult = Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "ABC Manufacturing Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();
        submission.Withdraw("Producer withdrew");

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(
                _submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.CannotAssignClosedSubmission");
    }

    [Fact]
    public async Task Handle_TransitionsReceivedToInReview()
    {
        // Arrange
        var command = new AssignSubmissionCommand(
            _submissionId,
            _underwriterId,
            "John Underwriter");

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
        submission.Status.Should().Be(Domain.Submission.Enums.SubmissionStatus.InReview);
    }

    [Fact]
    public async Task Handle_WithAlreadyInReviewSubmission_DoesNotChangeStatus()
    {
        // Arrange
        var command = new AssignSubmissionCommand(
            _submissionId,
            _underwriterId,
            "Jane Underwriter");

        var submissionResult = Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "ABC Manufacturing Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "Initial UW");

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(
                _submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        submission.Status.Should().Be(Domain.Submission.Enums.SubmissionStatus.InReview);
        submission.AssignedUnderwriterName.Should().Be("Jane Underwriter");
    }
}
