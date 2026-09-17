using ServiceDesk.Application.Common.Interfaces;

namespace ServiceDesk.Worker;

public class SystemStatusPollingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SystemStatusPollingWorker> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(15);

    public SystemStatusPollingWorker(IServiceProvider serviceProvider, ILogger<SystemStatusPollingWorker> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("System Status Health Polling Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var statusReader = scope.ServiceProvider.GetRequiredService<ISystemStatusReader>();

                var statuses = await statusReader.GetAllStatusesAsync(stoppingToken);
                foreach (var status in statuses)
                {
                    if (!status.Status.Equals("Operational", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("HEALTH ALERT: IT Service '{ServiceName}' is in state '{Status}': {Description}",
                            status.ServiceName, status.Status, status.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during IT system status polling cycle.");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }
}
