using ServiceDesk.Application.DTOs.Chat;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IChatModel
{
    Task<ChatResponseDto> GenerateCompletionAsync(ChatRequestDto request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamCompletionAsync(ChatRequestDto request, CancellationToken cancellationToken = default);
}
