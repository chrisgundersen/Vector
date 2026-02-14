using Microsoft.Extensions.Logging;
using Vector.Application.Common.Interfaces;
using Vector.Application.Submissions.Commands;
using Vector.Domain.Submission;
using Vector.Domain.Submission.Aggregates;

namespace Vector.Application.UnitTests.Submissions.Commands;

public class BindSubmissionCommandHandlerTests
{
    private readonly Mock<ISubmissionRepository> _submissionRepositoryMock;
    private readonly Mock<IExternalPolicyService> _externalPolicyServiceMock;
    private readonly Mock<IExternalCrmService> _externalCrmServiceMock;
    private readonly Mock<ILogger<BindSubmissionCommandHandler>> _loggerMock;
    private readonly BindSubmissionCommandHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _submissionId = Guid.NewGuid();

    public BindSubmissionCommandHandlerTests()
    {
        _submissionRepositoryMock = new Mock<ISubmissionRepository>();
        _externalPolicyServiceMock = new Mock<IExternalPolicyService>();
        _externalCrmServiceMock = new Mock<IExternalCrmService>();
        _loggerMock = new Mock<ILogger<BindSubmissionCommandHandler>>();

        _handler = new BindSubmissionCommandHandler(
            _submissionRepositoryMock.Object,
            _externalPolicyServiceMock.Object,
            _externalCrmServiceMock.Object,
            _loggerMock.Object);

        // Default PAS mock setup
        _externalPolicyServiceMock.Setup(x => x.CreatePolicyAsync(
                It.IsAny<PolicyCreationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Domain.Common.Result.Success(new PolicyCreationResult(
                "EXT-POL-001",
                "POL-2024-000001",
                DateTime.UtcNow,
                "Mock-PAS")));

        // Default CRM mock setup
        _externalCrmServiceMock.Setup(x => x.SyncCustomerAsync(
                It.IsAny<CustomerSyncRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Domain.Common.Result.Success(new CrmSyncResult(
                "CRM-CUST-001", true, DateTime.UtcNow, "Mock-CRM")));

        _externalCrmServiceMock.Setup(x => x.RecordActivityAsync(
                It.IsAny<CrmActivityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Domain.Common.Result.Success());
    }

    [Fact]
    public async Task Handle_WithQuotedSubmission_ReturnsSuccess()
    {
        // Arrange
        var command = new BindSubmissionCommand(_submissionId);

        var submissionResult = Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "ABC Manufacturing Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "Test Underwriter");
        submission.Quote(Domain.Submission.ValueObjects.Money.FromDecimal(25000, "USD"));

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(
                _submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.SubmissionNumber.Should().Be("SUB-2024-000001");
        result.Value.ExternalPolicyId.Should().Be("EXT-POL-001");
        result.Value.PolicyNumber.Should().Be("POL-2024-000001");

        submission.Status.Should().Be(Domain.Submission.Enums.SubmissionStatus.Bound);

        _submissionRepositoryMock.Verify(x => x.Update(submission), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentSubmission_ReturnsFailure()
    {
        // Arrange
        var command = new BindSubmissionCommand(_submissionId);

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
    public async Task Handle_WithNonQuotedSubmission_ReturnsFailure()
    {
        // Arrange
        var command = new BindSubmissionCommand(_submissionId);

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
        result.IsFailure.Should().BeTrue();

        _submissionRepositoryMock.Verify(x => x.Update(
            It.IsAny<Submission>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDeclinedSubmission_ReturnsFailure()
    {
        // Arrange
        var command = new BindSubmissionCommand(_submissionId);

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
    public async Task Handle_CallsExternalPolicyService()
    {
        // Arrange
        var command = new BindSubmissionCommand(_submissionId);

        var submissionResult = Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "ABC Manufacturing Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "Test Underwriter");
        submission.Quote(Domain.Submission.ValueObjects.Money.FromDecimal(25000, "USD"));

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(
                _submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _externalPolicyServiceMock.Verify(x => x.CreatePolicyAsync(
            It.Is<PolicyCreationRequest>(r =>
                r.SubmissionNumber == "SUB-2024-000001" &&
                r.InsuredName == "ABC Manufacturing Corp" &&
                r.Premium == 25000m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsExternalCrmService()
    {
        // Arrange
        var command = new BindSubmissionCommand(_submissionId);

        var submissionResult = Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "ABC Manufacturing Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "Test Underwriter");
        submission.Quote(Domain.Submission.ValueObjects.Money.FromDecimal(25000, "USD"));

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(
                _submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _externalCrmServiceMock.Verify(x => x.SyncCustomerAsync(
            It.Is<CustomerSyncRequest>(r => r.CustomerName == "ABC Manufacturing Corp"),
            It.IsAny<CancellationToken>()), Times.Once);

        _externalCrmServiceMock.Verify(x => x.RecordActivityAsync(
            It.Is<CrmActivityRequest>(r =>
                r.ActivityType == "PolicyBound" &&
                r.ReferenceNumber == "SUB-2024-000001"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPasFails_StillBindsSubmission()
    {
        // Arrange
        var command = new BindSubmissionCommand(_submissionId);

        var submissionResult = Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "ABC Manufacturing Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "Test Underwriter");
        submission.Quote(Domain.Submission.ValueObjects.Money.FromDecimal(25000, "USD"));

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(
                _submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        _externalPolicyServiceMock.Setup(x => x.CreatePolicyAsync(
                It.IsAny<PolicyCreationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Domain.Common.Result.Failure<PolicyCreationResult>(
                new Domain.Common.Error("PAS.Error", "Connection failed")));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ExternalPolicyId.Should().BeNull();
        result.Value.PolicyNumber.Should().BeNull();

        submission.Status.Should().Be(Domain.Submission.Enums.SubmissionStatus.Bound);
        _submissionRepositoryMock.Verify(x => x.Update(submission), Times.Once);
    }
}
