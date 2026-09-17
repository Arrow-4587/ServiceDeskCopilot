namespace ServiceDesk.Application.Common.Telemetry;

public class TelemetryContext
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime RequestStartTime { get; set; } = DateTime.UtcNow;
    public long ElapsedMilliseconds { get; set; }
    public int RetrievalHits { get; set; }
    public int ToolCallCount { get; set; }
    public bool SecretsDetectedAndRedacted { get; set; }
    public string? FallbackReason { get; set; }
}
