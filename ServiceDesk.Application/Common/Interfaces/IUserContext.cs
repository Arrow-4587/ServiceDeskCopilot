using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IUserContext
{
    Guid UserId { get; }
    string Username { get; }
    string Email { get; }
    UserRole Role { get; }
    bool IsAuthenticated { get; }
}
