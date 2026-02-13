using System.Reflection;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Vector.Infrastructure.Messaging;

namespace Vector.Infrastructure.IntegrationTests.Messaging;

public class AzureServiceBusServiceTests : IAsyncDisposable
{
    private readonly Mock<ServiceBusClient> _mockClient;
    private readonly Mock<ServiceBusSender> _mockSender;
    private readonly AzureServiceBusService _service;
    private readonly List<ServiceBusMessage> _sentMessages = [];

    public AzureServiceBusServiceTests()
    {
        _mockClient = new Mock<ServiceBusClient>();
        _mockSender = new Mock<ServiceBusSender>();

        _mockSender
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((msg, _) => _sentMessages.Add(msg))
            .Returns(Task.CompletedTask);

        _mockSender
            .Setup(s => s.ScheduleMessageAsync(
                It.IsAny<ServiceBusMessage>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(12345L);

        _mockClient
            .Setup(c => c.CreateSender(It.IsAny<string>()))
            .Returns(_mockSender.Object);

        var loggerMock = new Mock<ILogger<AzureServiceBusService>>();
        _service = new AzureServiceBusService(_mockClient.Object, loggerMock.Object);
    }

    public record TestMessage(string Content, int Value);

    [Fact]
    public async Task PublishAsync_SendsMessageToQueue()
    {
        // Arrange
        var message = new TestMessage("Hello", 42);

        // Act
        await _service.PublishAsync("test-queue", message);

        // Assert
        _mockSender.Verify(
            s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_CreatesSenderForQueue()
    {
        // Arrange
        var message = new TestMessage("Hello", 42);

        // Act
        await _service.PublishAsync("my-queue", message);

        // Assert
        _mockClient.Verify(c => c.CreateSender("my-queue"), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ReusesSenderForSameQueue()
    {
        // Arrange
        var message1 = new TestMessage("First", 1);
        var message2 = new TestMessage("Second", 2);

        // Act
        await _service.PublishAsync("same-queue", message1);
        await _service.PublishAsync("same-queue", message2);

        // Assert
        _mockClient.Verify(c => c.CreateSender("same-queue"), Times.Once);
        _mockSender.Verify(
            s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task PublishAsync_SetsMessageProperties()
    {
        // Arrange
        var message = new TestMessage("Hello", 42);

        // Act
        await _service.PublishAsync("test-queue", message);

        // Assert
        _sentMessages.Should().HaveCount(1);
        var sentMessage = _sentMessages.First();

        sentMessage.MessageId.Should().NotBeNullOrEmpty();
        sentMessage.ContentType.Should().Be("application/json");
        sentMessage.Subject.Should().Be("TestMessage");
        sentMessage.ApplicationProperties.Should().ContainKey("MessageType");
        sentMessage.ApplicationProperties.Should().ContainKey("Timestamp");
    }

    [Fact]
    public async Task PublishAsync_WithSessionId_SetsSessionId()
    {
        // Arrange
        var message = new TestMessage("Hello", 42);
        var sessionId = "session-123";

        // Act
        await _service.PublishAsync("test-queue", message, sessionId);

        // Assert
        _sentMessages.Should().HaveCount(1);
        var sentMessage = _sentMessages.First();
        sentMessage.SessionId.Should().Be(sessionId);
    }

    [Fact]
    public async Task PublishAsync_WithoutSessionId_NoSessionIdSet()
    {
        // Arrange
        var message = new TestMessage("Hello", 42);

        // Act
        await _service.PublishAsync("test-queue", message);

        // Assert
        _sentMessages.Should().HaveCount(1);
        var sentMessage = _sentMessages.First();
        sentMessage.SessionId.Should().BeNull();
    }

    [Fact]
    public async Task PublishAsync_SerializesMessageAsJson()
    {
        // Arrange
        var message = new TestMessage("Hello World", 999);

        // Act
        await _service.PublishAsync("test-queue", message);

        // Assert
        _sentMessages.Should().HaveCount(1);
        var sentMessage = _sentMessages.First();
        var body = sentMessage.Body.ToString();

        body.Should().Contain("content");
        body.Should().Contain("Hello World");
        body.Should().Contain("value");
        body.Should().Contain("999");
    }

    [Fact]
    public async Task ScheduleAsync_SchedulesMessageForFutureDelivery()
    {
        // Arrange
        var message = new TestMessage("Scheduled", 100);
        var scheduledTime = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        await _service.ScheduleAsync("test-queue", message, scheduledTime);

        // Assert
        _mockSender.Verify(
            s => s.ScheduleMessageAsync(
                It.IsAny<ServiceBusMessage>(),
                scheduledTime,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _service.PublishAsync<TestMessage>("test-queue", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task PublishAsync_WithInvalidQueueName_ThrowsArgumentException(string queueName)
    {
        // Arrange
        var message = new TestMessage("Hello", 42);

        // Act
        var act = () => _service.PublishAsync(queueName, message);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DisposeAsync_DisposesSenders()
    {
        // Arrange
        var message = new TestMessage("Hello", 42);
        await _service.PublishAsync("queue-1", message);
        await _service.PublishAsync("queue-2", message);

        _mockSender
            .Setup(s => s.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        // Act
        await _service.DisposeAsync();

        // Assert
        _mockSender.Verify(s => s.DisposeAsync(), Times.AtLeast(1));
    }

    [Fact]
    public async Task PublishAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        await _service.DisposeAsync();
        var message = new TestMessage("Hello", 42);

        // Act
        var act = () => _service.PublishAsync("test-queue", message);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_OnlyDisposesOnce()
    {
        // Arrange
        var message = new TestMessage("Hello", 42);
        await _service.PublishAsync("test-queue", message);

        var disposeCount = 0;
        _mockSender
            .Setup(s => s.DisposeAsync())
            .Callback(() => disposeCount++)
            .Returns(ValueTask.CompletedTask);

        // Act
        await _service.DisposeAsync();
        await _service.DisposeAsync();
        await _service.DisposeAsync();

        // Assert
        disposeCount.Should().Be(1);
    }

    public async ValueTask DisposeAsync()
    {
        await _service.DisposeAsync();
    }
}
