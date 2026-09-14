using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceDesk.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Infrastructure adapters (persistence, Azure AI Search, Azure OpenAI, Blob Storage, MCP, external gateways) registered here.
        return services;
    }
}
