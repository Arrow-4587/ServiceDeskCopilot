using ServiceDesk.Application.Common.Interfaces;

namespace ServiceDesk.Web.Services;

public class KnowledgeIngestionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KnowledgeIngestionBackgroundService> _logger;

    public KnowledgeIngestionBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<KnowledgeIngestionBackgroundService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[KnowledgeIngestionBackgroundService] Background service started. Scheduling non-blocking knowledge ingestion...");

        // Delay briefly to allow HTTP host to bind ports and start serving requests immediately
        await Task.Delay(1000, stoppingToken);

        if (stoppingToken.IsCancellationRequested)
            return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            
            // Pre-warm the approved knowledge documents cache immediately
            var knowledgeService = scope.ServiceProvider.GetService<IKnowledgeBaseService>();
            if (knowledgeService != null)
            {
                _logger.LogInformation("[KnowledgeIngestionBackgroundService] Pre-warming approved knowledge documents cache...");
                await knowledgeService.GetApprovedDocumentsAsync(stoppingToken);
            }

            var ingestionService = scope.ServiceProvider.GetRequiredService<IKnowledgeIngestionService>();

            _logger.LogInformation("[KnowledgeIngestionBackgroundService] Triggering background knowledge ingestion pipeline...");
            int ingestedCount = await ingestionService.IngestApprovedKnowledgeDocumentsAsync(stoppingToken);
            _logger.LogInformation("[KnowledgeIngestionBackgroundService] Completed background knowledge ingestion. Total chunks processed: {Count}", ingestedCount);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[KnowledgeIngestionBackgroundService] Ingestion task was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[KnowledgeIngestionBackgroundService] Background knowledge ingestion encountered an operational error. Grounded search remains active on existing Azure AI Search index.");
        }
    }
}
