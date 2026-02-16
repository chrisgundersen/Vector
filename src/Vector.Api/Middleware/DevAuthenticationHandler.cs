using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Vector.Api.Middleware;

/// <summary>
/// Authentication handler for local development that auto-authenticates all API requests
/// with Admin and Underwriter roles. Only registered when Authentication:DisableAuthentication is true.
/// </summary>
public class DevAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    internal const string SchemeName = "DevAuth";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new Claim[]
        {
            new(ClaimTypes.NameIdentifier, "local-dev-user"),
            new(ClaimTypes.Name, "Local Developer"),
            new(ClaimTypes.Email, "dev@vectormga.com"),
            new("tenant_id", "00000000-0000-0000-0000-000000000001"),
            new(ClaimTypes.Role, "Admin"),
            new(ClaimTypes.Role, "Underwriter"),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
