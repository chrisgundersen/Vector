using Vector.Domain.Submission.ValueObjects;

namespace Vector.Domain.UnitTests.Submission.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidData_ReturnsSuccess()
    {
        // Act
        var result = Money.Create(100.50m, "USD");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(100.50m);
        result.Value.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_WithEmptyCurrency_ReturnsFailure()
    {
        // Act
        var result = Money.Create(100m, "");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.CurrencyRequired");
    }

    [Fact]
    public void Create_WithInvalidCurrencyLength_ReturnsFailure()
    {
        // Act
        var result = Money.Create(100m, "US");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.InvalidCurrencyCode");
    }

    [Fact]
    public void Add_WithSameCurrency_ReturnsSum()
    {
        // Arrange
        var money1 = Money.FromDecimal(100m);
        var money2 = Money.FromDecimal(50m);

        // Act
        var result = money1.Add(money2);

        // Assert
        result.Amount.Should().Be(150m);
    }

    [Fact]
    public void Add_WithDifferentCurrency_ThrowsException()
    {
        // Arrange
        var usd = Money.FromDecimal(100m, "USD");
        var eur = Money.FromDecimal(100m, "EUR");

        // Act & Assert
        var act = () => usd.Add(eur);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Subtract_WithSameCurrency_ReturnsDifference()
    {
        // Arrange
        var money1 = Money.FromDecimal(100m);
        var money2 = Money.FromDecimal(30m);

        // Act
        var result = money1.Subtract(money2);

        // Assert
        result.Amount.Should().Be(70m);
    }

    [Fact]
    public void Multiply_ReturnProductWithRounding()
    {
        // Arrange
        var money = Money.FromDecimal(100.333m);

        // Act
        var result = money.Multiply(1.5m);

        // Assert
        result.Amount.Should().Be(150.50m);
    }

    [Fact]
    public void Zero_CreatesZeroAmount()
    {
        // Act
        var zero = Money.Zero();

        // Assert
        zero.Amount.Should().Be(0m);
        zero.IsZero.Should().BeTrue();
    }

    [Fact]
    public void IsPositive_WithPositiveAmount_ReturnsTrue()
    {
        // Arrange
        var money = Money.FromDecimal(100m);

        // Assert
        money.IsPositive.Should().BeTrue();
        money.IsNegative.Should().BeFalse();
    }

    [Fact]
    public void IsNegative_WithNegativeAmount_ReturnsTrue()
    {
        // Arrange
        var money = Money.FromDecimal(-50m);

        // Assert
        money.IsNegative.Should().BeTrue();
        money.IsPositive.Should().BeFalse();
    }
}
