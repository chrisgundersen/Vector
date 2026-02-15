using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Vector.Application;
using Vector.Application.Common.Interfaces;
using Vector.Infrastructure;
using Vector.Web.Underwriting.Components;
using Vector.Web.Underwriting.Hubs;
using Vector.Web.Underwriting.Services;

var builder = WebApplication.CreateBuilder(args);

var disableAuthentication = builder.Configuration.GetValue<bool>("Authentication:DisableAuthentication");

if (disableAuthentication)
{
    // For local development - use cookie auth with no login required
    builder.Services.AddAuthentication("LocalDev")
        .AddCookie("LocalDev", options =>
        {
            options.Cookie.Name = "VectorLocalDev";
            options.ExpireTimeSpan = TimeSpan.FromDays(1);
            options.Events.OnRedirectToLogin = context =>
            {
                // Don't redirect, just return 200 - we're in dev mode
                context.Response.StatusCode = 200;
                return Task.CompletedTask;
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        // No fallback policy - allow anonymous access
    });
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

// Add cascade authentication state for Blazor
builder.Services.AddCascadingAuthenticationState();

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
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

if (!disableAuthentication)
{
    app.MapControllers(); // For Identity UI controllers
}
app.MapHub<SubmissionHub>("/hubs/submissions"); // SignalR hub
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
