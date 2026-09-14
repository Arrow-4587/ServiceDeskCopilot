using Microsoft.Extensions.DependencyInjection;
using ServiceDesk.Application.UseCases;

namespace ServiceDesk.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Application Use Cases
        services.AddTransient<SearchKnowledgeUseCase>();
        services.AddTransient<GetSystemStatusUseCase>();
        services.AddTransient<CreateIncidentDraftUseCase>();
        services.AddTransient<ApproveAndSubmitIncidentUseCase>();

        return services;
    }
}
