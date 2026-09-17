using ServiceDesk.Application.DTOs.Status;

namespace ServiceDesk.Application.Common.Interfaces;

public interface ISystemStatusReader
{
    Task<ServiceStatusDto?> GetServiceStatusAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceStatusDto>> GetAllStatusesAsync(CancellationToken cancellationToken = default);
}
