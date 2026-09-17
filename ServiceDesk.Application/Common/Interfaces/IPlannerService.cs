using ServiceDesk.Application.DTOs.Agent;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IPlannerService
{
    Task<PlannerPlanDto> PlanAsync(string userMessage, CancellationToken cancellationToken = default);
}
