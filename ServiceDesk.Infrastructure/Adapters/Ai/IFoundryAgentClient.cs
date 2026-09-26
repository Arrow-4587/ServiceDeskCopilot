namespace ServiceDesk.Infrastructure.Adapters.Ai;

public interface IFoundryAgentClient
{
    Task<string> ExecuteAgentAsync(
        string agentNameOrId,
        string userPrompt,
        string? additionalInstructions = null,
        CancellationToken cancellationToken = default);

    Task<string> ResolveAssistantIdAsync(
        string agentNameOrId,
        CancellationToken cancellationToken = default);
}
