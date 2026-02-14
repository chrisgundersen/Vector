using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Vector.Application.Common.Interfaces;
using Vector.Application.DocumentProcessing.Services;
using Vector.Domain.Common;
using Vector.Domain.DocumentProcessing;
using Vector.Domain.EmailIntake;
using Vector.Domain.Routing;
using Vector.Domain.Submission;
using Vector.Domain.UnderwritingGuidelines;
using Vector.Infrastructure.Caching;
using Vector.Infrastructure.DocumentAI;
using Vector.Infrastructure.Email;
using Vector.Infrastructure.Messaging;
using Vector.Infrastructure.Persistence;
using Vector.Infrastructure.Persistence.Repositories;
using Vector.Infrastructure.Services;
using Vector.Infrastructure.ExternalSystems;
using Vector.Infrastructure.Storage;

namespace Vector.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var useInMemoryDatabase = configuration.GetValue<bool>("UseInMemoryDatabase");

        if (useInMemoryDatabase)
        {
            services.AddDbContext<VectorDbContext>(options =>
                options.UseInMemoryDatabase("VectorDb"));
        }
        else
        {
            services.AddDbContext<VectorDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(VectorDbContext).Assembly.FullName)));
        }

        // Register Unit of Work
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<VectorDbContext>());

        // Register Repositories
        services.AddScoped<IInboundEmailRepository, InboundEmailRepository>();
        services.AddScoped<IProcessingJobRepository, ProcessingJobRepository>();
        services.AddScoped<ISubmissionRepository, SubmissionRepository>();
        services.AddScoped<IDataCorrectionRepository, DataCorrectionRepository>();
        services.AddScoped<IRoutingRuleRepository, RoutingRuleRepository>();
        services.AddScoped<IRoutingDecisionRepository, RoutingDecisionRepository>();
        services.AddScoped<IProducerUnderwriterPairingRepository, ProducerUnderwriterPairingRepository>();
        services.AddScoped<IUnderwritingGuidelineRepository, UnderwritingGuidelineRepository>();

        // Register Services
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // Register external services (use mock for development)
        var useMockServices = configuration.GetValue<bool>("UseMockServices");

        if (useMockServices)
        {
            services.AddSingleton<IBlobStorageService, LocalBlobStorageService>();
            services.AddSingleton<ICacheService, InMemoryCacheService>();
            services.AddSingleton<IEmailService, MockEmailService>();
            services.AddSingleton<IDocumentIntelligenceService, MockDocumentIntelligenceService>();
            services.AddSingleton<IMessageBusService, InMemoryMessageBusService>();
        }
        else
        {
            // Azure services configuration
            ConfigureAzureServices(services, configuration);
        }

        // Always register email deduplication service (uses ICacheService)
        services.AddScoped<IEmailDeduplicationService, EmailDeduplicationService>();

        // Register application layer services
        services.AddScoped<ISubmissionCreationService, SubmissionCreationService>();
        services.AddScoped<IDataQualityScoringService, DataQualityScoringService>();

        // Register database seeder
        services.AddScoped<DatabaseSeeder>();

        // Register external system integrations (NoOp for development)
        // TODO: Replace with real implementations when PAS/CRM integrations are configured
        services.AddScoped<IExternalPolicyService, NoOpPolicyService>();
        services.AddScoped<IExternalCrmService, NoOpCrmService>();

        return services;
    }

    private static void ConfigureAzureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Azure Blob Storage
        ConfigureBlobStorage(services, configuration);

        // Redis Cache
        ConfigureCache(services, configuration);

        // Email Service (Microsoft Graph or Mock)
        ConfigureEmailService(services, configuration);

        // Message Bus (Azure Service Bus or InMemory)
        ConfigureMessageBus(services, configuration);

        // Document Intelligence (Azure or Mock)
        ConfigureDocumentIntelligence(services, configuration);
    }

    private static void ConfigureBlobStorage(IServiceCollection services, IConfiguration configuration)
    {
        var blobConnectionString = configuration.GetConnectionString("BlobStorage");
        if (!string.IsNullOrEmpty(blobConnectionString))
        {
            services.AddSingleton(new Azure.Storage.Blobs.BlobServiceClient(blobConnectionString));
            services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
        }
        else
        {
            services.AddSingleton<IBlobStorageService, LocalBlobStorageService>();
        }
    }

    private static void ConfigureCache(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(
                StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString));
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddSingleton<ICacheService, InMemoryCacheService>();
        }
    }

    private static void ConfigureEmailService(IServiceCollection services, IConfiguration configuration)
    {
        var emailProvider = configuration.GetValue<string>("EmailService:Provider");

        if (string.Equals(emailProvider, "Graph", StringComparison.OrdinalIgnoreCase))
        {
            // Configure Microsoft Graph email service
            var graphSection = configuration.GetSection(GraphEmailServiceOptions.SectionName);
            services.Configure<GraphEmailServiceOptions>(options =>
            {
                options.TenantId = graphSection.GetValue<string>("TenantId") ?? string.Empty;
                options.ClientId = graphSection.GetValue<string>("ClientId") ?? string.Empty;
                options.ClientSecret = graphSection.GetValue<string>("ClientSecret") ?? string.Empty;
                options.ProcessedFolderName = graphSection.GetValue("ProcessedFolderName", "Processed")!;
            });

            var graphOptions = graphSection.Get<GraphEmailServiceOptions>();

            if (graphOptions is not null &&
                !string.IsNullOrEmpty(graphOptions.TenantId) &&
                !string.IsNullOrEmpty(graphOptions.ClientId) &&
                !string.IsNullOrEmpty(graphOptions.ClientSecret))
            {
                var credential = new ClientSecretCredential(
                    graphOptions.TenantId,
                    graphOptions.ClientId,
                    graphOptions.ClientSecret);

                services.AddSingleton(new GraphServiceClient(credential));
                services.AddSingleton<IEmailService, GraphEmailService>();
            }
            else
            {
                // Fall back to mock if Graph not properly configured
                services.AddSingleton<IEmailService, MockEmailService>();
            }
        }
        else
        {
            services.AddSingleton<IEmailService, MockEmailService>();
        }
    }

    private static void ConfigureMessageBus(IServiceCollection services, IConfiguration configuration)
    {
        var messageBusProvider = configuration.GetValue<string>("MessageBus:Provider");

        if (string.Equals(messageBusProvider, "AzureServiceBus", StringComparison.OrdinalIgnoreCase))
        {
            // Configure Azure Service Bus
            var serviceBusSection = configuration.GetSection(AzureServiceBusOptions.SectionName);
            services.Configure<AzureServiceBusOptions>(options =>
            {
                options.ConnectionString = serviceBusSection.GetValue<string>("ConnectionString") ?? string.Empty;
                options.EmailIngestionQueue = serviceBusSection.GetValue("EmailIngestionQueue", "email-ingestion")!;
                options.DocumentProcessingQueue = serviceBusSection.GetValue("DocumentProcessingQueue", "document-processing")!;
            });

            var serviceBusConnectionString = serviceBusSection.GetValue<string>("ConnectionString");

            if (!string.IsNullOrEmpty(serviceBusConnectionString))
            {
                services.AddSingleton(new ServiceBusClient(serviceBusConnectionString));
                services.AddSingleton<IMessageBusService, AzureServiceBusService>();
            }
            else
            {
                // Fall back to in-memory if connection string not provided
                services.AddSingleton<IMessageBusService, InMemoryMessageBusService>();
            }
        }
        else
        {
            services.AddSingleton<IMessageBusService, InMemoryMessageBusService>();
        }
    }

    private static void ConfigureDocumentIntelligence(IServiceCollection services, IConfiguration configuration)
    {
        var docIntelligenceProvider = configuration.GetValue<string>("DocumentIntelligence:Provider");

        if (string.Equals(docIntelligenceProvider, "Azure", StringComparison.OrdinalIgnoreCase))
        {
            // Configure Azure Document Intelligence
            var azureSection = configuration.GetSection(AzureDocumentIntelligenceOptions.SectionName);
            services.Configure<AzureDocumentIntelligenceOptions>(options =>
            {
                options.Endpoint = azureSection.GetValue<string>("Endpoint") ?? string.Empty;
                options.ApiKey = azureSection.GetValue<string>("ApiKey") ?? string.Empty;
                options.ClassifierModelId = azureSection.GetValue<string>("ClassifierModelId");
                options.Acord125ModelId = azureSection.GetValue<string>("Acord125ModelId");
                options.Acord126ModelId = azureSection.GetValue<string>("Acord126ModelId");
                options.Acord130ModelId = azureSection.GetValue<string>("Acord130ModelId");
                options.Acord140ModelId = azureSection.GetValue<string>("Acord140ModelId");
                options.LossRunModelId = azureSection.GetValue<string>("LossRunModelId");
                options.ExposureScheduleModelId = azureSection.GetValue<string>("ExposureScheduleModelId");
                options.ModelVersion = azureSection.GetValue("ModelVersion", "1.0")!;
            });

            var endpoint = azureSection.GetValue<string>("Endpoint");
            var apiKey = azureSection.GetValue<string>("ApiKey");

            if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
            {
                services.AddSingleton(new DocumentAnalysisClient(
                    new Uri(endpoint),
                    new AzureKeyCredential(apiKey)));
                services.AddSingleton<IDocumentIntelligenceService, AzureDocumentIntelligenceService>();
            }
            else
            {
                // Fall back to mock if not properly configured
                services.AddSingleton<IDocumentIntelligenceService, MockDocumentIntelligenceService>();
            }
        }
        else
        {
            services.AddSingleton<IDocumentIntelligenceService, MockDocumentIntelligenceService>();
        }
    }
}
