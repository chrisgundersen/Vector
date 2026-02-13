using Vector.Domain.Common;

namespace Vector.Domain.UnitTests.Common;

public class EntityTests
{
    [Fact]
    public void Equals_WithSameId_ReturnsTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act & Assert
        entity1.Should().Be(entity2);
        (entity1 == entity2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentId_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid());
        var entity2 = new TestEntity(Guid.NewGuid());

        // Act & Assert
        entity1.Should().NotBe(entity2);
        (entity1 != entity2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameId_ReturnsSameHashCode()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act & Assert
        entity1.GetHashCode().Should().Be(entity2.GetHashCode());
    }

    private sealed class TestEntity(Guid id) : Entity(id)
    {
    }
}
