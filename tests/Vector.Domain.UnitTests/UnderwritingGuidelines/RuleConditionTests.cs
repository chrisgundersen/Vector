using FluentAssertions;
using Vector.Domain.UnderwritingGuidelines.Enums;
using Vector.Domain.UnderwritingGuidelines.ValueObjects;

namespace Vector.Domain.UnitTests.UnderwritingGuidelines;

public class RuleConditionTests
{
    [Fact]
    public void Evaluate_EqualsOperator_WithMatchingValue_ReturnsTrue()
    {
        // Arrange
        var condition = RuleCondition.Equals(RuleField.InsuredState, "CA");

        // Act
        var result = condition.Evaluate("CA");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_EqualsOperator_WithNonMatchingValue_ReturnsFalse()
    {
        // Arrange
        var condition = RuleCondition.Equals(RuleField.InsuredState, "CA");

        // Act
        var result = condition.Evaluate("TX");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_EqualsOperator_IsCaseInsensitive()
    {
        // Arrange
        var condition = RuleCondition.Equals(RuleField.InsuredState, "CA");

        // Act
        var result = condition.Evaluate("ca");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_GreaterThanOperator_WithGreaterValue_ReturnsTrue()
    {
        // Arrange
        var condition = RuleCondition.GreaterThan(RuleField.AnnualRevenue, "1000000");

        // Act
        var result = condition.Evaluate("5000000");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_GreaterThanOperator_WithLesserValue_ReturnsFalse()
    {
        // Arrange
        var condition = RuleCondition.GreaterThan(RuleField.AnnualRevenue, "1000000");

        // Act
        var result = condition.Evaluate("500000");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_LessThanOperator_WithLesserValue_ReturnsTrue()
    {
        // Arrange
        var condition = RuleCondition.LessThan(RuleField.YearsInBusiness, "5");

        // Act
        var result = condition.Evaluate("3");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_InOperator_WithMatchingValue_ReturnsTrue()
    {
        // Arrange
        var condition = RuleCondition.In(RuleField.InsuredState, "CA", "TX", "NY");

        // Act
        var result = condition.Evaluate("TX");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_InOperator_WithNonMatchingValue_ReturnsFalse()
    {
        // Arrange
        var condition = RuleCondition.In(RuleField.InsuredState, "CA", "TX", "NY");

        // Act
        var result = condition.Evaluate("FL");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_BetweenOperator_WithValueInRange_ReturnsTrue()
    {
        // Arrange
        var condition = RuleCondition.Between(RuleField.AnnualRevenue, "1000000", "10000000");

        // Act
        var result = condition.Evaluate("5000000");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_BetweenOperator_WithValueOutOfRange_ReturnsFalse()
    {
        // Arrange
        var condition = RuleCondition.Between(RuleField.AnnualRevenue, "1000000", "10000000");

        // Act
        var result = condition.Evaluate("20000000");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_BetweenOperator_WithValueAtMinBoundary_ReturnsTrue()
    {
        // Arrange
        var condition = RuleCondition.Between(RuleField.AnnualRevenue, "1000000", "10000000");

        // Act
        var result = condition.Evaluate("1000000");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Create_WithBetweenOperatorAndNoSecondaryValue_ReturnsFailure()
    {
        // Act
        var result = RuleCondition.Create(
            RuleField.AnnualRevenue,
            RuleOperator.Between,
            "1000000",
            null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RuleCondition.SecondaryValueRequired");
    }

    [Fact]
    public void Create_WithEmptyValueForRequiredOperator_ReturnsFailure()
    {
        // Act
        var result = RuleCondition.Create(
            RuleField.InsuredState,
            RuleOperator.Equals,
            "",
            null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RuleCondition.ValueRequired");
    }

    [Fact]
    public void Evaluate_IsEmptyOperator_WithNullValue_ReturnsTrue()
    {
        // Arrange
        var result = RuleCondition.Create(
            RuleField.InsuredState,
            RuleOperator.IsEmpty,
            "dummy", // Value is ignored for IsEmpty
            null);
        var condition = result.Value;

        // Act
        var evalResult = condition.Evaluate(null);

        // Assert
        evalResult.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_IsNotEmptyOperator_WithValue_ReturnsTrue()
    {
        // Arrange
        var result = RuleCondition.Create(
            RuleField.InsuredState,
            RuleOperator.IsNotEmpty,
            "dummy",
            null);
        var condition = result.Value;

        // Act
        var evalResult = condition.Evaluate("CA");

        // Assert
        evalResult.Should().BeTrue();
    }

    [Fact]
    public void Equality_SameConditions_AreEqual()
    {
        // Arrange
        var condition1 = RuleCondition.Equals(RuleField.InsuredState, "CA");
        var condition2 = RuleCondition.Equals(RuleField.InsuredState, "CA");

        // Assert
        condition1.Should().Be(condition2);
    }

    [Fact]
    public void ToString_ReturnsReadableRepresentation()
    {
        // Arrange
        var condition = RuleCondition.Equals(RuleField.InsuredState, "CA");

        // Act
        var result = condition.ToString();

        // Assert
        result.Should().Contain("InsuredState");
        result.Should().Contain("Equals");
        result.Should().Contain("CA");
    }
}
