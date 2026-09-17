using System.Text;
using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Application.Prompts;

namespace ServiceDesk.Application.Services;

public class RagGroundingService : IRagGroundingService
{
    public const string FallbackMessage = "I am sorry, but I do not have enough approved IT knowledge documentation to answer your request securely.";
    public const double MinimumRelevanceScoreThreshold = 0.5;

    private readonly IKnowledgeRetriever _retriever;
    private readonly IChatModel _chatModel;
    private readonly ILogger<RagGroundingService> _logger;

    public RagGroundingService(
        IKnowledgeRetriever retriever,
        IChatModel chatModel,
        ILogger<RagGroundingService> logger)
    {
        _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
        _chatModel = chatModel ?? throw new ArgumentNullException(nameof(chatModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RagAnswerResultDto> GenerateGroundedAnswerAsync(string userQuery, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userQuery))
        {
            return new RagAnswerResultDto(FallbackMessage, Array.Empty<CitationDto>(), Grounded: false, TriggeredFallback: true);
        }

        // 1. Search knowledge index with FilterApprovedOnly = true
        var searchQuery = new SearchQueryDto(userQuery.Trim(), TopK: 3, FilterApprovedOnly: true);
        var searchResults = await _retriever.SearchKnowledgeAsync(searchQuery, cancellationToken);

        // 2. Enforce Grounding Invariant: Approved == true AND Active == true AND Score >= Threshold
        var validResults = searchResults
            .Where(r => r.Approved && r.Active && r.Score >= MinimumRelevanceScoreThreshold)
            .ToList();

        // 3. Fallback Protocol if no sufficient grounding knowledge is found
        if (validResults.Count == 0)
        {
            _logger.LogInformation("RAG retrieval yielded 0 approved/relevant documents for query '{Query}'. Triggering Fallback Protocol.", userQuery);
            return new RagAnswerResultDto(FallbackMessage, Array.Empty<CitationDto>(), Grounded: false, TriggeredFallback: true);
        }

        // 4. Build Grounded System Context Prompt
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("RETRIEVED KNOWLEDGE CONTEXT:");

        for (int i = 0; i < validResults.Count; i++)
        {
            var res = validResults[i];
            contextBuilder.AppendLine($"--- Document {i + 1} ---");
            contextBuilder.AppendLine($"DocumentName: {res.DocumentName}");
            contextBuilder.AppendLine($"Version: {res.Version}");
            contextBuilder.AppendLine($"Section: {res.Section}");
            contextBuilder.AppendLine($"Page: {res.Page}");
            contextBuilder.AppendLine($"BlobPath: {res.BlobPath}");
            contextBuilder.AppendLine($"Content:\n{res.Content}");
            contextBuilder.AppendLine();
        }

        string fullPrompt = $"{SystemPrompts.SupportSpecialistSystemPrompt}\n\n{contextBuilder}\n\nUSER INQUIRY: {userQuery}";

        // 5. Generate Grounded AI Response
        var chatRequest = new ChatRequestDto(Guid.NewGuid(), fullPrompt);
        var modelResponse = await _chatModel.GenerateCompletionAsync(chatRequest, cancellationToken);

        if (modelResponse == null || string.IsNullOrWhiteSpace(modelResponse.Answer))
        {
            return new RagAnswerResultDto(FallbackMessage, Array.Empty<CitationDto>(), Grounded: false, TriggeredFallback: true);
        }

        // 6. Format Citations (DocumentName, Section, Page, Snippet, BlobPath)
        var citations = validResults
            .Select(r => new CitationDto(
                DocumentName: r.DocumentName,
                Section: r.Section,
                Page: r.Page,
                Snippet: r.Content.Length > 150 ? r.Content.Substring(0, 150) + "..." : r.Content,
                BlobPath: r.BlobPath
            ))
            .Distinct()
            .ToList();

        return new RagAnswerResultDto(
            Answer: modelResponse.Answer,
            Citations: citations,
            Grounded: true,
            TriggeredFallback: false
        );
    }
}
