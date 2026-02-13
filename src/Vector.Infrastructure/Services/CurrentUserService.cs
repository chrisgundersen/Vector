using Vector.Application.Common.Interfaces;

namespace Vector.Infrastructure.Services;

/// <summary>
/// Default implementation of current user service.
/// In production, this would be replaced by HTTP context-based implementation.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public Guid? TenantId { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = [];
    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
}
