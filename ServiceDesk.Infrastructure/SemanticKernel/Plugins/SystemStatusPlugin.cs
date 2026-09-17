using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ServiceDesk.Application.UseCases;

namespace ServiceDesk.Infrastructure.SemanticKernel.Plugins;

public class SystemStatusPlugin
{
    private readonly GetSystemStatusUseCase _statusUseCase;
    private readonly ILogger<SystemStatusPlugin> _logger;

    public SystemStatusPlugin(
        GetSystemStatusUseCase statusUseCase,
        ILogger<SystemStatusPlugin> logger)
    {
        _statusUseCase = statusUseCase ?? throw new ArgumentNullException(nameof(statusUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [KernelFunction, Description("Checks current operational status and outage reports for corporate IT services such as GlobalProtect VPN, Exchange Online, Microsoft Teams, Wi-Fi, or Print Spooler.")]
    public async Task<string> CheckServiceHealthAsync(
        [Description("Optional specific service name to filter (e.g. 'VPN', 'Teams', 'Outlook')")] string? serviceName = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[SemanticKernel:Plugin] Executing SystemStatusPlugin. Filter: {ServiceName}", serviceName);

        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            var single = await _statusUseCase.GetByServiceNameAsync(serviceName, cancellationToken);
            if (single != null)
            {
                return JsonSerializer.Serialize(new
                {
                    Service = single.ServiceName,
                    Status = single.Status,
                    Description = single.Description,
                    LastChecked = single.LastChecked
                });
            }
        }

        var all = await _statusUseCase.GetAllAsync(cancellationToken);
        var formatted = all.Select(s => new
        {
            Service = s.ServiceName,
            Status = s.Status,
            Description = s.Description
        });

        return JsonSerializer.Serialize(formatted);
    }
}
