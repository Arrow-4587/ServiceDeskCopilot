using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Incident;
using ServiceDesk.Application.UseCases;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Infrastructure.SemanticKernel.Plugins;

public class IncidentDraftPlugin
{
    private readonly CreateIncidentDraftUseCase _draftUseCase;
    private readonly IUserContext _userContext;
    private readonly ILogger<IncidentDraftPlugin> _logger;

    public IncidentDraftPlugin(
        CreateIncidentDraftUseCase draftUseCase,
        IUserContext userContext,
        ILogger<IncidentDraftPlugin> logger)
    {
        _draftUseCase = draftUseCase ?? throw new ArgumentNullException(nameof(draftUseCase));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [KernelFunction, Description("Creates a pending IT incident draft ticket for issues that cannot be resolved via self-service or when the employee requests an escalation ticket.")]
    public async Task<string> CreateIncidentDraftAsync(
        [Description("Short clear summary title of the issue")] string title,
        [Description("Detailed troubleshooting description and user observations")] string description,
        [Description("Category of the issue (e.g. Hardware, Network, Software, Identity & Access)")] string category = "General IT",
        [Description("System status evidence or outage notes if applicable")] string systemEvidence = "",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[SemanticKernel:Plugin] Executing IncidentDraftPlugin. Title: '{Title}', UserAuth={IsAuth}", title, _userContext.IsAuthenticated);

        if (!_userContext.IsAuthenticated)
        {
            return JsonSerializer.Serialize(new
            {
                Success = false,
                Message = "Authentication required. Please sign in to create an IT Service Desk incident draft."
            });
        }

        var dto = new CreateIncidentDraftDto(
            Title: title,
            Description: description,
            Category: category,
            Impact: IncidentImpact.Medium,
            Urgency: IncidentUrgency.Medium,
            SystemStatusEvidence: systemEvidence
        );

        var draft = await _draftUseCase.ExecuteAsync(dto, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            Success = true,
            DraftId = draft.Id,
            Title = draft.Title,
            Category = draft.Category,
            Priority = draft.ComputedPriority.ToString(),
            Status = draft.Status.ToString()
        });
    }
}
