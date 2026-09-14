using ServiceDesk.Application.DTOs.Knowledge;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IRagGroundingService
{
    Task<RagAnswerResultDto> GenerateGroundedAnswerAsync(string userQuery, CancellationToken cancellationToken = default);
}
