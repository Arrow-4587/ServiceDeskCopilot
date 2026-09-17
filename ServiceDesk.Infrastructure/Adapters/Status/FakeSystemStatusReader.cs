using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Status;

namespace ServiceDesk.Infrastructure.Adapters.Status;

public class FakeSystemStatusReader : ISystemStatusReader
{
    private static readonly List<ServiceStatusDto> Statuses = new()
    {
        new ServiceStatusDto("VPN (Cisco AnyConnect)", "Operational", "VPN Concentrator cluster active at 42% capacity.", DateTime.UtcNow),
        new ServiceStatusDto("Outlook (Exchange Online)", "Operational", "Exchange Online mail routing fully operational.", DateTime.UtcNow),
        new ServiceStatusDto("Microsoft Teams", "DegradedPerformance", "Audio call latency elevated in US-East region; mitigation in progress.", DateTime.UtcNow),
        new ServiceStatusDto("Entra ID (Azure AD)", "Operational", "MFA and Single Sign-On services operating normally.", DateTime.UtcNow),
        new ServiceStatusDto("Corporate Wi-Fi", "Operational", "All enterprise WPA2 wireless controllers online.", DateTime.UtcNow),
        new ServiceStatusDto("Payroll (SAP HR)", "Operational", "Payroll portal operating normally.", DateTime.UtcNow)
    };

    public Task<IReadOnlyList<ServiceStatusDto>> GetAllStatusesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<ServiceStatusDto>>(Statuses);
    }

    public Task<ServiceStatusDto?> GetServiceStatusAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            return Task.FromResult<ServiceStatusDto?>(null);

        string query = serviceName.Trim().ToLowerInvariant();

        var match = Statuses.FirstOrDefault(s =>
            s.ServiceName.ToLowerInvariant().Contains(query) ||
            QueryMatchesAlias(s.ServiceName, query)
        );

        return Task.FromResult(match);
    }

    private static bool QueryMatchesAlias(string serviceName, string query)
    {
        if (serviceName.Contains("VPN", StringComparison.OrdinalIgnoreCase) && (query.Contains("vpn") || query.Contains("anyconnect") || query.Contains("remote")))
            return true;
        if (serviceName.Contains("Outlook", StringComparison.OrdinalIgnoreCase) && (query.Contains("mail") || query.Contains("email") || query.Contains("exchange")))
            return true;
        if (serviceName.Contains("Teams", StringComparison.OrdinalIgnoreCase) && (query.Contains("chat") || query.Contains("call") || query.Contains("teams")))
            return true;
        if (serviceName.Contains("Entra", StringComparison.OrdinalIgnoreCase) && (query.Contains("sso") || query.Contains("mfa") || query.Contains("login") || query.Contains("azure ad")))
            return true;
        if (serviceName.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) && (query.Contains("wifi") || query.Contains("wireless") || query.Contains("internet")))
            return true;
        if (serviceName.Contains("Payroll", StringComparison.OrdinalIgnoreCase) && (query.Contains("sap") || query.Contains("pay") || query.Contains("hr")))
            return true;

        return false;
    }
}
