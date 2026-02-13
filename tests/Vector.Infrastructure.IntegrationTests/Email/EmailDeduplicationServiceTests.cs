using Microsoft.Extensions.Logging;
using Vector.Application.Common.Interfaces;
using Vector.Infrastructure.Caching;
using Vector.Infrastructure.Email;

namespace Vector.Infrastructure.IntegrationTests.Email;

public class EmailDeduplicationServiceTests
{
    private readonly EmailDeduplicationService _deduplicationService;
    private readonly ICacheService _cacheService;

    public EmailDeduplicationServiceTests()
    {
        _cacheService = new InMemoryCacheService();

        var loggerMock = new Mock<ILogger<EmailDeduplicationService>>();
        _deduplicationService = new EmailDeduplicationService(_cacheService, loggerMock.Object);
    }

    [Fact]
    public async Task IsProcessedAsync_WhenNotProcessed_ReturnsFalse()
    {
        // Arrange
        var contentHash = "abc123def456";

        // Act
        var result = await _deduplicationService.IsProcessedAsync(contentHash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsProcessedAsync_WhenProcessed_ReturnsTrue()
    {
        // Arrange
        var contentHash = "abc123def456";
        await _deduplicationService.MarkAsProcessedAsync(contentHash);

        // Act
        var result = await _deduplicationService.IsProcessedAsync(contentHash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_StoresInCache()
    {
        // Arrange
        var contentHash = "test-hash-789";

        // Act
        await _deduplicationService.MarkAsProcessedAsync(contentHash);

        // Assert
        var isProcessed = await _deduplicationService.IsProcessedAsync(contentHash);
        isProcessed.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_WithCustomExpiry_AcceptsExpiry()
    {
        // Arrange
        var contentHash = "expiry-test-hash";
        var customExpiry = TimeSpan.FromMinutes(5);

        // Act
        await _deduplicationService.MarkAsProcessedAsync(contentHash, customExpiry);

        // Assert
        var isProcessed = await _deduplicationService.IsProcessedAsync(contentHash);
        isProcessed.Should().BeTrue();
    }

    [Fact]
    public async Task IsProcessedAsync_DifferentHashes_ReturnsCorrectly()
    {
        // Arrange
        var hash1 = "hash-one";
        var hash2 = "hash-two";
        await _deduplicationService.MarkAsProcessedAsync(hash1);

        // Act
        var result1 = await _deduplicationService.IsProcessedAsync(hash1);
        var result2 = await _deduplicationService.IsProcessedAsync(hash2);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeFalse();
    }

    [Fact]
    public async Task IsMessageProcessedAsync_WhenNotProcessed_ReturnsFalse()
    {
        // Arrange
        var mailboxId = "submissions@example.com";
        var messageId = "msg-001";

        // Act
        var result = await _deduplicationService.IsMessageProcessedAsync(mailboxId, messageId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsMessageProcessedAsync_WhenProcessed_ReturnsTrue()
    {
        // Arrange
        var mailboxId = "submissions@example.com";
        var messageId = "msg-002";
        await _deduplicationService.MarkMessageAsProcessedAsync(mailboxId, messageId);

        // Act
        var result = await _deduplicationService.IsMessageProcessedAsync(mailboxId, messageId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task MarkMessageAsProcessedAsync_StoresInCache()
    {
        // Arrange
        var mailboxId = "test@example.com";
        var messageId = "test-msg-001";

        // Act
        await _deduplicationService.MarkMessageAsProcessedAsync(mailboxId, messageId);

        // Assert
        var isProcessed = await _deduplicationService.IsMessageProcessedAsync(mailboxId, messageId);
        isProcessed.Should().BeTrue();
    }

    [Fact]
    public async Task IsMessageProcessedAsync_SameMessageDifferentMailbox_ReturnsFalse()
    {
        // Arrange
        var mailbox1 = "mailbox1@example.com";
        var mailbox2 = "mailbox2@example.com";
        var messageId = "common-msg-id";

        await _deduplicationService.MarkMessageAsProcessedAsync(mailbox1, messageId);

        // Act
        var result = await _deduplicationService.IsMessageProcessedAsync(mailbox2, messageId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsMessageProcessedAsync_DifferentMessagesSameMailbox_ReturnsCorrectly()
    {
        // Arrange
        var mailboxId = "shared@example.com";
        var msg1 = "msg-1";
        var msg2 = "msg-2";

        await _deduplicationService.MarkMessageAsProcessedAsync(mailboxId, msg1);

        // Act
        var result1 = await _deduplicationService.IsMessageProcessedAsync(mailboxId, msg1);
        var result2 = await _deduplicationService.IsMessageProcessedAsync(mailboxId, msg2);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task IsProcessedAsync_WithInvalidContentHash_ThrowsArgumentException(string contentHash)
    {
        // Act
        var act = () => _deduplicationService.IsProcessedAsync(contentHash);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task MarkAsProcessedAsync_WithInvalidContentHash_ThrowsArgumentException(string contentHash)
    {
        // Act
        var act = () => _deduplicationService.MarkAsProcessedAsync(contentHash);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("", "msg-001")]
    [InlineData(" ", "msg-001")]
    [InlineData("mailbox@test.com", "")]
    [InlineData("mailbox@test.com", " ")]
    public async Task IsMessageProcessedAsync_WithInvalidParameters_ThrowsArgumentException(
        string mailboxId,
        string messageId)
    {
        // Act
        var act = () => _deduplicationService.IsMessageProcessedAsync(mailboxId, messageId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("", "msg-001")]
    [InlineData(" ", "msg-001")]
    [InlineData("mailbox@test.com", "")]
    [InlineData("mailbox@test.com", " ")]
    public async Task MarkMessageAsProcessedAsync_WithInvalidParameters_ThrowsArgumentException(
        string mailboxId,
        string messageId)
    {
        // Act
        var act = () => _deduplicationService.MarkMessageAsProcessedAsync(mailboxId, messageId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
