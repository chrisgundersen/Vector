using System.Security.Claims;

namespace Vector.Web.Underwriting.Services;

/// <summary>
/// Service for managing mock users during local development.
/// </summary>
public class MockUserService
{
    private MockUser _currentUser = DefaultUsers[0]; // Default to first underwriter

    public MockUser CurrentUser => _currentUser;

    public void SetCurrentUser(string userId)
    {
        _currentUser = AllUsers.FirstOrDefault(u => u.Id == userId) ?? DefaultUsers[0];
    }

    public ClaimsPrincipal GetClaimsPrincipal()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _currentUser.Id),
            new(ClaimTypes.Name, _currentUser.Name),
            new(ClaimTypes.Email, _currentUser.Email),
            new("tenant_id", _currentUser.TenantId.ToString())
        };

        foreach (var role in _currentUser.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "MockAuth");
        return new ClaimsPrincipal(identity);
    }

    public static IReadOnlyList<MockUser> AllUsers => [..DefaultUsers, ..Producers];

    public static IReadOnlyList<MockUser> Underwriters => DefaultUsers.Where(u => u.Roles.Contains("Underwriter")).ToList();

    public static IReadOnlyList<MockUser> ProducerUsers => Producers;

    private static readonly MockUser[] DefaultUsers =
    [
        new MockUser(
            "11111111-1111-1111-1111-111111111111",
            "John Smith",
            "john.smith@vectormga.com",
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            ["Underwriter", "Admin"],
            "Senior Underwriter",
            null),

        new MockUser(
            "22222222-2222-2222-2222-222222222222",
            "Jane Doe",
            "jane.doe@vectormga.com",
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            ["Underwriter"],
            "Underwriter",
            null),

        new MockUser(
            "11111111-1111-1111-1111-111111111112",
            "Mike Johnson",
            "mike.johnson@vectormga.com",
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            ["Underwriter"],
            "Junior Underwriter",
            null),

        new MockUser(
            "admin-user-id",
            "Admin User",
            "admin@vectormga.com",
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            ["Admin"],
            "System Administrator",
            null)
    ];

    private static readonly MockUser[] Producers =
    [
        new MockUser(
            "33333333-3333-3333-3333-333333333333",
            "ABC Insurance Agency",
            "submissions@abcinsurance.com",
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            ["Producer"],
            "Producer",
            Guid.Parse("33333333-3333-3333-3333-333333333333")),

        new MockUser(
            "44444444-4444-4444-4444-444444444444",
            "XYZ Brokers Inc",
            "newbusiness@xyzbrokers.com",
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            ["Producer"],
            "Producer",
            Guid.Parse("44444444-4444-4444-4444-444444444444")),

        new MockUser(
            "33333333-3333-3333-3333-333333333334",
            "Marsh McLennan",
            "submissions@marsh.com",
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            ["Producer"],
            "Producer",
            Guid.Parse("33333333-3333-3333-3333-333333333334")),

        new MockUser(
            "33333333-3333-3333-3333-333333333335",
            "Aon Risk Solutions",
            "newbusiness@aon.com",
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            ["Producer"],
            "Producer",
            Guid.Parse("33333333-3333-3333-3333-333333333335"))
    ];
}

public record MockUser(
    string Id,
    string Name,
    string Email,
    Guid TenantId,
    string[] Roles,
    string Title,
    Guid? ProducerId)
{
    public bool IsUnderwriter => Roles.Contains("Underwriter");
    public bool IsProducer => Roles.Contains("Producer");
    public bool IsAdmin => Roles.Contains("Admin");
}
