using Vector.Domain.Common;

namespace Vector.Domain.UnitTests.Common;

public class AuditableAggregateRootTests
{
    private sealed class TestAuditableAggregate : AuditableAggregateRoot
    {
        public TestAuditableAggregate() : base()
        {
        }

        public TestAuditableAggregate(Guid id) : base(id)
        {
        }
    }

    private sealed class TestAuditableAggregateWithTypedId : AuditableAggregateRoot<int>
    {
        public TestAuditableAggregateWithTypedId(int id) : base(id)
        {
        }
    }

    [Fact]
    public void Constructor_GeneratesNewGuid()
    {
        // Act
        var aggregate = new TestAuditableAggregate();

        // Assert
        aggregate.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_WithGuid_UsesProvidedId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var aggregate = new TestAuditableAggregate(id);

        // Assert
        aggregate.Id.Should().Be(id);
    }

    [Fact]
    public void SetCreatedAudit_SetsCreatedAtAndCreatedBy()
    {
        // Arrange
        var aggregate = new TestAuditableAggregate();
        var userId = "user@example.com";

        // Act
        aggregate.SetCreatedAudit(userId);

        // Assert
        aggregate.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        aggregate.CreatedBy.Should().Be(userId);
    }

    [Fact]
    public void SetCreatedAudit_WithNullUserId_SetsNullCreatedBy()
    {
        // Arrange
        var aggregate = new TestAuditableAggregate();

        // Act
        aggregate.SetCreatedAudit(null);

        // Assert
        aggregate.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        aggregate.CreatedBy.Should().BeNull();
    }

    [Fact]
    public void SetModifiedAudit_SetsLastModifiedAtAndLastModifiedBy()
    {
        // Arrange
        var aggregate = new TestAuditableAggregate();
        var userId = "user@example.com";

        // Act
        aggregate.SetModifiedAudit(userId);

        // Assert
        aggregate.LastModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        aggregate.LastModifiedBy.Should().Be(userId);
    }

    [Fact]
    public void SetModifiedAudit_WithNullUserId_SetsNullLastModifiedBy()
    {
        // Arrange
        var aggregate = new TestAuditableAggregate();

        // Act
        aggregate.SetModifiedAudit(null);

        // Assert
        aggregate.LastModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        aggregate.LastModifiedBy.Should().BeNull();
    }

    [Fact]
    public void TypedIdConstructor_UsesProvidedId()
    {
        // Arrange
        var id = 42;

        // Act
        var aggregate = new TestAuditableAggregateWithTypedId(id);

        // Assert
        aggregate.Id.Should().Be(id);
    }

    [Fact]
    public void AuditProperties_InitiallyEmpty()
    {
        // Arrange & Act
        var aggregate = new TestAuditableAggregate();

        // Assert
        aggregate.CreatedAt.Should().Be(default(DateTime));
        aggregate.CreatedBy.Should().BeNull();
        aggregate.LastModifiedAt.Should().BeNull();
        aggregate.LastModifiedBy.Should().BeNull();
    }

    [Fact]
    public void SetCreatedAudit_DoesNotAffectModifiedAudit()
    {
        // Arrange
        var aggregate = new TestAuditableAggregate();

        // Act
        aggregate.SetCreatedAudit("creator@example.com");

        // Assert
        aggregate.LastModifiedAt.Should().BeNull();
        aggregate.LastModifiedBy.Should().BeNull();
    }

    [Fact]
    public void SetModifiedAudit_DoesNotAffectCreatedAudit()
    {
        // Arrange
        var aggregate = new TestAuditableAggregate();
        aggregate.SetCreatedAudit("creator@example.com");
        var originalCreatedAt = aggregate.CreatedAt;

        // Act
        aggregate.SetModifiedAudit("modifier@example.com");

        // Assert
        aggregate.CreatedAt.Should().Be(originalCreatedAt);
        aggregate.CreatedBy.Should().Be("creator@example.com");
    }

    [Fact]
    public void SetModifiedAudit_CanBeCalledMultipleTimes()
    {
        // Arrange
        var aggregate = new TestAuditableAggregate();
        aggregate.SetModifiedAudit("user1@example.com");
        var firstModifiedAt = aggregate.LastModifiedAt;

        // Wait a bit to ensure time difference
        Thread.Sleep(10);

        // Act
        aggregate.SetModifiedAudit("user2@example.com");

        // Assert
        aggregate.LastModifiedBy.Should().Be("user2@example.com");
        aggregate.LastModifiedAt.Should().BeOnOrAfter(firstModifiedAt!.Value);
    }
}
