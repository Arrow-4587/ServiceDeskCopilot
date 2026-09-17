using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Application.UseCases;

namespace ServiceDesk.Infrastructure.SemanticKernel.Plugins;

public class KnowledgeSearchPlugin
{
    private readonly SearchKnowledgeUseCase _searchKnowledgeUseCase;
    private readonly ILogger<KnowledgeSearchPlugin> _logger;

    public KnowledgeSearchPlugin(
        SearchKnowledgeUseCase searchKnowledgeUseCase,
        ILogger<KnowledgeSearchPlugin> logger)
    {
        _searchKnowledgeUseCase = searchKnowledgeUseCase ?? throw new ArgumentNullException(nameof(searchKnowledgeUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [KernelFunction, Description("Searches approved corporate IT policy documents and troubleshooting guides stored on Azure Cloud for answers to employee questions.")]
    public async Task<string> SearchPolicyDocumentsAsync(
        [Description("The search query keywords or technical issue description")] string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return "No query provided for policy search.";
        }

        _logger.LogInformation("[SemanticKernel:Plugin] Executing KnowledgeSearchPlugin with query: '{Query}'", query);

        var results = await _searchKnowledgeUseCase.ExecuteAsync(new SearchQueryDto(query), cancellationToken);

        if (results.Count == 0)
        {
            _logger.LogInformation("[SemanticKernel:Plugin] No matching approved documents found for query '{Query}'.", query);
            return "NO_APPROVED_KNOWLEDGE_FOUND";
        }

        var simplifiedResults = results.Take(3).Select(r => new
        {
            DocumentName = r.DocumentName,
            Section = r.Section,
            Page = r.Page,
            Excerpt = r.Content.Length > 350 ? r.Content.Substring(0, 350) + "..." : r.Content,
            BlobPath = r.BlobPath
        });

        return JsonSerializer.Serialize(simplifiedResults);
    }
}
