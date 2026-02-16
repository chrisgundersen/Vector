using Microsoft.AspNetCore.Authentication;
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
    // For local development - use mock user service for user switching
    builder.Services.AddSingleton<MockUserService>();

    // For local development - use custom auth handler that auto-authenticates all requests
    builder.Services.AddAuthentication("LocalDev")
        .AddScheme<AuthenticationSchemeOptions, LocalDevAuthenticationHandler>("LocalDev", null);

    // Authorization allows all authenticated users
    builder.Services.AddAuthorizationBuilder();

    // Provide authentication state for Blazor components with user switching support
    builder.Services.AddScoped<MockAuthenticationStateProvider>();
    builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<MockAuthenticationStateProvider>());

    // Required for CascadingAuthenticationState to work with SSR
    builder.Services.AddCascadingAuthenticationState();
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

    // Required for CascadingAuthenticationState to work with SSR
    builder.Services.AddCascadingAuthenticationState();
}

// Add SignalR for real-time updates
builder.Services.AddSignalR();

// Add application services (MediatR)
builder.Services.AddApplication();

// Register Web.Underwriting notification handlers for domain event → SignalR bridging
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<Vector.Web.Underwriting.EventHandlers.SubmissionCreatedNotificationHandler>());

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

// Health check endpoint
app.MapGet("/health", () => "OK");

if (!disableAuthentication)
{
    app.MapControllers(); // For Identity UI controllers
}

app.MapHub<SubmissionHub>("/hubs/submissions"); // SignalR hub

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Fallback for unmatched routes - renders App component which shows NotFound via Router
app.MapFallback(context => new Microsoft.AspNetCore.Http.HttpResults.RazorComponentResult<App>().ExecuteAsync(context));

app.Run();
