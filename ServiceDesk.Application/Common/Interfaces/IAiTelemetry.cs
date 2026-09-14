namespace ServiceDesk.Application.Common.Interfaces;

public interface IAiTelemetry
{
    void TrackRequestLatency(string operationName, TimeSpan duration, string correlationId);
    void TrackTokenUsage(string modelName, int promptTokens, int completionTokens, string correlationId);
    void TrackToolCall(string toolName, bool succeeded, string correlationId);
    void TrackAgentStep(string agentName, string stepDescription, string correlationId);
}
