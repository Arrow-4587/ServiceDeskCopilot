using ServiceDesk.Application.Common.Interfaces;

namespace ServiceDesk.Worker;

public class KnowledgeIngestionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KnowledgeIngestionWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public KnowledgeIngestionWorker(IServiceProvider serviceProvider, ILogger<KnowledgeIngestionWorker> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Knowledge Ingestion Background Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var ingestionService = scope.ServiceProvider.GetRequiredService<IKnowledgeIngestionService>();

                int ingestedCount = await ingestionService.IngestApprovedKnowledgeDocumentsAsync(stoppingToken);
                _logger.LogInformation("Knowledge Ingestion Worker processed {Count} document chunks at {Time}.", ingestedCount, DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during background knowledge ingestion cycle.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}
