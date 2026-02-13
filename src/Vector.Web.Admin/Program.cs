using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Vector.Application;
using Vector.Application.Common.Interfaces;
using Vector.Infrastructure;
using Vector.Web.Admin.Components;
using Vector.Web.Admin.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Microsoft Entra ID authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Admin"));

    // Fallback policy requires authentication for all pages
    options.FallbackPolicy = options.DefaultPolicy;
});

// Add cascade authentication state for Blazor
builder.Services.AddCascadingAuthenticationState();

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
app.MapControllers(); // For Identity UI controllers
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
