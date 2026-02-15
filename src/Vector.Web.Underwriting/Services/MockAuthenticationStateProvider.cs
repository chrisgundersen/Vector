using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Vector.Web.Underwriting.Services;

/// <summary>
/// Authentication state provider that uses MockUserService for local development.
/// Allows switching between different test users.
/// </summary>
public class MockAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly MockUserService _mockUserService;

    public MockAuthenticationStateProvider(MockUserService mockUserService)
    {
        _mockUserService = mockUserService;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var principal = _mockUserService.GetClaimsPrincipal();
        return Task.FromResult(new AuthenticationState(principal));
    }

    public void SwitchUser(string userId)
    {
        _mockUserService.SetCurrentUser(userId);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public MockUser CurrentUser => _mockUserService.CurrentUser;
}
