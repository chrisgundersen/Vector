using Vector.Domain.EmailIntake.Aggregates;

namespace Vector.Domain.UnitTests.EmailIntake;

public class InboundEmailTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var externalMessageId = "msg-123";
        var mailboxId = "inbox@test.com";
        var fromAddress = "sender@example.com";
        var subject = "Test Subject";
        var bodyPreview = "Test body preview";
        var bodyContent = "Test body content";

        // Act
        var result = InboundEmail.Create(
            _tenantId,
            externalMessageId,
            mailboxId,
            fromAddress,
            subject,
            bodyPreview,
            bodyContent,
            DateTime.UtcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(_tenantId);
        result.Value.ExternalMessageId.Should().Be(externalMessageId);
        result.Value.MailboxId.Should().Be(mailboxId);
        result.Value.FromAddress.Value.Should().Be(fromAddress);
        result.Value.Subject.Should().Be(subject);
        result.Value.Status.Should().Be(InboundEmailStatus.Received);
    }

    [Fact]
    public void Create_WithEmptyTenantId_ReturnsFailure()
    {
        // Act
        var result = InboundEmail.Create(
            Guid.Empty,
            "msg-123",
            "inbox@test.com",
            "sender@example.com",
            "Subject",
            "Body preview",
            "Body content",
            DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("InboundEmail.InvalidTenant");
    }

    [Fact]
    public void Create_WithInvalidEmailAddress_ReturnsFailure()
    {
        // Act
        var result = InboundEmail.Create(
            _tenantId,
            "msg-123",
            "inbox@test.com",
            "invalid-email",
            "Subject",
            "Body preview",
            "Body content",
            DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EmailAddress.InvalidFormat");
    }

    [Fact]
    public void AddAttachment_ToReceivedEmail_ReturnsSuccess()
    {
        // Arrange
        var emailResult = InboundEmail.Create(
            _tenantId,
            "msg-123",
            "inbox@test.com",
            "sender@example.com",
            "Subject",
            "Body preview",
            "Body content",
            DateTime.UtcNow);

        var email = emailResult.Value;
        var content = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var attachmentResult = email.AddAttachment(
            "test.pdf",
            "application/pdf",
            content.Length,
            content,
            "https://storage/test.pdf");

        // Assert
        attachmentResult.IsSuccess.Should().BeTrue();
        email.Attachments.Should().HaveCount(1);
        email.Attachments.First().Metadata.FileName.Should().Be("test.pdf");
    }

    [Fact]
    public void StartProcessing_FromReceivedStatus_RaisesEmailReceivedEvent()
    {
        // Arrange
        var emailResult = InboundEmail.Create(
            _tenantId,
            "msg-123",
            "inbox@test.com",
            "sender@example.com",
            "Subject",
            "Body preview",
            "Body content",
            DateTime.UtcNow);

        var email = emailResult.Value;

        // Act
        email.StartProcessing();

        // Assert
        email.Status.Should().Be(InboundEmailStatus.Processing);
        email.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void Create_WithEmptyExternalMessageId_ReturnsFailure()
    {
        // Act
        var result = InboundEmail.Create(
            _tenantId,
            "",
            "inbox@test.com",
            "sender@example.com",
            "Subject",
            "Body preview",
            "Body content",
            DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("InboundEmail.ExternalMessageIdRequired");
    }

    [Fact]
    public void Create_WithEmptyMailboxId_ReturnsFailure()
    {
        // Act
        var result = InboundEmail.Create(
            _tenantId,
            "msg-123",
            "",
            "sender@example.com",
            "Subject",
            "Body preview",
            "Body content",
            DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("InboundEmail.MailboxIdRequired");
    }

    [Fact]
    public void AddAttachment_ToCompletedEmail_ReturnsFailure()
    {
        // Arrange
        var email = InboundEmail.Create(
            _tenantId, "msg-123", "inbox@test.com",
            "sender@example.com", "Subject", "Preview", "Content",
            DateTime.UtcNow).Value;
        email.CompleteProcessing();

        // Act
        var result = email.AddAttachment(
            "test.pdf", "application/pdf", 100,
            new byte[] { 1, 2, 3 }, "https://storage/test.pdf");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("InboundEmail.CannotModifyCompletedEmail");
    }

    [Fact]
    public void AddAttachment_ToFailedEmail_ReturnsFailure()
    {
        // Arrange
        var email = InboundEmail.Create(
            _tenantId, "msg-123", "inbox@test.com",
            "sender@example.com", "Subject", "Preview", "Content",
            DateTime.UtcNow).Value;
        email.FailProcessing("Test error");

        // Act
        var result = email.AddAttachment(
            "test.pdf", "application/pdf", 100,
            new byte[] { 1, 2, 3 }, "https://storage/test.pdf");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("InboundEmail.CannotModifyCompletedEmail");
    }

    [Fact]
    public void AddAttachment_WithInvalidFileName_ReturnsFailure()
    {
        // Arrange
        var email = InboundEmail.Create(
            _tenantId, "msg-123", "inbox@test.com",
            "sender@example.com", "Subject", "Preview", "Content",
            DateTime.UtcNow).Value;

        // Act
        var result = email.AddAttachment(
            "", "application/pdf", 100,
            new byte[] { 1, 2, 3 }, "https://storage/test.pdf");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AttachmentMetadata.FileNameEmpty");
    }

    [Fact]
    public void StartProcessing_WhenAlreadyProcessing_DoesNotChangeStatus()
    {
        // Arrange
        var email = InboundEmail.Create(
            _tenantId, "msg-123", "inbox@test.com",
            "sender@example.com", "Subject", "Preview", "Content",
            DateTime.UtcNow).Value;
        email.StartProcessing();
        email.ClearDomainEvents();

        // Act
        email.StartProcessing();

        // Assert
        email.Status.Should().Be(InboundEmailStatus.Processing);
        email.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void CompleteProcessing_UpdatesStatusAndRaisesEvent()
    {
        // Arrange
        var email = InboundEmail.Create(
            _tenantId, "msg-123", "inbox@test.com",
            "sender@example.com", "Subject", "Preview", "Content",
            DateTime.UtcNow).Value;

        // Add and process an attachment
        email.AddAttachment("test.pdf", "application/pdf", 100,
            new byte[] { 1, 2, 3 }, "https://storage/test.pdf");
        email.ClearDomainEvents();

        // Act
        email.CompleteProcessing();

        // Assert
        email.Status.Should().Be(InboundEmailStatus.Completed);
        email.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void FailProcessing_SetsStatusAndErrorMessage()
    {
        // Arrange
        var email = InboundEmail.Create(
            _tenantId, "msg-123", "inbox@test.com",
            "sender@example.com", "Subject", "Preview", "Content",
            DateTime.UtcNow).Value;

        // Act
        email.FailProcessing("Test error message");

        // Assert
        email.Status.Should().Be(InboundEmailStatus.Failed);
        email.ProcessingError.Should().Be("Test error message");
    }

    [Fact]
    public void EmailAttachment_MarkAsProcessed_UpdatesStatus()
    {
        // Arrange
        var email = InboundEmail.Create(
            _tenantId, "msg-123", "inbox@test.com",
            "sender@example.com", "Subject", "Preview", "Content",
            DateTime.UtcNow).Value;
        var attachmentResult = email.AddAttachment(
            "test.pdf", "application/pdf", 100,
            new byte[] { 1, 2, 3 }, "https://storage/test.pdf");
        var attachment = attachmentResult.Value;

        // Act
        attachment.MarkAsProcessed();

        // Assert
        attachment.Status.Should().Be(EmailAttachmentStatus.Processed);
    }

    [Fact]
    public void EmailAttachment_MarkAsFailed_SetsStatusAndReason()
    {
        // Arrange
        var email = InboundEmail.Create(
            _tenantId, "msg-123", "inbox@test.com",
            "sender@example.com", "Subject", "Preview", "Content",
            DateTime.UtcNow).Value;
        var attachmentResult = email.AddAttachment(
            "test.pdf", "application/pdf", 100,
            new byte[] { 1, 2, 3 }, "https://storage/test.pdf");
        var attachment = attachmentResult.Value;

        // Act
        attachment.MarkAsFailed("Processing error");

        // Assert
        attachment.Status.Should().Be(EmailAttachmentStatus.Failed);
        attachment.FailureReason.Should().Be("Processing error");
    }

    [Fact]
    public void CompleteProcessing_CountsProcessedAndFailedAttachments()
    {
        // Arrange
        var email = InboundEmail.Create(
            _tenantId, "msg-123", "inbox@test.com",
            "sender@example.com", "Subject", "Preview", "Content",
            DateTime.UtcNow).Value;

        var attachment1 = email.AddAttachment("file1.pdf", "application/pdf", 100,
            new byte[] { 1 }, "https://storage/file1.pdf").Value;
        var attachment2 = email.AddAttachment("file2.pdf", "application/pdf", 100,
            new byte[] { 2 }, "https://storage/file2.pdf").Value;

        attachment1.MarkAsProcessed();
        attachment2.MarkAsFailed("Error");
        email.ClearDomainEvents();

        // Act
        email.CompleteProcessing();

        // Assert
        email.Status.Should().Be(InboundEmailStatus.Completed);
    }
}
