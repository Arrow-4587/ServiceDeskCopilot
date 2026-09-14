using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Status;

namespace ServiceDesk.Application.UseCases;

public class GetSystemStatusUseCase
{
    private readonly ISystemStatusReader _statusReader;

    public GetSystemStatusUseCase(ISystemStatusReader statusReader)
    {
        _statusReader = statusReader ?? throw new ArgumentNullException(nameof(statusReader));
    }

    public async Task<IReadOnlyList<ServiceStatusDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _statusReader.GetAllStatusesAsync(cancellationToken);
    }

    public async Task<ServiceStatusDto?> GetByServiceNameAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            return null;

        return await _statusReader.GetServiceStatusAsync(serviceName, cancellationToken);
    }
}
