using Vector.Domain.Common;

namespace Vector.Domain.DocumentProcessing.ValueObjects;

/// <summary>
/// Value object representing a single extracted field from a document.
/// </summary>
public sealed class ExtractedField : ValueObject
{
    public string FieldName { get; }
    public string? Value { get; }
    public ExtractionConfidence Confidence { get; }
    public string? BoundingBox { get; }
    public int? PageNumber { get; }

    private ExtractedField(
        string fieldName,
        string? value,
        ExtractionConfidence confidence,
        string? boundingBox,
        int? pageNumber)
    {
        FieldName = fieldName;
        Value = value;
        Confidence = confidence;
        BoundingBox = boundingBox;
        PageNumber = pageNumber;
    }

    public static Result<ExtractedField> Create(
        string fieldName,
        string? value,
        decimal confidence,
        string? boundingBox = null,
        int? pageNumber = null)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return Result.Failure<ExtractedField>(ExtractedFieldErrors.FieldNameRequired);
        }

        var confidenceResult = ExtractionConfidence.Create(confidence);
        if (confidenceResult.IsFailure)
        {
            return Result.Failure<ExtractedField>(confidenceResult.Error);
        }

        return Result.Success(new ExtractedField(
            fieldName,
            value,
            confidenceResult.Value,
            boundingBox,
            pageNumber));
    }

    public bool HasValue => !string.IsNullOrWhiteSpace(Value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FieldName;
        yield return Value;
        yield return Confidence;
        yield return BoundingBox;
        yield return PageNumber;
    }
}

public static class ExtractedFieldErrors
{
    public static readonly Error FieldNameRequired = new("ExtractedField.FieldNameRequired", "Field name is required.");
}
