using System.Security.Claims;
using Vector.Application.Common.Interfaces;

namespace Vector.Api.Middleware;

/// <summary>
/// Middleware that populates the current user service from authentication claims
/// or with default values when authentication is disabled.
/// </summary>
public class CurrentUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUserService, IConfiguration configuration)
    {
        var disableAuth = configuration.GetValue<bool>("Authentication:DisableAuthentication");

        if (disableAuth)
        {
            // Use default values for local development
            SetCurrentUserValues(currentUserService, new CurrentUserValues
            {
                UserId = "local-dev-user",
                UserName = "Local Developer",
                TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Roles = ["Admin", "Underwriter"]
            });
        }
        else if (context.User.Identity?.IsAuthenticated == true)
        {
            // Extract values from claims
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = context.User.FindFirstValue(ClaimTypes.Name);
            var tenantIdClaim = context.User.FindFirstValue("tenant_id");
            var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

            SetCurrentUserValues(currentUserService, new CurrentUserValues
            {
                UserId = userId,
                UserName = userName,
                TenantId = Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null,
                Roles = roles
            });
        }

        await next(context);
    }

    private static void SetCurrentUserValues(ICurrentUserService service, CurrentUserValues values)
    {
        // ICurrentUserService is a simple class with settable properties
        if (service is Infrastructure.Services.CurrentUserService userService)
        {
            userService.UserId = values.UserId;
            userService.UserName = values.UserName;
            userService.TenantId = values.TenantId;
            userService.Roles = values.Roles;
        }
    }

    private record CurrentUserValues
    {
        public string? UserId { get; init; }
        public string? UserName { get; init; }
        public Guid? TenantId { get; init; }
        public IReadOnlyCollection<string> Roles { get; init; } = [];
    }
}

/// <summary>
/// Extension methods for adding the current user middleware.
/// </summary>
public static class CurrentUserMiddlewareExtensions
{
    public static IApplicationBuilder UseCurrentUser(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CurrentUserMiddleware>();
    }
}
