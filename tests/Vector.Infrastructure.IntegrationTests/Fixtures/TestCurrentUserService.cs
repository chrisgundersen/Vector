using Vector.Application.Common.Interfaces;

namespace Vector.Infrastructure.IntegrationTests.Fixtures;

public class TestCurrentUserService : ICurrentUserService
{
    public string? UserId { get; set; } = "test-user";
    public string? UserName { get; set; } = "Test User";
    public Guid? TenantId { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = new[] { "Admin" };
    public bool IsAuthenticated => UserId is not null;
}
