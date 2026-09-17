using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;

namespace ServiceDesk.Infrastructure.Services;

public class DefaultAiTelemetry : IAiTelemetry
{
    private readonly ILogger<DefaultAiTelemetry> _logger;

    public DefaultAiTelemetry(ILogger<DefaultAiTelemetry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void TrackRequestLatency(string operationName, TimeSpan duration, string correlationId)
    {
        _logger.LogInformation("[Telemetry:Latency] Operation={Op} Duration={Ms}ms CorrelationId={Id}",
            operationName, duration.TotalMilliseconds, correlationId);
    }

    public void TrackTokenUsage(string modelName, int promptTokens, int completionTokens, string correlationId)
    {
        _logger.LogInformation("[Telemetry:Tokens] Model={Model} Prompt={Prompt} Completion={Completion} Total={Total} CorrelationId={Id}",
            modelName, promptTokens, completionTokens, promptTokens + completionTokens, correlationId);
    }

    public void TrackToolCall(string toolName, bool succeeded, string correlationId)
    {
        _logger.LogInformation("[Telemetry:Tool] Tool={Tool} Succeeded={Success} CorrelationId={Id}",
            toolName, succeeded, correlationId);
    }

    public void TrackAgentStep(string agentName, string stepDescription, string correlationId)
    {
        _logger.LogInformation("[Telemetry:Agent] Agent={Agent} Step='{Step}' CorrelationId={Id}",
            agentName, stepDescription, correlationId);
    }
}
