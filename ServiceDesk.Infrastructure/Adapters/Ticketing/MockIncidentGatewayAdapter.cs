using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Incident;

namespace ServiceDesk.Infrastructure.Adapters.Ticketing;

public class MockIncidentGatewayAdapter : IIncidentGateway
{
    private static int _ticketCounter = 1000;
    private static readonly object LockObj = new();
    private readonly ILogger<MockIncidentGatewayAdapter> _logger;

    public MockIncidentGatewayAdapter(ILogger<MockIncidentGatewayAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<TicketCreatedResponseDto> CreateTicketAsync(SubmitTicketRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        int nextId;
        lock (LockObj)
        {
            _ticketCounter++;
            nextId = _ticketCounter;
        }

        string ticketId = $"INC-2026-{nextId}";
        _logger.LogInformation("Mock ITSM Ticketing Gateway created ticket {TicketId} for Draft {DraftId} by User {UserId}", ticketId, request.DraftId, request.UserId);

        var response = new TicketCreatedResponseDto(
            TicketId: ticketId,
            Status: "Submitted",
            CreatedAt: DateTime.UtcNow,
            Message: "Incident ticket successfully created in mock ITSM gateway."
        );

        return Task.FromResult(response);
    }
}
