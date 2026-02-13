using Microsoft.Extensions.Logging;
using Vector.Infrastructure.Messaging;

namespace Vector.Infrastructure.IntegrationTests.Messaging;

public class InMemoryMessageBusServiceTests
{
    private readonly InMemoryMessageBusService _messageBus;

    public InMemoryMessageBusServiceTests()
    {
        var loggerMock = new Mock<ILogger<InMemoryMessageBusService>>();
        _messageBus = new InMemoryMessageBusService(loggerMock.Object);
    }

    public record TestMessage(string Content, int Value);

    [Fact]
    public async Task PublishAsync_EnqueuesMessage()
    {
        // Arrange
        var message = new TestMessage("Hello", 42);

        // Act
        await _messageBus.PublishAsync("test-queue", message);

        // Assert
        var count = _messageBus.GetQueueCount("test-queue");
        count.Should().Be(1);
    }

    [Fact]
    public async Task PublishAsync_WithSessionId_EnqueuesMessage()
    {
        // Arrange
        var message = new TestMessage("Hello", 42);

        // Act
        await _messageBus.PublishAsync("test-queue", message, "session-123");

        // Assert
        var count = _messageBus.GetQueueCount("test-queue");
        count.Should().Be(1);
    }

    [Fact]
    public async Task PublishAsync_MultipleMessages_AllEnqueued()
    {
        // Arrange
        var message1 = new TestMessage("First", 1);
        var message2 = new TestMessage("Second", 2);
        var message3 = new TestMessage("Third", 3);

        // Act
        await _messageBus.PublishAsync("test-queue", message1);
        await _messageBus.PublishAsync("test-queue", message2);
        await _messageBus.PublishAsync("test-queue", message3);

        // Assert
        var count = _messageBus.GetQueueCount("test-queue");
        count.Should().Be(3);
    }

    [Fact]
    public async Task TryDequeue_ReturnsFirstMessage()
    {
        // Arrange
        var message = new TestMessage("Hello", 42);
        await _messageBus.PublishAsync("test-queue", message);

        // Act
        var dequeued = _messageBus.TryDequeue<TestMessage>("test-queue");

        // Assert
        dequeued.Should().NotBeNull();
        dequeued!.Content.Should().Be("Hello");
        dequeued.Value.Should().Be(42);
    }

    [Fact]
    public async Task TryDequeue_RemovesMessage()
    {
        // Arrange
        var message = new TestMessage("Hello", 42);
        await _messageBus.PublishAsync("test-queue", message);

        // Act
        _messageBus.TryDequeue<TestMessage>("test-queue");

        // Assert
        var count = _messageBus.GetQueueCount("test-queue");
        count.Should().Be(0);
    }

    [Fact]
    public async Task TryDequeue_PreservesFifoOrder()
    {
        // Arrange
        await _messageBus.PublishAsync("test-queue", new TestMessage("First", 1));
        await _messageBus.PublishAsync("test-queue", new TestMessage("Second", 2));

        // Act & Assert
        var first = _messageBus.TryDequeue<TestMessage>("test-queue");
        first!.Content.Should().Be("First");

        var second = _messageBus.TryDequeue<TestMessage>("test-queue");
        second!.Content.Should().Be("Second");
    }

    [Fact]
    public void TryDequeue_EmptyQueue_ReturnsNull()
    {
        // Act
        var result = _messageBus.TryDequeue<TestMessage>("empty-queue");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryDequeue_NonExistentQueue_ReturnsNull()
    {
        // Act
        var result = _messageBus.TryDequeue<TestMessage>("non-existent-queue");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetQueueCount_EmptyQueue_ReturnsZero()
    {
        // Act
        var count = _messageBus.GetQueueCount("empty-queue");

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void GetQueueCount_NonExistentQueue_ReturnsZero()
    {
        // Act
        var count = _messageBus.GetQueueCount("non-existent-queue");

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task ScheduleAsync_EnqueuesMessage()
    {
        // Arrange
        var message = new TestMessage("Scheduled", 100);
        var scheduledTime = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        await _messageBus.ScheduleAsync("test-queue", message, scheduledTime);

        // Assert (in-memory impl publishes immediately)
        var count = _messageBus.GetQueueCount("test-queue");
        count.Should().Be(1);
    }

    [Fact]
    public async Task PublishAsync_DifferentQueues_QueuesAreSeparate()
    {
        // Arrange
        var message1 = new TestMessage("Queue1", 1);
        var message2 = new TestMessage("Queue2", 2);

        // Act
        await _messageBus.PublishAsync("queue-1", message1);
        await _messageBus.PublishAsync("queue-2", message2);

        // Assert
        _messageBus.GetQueueCount("queue-1").Should().Be(1);
        _messageBus.GetQueueCount("queue-2").Should().Be(1);

        var fromQueue1 = _messageBus.TryDequeue<TestMessage>("queue-1");
        fromQueue1!.Content.Should().Be("Queue1");

        var fromQueue2 = _messageBus.TryDequeue<TestMessage>("queue-2");
        fromQueue2!.Content.Should().Be("Queue2");
    }
}
