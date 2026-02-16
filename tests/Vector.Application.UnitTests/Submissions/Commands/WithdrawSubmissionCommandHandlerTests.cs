using Microsoft.Extensions.Logging;
using Vector.Application.Submissions.Commands;
using Vector.Domain.Submission;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Enums;

namespace Vector.Application.UnitTests.Submissions.Commands;

public class WithdrawSubmissionCommandHandlerTests
{
    private readonly Mock<ISubmissionRepository> _submissionRepositoryMock;
    private readonly Mock<ILogger<WithdrawSubmissionCommandHandler>> _loggerMock;
    private readonly WithdrawSubmissionCommandHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _submissionId = Guid.NewGuid();

    public WithdrawSubmissionCommandHandlerTests()
    {
        _submissionRepositoryMock = new Mock<ISubmissionRepository>();
        _loggerMock = new Mock<ILogger<WithdrawSubmissionCommandHandler>>();

        _handler = new WithdrawSubmissionCommandHandler(
            _submissionRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_WithdrawsSubmission()
    {
        var command = new WithdrawSubmissionCommand(_submissionId, "Customer changed mind");

        var submission = Submission.Create(_tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        submission.Status.Should().Be(SubmissionStatus.Withdrawn);
        _submissionRepositoryMock.Verify(x => x.Update(submission), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSubmissionNotFound_ReturnsFailure()
    {
        var command = new WithdrawSubmissionCommand(_submissionId, "Reason");

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Submission?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.NotFound");
    }

    [Fact]
    public async Task Handle_WhenCannotWithdraw_ReturnsFailure()
    {
        var command = new WithdrawSubmissionCommand(_submissionId, "Too late");

        var submission = Submission.Create(_tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "John Smith");
        submission.Quote(Domain.Submission.ValueObjects.Money.FromDecimal(10000));
        submission.Bind();

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _submissionRepositoryMock.Verify(x => x.Update(It.IsAny<Submission>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNullReason_UsesDefaultReason()
    {
        var command = new WithdrawSubmissionCommand(_submissionId, null);

        var submission = Submission.Create(_tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        submission.Status.Should().Be(SubmissionStatus.Withdrawn);
    }
}
