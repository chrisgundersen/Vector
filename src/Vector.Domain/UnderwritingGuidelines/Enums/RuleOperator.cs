namespace Vector.Domain.UnderwritingGuidelines.Enums;

/// <summary>
/// Operators used in rule conditions.
/// </summary>
public enum RuleOperator
{
    /// <summary>
    /// Equals comparison.
    /// </summary>
    Equals = 0,

    /// <summary>
    /// Not equals comparison.
    /// </summary>
    NotEquals = 1,

    /// <summary>
    /// Greater than comparison.
    /// </summary>
    GreaterThan = 2,

    /// <summary>
    /// Greater than or equal comparison.
    /// </summary>
    GreaterThanOrEqual = 3,

    /// <summary>
    /// Less than comparison.
    /// </summary>
    LessThan = 4,

    /// <summary>
    /// Less than or equal comparison.
    /// </summary>
    LessThanOrEqual = 5,

    /// <summary>
    /// Value is in a list.
    /// </summary>
    In = 6,

    /// <summary>
    /// Value is not in a list.
    /// </summary>
    NotIn = 7,

    /// <summary>
    /// Value contains substring.
    /// </summary>
    Contains = 8,

    /// <summary>
    /// Value starts with substring.
    /// </summary>
    StartsWith = 9,

    /// <summary>
    /// Value ends with substring.
    /// </summary>
    EndsWith = 10,

    /// <summary>
    /// Value is between two values (inclusive).
    /// </summary>
    Between = 11,

    /// <summary>
    /// Value is null or empty.
    /// </summary>
    IsEmpty = 12,

    /// <summary>
    /// Value is not null or empty.
    /// </summary>
    IsNotEmpty = 13
}
