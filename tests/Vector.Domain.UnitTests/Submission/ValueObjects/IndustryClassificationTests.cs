using Vector.Domain.Submission.ValueObjects;

namespace Vector.Domain.UnitTests.Submission.ValueObjects;

public class IndustryClassificationTests
{
    [Fact]
    public void Create_WithValidData_ReturnsSuccess()
    {
        // Act
        var result = IndustryClassification.Create("332618", "5051", "Metal Stamping");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NaicsCode.Should().Be("332618");
        result.Value.SicCode.Should().Be("5051");
        result.Value.Description.Should().Be("Metal Stamping");
    }

    [Fact]
    public void Create_WithNullSicCode_ReturnsSuccess()
    {
        // Act
        var result = IndustryClassification.Create("541511", null, "Custom Computer Programming Services");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SicCode.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        // Act
        var result = IndustryClassification.Create("332618", "  5051  ", "  Metal Stamping  ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SicCode.Should().Be("5051");
        result.Value.Description.Should().Be("Metal Stamping");
    }

    [Fact]
    public void Create_WithEmptyNaicsCode_ReturnsFailure()
    {
        // Act
        var result = IndustryClassification.Create("", "5051", "Metal Stamping");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IndustryClassification.NaicsCodeRequired");
    }

    [Fact]
    public void Create_WithNullNaicsCode_ReturnsFailure()
    {
        // Act
        var result = IndustryClassification.Create(null!, "5051", "Metal Stamping");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IndustryClassification.NaicsCodeRequired");
    }

    [Fact]
    public void Create_WithWhitespaceNaicsCode_ReturnsFailure()
    {
        // Act
        var result = IndustryClassification.Create("   ", "5051", "Metal Stamping");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IndustryClassification.NaicsCodeRequired");
    }

    [Fact]
    public void Create_WithTooShortNaicsCode_ReturnsFailure()
    {
        // Act
        var result = IndustryClassification.Create("1", "5051", "Description");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IndustryClassification.InvalidNaicsCode");
    }

    [Fact]
    public void Create_WithTooLongNaicsCode_ReturnsFailure()
    {
        // Act
        var result = IndustryClassification.Create("1234567", "5051", "Description");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IndustryClassification.InvalidNaicsCode");
    }

    [Fact]
    public void Create_WithNonNumericNaicsCode_ReturnsFailure()
    {
        // Act
        var result = IndustryClassification.Create("33A618", "5051", "Description");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IndustryClassification.InvalidNaicsCode");
    }

    [Fact]
    public void Create_WithEmptyDescription_ReturnsFailure()
    {
        // Act
        var result = IndustryClassification.Create("332618", "5051", "");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IndustryClassification.DescriptionRequired");
    }

    [Fact]
    public void Create_WithWhitespaceDescription_ReturnsFailure()
    {
        // Act
        var result = IndustryClassification.Create("332618", "5051", "   ");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IndustryClassification.DescriptionRequired");
    }

    [Theory]
    [InlineData("11", "11")]
    [InlineData("332618", "33")]
    [InlineData("541511", "54")]
    public void Sector_ReturnsTwoDigitPrefix(string naicsCode, string expectedSector)
    {
        // Arrange
        var classification = IndustryClassification.Create(naicsCode, null, "Test Description").Value;

        // Act
        var sector = classification.Sector;

        // Assert
        sector.Should().Be(expectedSector);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var classification = IndustryClassification.Create("332618", "5051", "Metal Stamping").Value;

        // Act
        var result = classification.ToString();

        // Assert
        result.Should().Be("332618 - Metal Stamping");
    }

    [Fact]
    public void Equality_WithSameNaicsAndSic_AreEqual()
    {
        // Arrange
        var classification1 = IndustryClassification.Create("332618", "5051", "Metal Stamping").Value;
        var classification2 = IndustryClassification.Create("332618", "5051", "Different Description").Value;

        // Assert
        classification1.Should().Be(classification2);
    }

    [Fact]
    public void Equality_WithDifferentNaicsCode_AreNotEqual()
    {
        // Arrange
        var classification1 = IndustryClassification.Create("332618", "5051", "Metal Stamping").Value;
        var classification2 = IndustryClassification.Create("541511", "5051", "Metal Stamping").Value;

        // Assert
        classification1.Should().NotBe(classification2);
    }

    [Fact]
    public void Equality_WithDifferentSicCode_AreNotEqual()
    {
        // Arrange
        var classification1 = IndustryClassification.Create("332618", "5051", "Description").Value;
        var classification2 = IndustryClassification.Create("332618", "5052", "Description").Value;

        // Assert
        classification1.Should().NotBe(classification2);
    }

    [Fact]
    public void Equality_WithNullSicCode_BothNull_AreEqual()
    {
        // Arrange
        var classification1 = IndustryClassification.Create("332618", null, "Description 1").Value;
        var classification2 = IndustryClassification.Create("332618", null, "Description 2").Value;

        // Assert
        classification1.Should().Be(classification2);
    }

    [Fact]
    public void Equality_WithNullSicCode_OneNull_AreNotEqual()
    {
        // Arrange
        var classification1 = IndustryClassification.Create("332618", "5051", "Description").Value;
        var classification2 = IndustryClassification.Create("332618", null, "Description").Value;

        // Assert
        classification1.Should().NotBe(classification2);
    }
}
