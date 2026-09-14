namespace ServiceDesk.Application.DTOs.Incident;

public record TicketCreatedResponseDto(
    string TicketId,
    string Status,
    DateTime CreatedAt,
    string Message
);
