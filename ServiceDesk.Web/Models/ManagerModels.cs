namespace ServiceDesk.Web.Models;

/// <summary>View-model for a single AI agent's metrics on the Manager dashboard.</summary>
public class ManagerAgentMetric
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "";
    public string ColorMuted { get; set; } = "";
    public int Requests { get; set; }
    public double SuccessRate { get; set; }
    public int AvgLatencyMs { get; set; }
    public int TotalTokens { get; set; }
}

/// <summary>View-model for a knowledge-base file card on the Manager dashboard.</summary>
public class ManagerKnowledgeFile
{
    public string FileName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime LastModified { get; set; }

    public string SizeFormatted => SizeBytes < 1024 ? $"{SizeBytes} B"
        : SizeBytes < 1048576 ? $"{SizeBytes / 1024.0:0.0} KB"
        : $"{SizeBytes / 1048576.0:0.0} MB";
}
