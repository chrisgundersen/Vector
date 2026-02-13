using Vector.Domain.Common;

namespace Vector.Domain.UnitTests.Common;

public class ValueObjectTests
{
    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var value1 = new TestValueObject("Test", 42);
        var value2 = new TestValueObject("Test", 42);

        // Act & Assert
        value1.Should().Be(value2);
        (value1 == value2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        var value1 = new TestValueObject("Test", 42);
        var value2 = new TestValueObject("Test", 43);

        // Act & Assert
        value1.Should().NotBe(value2);
        (value1 != value2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameValues_ReturnsSameHashCode()
    {
        // Arrange
        var value1 = new TestValueObject("Test", 42);
        var value2 = new TestValueObject("Test", 42);

        // Act & Assert
        value1.GetHashCode().Should().Be(value2.GetHashCode());
    }

    private sealed class TestValueObject(string name, int number) : ValueObject
    {
        public string Name { get; } = name;
        public int Number { get; } = number;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Name;
            yield return Number;
        }
    }
}
