using Vector.Domain.Common;

namespace Vector.Domain.EmailIntake.ValueObjects;

/// <summary>
/// Value object representing metadata about an email attachment.
/// </summary>
public sealed class AttachmentMetadata : ValueObject
{
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public long SizeInBytes { get; private set; }
    public ContentHash ContentHash { get; private set; }

    // EF Core constructor
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value
    private AttachmentMetadata() { }
#pragma warning restore CS8618

    private AttachmentMetadata(string fileName, string contentType, long sizeInBytes, ContentHash contentHash)
    {
        FileName = fileName;
        ContentType = contentType;
        SizeInBytes = sizeInBytes;
        ContentHash = contentHash;
    }

    public static Result<AttachmentMetadata> Create(
        string fileName,
        string contentType,
        long sizeInBytes,
        ContentHash contentHash)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Result.Failure<AttachmentMetadata>(AttachmentMetadataErrors.FileNameEmpty);
        }

        if (fileName.Length > 255)
        {
            return Result.Failure<AttachmentMetadata>(AttachmentMetadataErrors.FileNameTooLong);
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            return Result.Failure<AttachmentMetadata>(AttachmentMetadataErrors.ContentTypeEmpty);
        }

        if (sizeInBytes < 0)
        {
            return Result.Failure<AttachmentMetadata>(AttachmentMetadataErrors.InvalidSize);
        }

        return Result.Success(new AttachmentMetadata(fileName, contentType, sizeInBytes, contentHash));
    }

    public string FileExtension => Path.GetExtension(FileName).ToLowerInvariant();

    public bool IsPdf => FileExtension == ".pdf" || ContentType == "application/pdf";

    public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    public bool IsExcel => FileExtension is ".xlsx" or ".xls" ||
                           ContentType is "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                               or "application/vnd.ms-excel";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FileName;
        yield return ContentType;
        yield return SizeInBytes;
        yield return ContentHash;
    }
}

public static class AttachmentMetadataErrors
{
    public static readonly Error FileNameEmpty = new("AttachmentMetadata.FileNameEmpty", "File name cannot be empty.");
    public static readonly Error FileNameTooLong = new("AttachmentMetadata.FileNameTooLong", "File name cannot exceed 255 characters.");
    public static readonly Error ContentTypeEmpty = new("AttachmentMetadata.ContentTypeEmpty", "Content type cannot be empty.");
    public static readonly Error InvalidSize = new("AttachmentMetadata.InvalidSize", "Size cannot be negative.");
}
