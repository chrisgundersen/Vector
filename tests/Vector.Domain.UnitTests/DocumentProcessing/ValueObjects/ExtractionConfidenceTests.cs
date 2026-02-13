using Vector.Domain.DocumentProcessing.ValueObjects;

namespace Vector.Domain.UnitTests.DocumentProcessing.ValueObjects;

public class ExtractionConfidenceTests
{
    [Fact]
    public void Create_WithValidScore_ReturnsSuccess()
    {
        // Act
        var result = ExtractionConfidence.Create(0.85m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Score.Should().Be(0.85m);
    }

    [Fact]
    public void Create_WithZero_ReturnsSuccess()
    {
        // Act
        var result = ExtractionConfidence.Create(0m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Score.Should().Be(0m);
    }

    [Fact]
    public void Create_WithOne_ReturnsSuccess()
    {
        // Act
        var result = ExtractionConfidence.Create(1m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Score.Should().Be(1m);
    }

    [Fact]
    public void Create_WithNegativeScore_ReturnsFailure()
    {
        // Act
        var result = ExtractionConfidence.Create(-0.1m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ExtractionConfidence.InvalidScore");
    }

    [Fact]
    public void Create_WithScoreGreaterThanOne_ReturnsFailure()
    {
        // Act
        var result = ExtractionConfidence.Create(1.1m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ExtractionConfidence.InvalidScore");
    }

    [Fact]
    public void Create_RoundsToFourDecimalPlaces()
    {
        // Act
        var result = ExtractionConfidence.Create(0.85678m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Score.Should().Be(0.8568m);
    }

    [Fact]
    public void IsHighConfidence_WithScoreAbove90_ReturnsTrue()
    {
        // Arrange
        var confidence = ExtractionConfidence.Create(0.95m).Value;

        // Act & Assert
        confidence.IsHighConfidence.Should().BeTrue();
    }

    [Fact]
    public void IsHighConfidence_WithScoreExactly90_ReturnsTrue()
    {
        // Arrange
        var confidence = ExtractionConfidence.Create(0.90m).Value;

        // Act & Assert
        confidence.IsHighConfidence.Should().BeTrue();
    }

    [Fact]
    public void IsHighConfidence_WithScoreBelow90_ReturnsFalse()
    {
        // Arrange
        var confidence = ExtractionConfidence.Create(0.89m).Value;

        // Act & Assert
        confidence.IsHighConfidence.Should().BeFalse();
    }

    [Fact]
    public void IsMediumConfidence_WithScoreBetween70And90_ReturnsTrue()
    {
        // Arrange
        var confidence = ExtractionConfidence.Create(0.80m).Value;

        // Act & Assert
        confidence.IsMediumConfidence.Should().BeTrue();
    }

    [Fact]
    public void IsMediumConfidence_WithScoreExactly70_ReturnsTrue()
    {
        // Arrange
        var confidence = ExtractionConfidence.Create(0.70m).Value;

        // Act & Assert
        confidence.IsMediumConfidence.Should().BeTrue();
    }

    [Fact]
    public void IsMediumConfidence_WithScoreBelow70_ReturnsFalse()
    {
        // Arrange
        var confidence = ExtractionConfidence.Create(0.69m).Value;

        // Act & Assert
        confidence.IsMediumConfidence.Should().BeFalse();
    }

    [Fact]
    public void IsLowConfidence_WithScoreBelow70_ReturnsTrue()
    {
        // Arrange
        var confidence = ExtractionConfidence.Create(0.50m).Value;

        // Act & Assert
        confidence.IsLowConfidence.Should().BeTrue();
    }

    [Fact]
    public void RequiresReview_WithScoreBelow80_ReturnsTrue()
    {
        // Arrange
        var confidence = ExtractionConfidence.Create(0.79m).Value;

        // Act & Assert
        confidence.RequiresReview.Should().BeTrue();
    }

    [Fact]
    public void RequiresReview_WithScoreAtOrAbove80_ReturnsFalse()
    {
        // Arrange
        var confidence = ExtractionConfidence.Create(0.80m).Value;

        // Act & Assert
        confidence.RequiresReview.Should().BeFalse();
    }

    [Fact]
    public void High_ReturnsConfidenceOf95Percent()
    {
        // Act & Assert
        ExtractionConfidence.High.Score.Should().Be(0.95m);
    }

    [Fact]
    public void Medium_ReturnsConfidenceOf75Percent()
    {
        // Act & Assert
        ExtractionConfidence.Medium.Score.Should().Be(0.75m);
    }

    [Fact]
    public void Low_ReturnsConfidenceOf50Percent()
    {
        // Act & Assert
        ExtractionConfidence.Low.Score.Should().Be(0.50m);
    }

    [Fact]
    public void Unknown_ReturnsConfidenceOfZero()
    {
        // Act & Assert
        ExtractionConfidence.Unknown.Score.Should().Be(0m);
    }

    [Fact]
    public void ToString_ReturnsFormattedPercentage()
    {
        // Arrange
        var confidence = ExtractionConfidence.Create(0.9567m).Value;

        // Act
        var result = confidence.ToString();

        // Assert - P1 format shows 1 decimal place percentage
        result.Should().Contain("95");
        result.Should().Contain("%");
    }

    [Fact]
    public void Equality_SameScore_AreEqual()
    {
        // Arrange
        var confidence1 = ExtractionConfidence.Create(0.85m).Value;
        var confidence2 = ExtractionConfidence.Create(0.85m).Value;

        // Act & Assert
        confidence1.Should().Be(confidence2);
    }

    [Fact]
    public void Equality_DifferentScore_AreNotEqual()
    {
        // Arrange
        var confidence1 = ExtractionConfidence.Create(0.85m).Value;
        var confidence2 = ExtractionConfidence.Create(0.90m).Value;

        // Act & Assert
        confidence1.Should().NotBe(confidence2);
    }
}
