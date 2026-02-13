using Vector.Application.EmailIntake.Queries;
using Vector.Domain.EmailIntake;
using Vector.Domain.EmailIntake.Aggregates;

namespace Vector.Application.UnitTests.EmailIntake.Queries;

public class GetInboundEmailQueryHandlerTests
{
    private readonly Mock<IInboundEmailRepository> _emailRepositoryMock;
    private readonly GetInboundEmailQueryHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();

    public GetInboundEmailQueryHandlerTests()
    {
        _emailRepositoryMock = new Mock<IInboundEmailRepository>();
        _handler = new GetInboundEmailQueryHandler(_emailRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ReturnsDto()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var query = new GetInboundEmailQuery(emailId);

        var emailResult = InboundEmail.Create(
            _tenantId,
            "msg-001",
            "submissions@example.com",
            "broker@test.com",
            "Submission: ABC Manufacturing",
            "Please find attached...",
            "Full email body content",
            DateTime.UtcNow.AddHours(-1));
        var email = emailResult.Value;

        _emailRepositoryMock.Setup(x => x.GetByIdAsync(
                emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(email.Id);
        result.TenantId.Should().Be(_tenantId);
        result.ExternalMessageId.Should().Be("msg-001");
        result.MailboxId.Should().Be("submissions@example.com");
        result.FromAddress.Should().Be("broker@test.com");
        result.Subject.Should().Be("Submission: ABC Manufacturing");
        result.Status.Should().Be("Received");
    }

    [Fact]
    public async Task Handle_WithNonExistentEmail_ReturnsNull()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var query = new GetInboundEmailQuery(emailId);

        _emailRepositoryMock.Setup(x => x.GetByIdAsync(
                emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InboundEmail?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithEmailAndAttachments_ReturnsAttachmentsInDto()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var query = new GetInboundEmailQuery(emailId);

        var emailResult = InboundEmail.Create(
            _tenantId,
            "msg-001",
            "submissions@example.com",
            "broker@test.com",
            "Submission: ABC Manufacturing",
            "Please find attached...",
            "Full email body content",
            DateTime.UtcNow.AddHours(-1));
        var email = emailResult.Value;

        email.AddAttachment(
            "ACORD125.pdf",
            "application/pdf",
            245000,
            new byte[100],
            "https://storage.example.com/attachments/acord125.pdf");

        email.AddAttachment(
            "LossRuns.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            85000,
            new byte[50],
            "https://storage.example.com/attachments/lossruns.xlsx");

        _emailRepositoryMock.Setup(x => x.GetByIdAsync(
                emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.AttachmentCount.Should().Be(2);
        result.Attachments.Should().HaveCount(2);
        result.Attachments.Select(a => a.FileName).Should().Contain("ACORD125.pdf");
        result.Attachments.Select(a => a.FileName).Should().Contain("LossRuns.xlsx");
    }

    [Fact]
    public async Task Handle_WithProcessingEmail_ReturnsCorrectStatus()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var query = new GetInboundEmailQuery(emailId);

        var emailResult = InboundEmail.Create(
            _tenantId,
            "msg-001",
            "submissions@example.com",
            "broker@test.com",
            "Submission: ABC Manufacturing",
            "Please find attached...",
            "Full email body content",
            DateTime.UtcNow.AddHours(-1));
        var email = emailResult.Value;
        email.StartProcessing();

        _emailRepositoryMock.Setup(x => x.GetByIdAsync(
                emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be("Processing");
    }

    [Fact]
    public async Task Handle_WithCompletedEmail_ReturnsCorrectStatus()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var query = new GetInboundEmailQuery(emailId);

        var emailResult = InboundEmail.Create(
            _tenantId,
            "msg-001",
            "submissions@example.com",
            "broker@test.com",
            "Submission: ABC Manufacturing",
            "Please find attached...",
            "Full email body content",
            DateTime.UtcNow.AddHours(-1));
        var email = emailResult.Value;
        email.StartProcessing();
        email.CompleteProcessing();

        _emailRepositoryMock.Setup(x => x.GetByIdAsync(
                emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task Handle_WithAttachmentDetails_ReturnsFullAttachmentInfo()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var query = new GetInboundEmailQuery(emailId);

        var emailResult = InboundEmail.Create(
            _tenantId,
            "msg-001",
            "submissions@example.com",
            "broker@test.com",
            "Submission: ABC Manufacturing",
            "Please find attached...",
            "Full email body content",
            DateTime.UtcNow.AddHours(-1));
        var email = emailResult.Value;

        email.AddAttachment(
            "ACORD125.pdf",
            "application/pdf",
            245000,
            new byte[100],
            "https://storage.example.com/attachments/acord125.pdf");

        _emailRepositoryMock.Setup(x => x.GetByIdAsync(
                emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var attachment = result!.Attachments.First();
        attachment.FileName.Should().Be("ACORD125.pdf");
        attachment.ContentType.Should().Be("application/pdf");
        attachment.SizeInBytes.Should().Be(245000);
        attachment.BlobStorageUrl.Should().Be("https://storage.example.com/attachments/acord125.pdf");
        attachment.Status.Should().Be("Extracted");
    }
}
