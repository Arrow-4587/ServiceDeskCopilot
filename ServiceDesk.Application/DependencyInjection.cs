using Microsoft.Extensions.DependencyInjection;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.Services;
using ServiceDesk.Application.Services.Agent;
using ServiceDesk.Application.Services.Chat;
using ServiceDesk.Application.Services.Mcp;
using ServiceDesk.Application.UseCases;

namespace ServiceDesk.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Application Services
        services.AddTransient<IRagGroundingService, RagGroundingService>();
        services.AddTransient<IChatConversationService, ChatConversationService>();
        services.AddTransient<IIncidentReviewerService, IncidentReviewerService>();
        services.AddTransient<IPlannerService, PlannerService>();
        services.AddTransient<ISupportSpecialistService, SupportSpecialistService>();
        services.AddTransient<IKnowledgeBaseService, ServiceDesk.Application.Services.Knowledge.KnowledgeBaseService>();
        services.AddTransient<AgentWorkflowCoordinator>();
        services.AddTransient<IAgentWorkflow, AgentWorkflowCoordinator>();

        // Application Use Cases
        services.AddTransient<SearchKnowledgeUseCase>();
        services.AddTransient<GetSystemStatusUseCase>();
        services.AddTransient<CreateIncidentDraftUseCase>();
        services.AddTransient<ApproveAndSubmitIncidentUseCase>();

        return services;
    }
}
