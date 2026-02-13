using Microsoft.Extensions.Logging;
using Vector.Application.Submissions.Commands;
using Vector.Domain.Submission;
using Vector.Domain.Submission.Aggregates;

namespace Vector.Application.UnitTests.Submissions.Commands;

public class CreateSubmissionCommandHandlerTests
{
    private readonly Mock<ISubmissionRepository> _submissionRepositoryMock;
    private readonly Mock<ILogger<CreateSubmissionCommandHandler>> _loggerMock;
    private readonly CreateSubmissionCommandHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateSubmissionCommandHandlerTests()
    {
        _submissionRepositoryMock = new Mock<ISubmissionRepository>();
        _loggerMock = new Mock<ILogger<CreateSubmissionCommandHandler>>();

        _handler = new CreateSubmissionCommandHandler(
            _submissionRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsSuccessWithSubmissionId()
    {
        // Arrange
        var command = new CreateSubmissionCommand(
            _tenantId,
            "ABC Manufacturing Corp");

        _submissionRepositoryMock.Setup(x => x.GenerateSubmissionNumberAsync(
                _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("SUB-2024-000001");

        _submissionRepositoryMock.Setup(x => x.AddAsync(
                It.IsAny<Submission>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _submissionRepositoryMock.Verify(x => x.AddAsync(
            It.Is<Submission>(s =>
                s.TenantId == _tenantId &&
                s.Insured.Name == "ABC Manufacturing Corp" &&
                s.SubmissionNumber == "SUB-2024-000001"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithProcessingJobId_CreatesSubmissionWithReference()
    {
        // Arrange
        var processingJobId = Guid.NewGuid();
        var command = new CreateSubmissionCommand(
            _tenantId,
            "ABC Manufacturing Corp",
            processingJobId);

        _submissionRepositoryMock.Setup(x => x.GenerateSubmissionNumberAsync(
                _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("SUB-2024-000002");

        _submissionRepositoryMock.Setup(x => x.AddAsync(
                It.IsAny<Submission>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _submissionRepositoryMock.Verify(x => x.AddAsync(
            It.Is<Submission>(s => s.ProcessingJobId == processingJobId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInboundEmailId_CreatesSubmissionWithReference()
    {
        // Arrange
        var inboundEmailId = Guid.NewGuid();
        var command = new CreateSubmissionCommand(
            _tenantId,
            "ABC Manufacturing Corp",
            null,
            inboundEmailId);

        _submissionRepositoryMock.Setup(x => x.GenerateSubmissionNumberAsync(
                _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("SUB-2024-000003");

        _submissionRepositoryMock.Setup(x => x.AddAsync(
                It.IsAny<Submission>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _submissionRepositoryMock.Verify(x => x.AddAsync(
            It.Is<Submission>(s => s.InboundEmailId == inboundEmailId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyTenantId_ReturnsFailure()
    {
        // Arrange
        var command = new CreateSubmissionCommand(
            Guid.Empty,
            "ABC Manufacturing Corp");

        _submissionRepositoryMock.Setup(x => x.GenerateSubmissionNumberAsync(
                Guid.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync("SUB-2024-000001");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.InvalidTenant");

        _submissionRepositoryMock.Verify(x => x.AddAsync(
            It.IsAny<Submission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyInsuredName_ReturnsFailure()
    {
        // Arrange
        var command = new CreateSubmissionCommand(
            _tenantId,
            "");

        _submissionRepositoryMock.Setup(x => x.GenerateSubmissionNumberAsync(
                _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("SUB-2024-000001");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.InsuredNameRequired");
    }

    [Fact]
    public async Task Handle_WithWhitespaceInsuredName_ReturnsFailure()
    {
        // Arrange
        var command = new CreateSubmissionCommand(
            _tenantId,
            "   ");

        _submissionRepositoryMock.Setup(x => x.GenerateSubmissionNumberAsync(
                _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("SUB-2024-000001");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Submission.InsuredNameRequired");
    }

    [Fact]
    public async Task Handle_CreatesSubmissionWithReceivedStatus()
    {
        // Arrange
        var command = new CreateSubmissionCommand(
            _tenantId,
            "ABC Manufacturing Corp");

        Submission? capturedSubmission = null;

        _submissionRepositoryMock.Setup(x => x.GenerateSubmissionNumberAsync(
                _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("SUB-2024-000001");

        _submissionRepositoryMock.Setup(x => x.AddAsync(
                It.IsAny<Submission>(), It.IsAny<CancellationToken>()))
            .Callback<Submission, CancellationToken>((s, _) => capturedSubmission = s)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedSubmission.Should().NotBeNull();
        capturedSubmission!.Status.Should().Be(Domain.Submission.Enums.SubmissionStatus.Received);
    }
}
