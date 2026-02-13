using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Vector.Application.Common.Interfaces;

namespace Vector.Web.Underwriting.Services;

/// <summary>
/// Implementation of ICurrentUserService for Blazor Server that extracts user information
/// from the authentication state.
/// </summary>
public class BlazorServerCurrentUserService(AuthenticationStateProvider authenticationStateProvider) : ICurrentUserService
{
    private ClaimsPrincipal? _user;

    public string? UserId => GetClaim(ClaimTypes.NameIdentifier);

    public string? UserName => GetClaim(ClaimTypes.Name) ?? GetClaim("name");

    public Guid? TenantId
    {
        get
        {
            var tenantClaim = GetClaim("tenant_id") ?? GetClaim("tid");
            return Guid.TryParse(tenantClaim, out var tenantId) ? tenantId : null;
        }
    }

    public IReadOnlyCollection<string> Roles
    {
        get
        {
            EnsureUser();
            return _user?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            EnsureUser();
            return _user?.Identity?.IsAuthenticated ?? false;
        }
    }

    private string? GetClaim(string claimType)
    {
        EnsureUser();
        return _user?.FindFirst(claimType)?.Value;
    }

    private void EnsureUser()
    {
        if (_user is null)
        {
            var authState = authenticationStateProvider.GetAuthenticationStateAsync().GetAwaiter().GetResult();
            _user = authState.User;
        }
    }
}
