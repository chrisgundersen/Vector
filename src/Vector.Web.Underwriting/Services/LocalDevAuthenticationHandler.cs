using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Vector.Web.Underwriting.Services;

/// <summary>
/// Authentication handler for local development that automatically authenticates all requests.
/// Uses MockUserService to support switching between test users.
/// </summary>
public class LocalDevAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly MockUserService _mockUserService;

    public LocalDevAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        MockUserService mockUserService)
        : base(options, logger, encoder)
    {
        _mockUserService = mockUserService;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var principal = _mockUserService.GetClaimsPrincipal();
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
