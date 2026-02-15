using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using System.Text.Json;
using Vector.Api;
using Vector.Api.Middleware;
using Vector.Application;
using Vector.Infrastructure;
using Vector.Infrastructure.Persistence;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Vector API");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // Add services
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApiServices(builder.Configuration);

    var app = builder.Build();

    // Initialize database
    await InitializeDatabaseAsync(app);

    // Configure the HTTP request pipeline
    app.UseCorrelationId();
    app.UseGlobalExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Vector API v1");
            c.RoutePrefix = string.Empty;
        });
    }

    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            var correlationId = httpContext.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeader].FirstOrDefault();
            if (!string.IsNullOrEmpty(correlationId))
            {
                diagnosticContext.Set("CorrelationId", correlationId);
            }
        };
    });
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Health check endpoints for Kubernetes probes
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = WriteHealthCheckResponse
    });

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live"),
        ResponseWriter = WriteHealthCheckResponse
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = WriteHealthCheckResponse
    });

    app.Run();

static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var response = new
    {
        status = report.Status.ToString(),
        totalDuration = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            duration = e.Value.Duration.TotalMilliseconds,
            description = e.Value.Description,
            exception = e.Value.Exception?.Message,
            data = e.Value.Data
        })
    };

    return context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    }));
}

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var configuration = services.GetRequiredService<IConfiguration>();

    try
    {
        var useInMemoryDatabase = configuration.GetValue<bool>("UseInMemoryDatabase");
        var sqliteConnectionString = configuration.GetConnectionString("Sqlite");
        var context = services.GetRequiredService<VectorDbContext>();

        if (!string.IsNullOrEmpty(sqliteConnectionString))
        {
            // SQLite for local development - use EnsureCreated (no migrations)
            logger.LogInformation("Using SQLite database, ensuring created...");
            await context.Database.EnsureCreatedAsync();
        }
        else if (!useInMemoryDatabase)
        {
            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");
        }
        else
        {
            logger.LogInformation("Using in-memory database, ensuring created...");
            await context.Database.EnsureCreatedAsync();
        }

        var seedDatabase = configuration.GetValue<bool>("SeedDatabase");
        if (seedDatabase)
        {
            logger.LogInformation("Seeding database...");
            var seeder = services.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
            logger.LogInformation("Database seeding completed");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
        throw;
    }
}
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
