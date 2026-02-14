using System.Security.Cryptography;
using System.Text;
using Vector.Domain.Common;

namespace Vector.Domain.EmailIntake.ValueObjects;

/// <summary>
/// Value object representing a content hash for deduplication.
/// </summary>
public sealed class ContentHash : ValueObject
{
    public string Value { get; private set; }
    public string Algorithm { get; private set; }

    // EF Core constructor
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value
    private ContentHash() { }
#pragma warning restore CS8618

    private ContentHash(string value, string algorithm)
    {
        Value = value;
        Algorithm = algorithm;
    }

    public static ContentHash ComputeSha256(string content)
    {
        ArgumentException.ThrowIfNullOrEmpty(content);

        var bytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = SHA256.HashData(bytes);
        var hashString = Convert.ToHexStringLower(hashBytes);

        return new ContentHash(hashString, "SHA256");
    }

    public static ContentHash ComputeSha256(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var hashBytes = SHA256.HashData(content);
        var hashString = Convert.ToHexStringLower(hashBytes);

        return new ContentHash(hashString, "SHA256");
    }

    public static ContentHash FromExisting(string hash, string algorithm = "SHA256")
    {
        ArgumentException.ThrowIfNullOrEmpty(hash);
        return new ContentHash(hash, algorithm);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return Algorithm;
    }

    public override string ToString() => $"{Algorithm}:{Value}";
}
