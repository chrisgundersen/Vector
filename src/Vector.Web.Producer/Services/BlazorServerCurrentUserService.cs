using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Vector.Application.Common.Interfaces;

namespace Vector.Web.Producer.Services;

/// <summary>
/// Current user service implementation for Blazor Server apps.
/// Extracts user information from the authentication state.
/// </summary>
public class BlazorServerCurrentUserService : ICurrentUserService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private ClaimsPrincipal? _user;
    private bool _initialized;

    public BlazorServerCurrentUserService(AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        _user = authState.User;
        _initialized = true;
    }

    public string? UserId
    {
        get
        {
            EnsureInitializedAsync().GetAwaiter().GetResult();
            return _user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _user?.FindFirst("oid")?.Value  // Azure AD Object ID
                ?? _user?.FindFirst("sub")?.Value; // Subject claim
        }
    }

    public string? UserName
    {
        get
        {
            EnsureInitializedAsync().GetAwaiter().GetResult();
            return _user?.FindFirst("name")?.Value
                ?? _user?.FindFirst(ClaimTypes.Name)?.Value
                ?? _user?.Identity?.Name;
        }
    }

    public Guid? TenantId
    {
        get
        {
            EnsureInitializedAsync().GetAwaiter().GetResult();
            var tenantClaim = _user?.FindFirst("tid")?.Value  // Azure AD Tenant ID
                ?? _user?.FindFirst("tenant_id")?.Value;

            return Guid.TryParse(tenantClaim, out var tenantId) ? tenantId : null;
        }
    }

    public IReadOnlyCollection<string> Roles
    {
        get
        {
            EnsureInitializedAsync().GetAwaiter().GetResult();
            return _user?.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList()
                .AsReadOnly() ?? (IReadOnlyCollection<string>)[];
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            EnsureInitializedAsync().GetAwaiter().GetResult();
            return _user?.Identity?.IsAuthenticated ?? false;
        }
    }
}
