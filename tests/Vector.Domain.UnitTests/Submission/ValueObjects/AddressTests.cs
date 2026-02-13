using Vector.Domain.Submission.ValueObjects;

namespace Vector.Domain.UnitTests.Submission.ValueObjects;

public class AddressTests
{
    [Fact]
    public void Create_WithValidData_ReturnsSuccess()
    {
        // Act
        var result = Address.Create("123 Main St", "Suite 100", "Austin", "TX", "78701", "USA");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Street1.Should().Be("123 Main St");
        result.Value.Street2.Should().Be("Suite 100");
        result.Value.City.Should().Be("Austin");
        result.Value.State.Should().Be("TX");
        result.Value.PostalCode.Should().Be("78701");
        result.Value.Country.Should().Be("USA");
    }

    [Fact]
    public void Create_WithNullStreet2_ReturnsSuccess()
    {
        // Act
        var result = Address.Create("123 Main St", null, "Austin", "TX", "78701");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Street2.Should().BeNull();
    }

    [Fact]
    public void Create_WithDefaultCountry_UsesUSA()
    {
        // Act
        var result = Address.Create("123 Main St", null, "Austin", "TX", "78701");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Country.Should().Be("USA");
    }

    [Fact]
    public void Create_WithEmptyStreet1_ReturnsFailure()
    {
        // Act
        var result = Address.Create("", null, "Austin", "TX", "78701");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Address.Street1Required");
    }

    [Fact]
    public void Create_WithWhitespaceStreet1_ReturnsFailure()
    {
        // Act
        var result = Address.Create("   ", null, "Austin", "TX", "78701");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Address.Street1Required");
    }

    [Fact]
    public void Create_WithEmptyCity_ReturnsFailure()
    {
        // Act
        var result = Address.Create("123 Main St", null, "", "TX", "78701");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Address.CityRequired");
    }

    [Fact]
    public void Create_WithEmptyState_ReturnsFailure()
    {
        // Act
        var result = Address.Create("123 Main St", null, "Austin", "", "78701");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Address.StateRequired");
    }

    [Fact]
    public void Create_WithEmptyPostalCode_ReturnsFailure()
    {
        // Act
        var result = Address.Create("123 Main St", null, "Austin", "TX", "");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Address.PostalCodeRequired");
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        // Act
        var result = Address.Create("  123 Main St  ", "  Suite 100  ", "  Austin  ", "  tx  ", "  78701  ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Street1.Should().Be("123 Main St");
        result.Value.Street2.Should().Be("Suite 100");
        result.Value.City.Should().Be("Austin");
        result.Value.State.Should().Be("TX");
        result.Value.PostalCode.Should().Be("78701");
    }

    [Fact]
    public void Create_UppercasesState()
    {
        // Act
        var result = Address.Create("123 Main St", null, "Austin", "tx", "78701");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.State.Should().Be("TX");
    }

    [Fact]
    public void Create_UppercasesCountry()
    {
        // Act
        var result = Address.Create("123 Main St", null, "Austin", "TX", "78701", "usa");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Country.Should().Be("USA");
    }

    [Fact]
    public void FullAddress_WithAllFields_ReturnsFormattedString()
    {
        // Arrange
        var address = Address.Create("123 Main St", "Suite 100", "Austin", "TX", "78701", "USA").Value;

        // Act
        var fullAddress = address.FullAddress;

        // Assert
        fullAddress.Should().Contain("123 Main St");
        fullAddress.Should().Contain("Suite 100");
        fullAddress.Should().Contain("Austin");
        fullAddress.Should().Contain("TX 78701");
        fullAddress.Should().Contain("USA");
    }

    [Fact]
    public void FullAddress_WithoutStreet2_OmitsStreet2()
    {
        // Arrange
        var address = Address.Create("123 Main St", null, "Austin", "TX", "78701", "USA").Value;

        // Act
        var fullAddress = address.FullAddress;

        // Assert
        fullAddress.Should().NotContain(", ,");
    }

    [Fact]
    public void ToString_ReturnsFullAddress()
    {
        // Arrange
        var address = Address.Create("123 Main St", null, "Austin", "TX", "78701", "USA").Value;

        // Act
        var result = address.ToString();

        // Assert
        result.Should().Be(address.FullAddress);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", "Suite 100", "Austin", "TX", "78701", "USA").Value;
        var address2 = Address.Create("123 Main St", "Suite 100", "Austin", "TX", "78701", "USA").Value;

        // Act & Assert
        address1.Should().Be(address2);
    }

    [Fact]
    public void Equality_DifferentStreet1_AreNotEqual()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", null, "Austin", "TX", "78701", "USA").Value;
        var address2 = Address.Create("456 Oak Ave", null, "Austin", "TX", "78701", "USA").Value;

        // Act & Assert
        address1.Should().NotBe(address2);
    }

    [Fact]
    public void Equality_DifferentCity_AreNotEqual()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", null, "Austin", "TX", "78701", "USA").Value;
        var address2 = Address.Create("123 Main St", null, "Houston", "TX", "77001", "USA").Value;

        // Act & Assert
        address1.Should().NotBe(address2);
    }

    [Fact]
    public void Equality_DifferentState_AreNotEqual()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", null, "Austin", "TX", "78701", "USA").Value;
        var address2 = Address.Create("123 Main St", null, "Austin", "CA", "78701", "USA").Value;

        // Act & Assert
        address1.Should().NotBe(address2);
    }
}
