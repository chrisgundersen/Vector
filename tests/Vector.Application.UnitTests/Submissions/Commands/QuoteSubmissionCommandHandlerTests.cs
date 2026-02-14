using Microsoft.Extensions.Logging;
using Vector.Application.Submissions.Commands;
using Vector.Domain.Submission;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.ValueObjects;

namespace Vector.Application.UnitTests.Submissions.Commands;

public class QuoteSubmissionCommandHandlerTests
{
    private readonly Mock<ISubmissionRepository> _submissionRepositoryMock;
    private readonly Mock<ILogger<QuoteSubmissionCommandHandler>> _loggerMock;
    private readonly QuoteSubmissionCommandHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _submissionId = Guid.NewGuid();

    public QuoteSubmissionCommandHandlerTests()
    {
        _submissionRepositoryMock = new Mock<ISubmissionRepository>();
        _loggerMock = new Mock<ILogger<QuoteSubmissionCommandHandler>>();

        _handler = new QuoteSubmissionCommandHandler(
            _submissionRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidSubmission_ReturnsSuccess()
    {
        // Arrange
        var command = new QuoteSubmissionCommand(_submissionId, 25000m, "USD");

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
        submission.QuotedPremium.Should().NotBeNull();
        submission.QuotedPremium!.Amount.Should().Be(25000m);
        submission.QuotedPremium.Currency.Should().Be("USD");

        _submissionRepositoryMock.Verify(x => x.Update(submission), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentSubmission_ReturnsFailure()
    {
        // Arrange
        var command = new QuoteSubmissionCommand(_submissionId, 25000m, "USD");

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
        var command = new QuoteSubmissionCommand(_submissionId, 25000m, "USD");

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

        _submissionRepositoryMock.Verify(x => x.Update(
            It.IsAny<Submission>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDifferentCurrency_UsesSpecifiedCurrency()
    {
        // Arrange
        var command = new QuoteSubmissionCommand(_submissionId, 30000m, "CAD");

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
        submission.QuotedPremium!.Currency.Should().Be("CAD");
    }

    [Fact]
    public async Task Handle_TransitionsSubmissionToQuoted()
    {
        // Arrange
        var command = new QuoteSubmissionCommand(_submissionId, 25000m, "USD");

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
        submission.Status.Should().Be(Domain.Submission.Enums.SubmissionStatus.Quoted);
    }
}
