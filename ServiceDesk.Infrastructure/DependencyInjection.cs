using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Infrastructure.Adapters.Ai;
using ServiceDesk.Infrastructure.Adapters.Search;
using ServiceDesk.Infrastructure.Adapters.Status;
using ServiceDesk.Infrastructure.Adapters.Storage;
using ServiceDesk.Infrastructure.Adapters.Ticketing;
using ServiceDesk.Infrastructure.Persistence;
using ServiceDesk.Infrastructure.Persistence.Repositories;
using System.ClientModel;
using Azure.AI.OpenAI;
using ServiceDesk.Infrastructure.SemanticKernel.Agents;
using ServiceDesk.Infrastructure.SemanticKernel.Plugins;
using ServiceDesk.Infrastructure.Services;
using Azure;

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
        
        // Audit Logger
        services.AddScoped<IAuditLogger, AuditLogger>();

        // Knowledge Source Store (Azure Blob Cloud Storage with Local Fallback)
        services.AddSingleton<LocalFileKnowledgeSourceStore>();
        services.AddSingleton<AzureBlobKnowledgeSourceStore>();
        services.AddSingleton<IKnowledgeSourceStore>(sp => sp.GetRequiredService<AzureBlobKnowledgeSourceStore>());

        // Status Reader & Ticketing Gateway
        services.AddSingleton<ISystemStatusReader, FakeSystemStatusReader>();
        services.AddSingleton<IIncidentGateway, MockIncidentGatewayAdapter>();

        // Search Adapters & Ingestion Services
        services.AddHttpClient();
        services.AddSingleton<AzureOpenAiEmbeddingService>();
        services.AddSingleton<InMemoryKnowledgeSearchAdapter>();
        services.AddSingleton<AzureAiSearchKnowledgeStoreAdapter>();

        var useMockSearchConfig = configuration["FeatureFlags:UseMockSearchProvider"];
        bool useMockSearch = string.IsNullOrEmpty(useMockSearchConfig) || (bool.TryParse(useMockSearchConfig, out var parsedSearch) && parsedSearch);

        if (useMockSearch)
        {
            services.AddSingleton<IKnowledgeIndexStore>(sp => sp.GetRequiredService<InMemoryKnowledgeSearchAdapter>());
            services.AddSingleton<IKnowledgeRetriever>(sp => sp.GetRequiredService<InMemoryKnowledgeSearchAdapter>());
        }
        else
        {
            services.AddSingleton<IKnowledgeIndexStore>(sp => sp.GetRequiredService<AzureAiSearchKnowledgeStoreAdapter>());
            services.AddSingleton<IKnowledgeRetriever>(sp => sp.GetRequiredService<AzureAiSearchKnowledgeStoreAdapter>());
        }

        services.AddScoped<IKnowledgeIngestionService, KnowledgeIngestionService>();

        // AI Services & Preprocessors
        services.AddSingleton<IAiTelemetry, DefaultAiTelemetry>();
        services.AddSingleton<ISecretRedactionService, SecretRedactionService>();
        services.AddSingleton<SecretRedactionService>();

        var useMockConfig = configuration["FeatureFlags:UseMockAiProvider"];
        bool useMockAi = string.IsNullOrEmpty(useMockConfig) || bool.TryParse(useMockConfig, out var parsed) && parsed;
        string azureEndpoint = configuration["AzureOpenAI:Endpoint"]?.Trim() ?? string.Empty;
        string azureApiKey = configuration["AzureOpenAI:ApiKey"]?.Trim() ?? string.Empty;

        if (!useMockAi && !string.IsNullOrWhiteSpace(azureEndpoint) && !string.IsNullOrWhiteSpace(azureApiKey))
        {
            services.AddHttpClient<AzureOpenAiChatModelAdapter>();
            services.AddTransient<IChatModel, AzureOpenAiChatModelAdapter>();
        }
        else 
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(azureEndpoint)) missing.Add("AzureOpenAI:Endpoint");
            if (string.IsNullOrWhiteSpace(azureApiKey)) missing.Add("AzureOpenAI:ApiKey");
            if (!useMockAi && missing.Count > 0)
            {
                throw new InvalidOperationException($"Azure OpenAI configuration is incomplete. Missing: {string.Join(", ", missing)}. Set these values in appsettings or environment variables.");
            }

            throw new InvalidOperationException("UseMockAiProvider must be true when Azure OpenAI is not configured.");
        }

        // Semantic Kernel Core & Plugins
        services.AddTransient<Kernel>(sp => Kernel.CreateBuilder().Build());
        services.AddTransient<KnowledgeSearchPlugin>();
        services.AddTransient<SystemStatusPlugin>();
        services.AddTransient<IncidentDraftPlugin>();

        // 3 Semantic Kernel Agents (Planner, Specialist, Reviewer)
        services.AddTransient<SemanticKernelPlannerAgent>();
        services.AddTransient<SemanticKernelSpecialistAgent>();
        services.AddTransient<SemanticKernelReviewerAgent>();

        // Semantic Kernel & Azure AI Foundry Workflow Adapters
        services.AddHttpClient<AzureAiFoundryAgentWorkflowAdapter>();
        services.AddTransient<SemanticKernelAgentWorkflowAdapter>();
        services.AddTransient<AzureAiFoundryAgentWorkflowAdapter>();

        string foundryEndpoint = configuration["AzureAiFoundry:ProjectEndpoint"] ?? string.Empty;
        string foundryKey = configuration["AzureAiFoundry:ApiKey"] ?? string.Empty;
        bool hasFoundry = !string.IsNullOrWhiteSpace(foundryEndpoint) && !string.IsNullOrWhiteSpace(foundryKey);

        if (hasFoundry)
        {
            // Register AzureOpenAIClient using the endpoint URI and ApiKeyCredential
            services.AddSingleton(new AzureOpenAIClient(new Uri(foundryEndpoint), new ApiKeyCredential(foundryKey)));

            services.AddTransient<IAgentWorkflow, AzureAiFoundryAgentWorkflowAdapter>();
        }
        else
        {
            services.AddTransient<IAgentWorkflow, SemanticKernelAgentWorkflowAdapter>();
        }

        return services;
    }
}

