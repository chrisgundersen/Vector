using Vector.Domain.Common;

namespace Vector.Domain.DocumentProcessing.ValueObjects;

/// <summary>
/// Value object representing confidence level of extracted data.
/// </summary>
public sealed class ExtractionConfidence : ValueObject
{
    public decimal Score { get; private set; }

    // EF Core constructor
    private ExtractionConfidence() { }

    private ExtractionConfidence(decimal score)
    {
        Score = score;
    }

    public static Result<ExtractionConfidence> Create(decimal score)
    {
        if (score < 0 || score > 1)
        {
            return Result.Failure<ExtractionConfidence>(ExtractionConfidenceErrors.InvalidScore);
        }

        return Result.Success(new ExtractionConfidence(Math.Round(score, 4)));
    }

    public static ExtractionConfidence High => new(0.95m);
    public static ExtractionConfidence Medium => new(0.75m);
    public static ExtractionConfidence Low => new(0.50m);
    public static ExtractionConfidence Unknown => new(0m);

    public bool IsHighConfidence => Score >= 0.90m;
    public bool IsMediumConfidence => Score >= 0.70m && Score < 0.90m;
    public bool IsLowConfidence => Score < 0.70m;
    public bool RequiresReview => Score < 0.80m;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Score;
    }

    public override string ToString() => $"{Score:P1}";
}

public static class ExtractionConfidenceErrors
{
    public static readonly Error InvalidScore = new("ExtractionConfidence.InvalidScore", "Confidence score must be between 0 and 1.");
}
