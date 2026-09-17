using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Entities;

public class SystemPromptOverride
{
    public Guid Id { get; private set; }
    public UserRole TargetRole { get; private set; }
    public string PromptText { get; private set; } = string.Empty;
    public DateTime UpdatedAt { get; private set; }
    public string UpdatedBy { get; private set; } = string.Empty;

    private SystemPromptOverride() { }

    public SystemPromptOverride(UserRole targetRole, string promptText, string updatedBy)
    {
        Id = Guid.NewGuid();
        TargetRole = targetRole;
        PromptText = promptText ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy ?? string.Empty;
    }

    public void UpdatePrompt(string newText, string updatedBy)
    {
        PromptText = newText ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy ?? string.Empty;
    }
}
