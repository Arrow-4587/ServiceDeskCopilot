using Microsoft.Extensions.DependencyInjection;

namespace ServiceDesk.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Application layer services (use cases, workflow contracts, domain event handlers) will be registered here.
        return services;
    }
}
