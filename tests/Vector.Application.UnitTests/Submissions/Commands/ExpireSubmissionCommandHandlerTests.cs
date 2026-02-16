using Microsoft.Extensions.Logging;
using Vector.Application.Submissions.Commands;
using Vector.Domain.Submission;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Enums;

namespace Vector.Application.UnitTests.Submissions.Commands;

public class ExpireSubmissionCommandHandlerTests
{
    private readonly Mock<ISubmissionRepository> _submissionRepositoryMock;
    private readonly Mock<ILogger<ExpireSubmissionCommandHandler>> _loggerMock;
    private readonly ExpireSubmissionCommandHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _submissionId = Guid.NewGuid();

    public ExpireSubmissionCommandHandlerTests()
    {
        _submissionRepositoryMock = new Mock<ISubmissionRepository>();
        _loggerMock = new Mock<ILogger<ExpireSubmissionCommandHandler>>();

        _handler = new ExpireSubmissionCommandHandler(
            _submissionRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ExpiresSubmission()
    {
        var command = new ExpireSubmissionCommand(_submissionId, "Policy period passed");

        var submission = Submission.Create(_tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.MarkAsReceived();

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        submission.Status.Should().Be(SubmissionStatus.Expired);
        _submissionRepositoryMock.Verify(x => x.Update(submission), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSubmissionNotFound_ReturnsFailure()
    {
        var command = new ExpireSubmissionCommand(_submissionId, "Reason");

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(_submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Submission?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.NotFound");
    }

    [Fact]
    public async Task Handle_WhenCannotExpire_ReturnsFailure()
    {
        var command = new ExpireSubmissionCommand(_submissionId, "Cannot expire");

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
}
