namespace Vector.Application.Common.Interfaces;

/// <summary>
/// Interface for accessing current user context.
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    Guid? TenantId { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsAuthenticated { get; }
}
