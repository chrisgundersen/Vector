using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Application.Common;
using Vector.Domain.Common;
using Vector.Infrastructure.Services;

namespace Vector.Infrastructure.IntegrationTests.Services;

public class MediatRDomainEventDispatcherTests
{
    private readonly Mock<IPublisher> _publisherMock;
    private readonly Mock<ILogger<MediatRDomainEventDispatcher>> _loggerMock;
    private readonly MediatRDomainEventDispatcher _dispatcher;

    public MediatRDomainEventDispatcherTests()
    {
        _publisherMock = new Mock<IPublisher>();
        _loggerMock = new Mock<ILogger<MediatRDomainEventDispatcher>>();
        _dispatcher = new MediatRDomainEventDispatcher(_publisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task DispatchEventsAsync_WithSingleEvent_PublishesNotification()
    {
        var domainEvent = new TestDomainEvent("test-data");
        object? captured = null;

        _publisherMock.Setup(p => p.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((notification, _) => captured = notification);

        await _dispatcher.DispatchEventsAsync([domainEvent]);

        _publisherMock.Verify(
            p => p.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once);
        captured.Should().BeOfType<DomainEventNotification<TestDomainEvent>>();
        ((DomainEventNotification<TestDomainEvent>)captured!).DomainEvent.Should().Be(domainEvent);
    }

    [Fact]
    public async Task DispatchEventsAsync_WithMultipleEvents_PublishesAllNotifications()
    {
        var event1 = new TestDomainEvent("data-1");
        var event2 = new AnotherTestDomainEvent(42);

        await _dispatcher.DispatchEventsAsync([event1, event2]);

        _publisherMock.Verify(
            p => p.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task DispatchEventsAsync_WithNoEvents_DoesNotPublish()
    {
        await _dispatcher.DispatchEventsAsync([]);

        _publisherMock.Verify(
            p => p.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DispatchEventsAsync_CreatesCorrectGenericNotificationType()
    {
        var domainEvent = new TestDomainEvent("typed-check");
        object? capturedNotification = null;

        _publisherMock.Setup(p => p.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((notification, _) => capturedNotification = notification);

        await _dispatcher.DispatchEventsAsync([domainEvent]);

        capturedNotification.Should().NotBeNull();
        capturedNotification.Should().BeOfType<DomainEventNotification<TestDomainEvent>>();
        ((DomainEventNotification<TestDomainEvent>)capturedNotification!).DomainEvent.Data.Should().Be("typed-check");
    }

    [Fact]
    public async Task DispatchEventsAsync_WhenHandlerThrows_PropagatesException()
    {
        var domainEvent = new TestDomainEvent("will-fail");

        _publisherMock.Setup(p => p.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Handler failure"));

        var act = () => _dispatcher.DispatchEventsAsync([domainEvent]);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Handler failure");
    }

    [Fact]
    public async Task DispatchEventsAsync_WithMultipleEvents_WhenSecondHandlerThrows_FirstIsStillPublished()
    {
        var event1 = new TestDomainEvent("first");
        var event2 = new AnotherTestDomainEvent(42);
        var publishCount = 0;

        _publisherMock.Setup(p => p.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((_, _) =>
            {
                publishCount++;
                if (publishCount == 2)
                    throw new InvalidOperationException("Second handler failed");
            });

        var act = () => _dispatcher.DispatchEventsAsync([event1, event2]);

        await act.Should().ThrowAsync<InvalidOperationException>();
        publishCount.Should().Be(2);
    }

    private sealed record TestDomainEvent(string Data) : DomainEvent;
    private sealed record AnotherTestDomainEvent(int Value) : DomainEvent;
}
