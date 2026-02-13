namespace Vector.Domain.Common;

/// <summary>
/// Interface for entities that track audit information.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Gets the UTC timestamp when the entity was created.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// Gets the identifier of the user who created the entity.
    /// </summary>
    string? CreatedBy { get; }

    /// <summary>
    /// Gets the UTC timestamp when the entity was last modified.
    /// </summary>
    DateTime? LastModifiedAt { get; }

    /// <summary>
    /// Gets the identifier of the user who last modified the entity.
    /// </summary>
    string? LastModifiedBy { get; }
}
