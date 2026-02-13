using System.Text.RegularExpressions;
using Vector.Domain.Common;

namespace Vector.Domain.EmailIntake.ValueObjects;

/// <summary>
/// Value object representing a validated email address.
/// </summary>
public sealed partial class EmailAddress : ValueObject
{
    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static Result<EmailAddress> Create(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Failure<EmailAddress>(EmailAddressErrors.Empty);
        }

        email = email.Trim().ToLowerInvariant();

        if (email.Length > 256)
        {
            return Result.Failure<EmailAddress>(EmailAddressErrors.TooLong);
        }

        if (!EmailRegex().IsMatch(email))
        {
            return Result.Failure<EmailAddress>(EmailAddressErrors.InvalidFormat);
        }

        return Result.Success(new EmailAddress(email));
    }

    public string Domain => Value.Split('@')[1];
    public string LocalPart => Value.Split('@')[0];

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}

public static class EmailAddressErrors
{
    public static readonly Error Empty = new("EmailAddress.Empty", "Email address cannot be empty.");
    public static readonly Error TooLong = new("EmailAddress.TooLong", "Email address cannot exceed 256 characters.");
    public static readonly Error InvalidFormat = new("EmailAddress.InvalidFormat", "Email address format is invalid.");
}
