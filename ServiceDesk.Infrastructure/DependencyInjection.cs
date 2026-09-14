using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Infrastructure.Adapters.Ai;
using ServiceDesk.Infrastructure.Adapters.Search;
using ServiceDesk.Infrastructure.Adapters.Status;
using ServiceDesk.Infrastructure.Adapters.Storage;
using ServiceDesk.Infrastructure.Persistence;
using ServiceDesk.Infrastructure.Persistence.Repositories;
using ServiceDesk.Infrastructure.Services;

namespace ServiceDesk.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // EF Core DbContext Registration
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("UseInMemoryDatabase=true"))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("ServiceDeskCopilotDb"));
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
        }

        // Repositories
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IIncidentDraftRepository, IncidentDraftRepository>();

        // Knowledge Source Store & Status Reader
        services.AddSingleton<IKnowledgeSourceStore, LocalFileKnowledgeSourceStore>();
        services.AddSingleton<ISystemStatusReader, FakeSystemStatusReader>();

        // Search Adapters & Ingestion Services
        services.AddSingleton<InMemoryKnowledgeSearchAdapter>();
        services.AddSingleton<IKnowledgeIndexStore>(sp => sp.GetRequiredService<InMemoryKnowledgeSearchAdapter>());
        services.AddSingleton<IKnowledgeRetriever>(sp => sp.GetRequiredService<InMemoryKnowledgeSearchAdapter>());
        services.AddScoped<IKnowledgeIngestionService, KnowledgeIngestionService>();

        // AI Services & Preprocessors
        services.AddSingleton<ISecretRedactionService, SecretRedactionService>();
        services.AddSingleton<SecretRedactionService>();
        services.AddTransient<MockAiChatModelAdapter>();

        var useMockConfig = configuration["FeatureFlags:UseMockAiProvider"];
        bool useMockAi = string.IsNullOrEmpty(useMockConfig) || bool.TryParse(useMockConfig, out var parsed) && parsed;
        string azureEndpoint = configuration["AzureOpenAI:Endpoint"] ?? string.Empty;

        if (useMockAi || string.IsNullOrWhiteSpace(azureEndpoint))
        {
            services.AddTransient<IChatModel, MockAiChatModelAdapter>();
        }
        else
        {
            services.AddTransient<IChatModel, AzureOpenAiChatModelAdapter>();
        }

        return services;
    }
}

