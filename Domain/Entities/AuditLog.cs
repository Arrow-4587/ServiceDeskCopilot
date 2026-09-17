namespace ServiceDesk.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public string UserRole { get; private set; } = string.Empty;
    public string Details { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;

    private AuditLog() { }

    public AuditLog(string action, string userId, string userRole, string details, string ipAddress = "")
    {
        Id = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        Action = action ?? string.Empty;
        UserId = userId ?? string.Empty;
        UserRole = userRole ?? "Unknown";
        Details = details ?? string.Empty;
        IpAddress = ipAddress ?? string.Empty;
    }

    public AuditLog(string action, string userId, string details, string ipAddress = "")
        : this(action, userId, "Unknown", details, ipAddress)
    {
    }
}
