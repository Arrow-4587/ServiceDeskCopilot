using ServiceDesk.Application.DTOs.Agent;
using ServiceDesk.Application.DTOs.Knowledge;
using ServiceDesk.Application.DTOs.Status;

namespace ServiceDesk.Application.Common.Interfaces;

public interface ISupportSpecialistService
{
    Task<SpecialistResponseDto> TroubleshootAsync(
        string userMessage,
        PlannerPlanDto plan,
        IReadOnlyList<SearchResultDto> knowledge,
        IReadOnlyList<ServiceStatusDto> statuses,
        CancellationToken cancellationToken = default);
}
