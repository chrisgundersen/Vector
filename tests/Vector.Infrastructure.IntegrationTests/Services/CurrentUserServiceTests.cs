using Vector.Infrastructure.Services;

namespace Vector.Infrastructure.IntegrationTests.Services;

public class CurrentUserServiceTests
{
    private readonly CurrentUserService _currentUserService;

    public CurrentUserServiceTests()
    {
        _currentUserService = new CurrentUserService();
    }

    [Fact]
    public void IsAuthenticated_WithNoUserId_ReturnsFalse()
    {
        // Arrange
        _currentUserService.UserId = null;

        // Act
        var result = _currentUserService.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_WithEmptyUserId_ReturnsFalse()
    {
        // Arrange
        _currentUserService.UserId = "";

        // Act
        var result = _currentUserService.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_WithUserId_ReturnsTrue()
    {
        // Arrange
        _currentUserService.UserId = "user@example.com";

        // Act
        var result = _currentUserService.IsAuthenticated;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void UserId_CanBeSetAndRetrieved()
    {
        // Arrange
        var userId = "test-user-123";

        // Act
        _currentUserService.UserId = userId;

        // Assert
        _currentUserService.UserId.Should().Be(userId);
    }

    [Fact]
    public void UserName_CanBeSetAndRetrieved()
    {
        // Arrange
        var userName = "John Doe";

        // Act
        _currentUserService.UserName = userName;

        // Assert
        _currentUserService.UserName.Should().Be(userName);
    }

    [Fact]
    public void TenantId_CanBeSetAndRetrieved()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        _currentUserService.TenantId = tenantId;

        // Assert
        _currentUserService.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Roles_DefaultsToEmptyCollection()
    {
        // Assert
        _currentUserService.Roles.Should().BeEmpty();
    }

    [Fact]
    public void Roles_CanBeSetAndRetrieved()
    {
        // Arrange
        var roles = new[] { "Admin", "Underwriter" };

        // Act
        _currentUserService.Roles = roles;

        // Assert
        _currentUserService.Roles.Should().BeEquivalentTo(roles);
    }
}
