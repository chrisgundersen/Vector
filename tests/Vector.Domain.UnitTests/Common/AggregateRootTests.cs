using Vector.Domain.Common;

namespace Vector.Domain.UnitTests.Common;

public class AggregateRootTests
{
    [Fact]
    public void AddDomainEvent_AddsEventToCollection()
    {
        // Arrange
        var aggregate = new TestAggregate();
        var domainEvent = new TestDomainEvent();

        // Act
        aggregate.RaiseEvent(domainEvent);

        // Assert
        aggregate.DomainEvents.Should().ContainSingle();
        aggregate.DomainEvents.Should().Contain(domainEvent);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.RaiseEvent(new TestDomainEvent());
        aggregate.RaiseEvent(new TestDomainEvent());

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }

    private sealed class TestAggregate : AggregateRoot
    {
        public void RaiseEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
    }

    private sealed record TestDomainEvent : DomainEvent;
}
