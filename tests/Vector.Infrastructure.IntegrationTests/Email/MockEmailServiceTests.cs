using Microsoft.Extensions.Logging;
using Vector.Infrastructure.Email;

namespace Vector.Infrastructure.IntegrationTests.Email;

public class MockEmailServiceTests
{
    private readonly MockEmailService _emailService;

    public MockEmailServiceTests()
    {
        var loggerMock = new Mock<ILogger<MockEmailService>>();
        _emailService = new MockEmailService(loggerMock.Object);
    }

    [Fact]
    public async Task GetNewEmailsAsync_ReturnsEmails()
    {
        // Act
        var emails = await _emailService.GetNewEmailsAsync("submissions@example.com", 10);

        // Assert
        emails.Should().NotBeEmpty();
        emails.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetNewEmailsAsync_RespectsMaxResults()
    {
        // Act
        var emails = await _emailService.GetNewEmailsAsync("submissions@example.com", 1);

        // Assert
        emails.Should().HaveCountLessOrEqualTo(1);
    }

    [Fact]
    public async Task GetNewEmailsAsync_ReturnsEmailsWithRequiredFields()
    {
        // Act
        var emails = await _emailService.GetNewEmailsAsync("submissions@example.com", 10);

        // Assert
        emails.Should().AllSatisfy(email =>
        {
            email.MessageId.Should().NotBeNullOrEmpty();
            email.Subject.Should().NotBeNullOrEmpty();
            email.FromAddress.Should().NotBeNullOrEmpty();
            email.FromName.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task GetAttachmentsAsync_ReturnsAttachments()
    {
        // Act
        var attachments = await _emailService.GetAttachmentsAsync("submissions@example.com", "msg-001");

        // Assert
        attachments.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAttachmentsAsync_ReturnsAttachmentsWithRequiredFields()
    {
        // Act
        var attachments = await _emailService.GetAttachmentsAsync("submissions@example.com", "msg-001");

        // Assert
        attachments.Should().AllSatisfy(attachment =>
        {
            attachment.AttachmentId.Should().NotBeNullOrEmpty();
            attachment.FileName.Should().NotBeNullOrEmpty();
            attachment.ContentType.Should().NotBeNullOrEmpty();
            attachment.Size.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public async Task DownloadAttachmentAsync_ReturnsContent()
    {
        // Act
        var content = await _emailService.DownloadAttachmentAsync(
            "submissions@example.com",
            "msg-001",
            "att-001");

        // Assert
        content.Should().NotBeNull();
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task MoveToProcessedAsync_CompletesWithoutError()
    {
        // Act & Assert (should not throw)
        await _emailService.MoveToProcessedAsync("submissions@example.com", "msg-001");
    }

    [Fact]
    public async Task MarkAsReadAsync_CompletesWithoutError()
    {
        // Act & Assert (should not throw)
        await _emailService.MarkAsReadAsync("submissions@example.com", "msg-001");
    }
}
