using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Agent;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Application.DTOs.Status;
using ServiceDesk.Application.Prompts;

namespace ServiceDesk.Application.Services.Agent;

public class SupportSpecialistService : ISupportSpecialistService
{
    private readonly IChatModel _chatModel;
    private readonly ILogger<SupportSpecialistService> _logger;

    public SupportSpecialistService(IChatModel chatModel, ILogger<SupportSpecialistService> logger)
    {
        _chatModel = chatModel ?? throw new ArgumentNullException(nameof(chatModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SpecialistResponseDto> TroubleshootAsync(
        string userMessage,
        PlannerPlanDto plan,
        IReadOnlyList<SearchResultDto> knowledge,
        IReadOnlyList<ServiceStatusDto> statuses,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Support Specialist Agent executing troubleshooting for intent: {Intent}", plan.UserIntent);

        var citations = new List<CitationDto>();
        var answerBuilder = new StringBuilder();

        // 1. Process Knowledge Chunks & System Status
        if (knowledge != null && knowledge.Count > 0)
        {
            foreach (var item in knowledge.Take(3))
            {
                citations.Add(new CitationDto(
                    DocumentName: item.DocumentName,
                    Section: item.Section,
                    Page: item.Page,
                    Snippet: item.Content.Length > 200 ? item.Content.Substring(0, 200) + "..." : item.Content,
                    BlobPath: item.BlobPath
                ));
            }
        }

        var contextBuilder = new StringBuilder();
        if (statuses != null && statuses.Count > 0)
        {
            contextBuilder.AppendLine("CURRENT IT SERVICE HEALTH STATUSES:");
            foreach (var status in statuses)
            {
                contextBuilder.AppendLine($"- {status.ServiceName}: Status={status.Status}, Details={status.Description}");
            }
            contextBuilder.AppendLine();
        }

        if (knowledge != null && knowledge.Count > 0)
        {
            contextBuilder.AppendLine("RETRIEVED KNOWLEDGE CONTEXT:");
            foreach (var item in knowledge.Take(3))
            {
                contextBuilder.AppendLine($"[Document: {item.DocumentName} | Section: {item.Section} | Page: {item.Page}]");
                contextBuilder.AppendLine(item.Content);
                contextBuilder.AppendLine();
            }
        }
        else
        {
            contextBuilder.AppendLine("RETRIEVED KNOWLEDGE CONTEXT: No specific policy or knowledge base article was found for this query in the search index.");
        }

        string fullPrompt = $"{SystemPrompts.SupportSpecialistSystemPrompt}\n\n{contextBuilder}\n\nUSER INQUIRY: {userMessage}";
        var chatRequest = new ChatRequestDto(Guid.NewGuid(), fullPrompt);

        try
        {
            var modelResponse = await _chatModel.GenerateCompletionAsync(chatRequest, cancellationToken);
            if (modelResponse != null && !string.IsNullOrWhiteSpace(modelResponse.Answer))
            {
                string answer = modelResponse.Answer.Trim();
                var impact = IncidentImpact.Medium;
                var urgency = IncidentUrgency.Medium;
                bool suggestDraft = plan.RequiresIncidentDraft || (!plan.RequiresStatusCheck && (knowledge == null || knowledge.Count == 0));

                return new SpecialistResponseDto(
                    Answer: answer,
                    Citations: citations,
                    RequiresClarification: false,
                    SuggestIncidentDraft: suggestDraft,
                    SuggestedTitle: suggestDraft ? $"IT Support Request: {Truncate(userMessage, 60)}" : null,
                    SuggestedDescription: suggestDraft ? $"User reported the following issue which requires IT tier escalation:\n\n{userMessage}" : null,
                    Category: plan.DraftCategory ?? "General IT",
                    Impact: impact,
                    Urgency: urgency
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate completion from IChatModel. Using fallback summary.");
        }

        // Fallback text if IChatModel throws an unexpected error
        if (knowledge != null && knowledge.Count > 0)
        {
            var primaryDoc = knowledge.First();
            answerBuilder.AppendLine($"Based on approved corporate IT policy [{primaryDoc.DocumentName}, {primaryDoc.Section}, Page {primaryDoc.Page}]:\n");
            
            string excerpt = primaryDoc.Content.Trim();
            if (excerpt.Length > 500)
            {
                excerpt = excerpt.Substring(0, 500) + "...";
            }
            answerBuilder.AppendLine(excerpt);
            answerBuilder.AppendLine();
            answerBuilder.AppendLine($"For further details, refer to **{primaryDoc.DocumentName}** ({primaryDoc.Section}). If these diagnostic steps do not resolve your issue, I can generate an official incident draft for IT Service Desk escalation.");
        }
        else if (statuses != null && statuses.Count > 0 && plan.RequiresStatusCheck)
        {
            var degradedOrDown = statuses.Where(s => !string.Equals(s.Status, "Operational", StringComparison.OrdinalIgnoreCase)).ToList();
            if (degradedOrDown.Count > 0)
            {
                answerBuilder.AppendLine("Current Affected Services:\n");
                foreach (var item in degradedOrDown)
                {
                    answerBuilder.AppendLine($"- **{item.ServiceName}**: {item.Status} — {item.Description}");
                }
                var operational = statuses.Where(s => string.Equals(s.Status, "Operational", StringComparison.OrdinalIgnoreCase)).ToList();
                if (operational.Count > 0)
                {
                    answerBuilder.AppendLine($"\nAll other services ({string.Join(", ", operational.Select(o => o.ServiceName))}) are operational.");
                }
            }
            else
            {
                answerBuilder.AppendLine("All enterprise IT services are currently fully operational.");
            }

            return new SpecialistResponseDto(
                Answer: answerBuilder.ToString().Trim(),
                Citations: citations,
                RequiresClarification: false,
                SuggestIncidentDraft: false,
                SuggestedTitle: null,
                SuggestedDescription: null,
                Category: "Service Health"
            );
        }
        else
        {
            string fallbackTitle = $"IT Support Request: {Truncate(userMessage, 60)}";
            string fallbackDesc = $"User reported the following issue which requires IT tier escalation:\n\n{userMessage}";

            answerBuilder.AppendLine("I could not locate an approved IT knowledge document matching your specific request, or your issue requires tier-2 technician intervention.");
            answerBuilder.AppendLine("\nWould you like me to create an incident draft ticket for IT Service Desk review?");

            return new SpecialistResponseDto(
                Answer: answerBuilder.ToString().Trim(),
                Citations: citations,
                RequiresClarification: false,
                SuggestIncidentDraft: true,
                SuggestedTitle: fallbackTitle,
                SuggestedDescription: fallbackDesc,
                Category: plan.DraftCategory ?? "General IT",
                Impact: IncidentImpact.Medium,
                Urgency: IncidentUrgency.Medium
            );
        }

        return new SpecialistResponseDto(
            Answer: answerBuilder.ToString().Trim(),
            Citations: citations,
            RequiresClarification: false,
            SuggestIncidentDraft: false,
            SuggestedTitle: null,
            SuggestedDescription: null,
            Category: plan.DraftCategory
        );
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}
