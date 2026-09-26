using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Infrastructure.Adapters.Ai;
using ServiceDesk.Infrastructure.Adapters.Search;
using ServiceDesk.Infrastructure.Adapters.Status;
using ServiceDesk.Infrastructure.Adapters.Storage;
using ServiceDesk.Infrastructure.Adapters.Ticketing;
using ServiceDesk.Infrastructure.Persistence;
using ServiceDesk.Infrastructure.Persistence.Repositories;
using System.ClientModel;
using ServiceDesk.Infrastructure.Services;
using Azure;
using Azure.AI.OpenAI;
using ServiceDesk.Application.Services.Agent;

namespace ServiceDesk.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("UseInMemoryDatabase", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A SQL Server connection string is required. In-memory databases are disabled.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Repositories
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IIncidentDraftRepository, IncidentDraftRepository>();
        
        // System Status Reader
        services.AddSingleton<ISystemStatusReader, FakeSystemStatusReader>();

        // Incident Ticketing Gateway
        services.AddSingleton<IIncidentGateway, MockIncidentGatewayAdapter>();
        
        // Audit Logger
        services.AddScoped<IAuditLogger, AuditLogger>();

        // Knowledge source is Azure Blob Storage only. Local project files are not a runtime source.
        services.AddSingleton<AzureBlobKnowledgeSourceStore>();
        services.AddSingleton<IKnowledgeSourceStore>(sp => sp.GetRequiredService<AzureBlobKnowledgeSourceStore>());

        // Azure AI Search and Azure OpenAI embedding services are mandatory.
        services.AddHttpClient();
        services.AddSingleton<AzureOpenAiEmbeddingService>();
        services.AddSingleton<AzureAiSearchKnowledgeStoreAdapter>();
        services.AddSingleton<IKnowledgeIndexStore>(sp => sp.GetRequiredService<AzureAiSearchKnowledgeStoreAdapter>());
        services.AddSingleton<IKnowledgeRetriever>(sp => sp.GetRequiredService<AzureAiSearchKnowledgeStoreAdapter>());

        services.AddScoped<IKnowledgeIngestionService, KnowledgeIngestionService>();

        // AI Services & Preprocessors
        services.AddSingleton<IAiTelemetry, DefaultAiTelemetry>();
        services.AddSingleton<ISecretRedactionService, SecretRedactionService>();
        services.AddSingleton<SecretRedactionService>();

        bool useMockAi = bool.TryParse(configuration["FeatureFlags:UseMockAiProvider"], out var parsed) && parsed;
        string azureEndpoint = configuration["AzureOpenAI:Endpoint"]?.Trim() ?? string.Empty;
        string azureApiKey = configuration["AzureOpenAI:ApiKey"]?.Trim() ?? string.Empty;
        string embeddingDeployment = configuration["AzureOpenAI:EmbeddingDeploymentName"]?.Trim() ?? string.Empty;

        if (!useMockAi && !string.IsNullOrWhiteSpace(azureEndpoint) && !string.IsNullOrWhiteSpace(azureApiKey) && !string.IsNullOrWhiteSpace(embeddingDeployment))
        {
            services.AddHttpClient<AzureOpenAiChatModelAdapter>();
            services.AddTransient<IChatModel, AzureOpenAiChatModelAdapter>();
        }
        else 
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(azureEndpoint)) missing.Add("AzureOpenAI:Endpoint");
            if (string.IsNullOrWhiteSpace(azureApiKey)) missing.Add("AzureOpenAI:ApiKey");
            if (string.IsNullOrWhiteSpace(embeddingDeployment)) missing.Add("AzureOpenAI:EmbeddingDeploymentName");
            if (!useMockAi && missing.Count > 0)
            {
                throw new InvalidOperationException($"Azure OpenAI configuration is incomplete. Missing: {string.Join(", ", missing)}. Set these values in appsettings or environment variables.");
            }

            throw new InvalidOperationException("UseMockAiProvider must be false for Azure-only runtime operation.");
        }

        // Multi-Agent Workflow: Azure AI Foundry mode vs Explicit Local Development Fallback
        bool useFoundryWorkflow = (bool.TryParse(configuration["FeatureFlags:UseAzureAiFoundryAgentWorkflow"], out var parsedFoundry) && parsedFoundry) ||
                                  (bool.TryParse(configuration["AzureAiFoundry:UseAzureAiFoundryAgentWorkflow"], out var parsedFoundryAlt) && parsedFoundryAlt);

        bool enableLocalFallback = (bool.TryParse(configuration["FeatureFlags:EnableLocalAgentWorkflowFallback"], out var parsedLocal) && parsedLocal) ||
                                   (bool.TryParse(configuration["FeatureFlags:UseLocalAgentWorkflowFallback"], out var parsedLocalAlt) && parsedLocalAlt);

        services.AddHttpClient<AzureAiFoundryAgentWorkflowAdapter>();

        if (useFoundryWorkflow)
        {
            string foundryEndpoint = configuration["AzureAiFoundry:ProjectEndpoint"]?.Trim() ?? string.Empty;
            string foundryKey = configuration["AzureAiFoundry:ApiKey"]?.Trim() ?? string.Empty;
            string plannerName = configuration["AzureAiFoundry:PlannerAgentName"]?.Trim() ?? string.Empty;
            string specialistName = configuration["AzureAiFoundry:SupportSpecialistAgentName"]?.Trim() ?? string.Empty;
            string reviewerName = configuration["AzureAiFoundry:ReviewerAgentName"]?.Trim() ?? string.Empty;

            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(foundryEndpoint) || foundryEndpoint.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
                missing.Add("AzureAiFoundry:ProjectEndpoint");
            if (string.IsNullOrWhiteSpace(foundryKey) || foundryKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
                missing.Add("AzureAiFoundry:ApiKey");
            if (plannerName.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
                missing.Add("AzureAiFoundry:PlannerAgentName");
            if (specialistName.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
                missing.Add("AzureAiFoundry:SupportSpecialistAgentName");
            if (reviewerName.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
                missing.Add("AzureAiFoundry:ReviewerAgentName");

            if (missing.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Azure AI Foundry agent workflow is enabled (UseAzureAiFoundryAgentWorkflow=true), but required configuration is missing or contains placeholder values. Missing: {string.Join(", ", missing)}.");
            }

            string openAiEndpoint = !string.IsNullOrWhiteSpace(azureEndpoint) ? azureEndpoint : ResolveOpenAiEndpoint(foundryEndpoint);
            string openAiKey = !string.IsNullOrWhiteSpace(azureApiKey) ? azureApiKey : foundryKey;

            services.AddSingleton(new AzureOpenAIClient(new Uri(openAiEndpoint), new ApiKeyCredential(openAiKey)));
            services.AddHttpClient<AzureOpenAiFoundryClient>();
            services.AddSingleton<IFoundryAgentClient, AzureOpenAiFoundryClient>();
            services.AddTransient<AzureAiFoundryAgentWorkflowAdapter>();
            services.AddTransient<IAgentWorkflow, AzureAiFoundryAgentWorkflowAdapter>();
        }
        else if (enableLocalFallback)
        {
            services.AddTransient<IAgentWorkflow, AgentWorkflowCoordinator>();
        }
        else
        {
            services.AddTransient<IAgentWorkflow, DisabledAgentWorkflow>();
        }

        return services;
    }

    private static string ResolveOpenAiEndpoint(string foundryEndpoint)
    {
        if (Uri.TryCreate(foundryEndpoint, UriKind.Absolute, out var uri))
        {
            if (uri.Host.EndsWith(".services.ai.azure.com", StringComparison.OrdinalIgnoreCase))
            {
                var resourceName = uri.Host.Substring(0, uri.Host.IndexOf(".services.ai.azure.com", StringComparison.OrdinalIgnoreCase));
                return $"https://{resourceName}.openai.azure.com/";
            }
            return uri.GetLeftPart(UriPartial.Authority);
        }
        return foundryEndpoint;
    }
}


