using ServiceDesk.Application.DTOs.Chat;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IChatConversationService
{
    Task<ChatResponseDto> ProcessChatMessageAsync(ChatRequestDto request, CancellationToken cancellationToken = default);
}
