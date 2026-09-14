using ServiceDesk.Application.DTOs.Incident;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IIncidentReviewerService
{
    Task<IncidentReviewResultDto> ReviewDraftAsync(IncidentDraft draft, CancellationToken cancellationToken = default);
    Task<IncidentReviewResultDto> ReviewDraftDtoAsync(CreateIncidentDraftDto dto, CancellationToken cancellationToken = default);
}
