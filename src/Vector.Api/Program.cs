using Serilog;
using Vector.Api;
using Vector.Application;
using Vector.Infrastructure;

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

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Vector API v1");
            c.RoutePrefix = string.Empty;
        });
    }

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
