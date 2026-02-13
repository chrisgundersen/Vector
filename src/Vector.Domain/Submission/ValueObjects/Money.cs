using Vector.Domain.Common;

namespace Vector.Domain.Submission.ValueObjects;

/// <summary>
/// Value object representing a monetary amount with currency.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency = "USD")
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return Result.Failure<Money>(MoneyErrors.CurrencyRequired);
        }

        if (currency.Length != 3)
        {
            return Result.Failure<Money>(MoneyErrors.InvalidCurrencyCode);
        }

        return Result.Success(new Money(Math.Round(amount, 2), currency.ToUpperInvariant()));
    }

    public static Money Zero(string currency = "USD") => new(0, currency);

    public static Money FromDecimal(decimal amount, string currency = "USD") =>
        new(Math.Round(amount, 2), currency.ToUpperInvariant());

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException("Cannot add money with different currencies.");
        }

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException("Cannot subtract money with different currencies.");
        }

        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor)
    {
        return new Money(Math.Round(Amount * factor, 2), Currency);
    }

    public bool IsPositive => Amount > 0;
    public bool IsNegative => Amount < 0;
    public bool IsZero => Amount == 0;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Currency} {Amount:N2}";
}

public static class MoneyErrors
{
    public static readonly Error CurrencyRequired = new("Money.CurrencyRequired", "Currency code is required.");
    public static readonly Error InvalidCurrencyCode = new("Money.InvalidCurrencyCode", "Currency code must be 3 characters.");
}
