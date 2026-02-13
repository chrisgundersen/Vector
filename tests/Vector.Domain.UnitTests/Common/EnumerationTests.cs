using Vector.Domain.Common;

namespace Vector.Domain.UnitTests.Common;

public class EnumerationTests
{
    private sealed class TestEnumeration : Enumeration<TestEnumeration>
    {
        public static readonly TestEnumeration Value1 = new(1, "First");
        public static readonly TestEnumeration Value2 = new(2, "Second");
        public static readonly TestEnumeration Value3 = new(3, "Third");

        private TestEnumeration(int value, string name) : base(value, name)
        {
        }
    }

    [Fact]
    public void FromValue_WithExistingValue_ReturnsEnumeration()
    {
        // Act
        var result = TestEnumeration.FromValue(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(TestEnumeration.Value1);
    }

    [Fact]
    public void FromValue_WithNonExistingValue_ReturnsNull()
    {
        // Act
        var result = TestEnumeration.FromValue(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromName_WithExistingName_ReturnsEnumeration()
    {
        // Act
        var result = TestEnumeration.FromName("Second");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(TestEnumeration.Value2);
    }

    [Fact]
    public void FromName_WithDifferentCase_ReturnsEnumeration()
    {
        // Act
        var result = TestEnumeration.FromName("THIRD");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(TestEnumeration.Value3);
    }

    [Fact]
    public void FromName_WithNonExistingName_ReturnsNull()
    {
        // Act
        var result = TestEnumeration.FromName("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetAll_ReturnsAllValues()
    {
        // Act
        var result = TestEnumeration.GetAll();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(TestEnumeration.Value1);
        result.Should().Contain(TestEnumeration.Value2);
        result.Should().Contain(TestEnumeration.Value3);
    }

    [Fact]
    public void Value_ReturnsCorrectValue()
    {
        // Assert
        TestEnumeration.Value1.Value.Should().Be(1);
        TestEnumeration.Value2.Value.Should().Be(2);
        TestEnumeration.Value3.Value.Should().Be(3);
    }

    [Fact]
    public void Name_ReturnsCorrectName()
    {
        // Assert
        TestEnumeration.Value1.Name.Should().Be("First");
        TestEnumeration.Value2.Name.Should().Be("Second");
        TestEnumeration.Value3.Name.Should().Be("Third");
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        // Act
        var result = TestEnumeration.Value1.ToString();

        // Assert
        result.Should().Be("First");
    }

    [Fact]
    public void Equals_WithSameInstance_ReturnsTrue()
    {
        // Arrange
        var value1 = TestEnumeration.Value1;
        var value2 = TestEnumeration.Value1;

        // Act
        var result = value1.Equals(value2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentInstance_ReturnsFalse()
    {
        // Arrange
        var value1 = TestEnumeration.Value1;
        var value2 = TestEnumeration.Value2;

        // Act
        var result = value1.Equals(value2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var value = TestEnumeration.Value1;

        // Act
        var result = value.Equals(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithObject_ReturnsCorrectResult()
    {
        // Arrange
        var value1 = TestEnumeration.Value1;
        object value2 = TestEnumeration.Value1;
        object value3 = TestEnumeration.Value2;

        // Assert
        value1.Equals(value2).Should().BeTrue();
        value1.Equals(value3).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        // Arrange
        var value = TestEnumeration.Value1;
        var otherObject = "Not an enumeration";

        // Act
        var result = value.Equals(otherObject);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameValue_ReturnsSameHash()
    {
        // Arrange
        var value1 = TestEnumeration.Value1;
        var value2 = TestEnumeration.FromValue(1);

        // Assert
        value1.GetHashCode().Should().Be(value2!.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentValue_ReturnsDifferentHash()
    {
        // Arrange
        var value1 = TestEnumeration.Value1;
        var value2 = TestEnumeration.Value2;

        // Assert
        value1.GetHashCode().Should().NotBe(value2.GetHashCode());
    }
}
