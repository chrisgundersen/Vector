using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Vector.Web.Underwriting.Services;

/// <summary>
/// Authentication state provider for local development that returns an anonymous authenticated user.
/// </summary>
public class AnonymousAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> _authenticationState = Task.FromResult(
        new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "local-dev-user"),
            new Claim(ClaimTypes.Name, "Local Developer"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "Underwriter"),
            new Claim("tenant_id", "00000000-0000-0000-0000-000000000001")
        ], "LocalDev"))));

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _authenticationState;
}
