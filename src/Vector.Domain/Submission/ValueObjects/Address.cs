using Vector.Domain.Common;

namespace Vector.Domain.Submission.ValueObjects;

/// <summary>
/// Value object representing a physical address.
/// </summary>
public sealed class Address : ValueObject
{
    public string Street1 { get; private set; }
    public string? Street2 { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string PostalCode { get; private set; }
    public string Country { get; private set; }

    // EF Core constructor
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value
    private Address() { }
#pragma warning restore CS8618

    private Address(
        string street1,
        string? street2,
        string city,
        string state,
        string postalCode,
        string country)
    {
        Street1 = street1;
        Street2 = street2;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    public static Result<Address> Create(
        string street1,
        string? street2,
        string city,
        string state,
        string postalCode,
        string country = "USA")
    {
        if (string.IsNullOrWhiteSpace(street1))
        {
            return Result.Failure<Address>(AddressErrors.Street1Required);
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            return Result.Failure<Address>(AddressErrors.CityRequired);
        }

        if (string.IsNullOrWhiteSpace(state))
        {
            return Result.Failure<Address>(AddressErrors.StateRequired);
        }

        if (string.IsNullOrWhiteSpace(postalCode))
        {
            return Result.Failure<Address>(AddressErrors.PostalCodeRequired);
        }

        return Result.Success(new Address(
            street1.Trim(),
            street2?.Trim(),
            city.Trim(),
            state.Trim().ToUpperInvariant(),
            postalCode.Trim(),
            country.Trim().ToUpperInvariant()));
    }

    public string FullAddress => string.Join(", ",
        new[] { Street1, Street2, City, $"{State} {PostalCode}", Country }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street1;
        yield return Street2;
        yield return City;
        yield return State;
        yield return PostalCode;
        yield return Country;
    }

    public override string ToString() => FullAddress;
}

public static class AddressErrors
{
    public static readonly Error Street1Required = new("Address.Street1Required", "Street address is required.");
    public static readonly Error CityRequired = new("Address.CityRequired", "City is required.");
    public static readonly Error StateRequired = new("Address.StateRequired", "State is required.");
    public static readonly Error PostalCodeRequired = new("Address.PostalCodeRequired", "Postal code is required.");
}
