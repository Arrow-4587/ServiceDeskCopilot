using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ServiceDesk.Application.DTOs.Agent;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Application.Prompts;

namespace ServiceDesk.Infrastructure.SemanticKernel.Agents;

public class SemanticKernelSpecialistAgent
{
    private readonly Kernel _kernel;
    private readonly ILogger<SemanticKernelSpecialistAgent> _logger;

    public SemanticKernelSpecialistAgent(Kernel kernel, ILogger<SemanticKernelSpecialistAgent> logger)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<SpecialistResponseDto> TroubleshootAsync(
        string userMessage,
        PlannerPlanDto plan,
        string? pluginSearchResultsJson,
        string? pluginStatusJson,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[SemanticKernel:Specialist] Synthesizing grounded diagnosis for intent: {Intent}", plan.UserIntent);

        var citations = new List<CitationDto>();
        var answerBuilder = new StringBuilder();

        // 1. Process Status evidence if provided
        if (!string.IsNullOrWhiteSpace(pluginStatusJson) && pluginStatusJson != "[]")
        {
            try
            {
                using var doc = JsonDocument.Parse(pluginStatusJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        var service = elem.GetProperty("Service").GetString();
                        var status = elem.GetProperty("Status").GetString();
                        var desc = elem.GetProperty("Description").GetString();

                        if (!string.IsNullOrWhiteSpace(plan.ServiceName) &&
                            service != null && service.Contains(plan.ServiceName, StringComparison.OrdinalIgnoreCase))
                        {
                            answerBuilder.AppendLine($"**Live Cloud Service Health:** `{service}` is currently **{status}** ({desc}).\n");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SemanticKernel:Specialist] Failed to parse status JSON.");
            }
        }

        // 2. Process Knowledge Search Results
        if (!string.IsNullOrWhiteSpace(pluginSearchResultsJson) && pluginSearchResultsJson != "NO_APPROVED_KNOWLEDGE_FOUND")
        {
            try
            {
                using var doc = JsonDocument.Parse(pluginSearchResultsJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                {
                    string primaryExcerpt = "";
                    string primaryDocName = "";
                    string primarySection = "";
                    int primaryPage = 1;
                    string primaryBlobPath = "";

                    int index = 0;
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        var docName = elem.GetProperty("DocumentName").GetString() ?? "Corporate IT Policy";
                        var section = elem.GetProperty("Section").GetString() ?? "General";
                        var page = elem.GetProperty("Page").GetInt32();
                        var excerpt = elem.GetProperty("Excerpt").GetString() ?? "";
                        var blobPath = elem.GetProperty("BlobPath").GetString() ?? "";

                        citations.Add(new CitationDto(
                            DocumentName: docName,
                            Section: section,
                            Page: page,
                            Snippet: excerpt,
                            BlobPath: blobPath
                        ));

                        if (index == 0)
                        {
                            primaryDocName = docName;
                            primarySection = section;
                            primaryPage = page;
                            primaryExcerpt = excerpt;
                            primaryBlobPath = blobPath;
                        }
                        index++;
                    }

                    answerBuilder.AppendLine($"Based on approved corporate IT policy [{primaryDocName}, {primarySection}, Page {primaryPage}]:\n");
                    answerBuilder.AppendLine(primaryExcerpt);
                    answerBuilder.AppendLine();
                    answerBuilder.AppendLine($"*Source:* [{primaryDocName}]({primaryBlobPath})");
                    answerBuilder.AppendLine("\nIf these troubleshooting steps do not resolve your issue, I can automatically initiate an incident draft for IT Service Desk escalation.");

                    return Task.FromResult(new SpecialistResponseDto(
                        Answer: answerBuilder.ToString().Trim(),
                        Citations: citations,
                        RequiresClarification: false,
                        SuggestIncidentDraft: false,
                        SuggestedTitle: null,
                        SuggestedDescription: null,
                        Category: plan.DraftCategory
                    ));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SemanticKernel:Specialist] Failed to parse knowledge JSON.");
            }
        }

        // 3. Fallback: No Knowledge Found or Out-of-Scope Query
        // We will no longer strictly reject queries based on hardcoded keywords.
        // If it reaches here, it means no knowledge was found. We should always offer to create a ticket.

        _logger.LogInformation("[SemanticKernel:Specialist] Unresolved IT query. Proposing incident draft.");
        string fallbackTitle = $"IT Support Request: {Truncate(userMessage, 60)}";
        string fallbackDesc = $"User reported the following issue requiring IT assistance:\n\n{userMessage}";

        answerBuilder.AppendLine("I could not find an approved IT policy document in our corporate repository matching your specific issue.");
        answerBuilder.AppendLine("\nWould you like me to create an incident draft ticket for IT Service Desk review?");

        return Task.FromResult(new SpecialistResponseDto(
            Answer: answerBuilder.ToString().Trim(),
            Citations: citations,
            RequiresClarification: false,
            SuggestIncidentDraft: true,
            SuggestedTitle: fallbackTitle,
            SuggestedDescription: fallbackDesc,
            Category: plan.DraftCategory ?? "General IT"
        ));
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}
