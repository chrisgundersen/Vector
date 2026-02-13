namespace Vector.Application.Common.Interfaces;

/// <summary>
/// Interface for date/time provider to support testing.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}
