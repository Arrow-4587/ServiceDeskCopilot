namespace ServiceDesk.Domain.Entities;

public class AdminConfig
{
    public Guid Id { get; private set; }
    public string ConfigKey { get; private set; } = string.Empty;
    public string ConfigValue { get; private set; } = string.Empty;
    public DateTime UpdatedAt { get; private set; }
    public string UpdatedBy { get; private set; } = string.Empty;

    private AdminConfig() { }

    public AdminConfig(string key, string value, string updatedBy)
    {
        Id = Guid.NewGuid();
        ConfigKey = key ?? string.Empty;
        ConfigValue = value ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy ?? string.Empty;
    }

    public void UpdateValue(string value, string updatedBy)
    {
        ConfigValue = value ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy ?? string.Empty;
    }
}
