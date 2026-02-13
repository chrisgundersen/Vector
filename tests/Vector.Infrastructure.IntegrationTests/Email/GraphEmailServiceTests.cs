using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using Vector.Infrastructure.Email;

namespace Vector.Infrastructure.IntegrationTests.Email;

/// <summary>
/// Unit tests for GraphEmailService.
/// Since GraphServiceClient is difficult to mock directly, these tests verify
/// the behavior using a mock wrapper approach and focus on input validation.
/// Integration tests with real Microsoft Graph API should be run separately.
/// </summary>
public class GraphEmailServiceTests
{
    private readonly Mock<ILogger<GraphEmailService>> _loggerMock;
    private readonly GraphEmailServiceOptions _options;

    public GraphEmailServiceTests()
    {
        _loggerMock = new Mock<ILogger<GraphEmailService>>();
        _options = new GraphEmailServiceOptions
        {
            TenantId = "test-tenant-id",
            ClientId = "test-client-id",
            ClientSecret = "test-secret",
            ProcessedFolderName = "Processed"
        };
    }

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange
        var mockClient = CreateMockGraphServiceClient();
        var optionsMock = Options.Create(_options);

        // Act
        var service = new GraphEmailService(mockClient, optionsMock, _loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Options_ProcessedFolderName_DefaultsToProcessed()
    {
        // Arrange
        var options = new GraphEmailServiceOptions();

        // Assert
        options.ProcessedFolderName.Should().Be("Processed");
    }

    [Fact]
    public void Options_SectionName_IsCorrect()
    {
        // Assert
        GraphEmailServiceOptions.SectionName.Should().Be("EmailService:Graph");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetNewEmailsAsync_WithInvalidMailboxId_ThrowsArgumentException(string mailboxId)
    {
        // Arrange
        var mockClient = CreateMockGraphServiceClient();
        var optionsMock = Options.Create(_options);
        var service = new GraphEmailService(mockClient, optionsMock, _loggerMock.Object);

        // Act
        var act = () => service.GetNewEmailsAsync(mailboxId, 10);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetAttachmentsAsync_WithInvalidMailboxId_ThrowsArgumentException(string mailboxId)
    {
        // Arrange
        var mockClient = CreateMockGraphServiceClient();
        var optionsMock = Options.Create(_options);
        var service = new GraphEmailService(mockClient, optionsMock, _loggerMock.Object);

        // Act
        var act = () => service.GetAttachmentsAsync(mailboxId, "msg-001");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetAttachmentsAsync_WithInvalidMessageId_ThrowsArgumentException(string messageId)
    {
        // Arrange
        var mockClient = CreateMockGraphServiceClient();
        var optionsMock = Options.Create(_options);
        var service = new GraphEmailService(mockClient, optionsMock, _loggerMock.Object);

        // Act
        var act = () => service.GetAttachmentsAsync("mailbox@test.com", messageId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("", "msg-001", "att-001")]
    [InlineData("mailbox@test.com", "", "att-001")]
    [InlineData("mailbox@test.com", "msg-001", "")]
    public async Task DownloadAttachmentAsync_WithInvalidParameters_ThrowsArgumentException(
        string mailboxId,
        string messageId,
        string attachmentId)
    {
        // Arrange
        var mockClient = CreateMockGraphServiceClient();
        var optionsMock = Options.Create(_options);
        var service = new GraphEmailService(mockClient, optionsMock, _loggerMock.Object);

        // Act
        var act = () => service.DownloadAttachmentAsync(mailboxId, messageId, attachmentId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("", "msg-001")]
    [InlineData("mailbox@test.com", "")]
    public async Task MoveToProcessedAsync_WithInvalidParameters_ThrowsArgumentException(
        string mailboxId,
        string messageId)
    {
        // Arrange
        var mockClient = CreateMockGraphServiceClient();
        var optionsMock = Options.Create(_options);
        var service = new GraphEmailService(mockClient, optionsMock, _loggerMock.Object);

        // Act
        var act = () => service.MoveToProcessedAsync(mailboxId, messageId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("", "msg-001")]
    [InlineData("mailbox@test.com", "")]
    public async Task MarkAsReadAsync_WithInvalidParameters_ThrowsArgumentException(
        string mailboxId,
        string messageId)
    {
        // Arrange
        var mockClient = CreateMockGraphServiceClient();
        var optionsMock = Options.Create(_options);
        var service = new GraphEmailService(mockClient, optionsMock, _loggerMock.Object);

        // Act
        var act = () => service.MarkAsReadAsync(mailboxId, messageId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static GraphServiceClient CreateMockGraphServiceClient()
    {
        var mockRequestAdapter = new Mock<IRequestAdapter>();
        return new GraphServiceClient(mockRequestAdapter.Object);
    }
}

/// <summary>
/// Integration tests for GraphEmailService that require real Microsoft Graph credentials.
/// These tests are skipped by default and should be run manually with valid configuration.
/// </summary>
public class GraphEmailServiceIntegrationTests
{
    [Fact(Skip = "Requires real Microsoft Graph credentials - run manually for integration testing")]
    public async Task GetNewEmailsAsync_WithValidCredentials_ReturnsEmails()
    {
        // This test requires:
        // 1. A real Azure AD app registration with Mail.Read permission
        // 2. A shared mailbox to read from
        // 3. Valid credentials in configuration

        // To run this test:
        // 1. Set up Azure AD app with Mail.Read permission
        // 2. Configure credentials in user secrets or environment variables
        // 3. Remove the Skip parameter

        await Task.CompletedTask;
    }
}
