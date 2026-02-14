using Vector.Domain.Common;

namespace Vector.Domain.Submission.ValueObjects;

/// <summary>
/// Value object representing industry classification (NAICS/SIC codes).
/// </summary>
public sealed class IndustryClassification : ValueObject
{
    public string NaicsCode { get; private set; }
    public string? SicCode { get; private set; }
    public string Description { get; private set; }

    // EF Core constructor
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value
    private IndustryClassification() { }
#pragma warning restore CS8618

    private IndustryClassification(string naicsCode, string? sicCode, string description)
    {
        NaicsCode = naicsCode;
        SicCode = sicCode;
        Description = description;
    }

    public static Result<IndustryClassification> Create(
        string naicsCode,
        string? sicCode,
        string description)
    {
        if (string.IsNullOrWhiteSpace(naicsCode))
        {
            return Result.Failure<IndustryClassification>(IndustryClassificationErrors.NaicsCodeRequired);
        }

        if (naicsCode.Length is < 2 or > 6)
        {
            return Result.Failure<IndustryClassification>(IndustryClassificationErrors.InvalidNaicsCode);
        }

        if (!naicsCode.All(char.IsDigit))
        {
            return Result.Failure<IndustryClassification>(IndustryClassificationErrors.InvalidNaicsCode);
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return Result.Failure<IndustryClassification>(IndustryClassificationErrors.DescriptionRequired);
        }

        return Result.Success(new IndustryClassification(naicsCode, sicCode?.Trim(), description.Trim()));
    }

    public string Sector => NaicsCode.Length >= 2 ? NaicsCode[..2] : NaicsCode;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return NaicsCode;
        yield return SicCode;
    }

    public override string ToString() => $"{NaicsCode} - {Description}";
}

public static class IndustryClassificationErrors
{
    public static readonly Error NaicsCodeRequired = new("IndustryClassification.NaicsCodeRequired", "NAICS code is required.");
    public static readonly Error InvalidNaicsCode = new("IndustryClassification.InvalidNaicsCode", "NAICS code must be 2-6 digits.");
    public static readonly Error DescriptionRequired = new("IndustryClassification.DescriptionRequired", "Industry description is required.");
}
