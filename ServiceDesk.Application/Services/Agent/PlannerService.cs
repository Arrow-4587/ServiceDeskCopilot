using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Agent;
using ServiceDesk.Application.Prompts;

namespace ServiceDesk.Application.Services.Agent;

public class PlannerService : IPlannerService
{
    private readonly ILogger<PlannerService> _logger;

    public PlannerService(ILogger<PlannerService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<PlannerPlanDto> PlanAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return Task.FromResult(new PlannerPlanDto(
                UserIntent: "EmptyQuery",
                RequiresKnowledgeSearch: false,
                SearchQuery: null,
                RequiresStatusCheck: false,
                ServiceName: null,
                RequiresIncidentDraft: false,
                DraftCategory: null,
                DiagnosticSummary: "Empty query received; requesting clarification."
            ));
        }

        var lower = userMessage.ToLowerInvariant();
        _logger.LogInformation("Planner Agent analyzing request intent: {UserMessage}", userMessage);

        bool isTicketRequest = lower.Contains("raise ticket") || lower.Contains("raise a ticket") ||
                               lower.Contains("create ticket") || lower.Contains("create a ticket") ||
                               lower.Contains("open ticket") || lower.Contains("open a ticket") ||
                               lower.Contains("report incident") || lower.Contains("submit ticket") ||
                               lower.Contains("submit a ticket") || lower.Contains("file a ticket") ||
                               lower.Contains("draft ticket") || lower.Contains("create draft") ||
                               lower.Contains("incident draft") || lower.Contains("draft an incident") ||
                               (lower.Contains("ticket") && (lower.Contains("please") || lower.Contains("need") || lower.Contains("want") || lower.Contains("can you") || lower.Contains("create") || lower.Contains("open") || lower.Contains("draft")));

        bool isStatusCheck = lower.Contains("status") || lower.Contains("down") ||
                             lower.Contains("outage") || lower.Contains("offline") ||
                             lower.Contains("broken") || lower.Contains("working") ||
                             lower.Contains("affected") || lower.Contains("health");

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
        else if (lower.Contains("hardware") || lower.Contains("laptop") || lower.Contains("monitor") || lower.Contains("keyboard"))
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
        string? searchQuery = requiresSearch ? userMessage : null;
        bool requiresStatus = isStatusCheck || detectedService != null;

        var plan = new PlannerPlanDto(
            UserIntent: intent,
            RequiresKnowledgeSearch: requiresSearch,
            SearchQuery: searchQuery,
            RequiresStatusCheck: requiresStatus,
            ServiceName: detectedService,
            RequiresIncidentDraft: isTicketRequest,
            DraftCategory: category,
            DiagnosticSummary: $"Plan formulated: Intent '{intent}', SearchRequired={requiresSearch}, StatusRequired={requiresStatus}, DraftRequired={isTicketRequest}."
        );

        return Task.FromResult(plan);
    }
}
