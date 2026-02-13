using Serilog;
using Vector.Application;
using Vector.Infrastructure;
using Vector.Worker.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Vector Worker");

    var builder = Host.CreateApplicationBuilder(args);

    // Configure Serilog
    builder.Services.AddSerilog((services, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // Add application and infrastructure services
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Add hosted services
    builder.Services.AddHostedService<EmailPollingWorker>();
    builder.Services.AddHostedService<DocumentProcessingWorker>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
