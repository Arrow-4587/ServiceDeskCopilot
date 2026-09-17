using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ServiceDesk.Application.DTOs.Agent;
using ServiceDesk.Application.Prompts;

namespace ServiceDesk.Infrastructure.SemanticKernel.Agents;

public class SemanticKernelPlannerAgent
{
    private readonly Kernel _kernel;
    private readonly ILogger<SemanticKernelPlannerAgent> _logger;

    public SemanticKernelPlannerAgent(Kernel kernel, ILogger<SemanticKernelPlannerAgent> logger)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PlannerPlanDto> PlanAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return new PlannerPlanDto(
                UserIntent: "EmptyQuery",
                RequiresKnowledgeSearch: false,
                SearchQuery: null,
                RequiresStatusCheck: false,
                ServiceName: null,
                RequiresIncidentDraft: false,
                DraftCategory: null,
                DiagnosticSummary: "Empty query received."
            );
        }

        _logger.LogInformation("[SemanticKernel:Planner] Formulating plan for inquiry: {UserMessage}", userMessage);

        var lower = userMessage.ToLowerInvariant();
        bool isTicketRequest = lower.Contains("raise ticket") || lower.Contains("create ticket") ||
                               lower.Contains("open ticket") || lower.Contains("report incident") ||
                               lower.Contains("broken") || lower.Contains("submit ticket");

        bool isStatusCheck = lower.Contains("status") || lower.Contains("down") ||
                             lower.Contains("outage") || lower.Contains("offline") ||
                             lower.Contains("working");

        string? detectedService = null;
        if (lower.Contains("vpn") || lower.Contains("anyconnect") || lower.Contains("cisco")) detectedService = "GlobalProtect VPN";
        else if (lower.Contains("outlook") || lower.Contains("email") || lower.Contains("exchange")) detectedService = "Exchange Online";
        else if (lower.Contains("teams") || lower.Contains("call") || lower.Contains("chat")) detectedService = "Microsoft Teams";
        else if (lower.Contains("printer") || lower.Contains("print")) detectedService = "Print Spooler";
        else if (lower.Contains("wifi") || lower.Contains("network")) detectedService = "Corporate Wi-Fi";

        string intent;
        string? category = null;

        if (lower.Contains("password") || lower.Contains("reset") || lower.Contains("unlock"))
        {
            intent = "PasswordManagement";
            category = "Identity & Access";
        }
        else if (lower.Contains("vpn") || lower.Contains("remote"))
        {
            intent = "VpnTroubleshooting";
            category = "Network";
        }
        else if (lower.Contains("printer") || lower.Contains("printing"))
        {
            intent = "PrinterSupport";
            category = "Hardware";
        }
        else if (lower.Contains("hardware") || lower.Contains("laptop") || lower.Contains("monitor"))
        {
            intent = "HardwareRequest";
            category = "Hardware";
        }
        else if (lower.Contains("software") || lower.Contains("install") || lower.Contains("license"))
        {
            intent = "SoftwareProvisioning";
            category = "Software";
        }
        else if (lower.Contains("wifi") || lower.Contains("wireless"))
        {
            intent = "WifiAccess";
            category = "Network";
        }
        else if (lower.Contains("email") || lower.Contains("outlook") || lower.Contains("mailbox"))
        {
            intent = "EmailConfiguration";
            category = "Collaboration";
        }
        else if (lower.Contains("teams") || lower.Contains("mfa") || lower.Contains("authenticator"))
        {
            intent = "MfaTeamsSupport";
            category = "Identity & Access";
        }
        else if (lower.Contains("security") || lower.Contains("phishing") || lower.Contains("malware"))
        {
            intent = "SecurityIncident";
            category = "Security";
        }
        else
        {
            intent = "GeneralITAssistance";
            category = "General IT";
        }

        bool requiresSearch = !isTicketRequest;
        bool requiresStatus = isStatusCheck || detectedService != null;

        var plan = new PlannerPlanDto(
            UserIntent: intent,
            RequiresKnowledgeSearch: requiresSearch,
            SearchQuery: requiresSearch ? userMessage : null,
            RequiresStatusCheck: requiresStatus,
            ServiceName: detectedService,
            RequiresIncidentDraft: isTicketRequest,
            DraftCategory: category,
            DiagnosticSummary: $"[SemanticKernel] Plan generated: Intent='{intent}', SearchRequired={requiresSearch}, StatusRequired={requiresStatus}, DraftRequired={isTicketRequest}."
        );

        return await Task.FromResult(plan);
    }
}
