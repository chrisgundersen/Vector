using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Vector.Application;
using Vector.Application.Common.Interfaces;
using Vector.Infrastructure;
using Vector.Web.Underwriting.Components;
using Vector.Web.Underwriting.Hubs;
using Vector.Web.Underwriting;
using Vector.Web.Underwriting.Services;

var builder = WebApplication.CreateBuilder(args);

var disableAuthentication = builder.Configuration.GetValue<bool>("Authentication:DisableAuthentication");

// Make auth setting available to Blazor components
builder.Services.AddSingleton(new AuthSettings { DisableAuthentication = disableAuthentication });

if (disableAuthentication)
{
    // For local development - minimal auth setup that doesn't redirect
    builder.Services.AddAuthentication("LocalDev")
        .AddCookie("LocalDev", options =>
        {
            // Prevent redirects - just allow access
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            };
        });

    // No authorization policies - allow all access
    builder.Services.AddAuthorizationBuilder()
        .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true) // Always allow
            .Build());

    // Provide a fake authentication state for Blazor components
    builder.Services.AddScoped<AuthenticationStateProvider, AnonymousAuthenticationStateProvider>();
}
else
{
    // Add Microsoft Entra ID authentication
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

    builder.Services.AddControllersWithViews()
        .AddMicrosoftIdentityUI();

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireUnderwriterRole", policy =>
            policy.RequireRole("Underwriter", "Admin"));

        // Fallback policy requires authentication for all pages
        options.FallbackPolicy = options.DefaultPolicy;
    });
}

// Add SignalR for real-time updates
builder.Services.AddSignalR();

// Add application services (MediatR)
builder.Services.AddApplication();

// Add infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add current user service for Blazor Server
builder.Services.AddScoped<ICurrentUserService, BlazorServerCurrentUserService>();

// Add Razor components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsProduction())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

if (!disableAuthentication)
{
    app.MapControllers(); // For Identity UI controllers
}

app.MapHub<SubmissionHub>("/hubs/submissions"); // SignalR hub
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
