using ServiceDesk.Application.DTOs.Incident;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IIncidentGateway
{
    Task<TicketCreatedResponseDto> CreateTicketAsync(SubmitTicketRequestDto request, CancellationToken cancellationToken = default);
}
