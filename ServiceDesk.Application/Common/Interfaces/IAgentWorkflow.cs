using ServiceDesk.Application.DTOs.Chat;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IAgentWorkflow
{
    Task<ChatResponseDto> ExecuteWorkflowAsync(ChatRequestDto request, CancellationToken cancellationToken = default);
}
