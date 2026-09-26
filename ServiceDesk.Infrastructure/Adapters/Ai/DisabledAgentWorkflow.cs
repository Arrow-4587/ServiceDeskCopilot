using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Chat;

namespace ServiceDesk.Infrastructure.Adapters.Ai;

public class DisabledAgentWorkflow : IAgentWorkflow
{
    public Task<ChatResponseDto> ExecuteWorkflowAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Azure AI Foundry agent workflow is disabled and local development fallback is not enabled. Configure FeatureFlags:UseAzureAiFoundryAgentWorkflow=true or FeatureFlags:EnableLocalAgentWorkflowFallback=true.");
    }
}
