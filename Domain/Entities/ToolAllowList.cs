using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Entities;

public class ToolAllowList
{
    public Guid Id { get; private set; }
    public UserRole TargetRole { get; private set; }
    public string ToolName { get; private set; } = string.Empty;
    public bool IsAllowed { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string UpdatedBy { get; private set; } = string.Empty;

    private ToolAllowList() { }

    public ToolAllowList(UserRole targetRole, string toolName, bool isAllowed, string updatedBy)
    {
        Id = Guid.NewGuid();
        TargetRole = targetRole;
        ToolName = toolName ?? string.Empty;
        IsAllowed = isAllowed;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy ?? string.Empty;
    }

    public void Toggle(bool isAllowed, string updatedBy)
    {
        IsAllowed = isAllowed;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy ?? string.Empty;
    }
}
