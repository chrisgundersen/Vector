using Vector.Domain.DocumentProcessing.ValueObjects;

namespace Vector.Domain.UnitTests.DocumentProcessing.ValueObjects;

public class ExtractedFieldTests
{
    [Fact]
    public void Create_WithValidData_ReturnsSuccess()
    {
        // Act
        var result = ExtractedField.Create("InsuredName", "ABC Manufacturing", 0.95m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FieldName.Should().Be("InsuredName");
        result.Value.Value.Should().Be("ABC Manufacturing");
        result.Value.Confidence.Score.Should().Be(0.95m);
    }

    [Fact]
    public void Create_WithBoundingBoxAndPage_ReturnsSuccess()
    {
        // Act
        var result = ExtractedField.Create("InsuredName", "ABC Manufacturing", 0.95m, "[10,20,200,40]", 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BoundingBox.Should().Be("[10,20,200,40]");
        result.Value.PageNumber.Should().Be(1);
    }

    [Fact]
    public void Create_WithNullValue_ReturnsSuccess()
    {
        // Act
        var result = ExtractedField.Create("OptionalField", null, 0.90m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyFieldName_ReturnsFailure()
    {
        // Act
        var result = ExtractedField.Create("", "Value", 0.95m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ExtractedField.FieldNameRequired");
    }

    [Fact]
    public void Create_WithWhitespaceFieldName_ReturnsFailure()
    {
        // Act
        var result = ExtractedField.Create("   ", "Value", 0.95m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ExtractedField.FieldNameRequired");
    }

    [Fact]
    public void Create_WithInvalidConfidence_ReturnsFailure()
    {
        // Act
        var result = ExtractedField.Create("FieldName", "Value", 1.5m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ExtractionConfidence.InvalidScore");
    }

    [Fact]
    public void Create_WithNegativeConfidence_ReturnsFailure()
    {
        // Act
        var result = ExtractedField.Create("FieldName", "Value", -0.1m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ExtractionConfidence.InvalidScore");
    }

    [Fact]
    public void HasValue_WithValue_ReturnsTrue()
    {
        // Arrange
        var field = ExtractedField.Create("Field", "Value", 0.95m).Value;

        // Act & Assert
        field.HasValue.Should().BeTrue();
    }

    [Fact]
    public void HasValue_WithNullValue_ReturnsFalse()
    {
        // Arrange
        var field = ExtractedField.Create("Field", null, 0.95m).Value;

        // Act & Assert
        field.HasValue.Should().BeFalse();
    }

    [Fact]
    public void HasValue_WithEmptyValue_ReturnsFalse()
    {
        // Arrange
        var field = ExtractedField.Create("Field", "", 0.95m).Value;

        // Act & Assert
        field.HasValue.Should().BeFalse();
    }

    [Fact]
    public void HasValue_WithWhitespaceValue_ReturnsFalse()
    {
        // Arrange
        var field = ExtractedField.Create("Field", "   ", 0.95m).Value;

        // Act & Assert
        field.HasValue.Should().BeFalse();
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var field1 = ExtractedField.Create("InsuredName", "ABC", 0.95m, "[0,0,100,20]", 1).Value;
        var field2 = ExtractedField.Create("InsuredName", "ABC", 0.95m, "[0,0,100,20]", 1).Value;

        // Act & Assert
        field1.Should().Be(field2);
    }

    [Fact]
    public void Equality_DifferentFieldName_AreNotEqual()
    {
        // Arrange
        var field1 = ExtractedField.Create("InsuredName", "ABC", 0.95m).Value;
        var field2 = ExtractedField.Create("PolicyNumber", "ABC", 0.95m).Value;

        // Act & Assert
        field1.Should().NotBe(field2);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        // Arrange
        var field1 = ExtractedField.Create("InsuredName", "ABC", 0.95m).Value;
        var field2 = ExtractedField.Create("InsuredName", "XYZ", 0.95m).Value;

        // Act & Assert
        field1.Should().NotBe(field2);
    }

    [Fact]
    public void Equality_DifferentConfidence_AreNotEqual()
    {
        // Arrange
        var field1 = ExtractedField.Create("InsuredName", "ABC", 0.95m).Value;
        var field2 = ExtractedField.Create("InsuredName", "ABC", 0.90m).Value;

        // Act & Assert
        field1.Should().NotBe(field2);
    }

    [Fact]
    public void Equality_DifferentBoundingBox_AreNotEqual()
    {
        // Arrange
        var field1 = ExtractedField.Create("InsuredName", "ABC", 0.95m, "[0,0,100,20]").Value;
        var field2 = ExtractedField.Create("InsuredName", "ABC", 0.95m, "[10,10,110,30]").Value;

        // Act & Assert
        field1.Should().NotBe(field2);
    }

    [Fact]
    public void Equality_DifferentPageNumber_AreNotEqual()
    {
        // Arrange
        var field1 = ExtractedField.Create("InsuredName", "ABC", 0.95m, null, 1).Value;
        var field2 = ExtractedField.Create("InsuredName", "ABC", 0.95m, null, 2).Value;

        // Act & Assert
        field1.Should().NotBe(field2);
    }
}
